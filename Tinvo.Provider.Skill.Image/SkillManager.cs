using Tinvo.Abstractions;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.DataStorage;

namespace Tinvo.Provider.Skill.Image
{
    public class SkillManager : DefaultFunctionManager
    {
        public SkillManager(IDataStorageService dataStorageService)
        {
            this.AddFunction(typeof(ImageSkill), nameof(ImageSkill.ImageAddTextWatermark), clsArgs: [dataStorageService]);
            this.AddFunction(typeof(ImageSkill), nameof(ImageSkill.ImageAddImageWatermark), clsArgs: [dataStorageService]);
            this.AddFunction(typeof(ImageSkill), nameof(ImageSkill.ImageConvertToGrayscale), clsArgs: [dataStorageService]);
            this.AddFunction(typeof(ImageSkill), nameof(ImageSkill.ImageResize), clsArgs: [dataStorageService]);
        }
    }
}
