using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace TrustInnova.Provider.OpenAI.API
{
    [APILoggingFilter]
    public interface IOpenAIImageAPI
    {
        [Timeout(60 * 10 * 1000)]
        [HttpPost("v1/images/generations")]
        [APIAuthFilter]
        public Task<ImageGenerationsResponse> ImageGenerationsAsync([NotNullJsonContent] ImageGenerationsRequest request, CancellationToken token = default);

        [Timeout(60 * 10 * 1000)]
        [HttpPost("v1/images/edits")]
        [APIAuthFilter]
        public Task<ImageGenerationsResponse> ImageEditsAsync([FormDataContent] ImageEditsRequest request, CancellationToken token = default);

        [Timeout(60 * 10 * 1000)]
        [HttpPost("v1/images/variations")]
        [APIAuthFilter]
        public Task<ImageGenerationsResponse> ImageVariationsAsync([FormDataContent] ImageVariationsRequest request, CancellationToken token = default);
    }

    public class ImageGenerationsRequest
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = null!;

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("n")]
        public uint? N { get; set; }

        [JsonPropertyName("quality")]
        public string? Quality { get; set; }

        [JsonPropertyName("response_format")]
        public string? ResponseFormat { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("style")]
        public string? Style { get; set; }

        [JsonPropertyName("user")]
        public string? User { get; set; }
    }

    public class ImageEditsRequest : IApiParameter
    {
        public Stream Image { get; set; } = null!;

        public string Prompt { get; set; } = null!;

        public Stream? Mask { get; set; }

        public string? Model { get; set; }

        public uint? N { get; set; }

        public string? Size { get; set; }

        public string? ResponseFormat { get; set; }

        public string? User { get; set; }

        public Task OnRequestAsync(ApiParameterContext context)
        {
            var formData = new MultipartFormDataContent
            {
                { new StreamContent(Image), "image", "image.png" },
                { new StringContent(Prompt), "prompt" },
            };
            if (Model != null)
                formData.Add(new StringContent(Model), "model");
            if (N != null)
                formData.Add(new StringContent(N.Value.ToString()), "n");
            if (Size != null)
                formData.Add(new StringContent(Size), "size");
            if (ResponseFormat != null)
                formData.Add(new StringContent(ResponseFormat), "response_format");
            if (Mask != null)
                formData.Add(new StreamContent(Mask), "mask", "mask.png");
            if (User != null)
                formData.Add(new StringContent(User), "user");
            context.HttpContext.RequestMessage.Content = formData;
            return Task.CompletedTask;
        }
    }

    public class ImageVariationsRequest : IApiParameter
    {
        public Stream Image { get; set; } = null!;

        public string? Model { get; set; }

        public uint? N { get; set; }

        public string? Size { get; set; }

        public string? ResponseFormat { get; set; }

        public string? User { get; set; }

        public Task OnRequestAsync(ApiParameterContext context)
        {
            var formData = new MultipartFormDataContent
            {
                { new StreamContent(Image), "image", "image.png" },
            };
            if (Model != null)
                formData.Add(new StringContent(Model), "model");
            if (N != null)
                formData.Add(new StringContent(N.Value.ToString()), "n");
            if (Size != null)
                formData.Add(new StringContent(Size), "size");
            if (ResponseFormat != null)
                formData.Add(new StringContent(ResponseFormat), "response_format");
            if (User != null)
                formData.Add(new StringContent(User), "user");
            context.HttpContext.RequestMessage.Content = formData;
            return Task.CompletedTask;
        }
    }

    public class ImageGenerationsResponse
    {
        [JsonPropertyName("created")]
        public long? Created { get; set; }

        [JsonPropertyName("data")]
        public List<ImageGenerationsImageResponse>? Data { get; set; }
    }

    public class ImageGenerationsImageResponse
    {
        [JsonPropertyName("b64_json")]
        public string? B64Json { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("revised_prompt")]
        public string? RevisedPrompt { get; set; }
    }
}
