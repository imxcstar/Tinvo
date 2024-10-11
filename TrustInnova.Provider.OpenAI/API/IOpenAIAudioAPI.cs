using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace TrustInnova.Provider.OpenAI.API
{
    [APILoggingFilter]
    public interface IOpenAIAudioAPI
    {
        [Timeout(60 * 10 * 1000)]
        [HttpPost("v1/audio/speech")]
        [APIAuthFilter]
        public Task<HttpResponseMessage> AudioSpeechAsync([NotNullJsonContent] AudioSpeechRequest request, CancellationToken token = default);

        [Timeout(60 * 10 * 1000)]
        [HttpPost("v1/audio/transcriptions")]
        [APIAuthFilter]
        public Task<AudioTranscriptionsResponse> AudioTranscriptionsAsync([FormDataContent] AudioTranscriptionsRequest request, CancellationToken token = default);

        [Timeout(60 * 10 * 1000)]
        [HttpPost("v1/audio/translations")]
        [APIAuthFilter]
        public Task<AudioTranslationsResponse> AudioTranslationsAsync([FormDataContent] AudioTranslationsRequest request, CancellationToken token = default);
    }

    public class AudioSpeechRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        [JsonPropertyName("input")]
        public string Input { get; set; } = null!;

        [JsonPropertyName("voice")]
        public string Voice { get; set; } = null!;

        [JsonPropertyName("response_format")]
        public string? ResponseFormat { get; set; }

        [JsonPropertyName("speed")]
        public float? Speed { get; set; }
    }

    public class AudioTranscriptionsRequest : IApiParameter
    {
        public Stream File { get; set; } = null!;

        public string FileName { get; set; } = null!;

        public string Model { get; set; } = null!;

        public string? Language { get; set; }

        public string? Prompt { get; set; }

        public string? ResponseFormat { get; set; }

        public float? Temperature { get; set; }

        public Task OnRequestAsync(ApiParameterContext context)
        {
            var formData = new MultipartFormDataContent
            {
                { new StreamContent(File), "file", FileName },
                { new StringContent(Model), "model" },
            };
            if (Language != null)
                formData.Add(new StringContent(Language), "language");
            if (Prompt != null)
                formData.Add(new StringContent(Prompt), "prompt");
            if (ResponseFormat != null)
                formData.Add(new StringContent(ResponseFormat), "response_format");
            if (Temperature != null)
                formData.Add(new StringContent(Temperature.Value.ToString()), "temperature");
            context.HttpContext.RequestMessage.Content = formData;
            return Task.CompletedTask;
        }
    }

    public class AudioTranscriptionsResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class AudioTranslationsRequest : IApiParameter
    {
        public Stream File { get; set; } = null!;

        public string FileName { get; set; } = null!;

        public string Model { get; set; } = null!;

        public string? Prompt { get; set; }

        public string? ResponseFormat { get; set; }

        public float? Temperature { get; set; }

        public Task OnRequestAsync(ApiParameterContext context)
        {
            var formData = new MultipartFormDataContent
            {
                { new StreamContent(File), "file", FileName },
                { new StringContent(Model), "model" },
            };
            if (Prompt != null)
                formData.Add(new StringContent(Prompt), "prompt");
            if (ResponseFormat != null)
                formData.Add(new StringContent(ResponseFormat), "response_format");
            if (Temperature != null)
                formData.Add(new StringContent(Temperature.Value.ToString()), "temperature");
            context.HttpContext.RequestMessage.Content = formData;
            return Task.CompletedTask;
        }
    }

    public class AudioTranslationsResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
