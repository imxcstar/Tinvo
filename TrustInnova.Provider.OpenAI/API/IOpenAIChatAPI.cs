using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebApiClientCore.Attributes;

namespace TrustInnova.Provider.OpenAI.API
{
    [APILoggingFilter]
    public interface IOpenAIChatAPI
    {
        [Timeout(60 * 10 * 1000)]
        [HttpPost("v1/chat/completions")]
        [APIStreamEnableFilter]
        [APIAuthFilter]
        public Task<HttpResponseMessage> ChatCompletionsStreamAsync([NotNullJsonContent] OpenAIChatCompletionCreateRequest request, CancellationToken token = default);
    }

    public class OpenAIToolInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public OpenAIFunctionInfo Function { get; set; }
    }

    public class ToolChoiceObjectInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public ToolChoiceObjectFunctionInfo Function { get; set; }
    }

    public class ToolChoiceObjectFunctionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class OpenAIChatCompletionCreateRequest
    {
        [JsonPropertyName("tools")]
        public List<OpenAIToolInfo>? Tools {
            get {
                return Functions?.Select(x => new OpenAIToolInfo()
                {
                    Function = x
                }).ToList();
            }
            set { }
        }

        [JsonPropertyName("tool_choice")]
        public object? ToolChoice { get; set; }

        [JsonPropertyName("functions")]
        public List<OpenAIFunctionInfo>? Functions { get; set; }

        [JsonPropertyName("messages")]
        public IList<OpenAIChatMessage> Messages { get; set; }

        [JsonPropertyName("top_p")]
        public float? TopP { get; set; }

        [JsonPropertyName("n")]
        public int? N { get; set; }

        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }

        [JsonIgnore]
        public string? Stop { get; set; }

        [JsonIgnore]
        public IList<string>? StopAsList { get; set; }

        [JsonPropertyName("stop")]
        public IList<string>? StopCalculated
        {
            get
            {
                if (Stop != null && StopAsList != null)
                {
                    throw new ValidationException("Stop and StopAsList can not be assigned at the same time. One of them is should be null.");
                }

                if (Stop != null)
                {
                    return new List<string> { Stop };
                }

                return StopAsList;
            }
        }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("presence_penalty")]
        public float? PresencePenalty { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public float? FrequencyPenalty { get; set; }

        [JsonPropertyName("logit_bias")]
        public object? LogitBias { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; }
    }

    public class OpenAIFunctionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("parameters")]
        public OpenAIFunctionParametersInfo Parameters { get; set; } = null!;
    }

    public class OpenAIFunctionParametersInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("properties")]
        public Dictionary<string, OpenAIFunctionParametersProperties> Properties { get; set; } = new Dictionary<string, OpenAIFunctionParametersProperties>();

        [JsonPropertyName("required")]
        public List<string> Required { get; set; } = new List<string>();
    }

    public class OpenAIFunctionParametersProperties
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";

        [JsonPropertyName("description")]
        public string Description { get; set; } = null!;

        [JsonPropertyName("enum")]
        public List<string> Enum { get; set; } = null!;
    }

    public record OpenAIBaseResponse
    {
        [JsonPropertyName("object")] public string? ObjectTypeName { get; set; }
        public bool Successful => Error == null;
        [JsonPropertyName("error")] public OpenAIError? Error { get; set; }
    }

    public record OpenAIError
    {
        [JsonPropertyName("code")] public string? Code { get; set; }

        [JsonPropertyName("message")] public string? Message { get; set; }

        [JsonPropertyName("param")] public string? Param { get; set; }

        [JsonPropertyName("type")] public string? Type { get; set; }
    }

    public record OpenAIUsageResponse
    {
        [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
    }

    public record OpenAIChatCompletionCreateResponse : OpenAIBaseResponse
    {
        [JsonPropertyName("model")] public string Model { get; set; } = null!;

        [JsonPropertyName("choices")] public List<OpenAIChatChoiceResponse> Choices { get; set; } = null!;

        [JsonPropertyName("usage")] public OpenAIUsageResponse Usage { get; set; } = null!;

        [JsonPropertyName("created")] public int CreatedAt { get; set; }

        [JsonPropertyName("id")] public string Id { get; set; } = null!;
    }

    public record OpenAIChatChoiceResponse
    {
        [JsonPropertyName("delta")]
        public OpenAIChatMessage Delta
        {
            get => Message;
            set => Message = value;
        }

        [JsonPropertyName("message")] public OpenAIChatMessage Message { get; set; } = null!;

        [JsonPropertyName("index")] public int? Index { get; set; }

        [JsonPropertyName("finish_reason")] public string FinishReason { get; set; } = null!;
    }

    public class OpenAIFunctionCallInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public string? Arguments { get; set; }
    }

    public class OpenAIToolCallsInfo
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("function")]
        public OpenAIFunctionCallInfo Function { get; set; }
    }

    public class OpenAIChatMessage
    {
        public OpenAIChatMessage(string role, object content, string? name = null)
        {
            Role = role;
            Content = content;
            Name = name;
        }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public object? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<OpenAIToolCallsInfo>? ToolCalls { get; set; }

        private OpenAIFunctionCallInfo? _functionCall;

        [JsonPropertyName("function_call")]
        public OpenAIFunctionCallInfo? FunctionCall
        {
            get
            {
                if (ToolCalls != null && ToolCalls.Count > 0)
                    return ToolCalls.First().Function;
                return _functionCall;
            }
            set => _functionCall = value;
        }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        public static OpenAIChatMessage FromAssistant(string content, string? name = null)
        {
            return new OpenAIChatMessage("assistant", content, name);
        }

        public static OpenAIChatMessage FromUser(string content, string? name = null)
        {
            return new OpenAIChatMessage("user", content, name);
        }

        public static OpenAIChatMessage FromSystem(string content, string? name = null)
        {
            return new OpenAIChatMessage("system", content, name);
        }

        public static OpenAIChatMessage FromBase64Image(string role, string text, string base64Image, string? name = null)
        {
            return new OpenAIChatMessage(role, new List<object>()
            {
                new
                {
                    type="text",
                    text
                },
                new
                {
                    type="image_url",
                    image_url = new
                    {
                        url=$"data:image/jpeg;base64,{base64Image}",
                        detail="low"
                    }
                }
            }, name);
        }

        public static OpenAIChatMessage FromURLImage(string role, string text, string url, string? name = null)
        {
            return new OpenAIChatMessage(role, new List<object>()
            {
                new
                {
                    type="text",
                    text
                },
                new
                {
                    type="image_url",
                    image_url = new
                    {
                        url,
                        detail="low"
                    }
                }
            }, name);
        }
    }
}
