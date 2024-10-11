namespace TrustInnova.Service.KBS
{
    public interface IKBSService
    {
        public Task<KBSInfo> AddKBSAsync(KBSType type, string name);

        public Task DeleteKBSAsync(KBSInfo kbsInfo);

        public Task<KBSInfo> EditKBSAsync(KBSInfo kbsInfo);

        public Task<List<KBSInfo>> GetKBSListAsync(KBSType? kbsType = null);

        public Task<KBSContentInfo> AddKBSContentAsync(KBSInfo kbsInfo, KBSContentType sourceType, string source, string content);

        public Task DeleteKBSContentAsync(KBSContentInfo kbsContentInfo);

        public Task<KBSContentInfo> EditKBSContentAsync(KBSContentInfo kbsContentInfo);

        public Task<List<KBSContentInfo>> GetKBSContentListAsync(KBSInfo kbsInfo, DateTime? lastTime = null, int count = 30);
    }

    public enum KBSType
    {
        Unknown,
        User,
        System
    }

    public class KBSInfo
    {
        public string ID { get; }
        public string Name { get; set; }
        public KBSType Type { get; set; }
        public DateTime CreateTime { get; set; }
        public bool IsSelect { get; set; } = false;

        public KBSInfo(string id)
        {
            ID = id;
        }
    }

    public class KBSContentInfo
    {
        public string ID { get; }
        public string Content { get; set; }
        public KBSContentType Type { get; set; }
        public string Source { get; set; }
        public DateTime CreateTime { get; set; }

        public KBSContentInfo(string id)
        {
            ID = id;
        }
    }

    public enum KBSContentType
    {
        Unknown,
        Text,
        File
    }
}
