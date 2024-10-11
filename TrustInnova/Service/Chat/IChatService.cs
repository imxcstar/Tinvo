using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TrustInnova.Pages.Chat.Component.ChatMsgList;

namespace TrustInnova.Service.Chat
{
    public interface IChatService
    {
        public List<ChatMsgGroupItemInfo> MsgGroupList { get; set; }

        public List<ChatMsgItemInfo> MsgList { get; set; }

        public List<AiAppInfo> AiAppList { get; set; }

        public EventCallback OnStateHasChange { get; set; }

        public Task LoadAiAppListAsync();

        public Task LoadMsgGroupListAsync();

        public Task LoadMoreMsgGroupListAsync();

        public Task LoadMsgListAsync(ChatMsgGroupItemInfo? msgGroup);

        public Task SendMsgAsync(string msg, AiAppInfo? aiApp = null, ChatMsgGroupItemInfo? msgGroup = null, List<string>? domainId = null, bool? kbsExactMode = null, CancellationToken cancellationToken = default);

        public Task SendMsgAsync(IBrowserFile file, AiAppInfo? aiApp = null, ChatMsgGroupItemInfo? msgGroup = null, List<string>? domainId = null, bool? kbsExactMode = null, CancellationToken cancellationToken = default);

        public Task<bool> UpdateMsgGroup(ChatMsgGroupItemInfo msgGroup);
    }
}
