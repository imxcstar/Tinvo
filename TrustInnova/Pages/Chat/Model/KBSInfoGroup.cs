using TrustInnova.Service.KBS;

namespace TrustInnova.Pages.Chat.Model;

public class KBSInfoGroup
{
    public string Name { get; set; } = null!;
    public KBSType KBSGroupType { get; set; }
    public List<KBSInfo> KBSList { get; set; } = new List<KBSInfo>();
    public bool IsReadOnly { get; set; }
}