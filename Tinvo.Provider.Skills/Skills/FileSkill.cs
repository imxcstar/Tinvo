using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Provider.Skills
{
    public class FileSkill
    {
        private readonly ILogger _logger;
        private readonly string _rootDirectory;

        public FileSkill(string rootDirectory)
        {
            _logger = Log.ForContext<FileSkill>();

            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException($"指定的根目录不存在: {rootDirectory}");
            }

            _rootDirectory = Path.GetFullPath(rootDirectory);
        }

        private string GetSafeFullPath(string relativePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, relativePath));

            if (!fullPath.StartsWith(_rootDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("访问被拒绝：路径超出允许目录");
            }

            return fullPath;
        }

        [Description("读取指定相对路径的文本文件内容")]
        public async Task<IAIChatHandleMessage> ReadTextFileAsync(
            [Description("相对路径（相对于根目录）"), Required] string relativePath)
        {
            _logger.Debug("FileSkill: ReadTextFileAsync {Path}", relativePath);
            try
            {
                var fullPath = GetSafeFullPath(relativePath);

                if (!File.Exists(fullPath))
                {
                    return new AIProviderHandleTextMessageResponse()
                    {
                        Message = "文件不存在"
                    };
                }

                var content = await File.ReadAllTextAsync(fullPath);
                return new AIProviderHandleTextMessageResponse()
                {
                    Message = content
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "读取文件失败: {Path}", relativePath);
                return new AIProviderHandleTextMessageResponse()
                {
                    Message = $"读取失败: {ex.Message}"
                };
            }
        }

        [Description("检查文件是否存在")]
        public Task<IAIChatHandleMessage> CheckFileExistsAsync(
            [Description("相对路径（相对于根目录）"), Required] string relativePath)
        {
            _logger.Debug("FileSkill: CheckFileExistsAsync {Path}", relativePath);
            try
            {
                var fullPath = GetSafeFullPath(relativePath);
                bool exists = File.Exists(fullPath);

                return Task.FromResult<IAIChatHandleMessage>(
                    new AIProviderHandleTextMessageResponse()
                    {
                        Message = exists ? "文件存在" : "文件不存在"
                    });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查文件存在性失败: {Path}", relativePath);
                return Task.FromResult<IAIChatHandleMessage>(
                    new AIProviderHandleTextMessageResponse()
                    {
                        Message = $"检查失败: {ex.Message}"
                    });
            }
        }

        [Description("获取文件信息")]
        public Task<IAIChatHandleMessage> GetFileInfoAsync(
            [Description("相对路径（相对于根目录）"), Required] string relativePath)
        {
            _logger.Debug("FileSkill: GetFileInfoAsync {Path}", relativePath);
            try
            {
                var fullPath = GetSafeFullPath(relativePath);

                if (!File.Exists(fullPath))
                {
                    return Task.FromResult<IAIChatHandleMessage>(
                        new AIProviderHandleTextMessageResponse()
                        {
                            Message = "文件不存在"
                        });
                }

                var fileInfo = new FileInfo(fullPath);
                var message = $"文件: {fileInfo.Name}\n" +
                              $"大小: {fileInfo.Length} 字节\n" +
                              $"创建时间: {fileInfo.CreationTime}\n" +
                              $"最后修改: {fileInfo.LastWriteTime}";

                return Task.FromResult<IAIChatHandleMessage>(
                    new AIProviderHandleTextMessageResponse()
                    {
                        Message = message
                    });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取文件信息失败: {Path}", relativePath);
                return Task.FromResult<IAIChatHandleMessage>(
                    new AIProviderHandleTextMessageResponse()
                    {
                        Message = $"获取失败: {ex.Message}"
                    });
            }
        }

        [Description("写入文本到文件（覆盖）")]
        public async Task<IAIChatHandleMessage> WriteTextFileAsync(
            [Description("相对路径（相对于根目录）"), Required] string relativePath,
            [Description("要写入的文本内容"), Required] string content)
        {
            _logger.Debug("FileSkill: WriteTextFileAsync {Path}", relativePath);
            try
            {
                var fullPath = GetSafeFullPath(relativePath);

                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(fullPath, content);

                return new AIProviderHandleTextMessageResponse()
                {
                    Message = "写入成功"
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "写入文件失败: {Path}", relativePath);
                return new AIProviderHandleTextMessageResponse()
                {
                    Message = $"写入失败: {ex.Message}"
                };
            }
        }

        [Description("追加文本到文件")]
        public async Task<IAIChatHandleMessage> AppendTextFileAsync(
            [Description("相对路径（相对于根目录）"), Required] string relativePath,
            [Description("要追加的文本内容"), Required] string content)
        {
            _logger.Debug("FileSkill: AppendTextFileAsync {Path}", relativePath);
            try
            {
                var fullPath = GetSafeFullPath(relativePath);

                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.AppendAllTextAsync(fullPath, content);

                return new AIProviderHandleTextMessageResponse()
                {
                    Message = "追加成功"
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "追加文件失败: {Path}", relativePath);
                return new AIProviderHandleTextMessageResponse()
                {
                    Message = $"追加失败: {ex.Message}"
                };
            }
        }

        [Description("列出目录下的文件和/或子目录")]
        public Task<IAIChatHandleMessage> ListDirectoryContentAsync(
            [Description("相对路径（相对于根目录）"), Required] string relativeDirectory,
            [Description("类型：0=文件，1=目录，2=全部"), Required] int type)
        {
            _logger.Debug("FileSkill: ListDirectoryContentAsync {Directory}, Type: {Type}", relativeDirectory, type);
            try
            {
                var fullPath = GetSafeFullPath(relativeDirectory);

                if (!Directory.Exists(fullPath))
                {
                    return Task.FromResult<IAIChatHandleMessage>(
                        new AIProviderHandleTextMessageResponse()
                        {
                            Message = "目录不存在"
                        });
                }

                var sb = new StringBuilder();
                ListDirectoryRecursive(fullPath, type, sb, 0);
                var message = sb.ToString();

                return Task.FromResult<IAIChatHandleMessage>(
                    new AIProviderHandleTextMessageResponse()
                    {
                        Message = message
                    });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "列出目录内容失败: {Directory}", relativeDirectory);
                return Task.FromResult<IAIChatHandleMessage>(
                    new AIProviderHandleTextMessageResponse()
                    {
                        Message = $"列出失败: {ex.Message}"
                    });
            }
        }

        private void ListDirectoryRecursive(string directory, int type, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 2);

            // 目录本身名字
            if (indent > 0)
                sb.AppendLine($"{indentStr}[{Path.GetFileName(directory)}]");

            // 文件
            if (type == 0 || type == 2)
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files)
                {
                    sb.AppendLine($"{indentStr}{(indent > 0 ? "  " : "")}{Path.GetFileName(file)}");
                }
            }

            // 子目录
            if (type == 1 || type == 2)
            {
                var dirs = Directory.GetDirectories(directory);
                foreach (var dir in dirs)
                {
                    sb.AppendLine($"{indentStr}[{Path.GetFileName(dir)}]");
                    ListDirectoryRecursive(dir, type, sb, indent + 1);
                }
            }
        }
    }
}
