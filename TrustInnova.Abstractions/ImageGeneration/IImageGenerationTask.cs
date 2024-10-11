using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Abstractions.ImageGeneration
{
    public interface IImageGenerationTask
    {
        public IAsyncEnumerable<Stream> GenerationAsync(ImageGenerationOptions options, CancellationToken cancellationToken = default);
        public IAsyncEnumerable<Stream> ReferenceGenerationAsync(ImageReferenceGenerationOptions options, CancellationToken cancellationToken = default);
        public IAsyncEnumerable<Stream> EditGenerationAsync(ImageEditGenerationOptions options, CancellationToken cancellationToken = default);
    }

    public class ImageGenerationOptions
    {
        public string Prompt { get; set; } = null!;

        public uint Number { get; set; }

        public string Size { get; set; } = null!;

        public ImageGenerationQualityLevel QualityLevel { get; set; }
    }

    public class ImageReferenceGenerationOptions
    {
        public Stream Image { get; set; } = null!;

        public uint Number { get; set; }

        public string Size { get; set; } = null!;

        public ImageGenerationQualityLevel QualityLevel { get; set; }
    }

    public class ImageEditGenerationOptions
    {
        public Stream Image { get; set; } = null!;

        public Stream? MaskImage { get; set; }

        public string Prompt { get; set; } = null!;

        public uint Number { get; set; }

        public string Size { get; set; } = null!;

        public ImageGenerationQualityLevel QualityLevel { get; set; }
    }

    public enum ImageGenerationQualityLevel
    {
        Default,
        HD
    }
}
