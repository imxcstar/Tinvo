using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Shared.SkillProviderSelect
{
    public class SkillProviderSelectModel
    {
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public List<SkillProviderSelectItemModel> Items { get; set; } = [];
    }

    public class SkillProviderSelectItemModel
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public SkillProviderSelectItemType Type { get; set; }
    }

    public enum SkillProviderSelectItemType
    {
        Chat,
        ASR,
        TTS,
        KBS,
        ImageAnalysis,
        ImageGeneration
    }
}
