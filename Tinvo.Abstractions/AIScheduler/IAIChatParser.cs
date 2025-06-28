using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tinvo.Abstractions.AIScheduler
{
    public interface IAIChatHandleMessage
    {
        public AIChatHandleMessageType Type { get; }
    }

    public enum AIChatHandleMessageType
    {
        TextMessage,
        ImageMessage,
        FunctionStart,
        FunctionCall,
        Refusal,
        ReasoningMessage,
        AudioMessage,
        FileMessage,
        AudioStreamMessage,
    }

    public class AIProviderHandleRefusalMessageResponse : IAIChatHandleMessage
    {
        public AIChatHandleMessageType Type => AIChatHandleMessageType.Refusal;

        public string? Refusal { get; set; }
    }

    public class AIProviderHandleReasoningMessageResponse : IAIChatHandleMessage
    {
        public AIChatHandleMessageType Type => AIChatHandleMessageType.ReasoningMessage;

        public required string Message { get; set; }
    }

    public class AIProviderHandleTextMessageResponse : IAIChatHandleMessage
    {
        public AIChatHandleMessageType Type => AIChatHandleMessageType.TextMessage;

        public required string Message { get; set; }
    }

    public class AIProviderHandleCustomFileMessageResponse : IAIChatHandleMessage
    {
        public required AIChatHandleMessageType Type { get; set; }

        public required string FileCustomID { get; set; }

        public string? FileOriginalID { get; set; }

        public string? FileOriginalName { get; set; }

        public string? FileOriginalMediaType { get; set; }

        public string? FileOriginalURL { get; set; }
    }

    public class AIProviderHandleFunctionCallResponse : IAIChatHandleMessage
    {
        public AIChatHandleMessageType Type => AIChatHandleMessageType.FunctionCall;

        [JsonIgnore]
        public IFunctionManager? FunctionManager { get; set; }

        public required string FunctionName { get; set; }

        public required string CallID { get; set; }

        public List<IAIChatHandleMessage>? Result { get; set; }

        public Dictionary<string, JsonElement>? Arguments { get; set; }
    }

    public class AIProviderHandleAudioStreamMessageResponse : IAIChatHandleMessage
    {
        public AIChatHandleMessageType Type => AIChatHandleMessageType.ReasoningMessage;

        public required Stream Stream { get; set; }
    }

    public interface IAIChatParser
    {
        public void ResetHandleState();
        public IAsyncEnumerable<IAIChatHandleMessage> Handle(object msg, IFunctionManager? functionManager);
    }

    public class IAIChatHandleMessageConverter : JsonConverter<IAIChatHandleMessage>
    {
        private static readonly Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>
        {
            { nameof(AIProviderHandleRefusalMessageResponse), typeof(AIProviderHandleRefusalMessageResponse) },
            { nameof(AIProviderHandleTextMessageResponse), typeof(AIProviderHandleTextMessageResponse) },
            { nameof(AIProviderHandleCustomFileMessageResponse), typeof(AIProviderHandleCustomFileMessageResponse) },
            { nameof(AIProviderHandleReasoningMessageResponse), typeof(AIProviderHandleReasoningMessageResponse) },
            { nameof(AIProviderHandleFunctionCallResponse), typeof(AIProviderHandleFunctionCallResponse) },
        };

        public override IAIChatHandleMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (!doc.RootElement.TryGetProperty("_type", out var typeProp))
                throw new JsonException("Missing _type property for IAIChatHandleMessage deserialization.");
            var typeName = typeProp.GetString();
            if (typeName == null || !TypeMapping.TryGetValue(typeName, out var targetType))
                throw new JsonException($"Unknown _type: {typeName}");
            var json = doc.RootElement.GetRawText();
            return (IAIChatHandleMessage?)JsonSerializer.Deserialize(json, targetType, options);
        }

        public override void Write(Utf8JsonWriter writer, IAIChatHandleMessage value, JsonSerializerOptions options)
        {
            var type = value.GetType();
            var json = JsonSerializer.SerializeToElement(value, type, options);
            writer.WriteStartObject();
            writer.WriteString("_type", type.Name);
            foreach (var prop in json.EnumerateObject())
            {
                prop.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
    }
}
