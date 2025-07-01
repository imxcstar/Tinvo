using System.ComponentModel;
using Tinvo.Abstractions;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.DataStorage;

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
                this.AddFunction(typeof(UrlSkill), nameof(UrlSkill.DownloadTextAsync), clsArgs: [new HttpClient()]);
                this.AddFunction(typeof(UrlSkill), nameof(UrlSkill.CheckUrlAvailableAsync), clsArgs: [new HttpClient()]);
                this.AddFunction(typeof(UrlSkill), nameof(UrlSkill.GetUrlHeadersAsync), clsArgs: [new HttpClient()]);
            }
        }
    }
}
