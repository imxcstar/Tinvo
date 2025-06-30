using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using System.Runtime.InteropServices;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.DataStorage;

namespace Tinvo.Application.AISkill
{
    public class AIUtilsSkill
    {
        private readonly ILogger _logger;
        private readonly IDataStorageService _dataStorageService;

        public AIUtilsSkill(IDataStorageService dataStorageService)
        {
            _logger = Log.ForContext<AIUtilsSkill>();
            _dataStorageService = dataStorageService;
        }

        [Description("写入文本内容到文件")]
        public IAIChatHandleMessage WriteFile([Description("文件名"), Required] string name, [Description("文本内容"), Required] string content)
        {
            _logger.Debug("AIUtilsSkill: WriteFile");
            if (!Directory.Exists("tmp"))
                Directory.CreateDirectory("tmp");
            var path = Path.Combine("tmp", name);
            var fullPath = Path.GetFullPath(path);
            File.WriteAllText(fullPath, content);
            return new AIProviderHandleTextMessageResponse()
            {
                Message = "写入成功"
            };
        }

        [Description("查询系统信息")]
        public IAIChatHandleMessage QuerySystemInformation()
        {
            string systemInfo = "Operating System: " + RuntimeInformation.OSDescription + Environment.NewLine;
            systemInfo += "OS Architecture: " + RuntimeInformation.OSArchitecture + Environment.NewLine;
            systemInfo += "Framework Description: " + RuntimeInformation.FrameworkDescription + Environment.NewLine;
            return new AIProviderHandleTextMessageResponse() { Message = systemInfo };
        }

        [Description("查询当前时间")]
        public IAIChatHandleMessage QueryNowDate()
        {
            return new AIProviderHandleTextMessageResponse() { Message = DateTime.Now.ToString() };
        }
    }
}
