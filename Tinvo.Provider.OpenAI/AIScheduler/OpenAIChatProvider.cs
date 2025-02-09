using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Abstractions;
using System.ComponentModel;
using OpenAI.Chat;
using System.ClientModel;
using OpenAI;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;
using OpenAIChatMessageContent = OpenAI.Chat.ChatMessageContent;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.ClientModel.Primitives;

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

        [Description("MaxTokens")]
        [DefaultValue(4096)]
        public int MaxTokens { get; set; } = 4096;

        [Description("FrequencyPenalty")]
        [DefaultValue(0)]
        public float FrequencyPenalty { get; set; } = 0;

        [Description("PresencePenalty")]
        [DefaultValue(0)]
        public float PresencePenalty { get; set; } = 0;

        [Description("Temperature")]
        [DefaultValue(0.6f)]
        public float Temperature { get; set; } = 0.6f;

        [Description("TopP")]
        [DefaultValue(1)]
        public float TopP { get; set; } = 1;

        [Description("流式")]
        [DefaultValue(true)]
        public bool IsStream { get; set; } = true;
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
        private readonly ChatClient _chatClient;
        private readonly OpenAIChatConfig _config;
        private readonly IAIChatParser _parser;

        public OpenAIChatProvider(OpenAIChatConfig config)
        {
            _config = config;
            _chatClient = new ChatClient(_config.Model, new ApiKeyCredential(_config.Token ?? ""), new OpenAIClientOptions()
            {
                Endpoint = new Uri(_config.BaseURL),
                NetworkTimeout = TimeSpan.FromDays(10),
                Transport = new BlazorHttpClientTransport()
            });
            _parser = new OpenAIProviderParser();
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

        private OpenAIChatMessage CreateOpenAIChatMessage(AuthorRole role, List<Abstractions.AIScheduler.ChatMessageContent> contents)
        {
            var msg = contents.Select(x =>
            {
                switch (x.ContentType)
                {
                    case ChatMessageContentType.Text:
                        return ChatMessageContentPart.CreateTextPart((string)x.Content);
                    case ChatMessageContentType.ImageBase64:
                        return ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(Convert.FromBase64String((string)x.Content)), "image/png");
                    case ChatMessageContentType.ImageURL:
                        return ChatMessageContentPart.CreateImagePart(new Uri((string)x.Content));
                    default:
                        throw new NotImplementedException();
                }
            });

            return role switch
            {
                AuthorRole.User => OpenAIChatMessage.CreateUserMessage(msg),
                AuthorRole.System => OpenAIChatMessage.CreateSystemMessage(msg),
                AuthorRole.Assistant => OpenAIChatMessage.CreateAssistantMessage(msg),
                AuthorRole.Tool => OpenAIChatMessage.CreateToolMessage(string.Join('-', contents.Select(x => x.ContentId)), msg),
                _ => throw new NotImplementedException(),
            };
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> ChatAsync(ChatHistory chat, ChatSettings? requestSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var options = new ChatCompletionOptions()
            {
                ToolChoice = (requestSettings?.FunctionManager == null || requestSettings.FunctionManager.FunctionInfos.Count <= 0) ? null : ChatToolChoice.CreateAutoChoice(),
                FrequencyPenalty = (float?)requestSettings?.FrequencyPenalty ?? _config.FrequencyPenalty,
                MaxOutputTokenCount = requestSettings?.MaxTokens ?? _config.MaxTokens,
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
                    foreach (var item in requestSettings.FunctionManager.FunctionInfos)
                    {
                        options.Tools.Add(ChatTool.CreateFunctionTool(item.Name, item.Description, BinaryData.FromObjectAsJson(item.Parameters), true));
                    }
            }

            var chatMessages = chat.Select(x => CreateOpenAIChatMessage(x.Role, x.Contents)).ToList();

            if (_config.IsStream)
            {
                AsyncCollectionResult<StreamingChatCompletionUpdate> ret = _chatClient.CompleteChatStreamingAsync(
                    chatMessages, options, cancellationToken
                );
                await foreach (var msg in ret)
                {
                    var handleRet = _parser.Handle(msg, requestSettings?.FunctionManager);
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
                var handleRet = _parser.Handle(ret, requestSettings?.FunctionManager);
                await foreach (var item2 in handleRet)
                {
                    yield return item2;
                }
            }
        }
    }
}
