using Tinvo.Service.KBS;

namespace Tinvo.Pages.Chat.Model;

public class KBSInfoGroup
{
    public string Name { get; set; } = null!;
    public KBSType KBSGroupType { get; set; }
    public List<KBSInfo> KBSList { get; set; } = new List<KBSInfo>();
    public bool IsReadOnly { get; set; }
}