using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Abstractions.ImageGeneration;
using TrustInnova.Provider.OpenAI.API;
using TrustInnova.Provider.OpenAI.ImageGeneration;

namespace TrustInnova.Provider.OpenAI.AIScheduler
{
    [ProviderTask("OpenAIChatHybrid", "OpenAI(多模态)")]
    public class OpenAIHybridProvider : IAIChatTask
    {
        private readonly IAIChatTask _chatProvider;
        private readonly IAIChatTask _visionChatProvider;
        private readonly IImageGenerationTask _imageAPI;
        private readonly ILogger _logger;

        public OpenAIHybridProvider([Description("默认聊天配置")] OpenAIChatConfig chatConfig, [Description("视觉聊天配置")] OpenAIChatConfig visionChatConfig, OpenAIImageGenerationConfig imageGenerationConfig)
        {
            _chatProvider = new OpenAIChatProvider(chatConfig);
            _visionChatProvider = new OpenAIVisionChatProvider(visionChatConfig);
            _imageAPI = new OpenAIImageGenerationTask(imageGenerationConfig);
            _logger = Log.ForContext<OpenAIHybridProvider>();
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            var ret = new ChatHistory();
            if (instructions == null)
                ret.AddMessage(AuthorRole.System, [new(Guid.NewGuid().ToString(), $@"You are ChatGPT, a large language model trained by OpenAI.
Current date: {DateTime.Now.ToString("yyyy-MM-dd")}", ChatMessageContentType.Text)]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(AuthorRole.System, [new(Guid.NewGuid().ToString(), instructions, ChatMessageContentType.Text)]);
            return ret;
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> ChatAsync(ChatHistory chat, ChatSettings? chatSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var fm = new FunctionManager();
            fm.AddCustomFunction("DrawImage", "画图", new FunctionParametersInfo()
            {
                Type = "object",
                Properties = new Dictionary<string, FunctionParametersProperties>()
                {
                    {
                        "keywords",
                        new FunctionParametersProperties()
                        {
                            Description="描述图片的关键字",
                            Type="string"
                        }
                    },
                    {
                        "n",
                        new FunctionParametersProperties()
                        {
                            Description="画多少张图，默认：1，最大：5",
                            Type="integer"
                        }
                    },
                    {
                        "referenceImage",
                        new FunctionParametersProperties()
                        {
                            Description="参考图片名字(选填)",
                            Type="string"
                        }
                    }
                },
                Required = ["keywords"]
            });
            fm.AddCustomFunction("EditImage", "修改图片", new FunctionParametersInfo()
            {
                Type = "object",
                Properties = new Dictionary<string, FunctionParametersProperties>()
                {
                    {
                        "name",
                        new FunctionParametersProperties()
                        {
                            Description="图片名字",
                            Type="string"
                        }
                    },
                    {
                        "keywords",
                        new FunctionParametersProperties()
                        {
                            Description="描述修改图片的关键字和要求关键字",
                            Type="string"
                        }
                    }
                },
                Required = ["name", "keywords"]
            });
            fm.AddCustomFunction("ReadImage", "分析图片", new FunctionParametersInfo()
            {
                Type = "object"
            });

            var intentionRet = _chatProvider.ChatAsync(chat, new ChatSettings()
            {
                FunctionManager = fm,
                Temperature = chatSettings!.Temperature,
                TopP = chatSettings.TopP,
                MaxTokens = chatSettings.MaxTokens
            }, cancellationToken);
            await foreach (var intention in intentionRet)
            {
                switch (intention.Type)
                {
                    case AIChatHandleResponseType.FunctionStart:
                        _logger.Debug("Intention Select: {type}", intention.Type);
                        break;
                    case AIChatHandleResponseType.FunctionCall:
                        _logger.Debug("Intention Select(FunctionCall): ", intention.Type);
                        if (intention is not AIProviderHandleFunctionCallResponse funHandleResponse)
                        {
                            _logger.Debug("Intention Select(FunctionCall): AI执行返回解释错误：{ret}", intention);
                            break;
                        }
                        _logger.Debug("Intention Select(FunctionCall): AI开始执行：{name}", funHandleResponse.FunctionName);
                        _logger.Debug("Intention Select(FunctionCall): AI开始执行参数：{args}", funHandleResponse.Arguments);
                        switch (funHandleResponse.FunctionName)
                        {
                            case "DrawImage":
                                if (funHandleResponse.Arguments == null)
                                    yield break;
                                ChatMessageContent? referenceImageMessageContent = null;
                                if (funHandleResponse.Arguments.TryGetValue("referenceImage", out var jReferenceImage))
                                {
                                    var referenceImageStr = jReferenceImage.GetString();
                                    var referenceImageID = Path.GetFileNameWithoutExtension(referenceImageStr);
                                    referenceImageMessageContent = chat.FirstOrDefault(x => x.Contents.Any(x2 => x2.ContentId == referenceImageID))
                                                                        ?.Contents.FirstOrDefault(x => x.ContentType == ChatMessageContentType.ImageBase64);
                                }
                                var n = 1;
                                if (funHandleResponse.Arguments.TryGetValue("n", out var jn))
                                {
                                    n = jn.GetInt32();
                                    if (n <= 0)
                                        n = 1;
                                    else if (n > 5)
                                        n = 5;
                                }
                                IAsyncEnumerable<Stream> imageRet;
                                if (referenceImageMessageContent == null)
                                {
                                    if (!funHandleResponse.Arguments.TryGetValue("keywords", out var jkeywords))
                                        yield break;
                                    var keywordsStr = jkeywords.GetString();
                                    if (string.IsNullOrWhiteSpace(keywordsStr))
                                        yield break;
                                    imageRet = _imageAPI.GenerationAsync(new ImageGenerationOptions()
                                    {
                                        Number = (uint)n,
                                        Prompt = keywordsStr,
                                        Size = "1024x1024"
                                    }, cancellationToken);
                                }
                                else
                                {
                                    imageRet = _imageAPI.ReferenceGenerationAsync(new ImageReferenceGenerationOptions()
                                    {
                                        Number = (uint)n,
                                        Image = new MemoryStream(Convert.FromBase64String((string)referenceImageMessageContent.Content)),
                                        Size = "1024x1024"
                                    }, cancellationToken);
                                }
                                await foreach (var item in imageRet)
                                {
                                    yield return new AIProviderHandleImageMessageResponse()
                                    {
                                        Image = item
                                    };
                                }
                                break;
                            case "EditImage":
                                {
                                    if (funHandleResponse.Arguments == null)
                                        yield break;
                                    if (!funHandleResponse.Arguments.TryGetValue("name", out var jname))
                                        yield break;
                                    var nameStr = jname.GetString();
                                    if (string.IsNullOrWhiteSpace(nameStr))
                                        yield break;
                                    if (!funHandleResponse.Arguments.TryGetValue("keywords", out var jeditKeywords))
                                        yield break;
                                    var editKeywordsStr = jeditKeywords.GetString();
                                    if (string.IsNullOrWhiteSpace(editKeywordsStr))
                                        yield break;
                                    var editImageID = Path.GetFileNameWithoutExtension(nameStr);
                                    var editImageMessageContent = chat.FirstOrDefault(x => x.Contents.Any(x2 => x2.ContentId == editImageID))
                                                                        ?.Contents.FirstOrDefault(x => x.ContentType == ChatMessageContentType.ImageBase64);
                                    if (editImageMessageContent == null)
                                        yield break;

                                    var vchat = chat.ShallowClone();
                                    vchat.AddMessage(AuthorRole.System, [new(Guid.NewGuid().ToString(), "根据用户提出的修改问题，请找出需要修改的范围，以 {\"x\":0,\"y\":0,\"w\":0,\"h\":0} 这样的格式回答，不要回答其它内容，就按json格式回答。（注意：请以512x512的大小范围来）", ChatMessageContentType.Text)]);
                                    var vchatRet = await _visionChatProvider.ChatReturnTextAsync(vchat, new ChatSettings()
                                    {
                                        Temperature = chatSettings!.Temperature,
                                        TopP = chatSettings.TopP,
                                        MaxTokens = chatSettings.MaxTokens
                                    }, cancellationToken);

                                    _logger.Information("editPosition: {vchatRet}", vchatRet);
                                    var editPosition = JsonSerializer.Deserialize<EditImagePosition>(vchatRet);
                                    Image<Rgba32> maskImage;
                                    if (editPosition != null && editPosition.X >= 0 && editPosition.Y >= 0 && editPosition.W <= 512 && editPosition.H <= 512)
                                    {
                                        maskImage = new Image<Rgba32>(1024, 1024, Color.Black);
                                        var nx = editPosition.X * 2;
                                        var ny = editPosition.Y * 2;
                                        var nw = editPosition.W * 2;
                                        var nh = editPosition.H * 2;
                                        for (int y = ny; y < ny + nh; y++)
                                        {
                                            for (int x = nx; x < nx + nw; x++)
                                            {
                                                maskImage[x, y] = Color.Transparent;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        maskImage = new Image<Rgba32>(1024, 1024);
                                    }
                                    var maskImageStream = new MemoryStream();
                                    await maskImage.SaveAsPngAsync(maskImageStream, cancellationToken);
                                    maskImageStream.Position = 0;
                                    var editImageRet = _imageAPI.EditGenerationAsync(new ImageEditGenerationOptions()
                                    {
                                        Image = new MemoryStream(Convert.FromBase64String((string)editImageMessageContent.Content)),
                                        MaskImage = maskImageStream,
                                        Number = 1,
                                        Prompt = editKeywordsStr,
                                        Size = "1024x1024"
                                    }, cancellationToken);
                                    await foreach (var item in editImageRet)
                                    {
                                        yield return new AIProviderHandleImageMessageResponse()
                                        {
                                            Image = item
                                        };
                                    }
                                    break;
                                }
                            case "ReadImage":
                                var intentionImageRet = _visionChatProvider.ChatAsync(chat, new ChatSettings()
                                {
                                    Temperature = chatSettings!.Temperature,
                                    TopP = chatSettings.TopP,
                                    MaxTokens = chatSettings.MaxTokens
                                }, cancellationToken);
                                await foreach (var item in intentionImageRet)
                                {
                                    yield return item;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        yield return intention;
                        break;
                }
            }
        }

        public class EditImagePosition
        {
            [JsonPropertyName("x")]
            public int X { get; set; }

            [JsonPropertyName("y")]
            public int Y { get; set; }

            [JsonPropertyName("h")]
            public int H { get; set; }

            [JsonPropertyName("w")]
            public int W { get; set; }
        }
    }
}
