using System.ComponentModel;
using Tinvo.Abstractions;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.DataStorage;
using Tinvo.Provider.Skills.Skills;

namespace Tinvo.Provider.Skills
{
    [TypeMetadataDisplayName("技能配置")]
    public class SkillConfig
    {
        [Description("启用图片处理技能")]
        [DefaultValue(false)]
        public bool EnableImageSkill { get; set; } = false;

        [Description("启用URL访问技能")]
        [DefaultValue(false)]
        public bool EnableURLSkill { get; set; } = false;

        [Description("启用CMD执行技能（危险）")]
        [DefaultValue(false)]
        public bool EnableCMDSkill { get; set; } = false;

        [Description("启用文件操作技能（危险）")]
        [DefaultValue(false)]
        public bool EnableFileSkill { get; set; } = false;

        [Description("启用目标跟踪技能")]
        [DefaultValue(false)]
        public bool EnableGoalTrackingSkill { get; set; } = false;
    }

    public class SkillManager : DefaultFunctionManager
    {
        public SkillManager(IDataStorageService dataStorageService, SkillConfig config)
        {
            if (config.EnableImageSkill)
            {
                this.AddFunction(typeof(ImageSkill), nameof(ImageSkill.ImageAddTextWatermark), clsArgs: [dataStorageService]);
                this.AddFunction(typeof(ImageSkill), nameof(ImageSkill.ImageAddImageWatermark), clsArgs: [dataStorageService]);
                this.AddFunction(typeof(ImageSkill), nameof(ImageSkill.ImageConvertToGrayscale), clsArgs: [dataStorageService]);
                this.AddFunction(typeof(ImageSkill), nameof(ImageSkill.ImageResize), clsArgs: [dataStorageService]);
            }

            if (config.EnableURLSkill)
            {
                this.AddFunction(typeof(UrlSkill), nameof(UrlSkill.DownloadTextAsync), clsArgs: [new HttpClient(), dataStorageService]);
                this.AddFunction(typeof(UrlSkill), nameof(UrlSkill.DownloadImageAsync), clsArgs: [new HttpClient(), dataStorageService]);
                this.AddFunction(typeof(UrlSkill), nameof(UrlSkill.CheckUrlAvailableAsync), clsArgs: [new HttpClient(), dataStorageService]);
                this.AddFunction(typeof(UrlSkill), nameof(UrlSkill.GetUrlHeadersAsync), clsArgs: [new HttpClient(), dataStorageService]);
            }

            if (config.EnableCMDSkill)
            {
                if (!Directory.Exists("tmp"))
                    Directory.CreateDirectory("tmp");
                var path = Path.GetFullPath("tmp");
                this.AddFunction(typeof(CmdSkill), nameof(CmdSkill.RunCommandAsync), clsArgs: [path]);
            }

            if (config.EnableCMDSkill)
            {
                if (!Directory.Exists("tmp"))
                    Directory.CreateDirectory("tmp");
                var path = Path.GetFullPath("tmp");
                this.AddFunction(typeof(FileSkill), nameof(FileSkill.CheckFileExistsAsync), clsArgs: [path]);
                this.AddFunction(typeof(FileSkill), nameof(FileSkill.ReadTextFileAsync), clsArgs: [path]);
                this.AddFunction(typeof(FileSkill), nameof(FileSkill.GetFileInfoAsync), clsArgs: [path]);
                this.AddFunction(typeof(FileSkill), nameof(FileSkill.WriteTextFileAsync), clsArgs: [path]);
                this.AddFunction(typeof(FileSkill), nameof(FileSkill.AppendTextFileAsync), clsArgs: [path]);
                this.AddFunction(typeof(FileSkill), nameof(FileSkill.ListDirectoryContentAsync), clsArgs: [path]);
            }

            if (config.EnableGoalTrackingSkill)
            {
                this.AddFunction(typeof(GoalTrackingSkill), nameof(GoalTrackingSkill.SetGoalAsync));
                this.AddFunction(typeof(GoalTrackingSkill), nameof(GoalTrackingSkill.AddGoalProgressAsync));
                this.AddFunction(typeof(GoalTrackingSkill), nameof(GoalTrackingSkill.GetGoalProgressAsync));
            }
        }
    }
}
