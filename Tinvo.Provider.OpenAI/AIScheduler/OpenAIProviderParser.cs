using OpenAI.Chat;
using Serilog;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Utils.Extend;

namespace Tinvo.Provider.OpenAI.AIScheduler
{
    public class OpenAIProviderParser : IAIChatParser
    {
        private string _handleFunctionName = "";
        private StringBuilder _functionContentBuilder = new();
        private Serilog.ILogger _logger;
        private bool _isInReasoning = false;

        public OpenAIProviderParser()
        {
            _logger = Log.ForContext<OpenAIProviderParser>();
        }

        public void ResetHandleState()
        {
        }

        private IDictionary<string, BinaryData>? GetAdditionalRawData(StreamingChatCompletionUpdate item)
        {
            var choice = item.GetPrivatePropertyValue<object>("InternalChoiceDelta");
            return choice?.GetPrivatePropertyValue<IDictionary<string, BinaryData>>("SerializedAdditionalRawData");
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> Handle(object msg, IFunctionManager? functionManager)
        {
            if (msg is ChatCompletion tmsg)
            {
                foreach (var item in tmsg.Content)
                {
                    _logger.Debug("AddHandleMsg 触发 {value} 信息", item.Kind.ToString());
                    switch (item.Kind)
                    {
                        case ChatMessageContentPartKind.Text:
                            _logger.Debug("AddHandleMsg文本信息：{value}", item.Text);
                            yield return new AIProviderHandleTextMessageResponse()
                            {
                                Message = item.Text
                            };
                            break;
                        case ChatMessageContentPartKind.Refusal:
                            yield return new AIProviderHandleRefusalMessageResponse();
                            break;
                        case ChatMessageContentPartKind.Image:
                            yield return new AIProviderHandleImageMessageResponse()
                            {
                                Image = item.ImageBytes.ToStream()
                            };
                            break;
                        default:
                            break;
                    }
                }

                switch (tmsg.FinishReason)
                {
                    case ChatFinishReason.ToolCalls:
                    case ChatFinishReason.FunctionCall:
                        foreach (var item in tmsg.ToolCalls)
                        {
                            switch (item.Kind)
                            {
                                case ChatToolCallKind.Function:
                                    var functionText = item.FunctionArguments.ToString();
                                    _handleFunctionName = item.FunctionName;
                                    _logger.Debug("AddHandleMsg函数信息：{name}, {value}", _handleFunctionName, functionText);
                                    _functionContentBuilder.Append(functionText);
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (msg is StreamingChatCompletionUpdate streamMsg)
            {
                var additionalRawData = GetAdditionalRawData(streamMsg);
                if (additionalRawData?.TryGetValue("reasoning_content", out var val) == true)
                {
                    var reasoningContent = val?.ToObjectFromJson<string>();
                    if (!string.IsNullOrEmpty(reasoningContent))
                    {
                        if (!_isInReasoning)
                        {
                            _isInReasoning = true;
                            yield return new AIProviderHandleReasoningStartResponse();
                        }

                        yield return new AIProviderHandleTextMessageResponse()
                        {
                            Message = reasoningContent
                        };
                    }
                }

                if (_isInReasoning)
                {
                    _isInReasoning = false;
                    yield return new AIProviderHandleReasoningEndResponse();
                }

                if (streamMsg.ContentUpdate.Count > 0)
                {
                    _handleFunctionName = "";
                    _functionContentBuilder.Clear();
                    foreach (var item in streamMsg.ContentUpdate)
                    {
                        _logger.Debug("AddHandleMsg 触发 {value} 信息", item.Kind.ToString());
                        switch (item.Kind)
                        {
                            case ChatMessageContentPartKind.Text:
                                _logger.Debug("AddHandleMsg文本信息：{value}", item.Text);
                                yield return new AIProviderHandleTextMessageResponse()
                                {
                                    Message = item.Text
                                };
                                break;
                            case ChatMessageContentPartKind.Refusal:
                                yield return new AIProviderHandleRefusalMessageResponse();
                                break;
                            case ChatMessageContentPartKind.Image:
                                yield return new AIProviderHandleImageMessageResponse()
                                {
                                    Image = item.ImageBytes.ToStream()
                                };
                                break;
                            default:
                                break;
                        }
                    }
                }

                foreach (var item in streamMsg.ToolCallUpdates)
                {
                    switch (item.Kind)
                    {
                        case ChatToolCallKind.Function:
                            var functionText = item.FunctionArgumentsUpdate.ToString();
                            _handleFunctionName = item.FunctionName;
                            _logger.Debug("AddHandleMsg函数信息：{name}, {value}", _handleFunctionName, functionText);
                            _functionContentBuilder.Append(functionText);
                            break;
                        default:
                            break;
                    }
                }

                if (streamMsg.FinishReason == ChatFinishReason.ToolCalls || streamMsg.FinishReason == ChatFinishReason.FunctionCall)
                {
                    var argStr = _functionContentBuilder.ToString();
                    _logger.Debug("调用函数前触发：{name}, {argStr}", _handleFunctionName, argStr);
                    yield return new AIProviderHandleFunctionCallResponse()
                    {
                        FunctionManager = functionManager!,
                        FunctionName = _handleFunctionName,
                        Arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argStr)
                    };
                }
            }
            else
            {
                _logger.Debug("AddHandleMsg: msg is not handle");
                yield break;
            }
        }
    }
}
