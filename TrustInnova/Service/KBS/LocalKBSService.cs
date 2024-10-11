
namespace TrustInnova.Service.KBS
{
    public class LocalKBSService : IKBSService
    {
        public Task<KBSInfo> AddKBSAsync(KBSType type, string name)
        {
            throw new Exception("暂不支持知识库");
        }

        public Task<KBSContentInfo> AddKBSContentAsync(KBSInfo kbsInfo, KBSContentType sourceType, string source, string content)
        {
            throw new Exception("暂不支持知识库");
        }

        public Task DeleteKBSAsync(KBSInfo kbsInfo)
        {
            throw new Exception("暂不支持知识库");
        }

        public Task DeleteKBSContentAsync(KBSContentInfo kbsContentInfo)
        {
            throw new Exception("暂不支持知识库");
        }

        public Task<KBSInfo> EditKBSAsync(KBSInfo kbsInfo)
        {
            throw new Exception("暂不支持知识库");
        }

        public Task<KBSContentInfo> EditKBSContentAsync(KBSContentInfo kbsContentInfo)
        {
            throw new Exception("暂不支持知识库");
        }

        public Task<List<KBSContentInfo>> GetKBSContentListAsync(KBSInfo kbsInfo, DateTime? lastTime = null, int count = 30)
        {
            throw new Exception("暂不支持知识库");
        }

        public Task<List<KBSInfo>> GetKBSListAsync(KBSType? kbsType = null)
        {
            throw new Exception("暂不支持知识库");
        }
    }
}
