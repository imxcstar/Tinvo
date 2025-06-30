using OpenAI.Chat;
using Serilog;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.DataStorage;
using Tinvo.Utils.Extend;

namespace Tinvo.Provider.OpenAI.AIScheduler
{
    public class OpenAIProviderToolInfo
    {
        public string? CallId { get; set; }
        public string? FunctionName { get; set; }
        public string Args { get; set; } = "";
    }

    public class OpenAIProviderParser : IAIChatParser
    {
        private readonly IDataStorageService _storageService;
        private Dictionary<string, OpenAIProviderToolInfo> _handleFunctions;
        private Serilog.ILogger _logger;
        private bool _isInReasoning = false;
        private bool _isThinkHandle = false;
        private string _lastHandleFunctionId = "";

        public OpenAIProviderParser(IDataStorageService storageService, bool isThinkHandle)
        {
            _storageService = storageService;
            _logger = Log.ForContext<OpenAIProviderParser>();
            _handleFunctions = new Dictionary<string, OpenAIProviderToolInfo>();
            _isThinkHandle = isThinkHandle;
        }

        public void ResetHandleState()
        {
        }

        private IDictionary<string, BinaryData>? GetAdditionalRawData(StreamingChatCompletionUpdate item)
        {
            var choice = item.GetPrivatePropertyValue<object>("InternalChoiceDelta");
            return choice?.GetPrivatePropertyValue<IDictionary<string, BinaryData>>("SerializedAdditionalRawData");
        }

