using Serilog;
using System.Diagnostics;

namespace TrustInnova.Utils.FFMpeg
{
    public class FFMpegService
    {
        private string _ffmpegPath;

        private readonly ILogger _logger;

        public FFMpegService(string? ffmpegPath = null)
        {
            _ffmpegPath = !string.IsNullOrWhiteSpace(ffmpegPath) ? ffmpegPath : DetectFFMpegPath();
            _logger = Log.ForContext<FFMpegService>();
        }

        public void SetPath(string path)
        {
            _ffmpegPath = path;
        }

        private static string DetectFFMpegPath()
        {
            string[] possiblePaths;
            string executableName;

            // Detect the operating system
            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                possiblePaths = new[] { "/usr/bin", "/usr/local/bin" };
                executableName = "ffmpeg";
            }
            else
            {
                possiblePaths = new[] { @"C:\Program Files\FFmpeg\bin" };
                executableName = "ffmpeg.exe";
            }

            // Find FFmpeg executable in the possible paths
            foreach (var path in possiblePaths)
            {
                var fullPath = Path.Combine(path, executableName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return executableName;
        }

        /// <summary>
        /// 转换为PCM
        /// </summary>
        /// <param name="audio"></param>
        /// <param name="inputType">pcm/wav/amr/m4a/aac</param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="tempDirectory"></param>
        /// <returns></returns>
        public async Task<(byte[] PcmData, string Output, string Error)> ToPCMAsync(byte[] audio, string inputType, int sampleRate, int channels, string? tempDirectory = null)
        {
            // Use the provided temp directory or the system temp directory
            tempDirectory ??= Path.GetTempPath();

            // Prepare unique input/output file names
            var inputFileName = Path.Combine(tempDirectory, $"input_{Path.GetRandomFileName()}.{inputType}");
            var outputFileName = Path.Combine(tempDirectory, $"output_{Path.GetRandomFileName()}.pcm");

            // Save input byte array to a temporary file
            await File.WriteAllBytesAsync(inputFileName, audio);

            // Start the FFmpeg process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-y -i \"{inputFileName}\" -ar {sampleRate} -ac {channels} -f s16le -vn \"{outputFileName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Read standard output and error streams asynchronously
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            // Wait for the process to exit
            await process.WaitForExitAsync();

            // Read the output file and delete temporary files
            var pcmData = await File.ReadAllBytesAsync(outputFileName);
            try
            {
                if (File.Exists(inputFileName))
                    File.Delete(inputFileName);
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);
            }
            catch (Exception ex)
            {
                _logger.Error("ToPCMAsync Delete File Error: {ex}", ex);
            }

            // Get output and error content
            string outputContent = await outputTask;
            string errorContent = await errorTask;

            // Return the converted PCM data, output and error content
            return (pcmData, outputContent, errorContent);
        }
    }
}
