using Mapster;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.DataStorage;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;
using OpenAIChatMessageContent = OpenAI.Chat.ChatMessageContent;

namespace Tinvo.Provider.OpenAI.AIScheduler
{
    [TypeMetadataDisplayName("聊天配置")]
    public class OpenAIChatConfig
    {
        [Description("地址")]
        [DefaultValue("https://api.openai.com")]
        public string BaseURL { get; set; } = "https://api.openai.com";

        [Description("密钥")]
        [TypeMetadataAllowNull]
        public string? Token { get; set; }

        [Description("模型")]
        public string Model { get; set; } = null!;

        [Description("最大输出Token")]
        [DefaultValue(8192)]
        public int MaxOutputTokens { get; set; } = 8192;

        [Description("FrequencyPenalty")]
        [DefaultValue(0)]
        public float FrequencyPenalty { get; set; } = 0;

        [Description("PresencePenalty")]
        [DefaultValue(0)]
        public float PresencePenalty { get; set; } = 0;

        [Description("Temperature")]
        [DefaultValue(0.6f)]
        [TypeMetadataAllowNull]
        public float? Temperature { get; set; } = 0.6f;

        [Description("TopP")]
        [DefaultValue(1)]
        public float TopP { get; set; } = 1;

        [Description("流式")]
        [DefaultValue(true)]
        public bool IsStream { get; set; } = true;

        [Description("思考处理")]
        [DefaultValue(false)]
        public bool ThinkHandle { get; set; } = false;

        [Description("兼容旧版API")]
        [DefaultValue(false)]
        public bool CompatibleOldAPI { get; set; } = false;

        [Description("响应模式")]
        [DefaultValue(false)]
        public bool IsResponseMode { get; set; } = false;

        [Description("网页搜索")]
        [DefaultValue(false)]
        public bool EnableWebSearch { get; set; } = false;
    }

    public class BlazorHttpClientTransport : HttpClientPipelineTransport
    {
        protected override void OnSendingRequest(PipelineMessage message, HttpRequestMessage httpRequest)
        {
            httpRequest.SetBrowserResponseStreamingEnabled(true);
        }
    }

    [ProviderTask("OpenAIChat", "OpenAI")]
    public class OpenAIChatProvider : IAIChatTask
    {
        private static JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IDataStorageServiceFactory _dataStorageServiceFactory;
        private readonly OpenAIChatConfig _config;

        private readonly ChatClient _chatClient;
        private readonly OpenAIResponseClient? _chatResponsetClient;

        private IDataStorageService _storageService;

        public async Task InitAsync()
        {
            _storageService = await _dataStorageServiceFactory.CreateAsync();
        }