        public async IAsyncEnumerable<IAIChatHandleMessage> Handle(object msg, IFunctionManager? functionManager)
        {
            if (msg is ChatCompletion tmsg)
            {
                foreach (var item in tmsg.Content)
                {
                    var customFileID = Guid.NewGuid().ToString();
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
                            yield return new AIProviderHandleRefusalMessageResponse()
                            {
                                Refusal = item.Refusal
                            };
                            break;
                        case ChatMessageContentPartKind.Image:
                            await _storageService.SetItemAsBinaryAsync(customFileID, item.ImageBytes.ToArray());
                            yield return new AIProviderHandleCustomFileMessageResponse()
                            {
                                Type = AIChatHandleMessageType.ImageMessage,
                                FileCustomID = customFileID,
                                FileOriginalID = item.FileId,
                                FileOriginalName = item.Filename,
                                FileOriginalMediaType = item.ImageBytesMediaType,
                                FileOriginalURL = item.ImageUri.ToString()
                            };
                            break;
                        case ChatMessageContentPartKind.InputAudio:
                            await _storageService.SetItemAsBinaryAsync(customFileID, item.InputAudioBytes.ToArray());
                            yield return new AIProviderHandleCustomFileMessageResponse()
                            {
                                Type = AIChatHandleMessageType.AudioMessage,
                                FileCustomID = customFileID,
                                FileOriginalID = item.FileId,
                                FileOriginalName = item.Filename,
                                FileOriginalMediaType = item.InputAudioFormat.ToString(),
                            };
                            break;
                        case ChatMessageContentPartKind.File:
                            await _storageService.SetItemAsBinaryAsync(customFileID, item.FileBytes.ToArray());
                            yield return new AIProviderHandleCustomFileMessageResponse()
                            {
                                Type = AIChatHandleMessageType.FileMessage,
                                FileCustomID = customFileID,
                                FileOriginalID = item.FileId,
                                FileOriginalName = item.Filename,
                                FileOriginalMediaType = item.FileBytesMediaType,
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
                                    _logger.Debug("AddHandleMsg函数信息：{id}, {name}, {args}", item.Id, item.FunctionName, functionText);
                                    if (!string.IsNullOrWhiteSpace(item.Id))
                                        _lastHandleFunctionId = item.Id;
                                    if (!_handleFunctions.ContainsKey(_lastHandleFunctionId))
                                        _handleFunctions[_lastHandleFunctionId] = new OpenAIProviderToolInfo();
                                    if (string.IsNullOrWhiteSpace(_handleFunctions[_lastHandleFunctionId].CallId))
                                        _handleFunctions[_lastHandleFunctionId].CallId = _lastHandleFunctionId;
                                    if (string.IsNullOrWhiteSpace(_handleFunctions[_lastHandleFunctionId].FunctionName))
                                        _handleFunctions[_lastHandleFunctionId].FunctionName = item.FunctionName;
                                    _handleFunctions[_lastHandleFunctionId].Args += functionText;
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
                        }

                        if (_isInReasoning)
                            yield return new AIProviderHandleReasoningMessageResponse()
                            {
                                Message = reasoningContent
                            };
                        else
                            yield return new AIProviderHandleTextMessageResponse()
                            {
                                Message = reasoningContent
                            };
                    }
                }

                if (_isInReasoning)
                {
                    _isInReasoning = false;
                }

                if (streamMsg.ContentUpdate.Count > 0)
                {
                    foreach (var item in streamMsg.ContentUpdate)
                    {
                        var customFileID = Guid.NewGuid().ToString();
                        _logger.Debug("AddHandleMsg 触发 {value} 信息", item.Kind.ToString());
                        switch (item.Kind)
                        {
                            case ChatMessageContentPartKind.Text:
                                if (_isThinkHandle)
                                {
                                    if (item.Text.Replace("\n", "") == "<think>")
                                        _isInReasoning = true;
                                    else if (item.Text.Replace("\n", "") == "</think>")
                                        _isInReasoning = false;
                                    else
                                    {
                                        _logger.Debug("AddHandleMsg文本信息：{value}", item.Text);
                                        if (_isInReasoning)

                                            yield return new AIProviderHandleReasoningMessageResponse()
                                            {
                                                Message = item.Text
                                            };
                                        else
                                            yield return new AIProviderHandleTextMessageResponse()
                                            {
                                                Message = item.Text
                                            };
                                    }
                                }
                                else
                                {
                                    _logger.Debug("AddHandleMsg文本信息：{value}", item.Text);
                                    yield return new AIProviderHandleTextMessageResponse()
                                    {
                                        Message = item.Text
                                    };
                                }

                                break;
                            case ChatMessageContentPartKind.Refusal:
                                yield return new AIProviderHandleRefusalMessageResponse()
                                {
                                    Refusal = item.Refusal
                                };
                                break;
                            case ChatMessageContentPartKind.Image:
                                await _storageService.SetItemAsBinaryAsync(customFileID, item.ImageBytes.ToArray());
                                yield return new AIProviderHandleCustomFileMessageResponse()
                                {
                                    Type = AIChatHandleMessageType.ImageMessage,
                                    FileCustomID = customFileID,
                                    FileOriginalID = item.FileId,
                                    FileOriginalName = item.Filename,
                                    FileOriginalMediaType = item.ImageBytesMediaType,
                                    FileOriginalURL = item.ImageUri.ToString()
                                };
                                break;
                            case ChatMessageContentPartKind.InputAudio:
                                await _storageService.SetItemAsBinaryAsync(customFileID, item.InputAudioBytes.ToArray());
                                yield return new AIProviderHandleCustomFileMessageResponse()
                                {
                                    Type = AIChatHandleMessageType.AudioMessage,
                                    FileCustomID = customFileID,
                                    FileOriginalID = item.FileId,
                                    FileOriginalName = item.Filename,
                                    FileOriginalMediaType = item.InputAudioFormat.ToString(),
                                };
                                break;
                            case ChatMessageContentPartKind.File:
                                await _storageService.SetItemAsBinaryAsync(customFileID, item.FileBytes.ToArray());
                                yield return new AIProviderHandleCustomFileMessageResponse()
                                {
                                    Type = AIChatHandleMessageType.FileMessage,
                                    FileCustomID = customFileID,
                                    FileOriginalID = item.FileId,
                                    FileOriginalName = item.Filename,
                                    FileOriginalMediaType = item.FileBytesMediaType,
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
                            var functionText = Encoding.UTF8.GetString(item.FunctionArgumentsUpdate.ToArray());
                            _logger.Debug("AddHandleMsg函数信息：{id}, {name}, {args}", item.ToolCallId, item.FunctionName, functionText);
                            if (!string.IsNullOrWhiteSpace(item.ToolCallId))
                                _lastHandleFunctionId = item.ToolCallId;
                            if (!_handleFunctions.ContainsKey(_lastHandleFunctionId))
                                _handleFunctions[_lastHandleFunctionId] = new OpenAIProviderToolInfo();
                            if (string.IsNullOrWhiteSpace(_handleFunctions[_lastHandleFunctionId].CallId))
                                _handleFunctions[_lastHandleFunctionId].CallId = _lastHandleFunctionId;
                            if (string.IsNullOrWhiteSpace(_handleFunctions[_lastHandleFunctionId].FunctionName))
                                _handleFunctions[_lastHandleFunctionId].FunctionName = item.FunctionName;
                            _handleFunctions[_lastHandleFunctionId].Args += functionText;
                            break;
                        default:
                            break;
                    }
                }

                if (streamMsg.FinishReason == ChatFinishReason.ToolCalls ||
                    streamMsg.FinishReason == ChatFinishReason.FunctionCall)
                {
                    foreach (var handleFunction in _handleFunctions.Values)
                    {
                        _logger.Debug("调用函数前触发：{id}, {name}, {args}", handleFunction.CallId, handleFunction.FunctionName, handleFunction.Args);
                        yield return new AIProviderHandleFunctionCallResponse()
                        {
                            FunctionManager = functionManager!,
                            FunctionName = handleFunction.FunctionName,
                            CallID = handleFunction.CallId,
                            Arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(handleFunction.Args)
                        };
                    }
                    _handleFunctions.Clear();
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