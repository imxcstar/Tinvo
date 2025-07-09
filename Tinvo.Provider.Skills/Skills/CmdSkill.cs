using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Provider.Skills
{
    public class CmdSkill
    {
        private readonly ILogger _logger;
        private readonly string _workingDirectory;

        public CmdSkill(string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
            {
                throw new ArgumentException("无效的工作目录路径", nameof(workingDirectory));
            }

            _logger = Log.ForContext<CmdSkill>();
            _workingDirectory = workingDirectory;
        }

        [Description("执行命令行指令")]
        public async Task<IAIChatHandleMessage> RunCommandAsync(
            [Description("要执行的命令行指令"), Required] string command)
        {
            _logger.Debug("CmdSkill: RunCommandAsync {Command} in {Directory}", command, _workingDirectory);

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd.exe" : "/bin/bash",
                    Arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/C {command}" : $"-c \"{command}\"",
                    WorkingDirectory = _workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                string resultMessage = string.IsNullOrWhiteSpace(error)
                    ? output
                    : $"执行出错:\n{error}\n\n输出:\n{output}";

                return new AIProviderHandleTextMessageResponse
                {
                    Message = resultMessage
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "命令执行失败: {Command}", command);
                return new AIProviderHandleTextMessageResponse
                {
                    Message = $"执行失败: {ex.Message}"
                };
            }
        }
    }
}
