using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Application.DB;

namespace TrustInnova.Application.AIAssistant.Entities
{
    [Table("assistant")]
    public class AssistantEntity : IDBEntity
    {
        public string Id { get; set; } = null!;

        public int Index { get; set; }

        public string Name { get; set; } = null!;

        public string HeadIcon { get; set; } = "";

        public string Prompt { get; set; } = "";

        public List<AssistantSkill> Skills { get; set; } = [];
    }

    public class AssistantSkill
    {
        public AssistantSkillType Type { get; set; }
        public AssistantSupportSkillType SupportType { get; set; }
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Content { get; set; }
    }

    public enum AssistantSupportSkillType
    {
        Chat,
        ASR,
        TTS,
        KBS,
        ImageAnalysis,
        ImageGeneration
    }

    public enum AssistantSkillType
    {
        Default,
        Prompt,
        Provider,
        API
    }
}
