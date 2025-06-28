using OpenAI.Chat;
using OpenAI.Responses;
using Serilog;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.DataStorage;

namespace Tinvo.Provider.OpenAI.AIScheduler
{
    public class OpenAIProviderResponsetParser : IAIChatParser
    {
        private readonly IDataStorageService _storageService;
        private StringBuilder _functionContentBuilder = new();
        private Serilog.ILogger _logger;
        private bool _isInReasoning = false;
        private bool _isThinkHandle = false;

        public OpenAIProviderResponsetParser(IDataStorageService storageService, bool isThinkHandle)
        {
            _storageService = storageService;
            _logger = Log.ForContext<OpenAIProviderParser>();
            _isThinkHandle = isThinkHandle;
        }

        public void ResetHandleState()
        {
        }

        public async IAsyncEnumerable<IAIChatHandleMessage> Handle(object msg, IFunctionManager? functionManager)
        {
            _logger.Debug("OpenAIProviderResponsetParser Handle, msg Type {value}", msg.GetType().FullName);
            if (msg is ClientResult<OpenAIResponse> tmsg)
            {
                foreach (var item in tmsg.Value.OutputItems)
                {
                    if (item is MessageResponseItem message)
                    {
                        foreach (var content in message.Content)
                        {
                            switch (content.Kind)
                            {
                                case ResponseContentPartKind.OutputText:
                                    yield return new AIProviderHandleTextMessageResponse() { Message = content.Text };
                                    break;
                                case ResponseContentPartKind.Refusal:
                                    yield return new AIProviderHandleRefusalMessageResponse() { Refusal = content.Refusal };
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else if (item is ReasoningResponseItem reasoning)
                    {
                        foreach (var summaryTextPart in reasoning.SummaryTextParts)
                        {
                            yield return new AIProviderHandleReasoningMessageResponse() { Message = summaryTextPart };
                        }
                    }
                }
            }
            else if (msg is StreamingResponseOutputTextDeltaUpdate textUpdate)
            {
                yield return new AIProviderHandleTextMessageResponse() { Message = textUpdate.Delta };
            }
            else if (msg is StreamingResponseRefusalDeltaUpdate refusalUpdate)
            {
                yield return new AIProviderHandleRefusalMessageResponse() { Refusal = refusalUpdate.Delta };
            }
            else if (msg is StreamingResponseOutputItemDoneUpdate outputItemDone)
            {
                if (outputItemDone.Item is FunctionCallResponseItem functionCall)
                    yield return new AIProviderHandleFunctionCallResponse()
                    {
                        FunctionManager = functionManager!,
                        FunctionName = functionCall.FunctionName,
                        CallID = functionCall.CallId,
                        Arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(functionCall.FunctionArguments)
                    };
            }
        }
    }
}