        public OpenAIChatProvider(IDataStorageServiceFactory storageServiceFactory, OpenAIChatConfig config)
        {
            _dataStorageServiceFactory = storageServiceFactory;
            _config = config;
            _chatClient = new ChatClient(_config.Model, new ApiKeyCredential(_config.Token ?? ""), new OpenAIClientOptions()
            {
                Endpoint = new Uri(_config.BaseURL),
                NetworkTimeout = TimeSpan.FromDays(10),
                Transport = new BlazorHttpClientTransport()
            });
            if (_config.IsResponseMode)
            {
                _chatResponsetClient = new OpenAIResponseClient(_config.Model, new ApiKeyCredential(_config.Token ?? ""), new OpenAIClientOptions()
                {
                    Endpoint = new Uri(_config.BaseURL),
                    NetworkTimeout = TimeSpan.FromDays(10),
                    Transport = new BlazorHttpClientTransport()
                });
            }
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            var ret = new ChatHistory();
            if (instructions == null)
                ret.AddMessage(AuthorRole.System, [new AIProviderHandleTextMessageResponse() { Message = $@"You are ChatGPT, a large language model trained by OpenAI.
Current date: {DateTime.Now.ToString("yyyy-MM-dd")}" }]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(AuthorRole.System, [new AIProviderHandleTextMessageResponse() { Message = instructions }]);
            return ret;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SerializedAdditionalRawData")]
        private extern static IDictionary<string, BinaryData>? GetSerializedAdditionalRawData(ChatCompletionOptions @this);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_SerializedAdditionalRawData")]
        private extern static void SetSerializedAdditionalRawData(ChatCompletionOptions @this, IDictionary<string, BinaryData>? value);

        private void SetMaxTokens(ChatCompletionOptions options, int value)
        {
            IDictionary<string, BinaryData>? rawData = GetSerializedAdditionalRawData(options);
            if (rawData == null)
            {
                rawData = new Dictionary<string, BinaryData>();
                SetSerializedAdditionalRawData(options, rawData);
            }
            rawData["max_tokens"] = BinaryData.FromObjectAsJson(value);
        }

        private async Task<List<OpenAIChatMessage>> CreateOpenAIChatMessage(IDataStorageService dataStorageService, AuthorRole role, List<IAIChatHandleMessage> contents, CancellationToken cancellationToken = default)
        {
            var currentRole = role;
            var msg = new List<ChatMessageContentPart>();
            var toolCallId = "";
            var functionName = "";
            BinaryData? functionCallArgs = null;
            var functionOtherMessage = new List<OpenAIChatMessage>();
            foreach (var content in contents)
            {
                ChatMessageContentPart? contentPart = null;
                if (content is AIProviderHandleTextMessageResponse textMessage)
                {
                    contentPart = ChatMessageContentPart.CreateTextPart(textMessage.Message);
                }
                else if (content is AIProviderHandleRefusalMessageResponse refusalMessage)
                {
                    contentPart = ChatMessageContentPart.CreateRefusalPart(refusalMessage.Refusal);
                }
                else if (content is AIProviderHandleFunctionCallResponse functionCallMessage)
                {
                    currentRole = AuthorRole.Tool;
                    toolCallId = functionCallMessage.CallID;
                    functionName = functionCallMessage.FunctionName;
                    functionCallArgs = BinaryData.FromObjectAsJson(functionCallMessage.Arguments);
                    var functionResult = functionCallMessage.Result?.FirstOrDefault(x => x is AIProviderHandleTextMessageResponse) as AIProviderHandleTextMessageResponse ?? new AIProviderHandleTextMessageResponse()
                    {
                        Message = "调用成功"
                    };
                    contentPart = ChatMessageContentPart.CreateTextPart(functionResult.Message);
                    if (functionCallMessage.Result != null)
                    {
                        foreach (var item in functionCallMessage.Result)
                        {
                            if (item is AIProviderHandleCustomFileMessageResponse fileMessage)
                            {
                                functionOtherMessage.Add(OpenAIChatMessage.CreateAssistantMessage(ChatMessageContentPart.CreateTextPart($"文件ID：{fileMessage.FileCustomID}")));
                                switch (fileMessage.Type)
                                {
                                    case AIChatHandleMessageType.ImageMessage:
                                        var imageData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                                        if (imageData != null)
                                        {
                                            functionOtherMessage.Add(OpenAIChatMessage.CreateAssistantMessage(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageData), "image/png")));
                                        }
                                        break;
                                    case AIChatHandleMessageType.AudioMessage:
                                        var audioData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                                        if (audioData != null)
                                            functionOtherMessage.Add(OpenAIChatMessage.CreateAssistantMessage(ChatMessageContentPart.CreateInputAudioPart(BinaryData.FromBytes(audioData), (fileMessage.FileOriginalMediaType ?? "").ToLower().Contains("mp3") ? ChatInputAudioFormat.Mp3 : ChatInputAudioFormat.Wav)));
                                        break;
                                    case AIChatHandleMessageType.FileMessage:
                                        if (!string.IsNullOrWhiteSpace(fileMessage.FileOriginalID))
                                        {
                                            functionOtherMessage.Add(OpenAIChatMessage.CreateAssistantMessage(ChatMessageContentPart.CreateFilePart(fileMessage.FileOriginalID)));
                                        }
                                        else
                                        {
                                            var fileData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                                            if (fileData != null)
                                                functionOtherMessage.Add(OpenAIChatMessage.CreateAssistantMessage(ChatMessageContentPart.CreateFilePart(BinaryData.FromBytes(fileData), fileMessage.FileOriginalMediaType, fileMessage.FileOriginalName)));
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
                else if (content is AIProviderHandleCustomFileMessageResponse fileMessage)
                {
                    msg.Add(ChatMessageContentPart.CreateTextPart($"文件ID：{fileMessage.FileCustomID}"));
                    switch (fileMessage.Type)
                    {
                        case AIChatHandleMessageType.ImageMessage:
                            var imageData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                            if (imageData != null)
                            {
                                contentPart = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageData), "image/png");
                            }
                            break;
                        case AIChatHandleMessageType.AudioMessage:
                            var audioData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                            if (audioData != null)
                                contentPart = ChatMessageContentPart.CreateInputAudioPart(BinaryData.FromBytes(audioData), (fileMessage.FileOriginalMediaType ?? "").ToLower().Contains("mp3") ? ChatInputAudioFormat.Mp3 : ChatInputAudioFormat.Wav);
                            break;
                        case AIChatHandleMessageType.FileMessage:
                            if (!string.IsNullOrWhiteSpace(fileMessage.FileOriginalID))
                            {
                                contentPart = ChatMessageContentPart.CreateFilePart(fileMessage.FileOriginalID);
                            }
                            else
                            {
                                var fileData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                                if (fileData != null)
                                    contentPart = ChatMessageContentPart.CreateFilePart(BinaryData.FromBytes(fileData), fileMessage.FileOriginalMediaType, fileMessage.FileOriginalName);
                            }
                            break;
                        default:
                            break;
                    }
                }
                if (contentPart != null)
                    msg.Add(contentPart);
            }

            return currentRole switch
            {
                AuthorRole.User => [OpenAIChatMessage.CreateUserMessage(msg)],
                AuthorRole.System => [OpenAIChatMessage.CreateSystemMessage(msg)],
                AuthorRole.Assistant => [OpenAIChatMessage.CreateAssistantMessage(msg), .. functionOtherMessage],
                AuthorRole.Tool => [
                    OpenAIChatMessage.CreateAssistantMessage([
                        ChatToolCall.CreateFunctionToolCall(toolCallId, functionName, functionCallArgs)
                    ]),
                    OpenAIChatMessage.CreateToolMessage(toolCallId, msg),
                    .. functionOtherMessage
                ],
                _ => throw new NotImplementedException(),
            };
        }

        private async Task<List<ResponseItem>> CreateOpenAIResponseItem(IDataStorageService dataStorageService, AuthorRole role, List<IAIChatHandleMessage> contents, CancellationToken cancellationToken = default)
        {
            var currentRole = role;
            var msg = new List<ResponseContentPart>();
            var toolCallId = "";
            var functionName = "";
            var functionCallResult = "";
            BinaryData? functionCallArgs = null;
            var functionOtherMessage = new List<ResponseItem>();
            foreach (var content in contents)
            {
                ResponseContentPart? contentPart = null;
                if (content is AIProviderHandleTextMessageResponse textMessage)
                {
                    if (role == AuthorRole.User)
                        contentPart = ResponseContentPart.CreateInputTextPart(textMessage.Message);
                    else if (role == AuthorRole.Tool)
                        functionCallResult = textMessage.Message;
                    else
                        contentPart = ResponseContentPart.CreateOutputTextPart(textMessage.Message, []);
                }
                else if (content is AIProviderHandleRefusalMessageResponse refusalMessage)
                {
                    contentPart = ResponseContentPart.CreateRefusalPart(refusalMessage.Refusal);
                }
                else if (content is AIProviderHandleFunctionCallResponse functionCallMessage)
                {
                    currentRole = AuthorRole.Tool;
                    toolCallId = functionCallMessage.CallID;
                    functionName = functionCallMessage.FunctionName;
                    functionCallArgs = BinaryData.FromObjectAsJson(functionCallMessage.Arguments);
                    var functionResult = functionCallMessage.Result?.FirstOrDefault(x => x is AIProviderHandleTextMessageResponse) as AIProviderHandleTextMessageResponse ?? new AIProviderHandleTextMessageResponse()
                    {
                        Message = "调用成功"
                    };
                    contentPart = ResponseContentPart.CreateOutputTextPart(functionResult.Message, []);
                    if (functionCallMessage.Result != null)
                    {
                        foreach (var item in functionCallMessage.Result)
                        {
                            if (item is AIProviderHandleCustomFileMessageResponse fileMessage)
                            {
                                functionOtherMessage.Add(ResponseItem.CreateAssistantMessageItem([ResponseContentPart.CreateOutputTextPart($"文件ID：{fileMessage.FileCustomID}", [])]));
                                switch (fileMessage.Type)
                                {
                                    case AIChatHandleMessageType.ImageMessage:
                                        var imageData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                                        if (imageData != null)
                                        {
                                            functionOtherMessage.Add(ResponseItem.CreateAssistantMessageItem([ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(imageData), "image/png")]));
                                        }
                                        break;
                                    case AIChatHandleMessageType.AudioMessage:
                                    case AIChatHandleMessageType.FileMessage:
                                        var fileData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                                        if (fileData != null)
                                            functionOtherMessage.Add(ResponseItem.CreateAssistantMessageItem([ResponseContentPart.CreateInputFilePart(fileMessage.FileCustomID, fileMessage.FileOriginalMediaType, BinaryData.FromBytes(fileData))]));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
                else if (content is AIProviderHandleCustomFileMessageResponse fileMessage)
                {
                    msg.Add(ResponseContentPart.CreateInputTextPart($"文件ID：{fileMessage.FileCustomID}"));
                    switch (fileMessage.Type)
                    {
                        case AIChatHandleMessageType.ImageMessage:
                            var imageData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                            if (imageData != null)
                                contentPart = ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(imageData), "image/png");
                            break;
                        case AIChatHandleMessageType.AudioMessage:
                        case AIChatHandleMessageType.FileMessage:
                            var fileData = await dataStorageService.GetItemAsBinaryAsync(fileMessage.FileCustomID, cancellationToken);
                            if (fileData != null)
                                contentPart = ResponseContentPart.CreateInputFilePart(fileMessage.FileCustomID, fileMessage.FileOriginalMediaType, BinaryData.FromBytes(fileData));
                            break;
                        default:
                            break;
                    }
                }
                if (contentPart != null)
                    msg.Add(contentPart);
            }

            return currentRole switch
            {
                AuthorRole.User => [ResponseItem.CreateUserMessageItem(msg)],
                AuthorRole.System => [ResponseItem.CreateSystemMessageItem(msg)],
                AuthorRole.Assistant => [ResponseItem.CreateAssistantMessageItem(msg)],
                AuthorRole.Tool => [
                    ResponseItem.CreateFunctionCallItem(toolCallId, functionName, functionCallArgs),
                    ResponseItem.CreateFunctionCallOutputItem(toolCallId, functionCallResult),
                ],
                _ => throw new NotImplementedException(),
            };
        }

        public async IAsyncEnumerable<IAIChatHandleMessage> ChatAsync(ChatHistory chat, ChatSettings? requestSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_config.IsResponseMode && _chatResponsetClient != null)
            {
                var options = new ResponseCreationOptions()
                {
                    MaxOutputTokenCount = requestSettings?.MaxOutputTokens ?? _config.MaxOutputTokens,
                    Temperature = (float?)requestSettings?.Temperature ?? _config.Temperature,
                    TopP = (float?)requestSettings?.TopP ?? _config.TopP,
                };
                if (requestSettings != null)
                {
                    if (requestSettings.FunctionManager != null && options.Tools != null)
                        foreach (var item in requestSettings.FunctionManager.GetFunctionInfos())
                        {
                            options.Tools.Add(ResponseTool.CreateFunctionTool(item.Name, item.Description, BinaryData.FromObjectAsJson(item.Parameters, serializerOptions), true));
                        }
                }
                if (_config.ThinkHandle)
                {
                    options.ReasoningOptions = new ResponseReasoningOptions()
                    {
                        ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium,
                        ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Concise
                    };
                }
                if (_config.EnableWebSearch && options.Tools != null)
                {
                    options.Tools.Add(ResponseTool.CreateWebSearchTool());
                }

                var chatMessages = new List<ResponseItem>();
                foreach (var chatPart in chat)
                {
                    chatMessages.AddRange(await CreateOpenAIResponseItem(_storageService, chatPart.Role, chatPart.Contents, cancellationToken));
                }

                var responsetParser = new OpenAIProviderResponsetParser(_storageService, _config.ThinkHandle);

                if (_config.IsStream)
                {
                    AsyncCollectionResult<StreamingResponseUpdate> ret = _chatResponsetClient.CreateResponseStreamingAsync(chatMessages, options, cancellationToken);
                    await foreach (var msg in ret)
                    {
                        var handleRet = responsetParser.Handle(msg, requestSettings?.FunctionManager);
                        await foreach (var item2 in handleRet)
                        {
                            yield return item2;
                        }
                    }
                }
                else
                {
                    ClientResult<OpenAIResponse> ret = await _chatResponsetClient.CreateResponseAsync(chatMessages, options, cancellationToken);
                    var handleRet = responsetParser.Handle(ret, requestSettings?.FunctionManager);
                    await foreach (var item2 in handleRet)
                    {
                        yield return item2;
                    }
                }
            }
            else
            {
                var options = new ChatCompletionOptions()
                {
                    ToolChoice = (requestSettings?.FunctionManager == null || requestSettings.FunctionManager.GetFunctionInfos().Count <= 0) ? null : ChatToolChoice.CreateAutoChoice(),
                    FrequencyPenalty = (float?)requestSettings?.FrequencyPenalty ?? _config.FrequencyPenalty,
                    MaxOutputTokenCount = requestSettings?.MaxOutputTokens ?? _config.MaxOutputTokens,
                    PresencePenalty = (float?)requestSettings?.PresencePenalty ?? _config.PresencePenalty,
                    Temperature = (float?)requestSettings?.Temperature ?? _config.Temperature,
                    TopP = (float?)requestSettings?.TopP ?? _config.TopP,
                };
                if (requestSettings != null)
                {
                    if (options.StopSequences != null)
                        foreach (var item in requestSettings.StopSequences)
                        {
                            options.StopSequences.Add(item);
                        }
                    if (requestSettings.FunctionManager != null && options.Tools != null)
                        foreach (var item in requestSettings.FunctionManager.GetFunctionInfos())
                        {
                            options.Tools.Add(ChatTool.CreateFunctionTool(item.Name, item.Description, BinaryData.FromObjectAsJson(item.Parameters, serializerOptions), true));
                        }
                }
                if (_config.ThinkHandle)
                {
                    options.ReasoningEffortLevel = ChatReasoningEffortLevel.Medium;
                }
                if (_config.EnableWebSearch)
                {
                    options.WebSearchOptions = new ChatWebSearchOptions();
                }

                var chatMessages = new List<OpenAIChatMessage>();
                foreach (var chatPart in chat)
                {
                    chatMessages.AddRange(await CreateOpenAIChatMessage(_storageService, chatPart.Role, chatPart.Contents, cancellationToken));
                }

                if (_config.CompatibleOldAPI)
                    SetMaxTokens(options, requestSettings?.MaxOutputTokens ?? _config.MaxOutputTokens);


                var parser = new OpenAIProviderParser(_storageService, _config.ThinkHandle);

                if (_config.IsStream)
                {
                    AsyncCollectionResult<StreamingChatCompletionUpdate> ret = _chatClient.CompleteChatStreamingAsync(
                        chatMessages, options, cancellationToken
                    );
                    await foreach (var msg in ret)
                    {
                        var handleRet = parser.Handle(msg, requestSettings?.FunctionManager);
                        await foreach (var item2 in handleRet)
                        {
                            yield return item2;
                        }
                    }
                }
                else
                {
                    ChatCompletion ret = await _chatClient.CompleteChatAsync(
                        chatMessages, options, cancellationToken
                    );
                    var handleRet = parser.Handle(ret, requestSettings?.FunctionManager);
                    await foreach (var item2 in handleRet)
                    {
                        yield return item2;
                    }
                }
            }
        }
    }
}
