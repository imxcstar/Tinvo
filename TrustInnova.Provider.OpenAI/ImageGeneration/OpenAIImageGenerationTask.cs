using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions.ImageGeneration;
using TrustInnova.Provider.OpenAI.API;
using TrustInnova.Abstractions;
using System.Net;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TrustInnova.Provider.OpenAI.ImageGeneration
{
    [TypeMetadataDisplayName("图片生成配置")]
    public class OpenAIImageGenerationConfig
    {
        [Description("地址")]
        [DefaultValue("https://api.openai.com")]
        public string BaseURL { get; set; } = "https://api.openai.com";

        [Description("代理(可选)")]
        [TypeMetadataAllowNull]
        public string? Proxy { get; set; }

        [Description("密钥")]
        [TypeMetadataAllowNull]
        public string? Token { get; set; }

        [Description("编辑图片模型")]
        public string EditGenerationModel { get; set; } = null!;

        [Description("生成图片模型")]
        public string GenerationModel { get; set; } = null!;

        [Description("以图改图模型")]
        public string ReferenceGenerationModel { get; set; } = null!;
    }

    [ProviderTask("OpenAIImageGeneration", "OpenAI图片生成")]
    public class OpenAIImageGenerationTask : IImageGenerationTask
    {
        private readonly IOpenAIImageAPI _imageAPI;
        private readonly OpenAIImageGenerationConfig _config;

        private readonly ILogger _logger;

        public OpenAIImageGenerationTask(OpenAIImageGenerationConfig config)
        {
            _config = config;
            _imageAPI = APIUtil.GetAPI<IOpenAIImageAPI>(_config.BaseURL, _config.Token, _config.Proxy);
            _logger = Log.ForContext<OpenAIImageGenerationTask>();
        }

        private async Task<ImageGenerationsResponse> _EditGenerationAsync(ImageEditGenerationOptions options, CancellationToken cancellationToken = default)
        {
            var ret = await _imageAPI.ImageEditsAsync(new ImageEditsRequest()
            {
                Model = _config.EditGenerationModel,
                N = options.Number,
                Prompt = options.Prompt,
                Image = options.Image,
                Mask = options.MaskImage,
                ResponseFormat = "b64_json",
                Size = options.Size,
            }, cancellationToken);
            if (ret == null || ret.Data == null || ret.Data.Count == 0 || ret.Data.Any(x => string.IsNullOrWhiteSpace(x.B64Json)))
            {
                _logger.Error("OpenAI Image Edit Generation Error: {value}", ret);
                throw new Exception("OpenAI Image Edit Generation Error");
            }
            return ret;
        }

        public async IAsyncEnumerable<Stream> EditGenerationAsync(ImageEditGenerationOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_config.EditGenerationModel.Equals("dall-e-3", StringComparison.CurrentCultureIgnoreCase) && options.Number > 1)
            {
                for (var i = 0; i < options.Number; i++)
                {
                    var toptions = options.Adapt<ImageEditGenerationOptions>();
                    toptions.Number = 1;
                    var tret = await _EditGenerationAsync(toptions, cancellationToken);
                    foreach (var item in tret.Data!)
                    {
                        yield return new MemoryStream(Convert.FromBase64String(item.B64Json!));
                    }
                }
                yield break;
            }
            var ret = await _EditGenerationAsync(options, cancellationToken);
            foreach (var item in ret.Data!)
            {
                yield return new MemoryStream(Convert.FromBase64String(item.B64Json!));
            }
        }

        private async Task<ImageGenerationsResponse> _GenerationAsync(ImageGenerationOptions options, CancellationToken cancellationToken = default)
        {
            var ret = await _imageAPI.ImageGenerationsAsync(new ImageGenerationsRequest()
            {
                Model = _config.GenerationModel,
                N = options.Number,
                Prompt = options.Prompt,
                Quality = options.QualityLevel switch
                {
                    ImageGenerationQualityLevel.HD => "hd",
                    _ => null
                },
                ResponseFormat = "b64_json",
                Size = options.Size,
            }, cancellationToken);
            if (ret == null || ret.Data == null || ret.Data.Count == 0 || ret.Data.Any(x => string.IsNullOrWhiteSpace(x.B64Json)))
            {
                _logger.Error("OpenAI Image Generation Error: {value}", ret);
                throw new Exception("OpenAI Image Generation Error");
            }
            return ret;
        }

        public async IAsyncEnumerable<Stream> GenerationAsync(ImageGenerationOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_config.GenerationModel.Equals("dall-e-3", StringComparison.CurrentCultureIgnoreCase) && options.Number > 1)
            {
                for (var i = 0; i < options.Number; i++)
                {
                    var toptions = options.Adapt<ImageGenerationOptions>();
                    toptions.Number = 1;
                    var tret = await _GenerationAsync(toptions, cancellationToken);
                    foreach (var item in tret.Data!)
                    {
                        yield return new MemoryStream(Convert.FromBase64String(item.B64Json!));
                    }
                }
                yield break;
            }
            var ret = await _GenerationAsync(options, cancellationToken);
            foreach (var item in ret.Data!)
            {
                yield return new MemoryStream(Convert.FromBase64String(item.B64Json!));
            }
        }

        private async Task<ImageGenerationsResponse> _ReferenceGenerationAsync(ImageReferenceGenerationOptions options, CancellationToken cancellationToken = default)
        {
            var ret = await _imageAPI.ImageVariationsAsync(new ImageVariationsRequest()
            {
                Model = _config.ReferenceGenerationModel,
                N = options.Number,
                Image = options.Image,
                ResponseFormat = "b64_json",
                Size = options.Size,
            }, cancellationToken);
            if (ret == null || ret.Data == null || ret.Data.Count == 0 || ret.Data.Any(x => string.IsNullOrWhiteSpace(x.B64Json)))
            {
                _logger.Error("OpenAI Image Reference Generation Error: {value}", ret);
                throw new Exception("OpenAI Image Reference Generation Error");
            }
            return ret;
        }

        public async IAsyncEnumerable<Stream> ReferenceGenerationAsync(ImageReferenceGenerationOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_config.ReferenceGenerationModel.Equals("dall-e-3", StringComparison.CurrentCultureIgnoreCase) && options.Number > 1)
            {
                for (var i = 0; i < options.Number; i++)
                {
                    var toptions = options.Adapt<ImageReferenceGenerationOptions>();
                    toptions.Number = 1;
                    var tret = await _ReferenceGenerationAsync(toptions, cancellationToken);
                    foreach (var item in tret.Data!)
                    {
                        yield return new MemoryStream(Convert.FromBase64String(item.B64Json!));
                    }
                }
                yield break;
            }
            var ret = await _ReferenceGenerationAsync(options, cancellationToken);
            foreach (var item in ret.Data!)
            {
                yield return new MemoryStream(Convert.FromBase64String(item.B64Json!));
            }
        }
    }
}
