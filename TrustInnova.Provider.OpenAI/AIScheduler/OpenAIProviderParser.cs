using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Provider.OpenAI.API;

namespace TrustInnova.Provider.OpenAI.AIScheduler
{
    public class OpenAIProviderParser : IAIChatParser
    {
        private enum HandleState
        {
            Message,
            Function
        }

        private string _handleFunctionName = "";
        private StringBuilder _functionContentBuilder = new();
        private HandleState _handleState = HandleState.Message;
        private Serilog.ILogger _logger;

        public OpenAIProviderParser()
        {
            _logger = Log.ForContext<OpenAIProviderParser>();
        }

        public void ResetHandleState()
        {
            _handleState = HandleState.Message;
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> Handle(object msg, IFunctionManager? functionManager)
        {
            _logger.Debug("{State}: AddHandleMsg: {value}", _handleState, msg == null ? "null" : JsonSerializer.Serialize(msg));
            var choices = (msg as OpenAIChatCompletionCreateResponse)?.Choices?.FirstOrDefault();
            if (choices == null || choices.Message == null)
            {
                _logger.Debug("{State}: AddHandleMsg: choices is null", _handleState);
                yield break;
            }
            _logger.Debug("{State}: AddHandleMsg函数信息：{value}", _handleState, _functionContentBuilder.ToString());
            switch (_handleState)
            {
                case HandleState.Message:
                    if (choices.Message.FunctionCall?.Name == null)
                    {
                        var contentStr = choices.Message.Content?.ToString();
                        if (!string.IsNullOrEmpty(contentStr))
                            yield return new AIProviderHandleTextMessageResponse()
                            {
                                Message = contentStr
                            };
                        break;
                    }
                    _handleFunctionName = choices.Message.FunctionCall.Name;
                    _functionContentBuilder = new StringBuilder();
                    _handleState = HandleState.Function;
                    yield return new AIProviderHandleFunctionStartResponse()
                    {
                        FunctionManager = functionManager!,
                        FunctionName = _handleFunctionName
                    };
                    break;
                case HandleState.Function:
                    var targuments = choices.Message.FunctionCall?.Arguments;
                    if (choices.FinishReason == "function_call")
                    {
                        var argStr = _functionContentBuilder.ToString();
                        _logger.Debug("调用函数前触发：{name}, {argStr}", _handleFunctionName, argStr);
                        yield return new AIProviderHandleFunctionCallResponse()
                        {
                            FunctionManager = functionManager!,
                            FunctionName = _handleFunctionName,
                            Arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argStr)
                        };
                        break;
                    }
                    else if (!string.IsNullOrEmpty(choices.Message.Content?.ToString()))
                    {
                        var contentStr = choices.Message.Content.ToString()!;
                        yield return new AIProviderHandleTextMessageResponse()
                        {
                            Message = contentStr
                        };
                        _handleState = HandleState.Message;
                    }
                    else if (targuments == null)
                        break;
                    _functionContentBuilder.Append(targuments);
                    break;
                default:
                    break;
            }
        }
    }
}
