using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Provider.XunFei.API;

namespace TrustInnova.Provider.XunFei.AIScheduler
{
    public class XFSparkDeskProviderParser : IAIChatParser
    {
        private Serilog.ILogger _logger;

        public XFSparkDeskProviderParser()
        {
            _logger = Log.ForContext<XFSparkDeskProviderParser>();
        }

        public void ResetHandleState()
        {
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> Handle(object msg, IFunctionManager? functionManager)
        {
            _logger.Debug("AddHandleMsg: {value}", msg == null ? "null" : JsonSerializer.Serialize(msg));
            if (msg is XFSparkDeskChatAPIResponse chatMsg)
            {
                var retContent = chatMsg?.Payload?.Choices?.Text.FirstOrDefault();
                if (retContent == null)
                {
                    _logger.Debug("AddHandleMsg(Chat): retContent is null");
                    yield break;
                }

                if (!string.IsNullOrEmpty(retContent.Content))
                {
                    _logger.Debug("AddHandleMsg(Chat): {value}", retContent.Content);
                    yield return new AIProviderHandleTextMessageResponse()
                    {
                        Message = retContent.Content
                    };
                    yield break;
                }

                if (retContent.FunctionCall != null)
                {
                    _logger.Debug("AddHandleMsg(Chat): Function Call, {name}, {args}", retContent.FunctionCall.Name, retContent.FunctionCall.Arguments);
                    yield return new AIProviderHandleFunctionStartResponse()
                    {
                        FunctionManager = functionManager!,
                        FunctionName = retContent.FunctionCall.Name
                    };
                    yield return new AIProviderHandleFunctionCallResponse()
                    {
                        FunctionManager = functionManager!,
                        FunctionName = retContent.FunctionCall.Name,
                        Arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(retContent.FunctionCall.Arguments)
                    };
                    yield break;
                }

                _logger.Debug("AddHandleMsg(Chat): retContent error, {retContent}", retContent);
            }
            else if (msg is XFSparkDeskImageAPIResponse imageMsg)
            {
                var retContent = imageMsg?.Payload?.Choices?.Text.FirstOrDefault();
                if (retContent == null)
                {
                    _logger.Debug("AddHandleMsg(Image): retContent is null");
                    yield break;
                }


                if (!string.IsNullOrEmpty(retContent.Content))
                {
                    _logger.Debug("AddHandleMsg(Image): {value}", retContent.Content);
                    yield return new AIProviderHandleTextMessageResponse()
                    {
                        Message = retContent.Content
                    };
                    yield break;
                }

                _logger.Debug("AddHandleMsg(Image): retContent error, {retContent}", retContent);
            }
        }
    }
}
