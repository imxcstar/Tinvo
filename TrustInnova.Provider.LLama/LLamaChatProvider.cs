using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using LLama.Transformers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Provider.LLama.HistoryTransform;
using static LLama.Common.ChatHistory;
using LLamaCommon = LLama.Common;

namespace TrustInnova.Provider.LLama
{
    [TypeMetadataDisplayName("聊天配置")]
    public class LLamaChatConfig
    {
        public string ModelPath { get; set; } = "";

        [TypeMetadataDisplayStyles(LineNumber = 10)]
        public string Template { get; set; } = "";

        public List<string> AntiPrompts { get; set; } = [];

        [DefaultValue(8192)]
        public uint ContextSize { get; set; } = 8192;

        [DefaultValue(0.8f)]
        public float Temperature { get; set; } = 0.8f;

        [DefaultValue(0.95f)]
        public float TopP { get; set; } = 0.95f;

        [DefaultValue(0)]
        public int TopK { get; set; } = 0;

        [DefaultValue(2000)]
        public int MaxTokens { get; set; } = 2000;

        [TypeMetadataAllowNull]
        public string? StopPrompts { get; set; }
    }

    internal static class LLamaLoader
    {
        private static ConcurrentDictionary<string, LLamaWeights> Models { get; set; } = new ConcurrentDictionary<string, LLamaWeights>();
        private static ConcurrentDictionary<string, ModelParams> ModelParamss { get; set; } = new ConcurrentDictionary<string, ModelParams>();

        public static ModelParams GetModelParams(string modelPath, uint contextSize)
        {
            return ModelParamss.GetOrAdd(modelPath, v2 => new ModelParams(modelPath)
            {
                ContextSize = contextSize,
                GpuLayerCount = 1000
            });
        }

        public static LLamaWeights GetModel(string modelPath, uint contextSize)
        {
            var modelParams = GetModelParams(modelPath, contextSize);
            return Models.GetOrAdd(modelPath, v2 => LLamaWeights.LoadFromFile(modelParams));
        }
    }

    [ProviderTask("LLamaChat", "LLama")]
    public class LLamaChatProvider : IAIChatTask
    {
        private LLamaChatConfig _config;
        private string _modelPath;

        public LLamaChatProvider(LLamaChatConfig config)
        {
            _config = config;
            _modelPath = config.ModelPath;
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> ChatAsync(Abstractions.AIScheduler.ChatHistory chat, ChatSettings? chatSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var modelParams = LLamaLoader.GetModelParams(_modelPath, _config.ContextSize);
            var model = LLamaLoader.GetModel(_modelPath, _config.ContextSize);
            var executor = new InteractiveExecutor(model.CreateContext(modelParams));
            var session = new ChatSession(executor);
            session.WithHistoryTransform(new TemplateHistoryTransform(_config.Template));
            var chatHistory = new LLamaCommon.ChatHistory(chat.Select(x =>
            {
                var fContnet = x.Contents.FirstOrDefault();
                if (fContnet == null)
                    return null;

                if (x.Role == Abstractions.AIScheduler.AuthorRole.Assistant)
                {
                    if (fContnet.ContentType != ChatMessageContentType.Text)
                        throw new NotSupportedException("LLama其它角色发送不支持的内容类型");
                    return new Message(LLamaCommon.AuthorRole.Assistant, (string)fContnet.Content);
                }
                else
                {
                    var role = x.Role switch
                    {
                        Abstractions.AIScheduler.AuthorRole.User => LLamaCommon.AuthorRole.User,
                        _ => LLamaCommon.AuthorRole.System
                    };
                    switch (fContnet.ContentType)
                    {
                        case ChatMessageContentType.Text:
                            return new Message(role, (string)fContnet.Content);
                        case ChatMessageContentType.ImageBase64:
                        case ChatMessageContentType.ImageURL:
                        case ChatMessageContentType.DocStream:
                        case ChatMessageContentType.DocURL:
                        default:
                            throw new NotSupportedException("LLama发送不支持的内容类型");
                    }
                }
            }).Where(x => x != null).ToArray()!);
            var inferenceParams = new InferenceParams()
            {
                MaxTokens = chatSettings?.MaxTokens ?? _config.MaxTokens,
                SamplingPipeline = new DefaultSamplingPipeline()
                {
                    Temperature = Convert.ToSingle(chatSettings?.Temperature ?? _config.Temperature),
                    TopP = Convert.ToSingle(chatSettings?.TopP ?? _config.TopP),
                    TopK = _config.TopK
                },
                AntiPrompts = chatSettings?.StopSequences?.ToList() ?? [],
            };
            inferenceParams.AntiPrompts = _config.AntiPrompts;
            IAsyncEnumerable<string> chatRet;
            chatRet = session.ChatAsync(chatHistory, inferenceParams, cancellationToken);
            var message = new StringBuilder();
            await foreach (var item in chatRet)
            {
                if ((!string.IsNullOrEmpty(_config.StopPrompts) && item == _config.StopPrompts) || _config.AntiPrompts.Contains(item))
                {
                    session.AddAssistantMessage(message.ToString());
                    break;
                }
                message.Append(item);
                yield return new AIProviderHandleTextMessageResponse()
                {
                    Message = item
                };
            }
        }

        public Abstractions.AIScheduler.ChatHistory CreateNewChat(string? instructions = null)
        {
            var ret = new Abstractions.AIScheduler.ChatHistory();
            if (instructions == null)
                ret.AddMessage(Abstractions.AIScheduler.AuthorRole.System, [new(Guid.NewGuid().ToString(), $@"现在的时间为：{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}", ChatMessageContentType.Text)]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(Abstractions.AIScheduler.AuthorRole.System, [new(Guid.NewGuid().ToString(), instructions, ChatMessageContentType.Text)]);
            return ret;
        }
    }
}
