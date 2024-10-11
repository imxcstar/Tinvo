using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Abstractions.ImageAnalysis
{
    public interface IImageAnalysisTask
    {
        public IAsyncEnumerable<string> AnalysisAsync(ImageAnalysisOptions options, CancellationToken cancellationToken = default);
    }

    public class ImageAnalysisOptions
    {
        public string? Prompt { get; set; }

        public Stream Image { get; set; } = null!;
    }
}
