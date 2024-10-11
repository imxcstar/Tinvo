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

namespace TrustInnova.Application.AISkill
{
    public class AIUtilsSkill
    {
        private readonly ILogger _logger;

        public AIUtilsSkill()
        {
            _logger = Log.ForContext<AIUtilsSkill>();
        }

        [Description("画图")]
        public object DrawImage([Description("图片关键字"), Required] string keyword)
        {
            _logger.Debug("AIUtilsSkill: Draw, {keyword}", keyword);
            return "";
        }

        [Description("查询系统信息")]
        public object QuerySystemInformation()
        {
            string systemInfo = "Operating System: " + RuntimeInformation.OSDescription + Environment.NewLine;
            systemInfo += "OS Architecture: " + RuntimeInformation.OSArchitecture + Environment.NewLine;
            systemInfo += "Framework Description: " + RuntimeInformation.FrameworkDescription + Environment.NewLine;
            return systemInfo;
        }
    }
}
