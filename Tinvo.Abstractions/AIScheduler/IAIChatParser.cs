using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tinvo.Abstractions.AIScheduler
{
    public interface IAIChatHandleResponse
    {
        public AIChatHandleResponseType Type { get; }
    }

    public enum AIChatHandleResponseType
    {
        TextMessage,
        ImageMessage,
        FunctionStart,
        FunctionCall,
        Refusal,
        ReasoningStart,
        ReasoningEnd,
    }

    public class AIProviderHandleRefusalMessageResponse : IAIChatHandleResponse
    {
        public AIChatHandleResponseType Type => AIChatHandleResponseType.Refusal;
    }

    public class AIProviderHandleReasoningStartResponse : IAIChatHandleResponse
    {
        public AIChatHandleResponseType Type => AIChatHandleResponseType.ReasoningStart;
    }

    public class AIProviderHandleReasoningEndResponse : IAIChatHandleResponse
    {
        public AIChatHandleResponseType Type => AIChatHandleResponseType.ReasoningEnd;
    }

    public class AIProviderHandleTextMessageResponse : IAIChatHandleResponse
    {
        public AIChatHandleResponseType Type => AIChatHandleResponseType.TextMessage;

        public string Message { get; set; }
    }

    public class AIProviderHandleImageMessageResponse : IAIChatHandleResponse
    {
        public AIChatHandleResponseType Type => AIChatHandleResponseType.ImageMessage;

        public Stream Image { get; set; }
    }

    public class AIProviderHandleFunctionStartResponse : IAIChatHandleResponse
    {
        public AIChatHandleResponseType Type => AIChatHandleResponseType.FunctionStart;

        public IFunctionManager FunctionManager { get; set; }

        public string FunctionName { get; set; } = null!;
    }

    public class AIProviderHandleFunctionCallResponse : IAIChatHandleResponse
    {
        public AIChatHandleResponseType Type => AIChatHandleResponseType.FunctionCall;

        public IFunctionManager FunctionManager { get; set; }

        public string FunctionName { get; set; } = null!;

        public Dictionary<string, JsonElement>? Arguments { get; set; }
    }

    public interface IAIChatParser
    {
        public void ResetHandleState();
        public IAsyncEnumerable<IAIChatHandleResponse> Handle(object msg, IFunctionManager? functionManager);
    }
}
