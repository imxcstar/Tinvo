using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Serilog;
using System.Text.Json;
using TrustInnova.Pages.Chat;
using TrustInnova.Pages.Chat.Component.ChatMsgList;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TrustInnova.Application.DataStorage;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Abstractions;
using TrustInnova.Application.Provider;
using TrustInnova.Application.AIAssistant;
using System.Text;

namespace TrustInnova.Service.Chat
{
    public class MsgCacheInfo
    {
        public ChatMsgGroupItemInfo MsgGroup { get; set; }
        public List<ChatMsgItemInfo> MsgList { get; set; }
    }

    public class LocalChatService : IChatService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IDataStorageService _dataStorageService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ProviderRegisterer _providerRegisterer;
        private readonly AIAssistantService _aiAssistantService;
        private readonly ProviderService _providerService;

        public LocalChatService(IDataStorageService dataStorageService, ProviderRegisterer providerRegisterer, AIAssistantService aiAssistantService, ProviderService providerService)
        {
            _logger = Log.ForContext<LocalChatService>();
            _dataStorageService = dataStorageService;
            _providerRegisterer = providerRegisterer;
            _aiAssistantService = aiAssistantService;
            _providerService = providerService;
        }

        private List<MsgCacheInfo> _msgCaches;

        public List<ChatMsgGroupItemInfo> MsgGroupList { get; set; } = [];
        public List<ChatMsgItemInfo> MsgList { get; set; } = [];
        public List<AiAppInfo> AiAppList { get; set; } = [];
        public EventCallback OnStateHasChange { get; set; }


        public async Task LoadAiAppListAsync()
        {
            await _aiAssistantService.InitAsync();
            List<AiAppInfo> aiAppList = _aiAssistantService.GetAssistants().Select(x => new AiAppInfo()
            {
                Id = x.Id,
                Name = x.Name,
                Assistant = x,
                OrderIndex = x.Index
            }).ToList();
            AiAppList = [.. aiAppList];
        }

        public async Task LoadMoreMsgGroupListAsync()
        {
            var lMsg = MsgGroupList.LastOrDefault();
            if (lMsg == null)
                return;
            var nMsgs = _msgCaches.Where(x => x.MsgGroup.CreateTime < lMsg.CreateTime).Select(x => x.MsgGroup);
            MsgGroupList.AddRange(nMsgs);
            await OnStateHasChange.InvokeAsync();
        }

        public async Task LoadMsgGroupListAsync()
        {
            var ret = await _dataStorageService.GetItemAsync<List<MsgCacheInfo>>("msgCache");
            if (ret == null)
            {
                _msgCaches = [];
                MsgGroupList = [];
                return;
            }
            _msgCaches = ret;
            MsgGroupList = _msgCaches.Take(30).Select(x => x.MsgGroup).ToList();
            await OnStateHasChange.InvokeAsync();
        }

        public async Task LoadMsgListAsync(ChatMsgGroupItemInfo? msgGroup)
        {
            var msgList = msgGroup == null ? null : _msgCaches.FirstOrDefault(x => x.MsgGroup.Id == msgGroup.Id)?.MsgList;
            if (msgList == null)
                MsgList = [];
            else
                MsgList = msgList;
            await OnStateHasChange.InvokeAsync();
        }

        private async Task SendAnyMsgAsync(string msg, AiAppInfo? aiApp, ChatContentType chatContentType, IBrowserFile? file = null, ChatMsgGroupItemInfo? msgGroup = null, List<string>? domainId = null, bool? kbsExactMode = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ChatMsgGroupItemInfo tmsgGroup;
                if (string.IsNullOrWhiteSpace(msgGroup?.Id))
                    tmsgGroup = MsgGroupList.First();
                else
                    tmsgGroup = msgGroup;
                if (string.IsNullOrWhiteSpace(tmsgGroup.Id))
                {
                    tmsgGroup.Id = Guid.NewGuid().ToString();
                    tmsgGroup.Title = string.IsNullOrWhiteSpace(msg) ? "新的聊天" : string.Join("", msg.Take(16));
                }
                aiApp ??= AiAppList.First();
                var msgCache = _msgCaches.FirstOrDefault(x => x.MsgGroup.Id == tmsgGroup.Id);
                if (file != null)
                {
                    switch (chatContentType)
                    {
                        case ChatContentType.Image:
                            {
                                using var imageStream = new MemoryStream();
                                using var fileStream = file.OpenReadStream(30 * 1024 * 1024);
                                await fileStream.CopyToAsync(imageStream);
                                msg = Convert.ToBase64String(imageStream.ToArray());
                            }
                            break;
                        default:
                            msg = file.Name;
                            break;
                    }
                }
                var newMsg = new ChatMsgItemInfo()
                {
                    Id = Guid.NewGuid().ToString(),
                    AiApp = aiApp,
                    Content = msg,
                    ContentType = chatContentType,
                    UserType = ChatUserType.Sender,
                    CreateTime = DateTime.Now
                };
                var newRetMsg = new ChatMsgItemInfo()
                {
                    Id = Guid.NewGuid().ToString(),
                    AiApp = aiApp,
                    Content = "AI正在思考...",
                    ContentType = ChatContentType.Text,
                    UserType = ChatUserType.Receiver,
                    CreateTime = DateTime.Now
                };
                List<ChatMsgItemInfo> nMsgs = [newMsg, newRetMsg];
                if (msgCache == null)
                {
                    msgCache = new MsgCacheInfo()
                    {
                        MsgGroup = tmsgGroup,
                        MsgList = []
                    };
                    _msgCaches.Insert(0, msgCache);
                    MsgList = msgCache.MsgList;
                }
                MsgList.AddRange(nMsgs);
                await _dataStorageService.SetItemAsync("msgCache", _msgCaches, cancellationToken);
                await OnStateHasChange.InvokeAsync();
                var ai = aiApp.GetAIProvider(_providerService);
                var msgChat = ai.CreateNewChat(aiApp.Assistant.Prompt);
                var msgHistory = msgCache.MsgList[..^1];
                foreach (var item in msgHistory)
                {
                    var contentType = item.ContentType switch
                    {
                        ChatContentType.Text => ChatMessageContentType.Text,
                        ChatContentType.Image => ChatMessageContentType.ImageBase64,
                        _ => throw new NotSupportedException("不支持的内容类型")
                    };
                    switch (item.UserType)
                    {
                        case ChatUserType.Sender:
                            if (item.ContentType == ChatContentType.Image)
                            {
                                msgChat.AddMessage(AuthorRole.User, [new(Guid.NewGuid().ToString(), "我向你发送了一张图片，解释下这图片内容", ChatMessageContentType.Text), new(item.Id, item.Content, contentType)]);
                            }
                            else
                            {
                                msgChat.AddMessage(AuthorRole.User, [new(item.Id, item.Content, contentType)]);
                            }
                            break;
                        case ChatUserType.Receiver:
                            msgChat.AddMessage(AuthorRole.Assistant, [new(item.Id, item.Content, contentType)]);
                            break;
                        default:
                            break;
                    }
                }
                var isFirstMsg = true;
                var functionManager = new FunctionManager();
                //functionManager.AddFunction(typeof(AIUtilsSkill), nameof(AIUtilsSkill.DrawImage));
                //functionManager.AddFunction(typeof(AIUtilsSkill), nameof(AIUtilsSkill.QuerySystemInformation));
                var ret = ai.ChatAsync(msgChat, new ChatSettings()
                {
                    FunctionManager = functionManager,
                    SessionId = tmsgGroup.Id
                }, cancellationToken);
                await foreach (var item in ret)
                {
                    if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException();
                    if (item == null)
                        continue;
                    switch (item.Type)
                    {
                        case AIChatHandleResponseType.TextMessage:
                            var messageResponse = item as AIProviderHandleTextMessageResponse;
                            if (messageResponse == null)
                            {
                                _logger.Debug("AI执行返回解释错误：{ret}", item);
                                break;
                            }
                            if (isFirstMsg)
                            {
                                newRetMsg.Content = messageResponse.Message;
                                isFirstMsg = false;
                            }
                            else
                            {
                                newRetMsg.Content += messageResponse.Message;
                            }
                            await OnStateHasChange.InvokeAsync();
                            break;
                        case AIChatHandleResponseType.ImageMessage:
                            var imageMessageResponse = item as AIProviderHandleImageMessageResponse;
                            if (imageMessageResponse == null)
                            {
                                _logger.Debug("AI执行返回解释错误：{ret}", item);
                                break;
                            }
                            if (!isFirstMsg)
                            {
                                newRetMsg = new ChatMsgItemInfo()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    AiApp = aiApp,
                                    Content = "AI正在绘制下一张图片...",
                                    ContentType = ChatContentType.Text,
                                    UserType = ChatUserType.Receiver,
                                    CreateTime = DateTime.Now
                                };
                                MsgList.Add(newRetMsg);
                            }
                            newRetMsg.Content = Convert.ToBase64String((imageMessageResponse.Image as MemoryStream)!.ToArray());
                            newRetMsg.ContentType = ChatContentType.Image;
                            isFirstMsg = false;
                            await OnStateHasChange.InvokeAsync();
                            break;
                        case AIChatHandleResponseType.FunctionStart:
                            var funStartResponse = item as AIProviderHandleFunctionStartResponse;
                            if (funStartResponse == null)
                            {
                                _logger.Debug("AI执行返回解释错误：{ret}", item);
                                break;
                            }
                            _logger.Debug($"AI触发了：{funStartResponse.FunctionName}");
                            break;
                        case AIChatHandleResponseType.FunctionCall:
                            var funHandleResponse = item as AIProviderHandleFunctionCallResponse;
                            if (funHandleResponse == null)
                            {
                                _logger.Debug("AI执行返回解释错误：{ret}", item);
                                break;
                            }
                            _logger.Debug("AI开始执行：{name}", funHandleResponse.FunctionName);
                            _logger.Debug("AI开始执行参数：{args}", funHandleResponse.Arguments);
                            var functionMetaInfo = funHandleResponse.FunctionManager.GetFnctionMetaInfo(funHandleResponse.FunctionName);
                            var funCallRet = functionMetaInfo.Call(funHandleResponse.Arguments?.Select(x =>
                            {
                                if (!functionMetaInfo.FunctionInfo.Parameters.Properties.TryGetValue(x.Key, out var argInfo))
                                    return null;
                                return JsonSerializer.Deserialize(x.Value.GetRawText(), argInfo.RawType!);
                            }).Where(x => x != null).ToArray());
                            _logger.Debug("AI执行返回：{ret}", ret);
                            if (funCallRet != null && funCallRet is string funCallRetStr)
                            {
                                if (isFirstMsg)
                                {
                                    newRetMsg.Content = funCallRetStr;
                                    isFirstMsg = false;
                                }
                                else
                                {
                                    newRetMsg.Content += funCallRetStr;
                                }
                                await OnStateHasChange.InvokeAsync();
                            }
                            break;
                        default:
                            break;
                    }
                    await Task.Delay(1, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                    throw;
            }
            await _dataStorageService.SetItemAsync("msgCache", _msgCaches);
        }

        public async Task SendMsgAsync(string msg, AiAppInfo? aiApp = null, ChatMsgGroupItemInfo? msgGroup = null, List<string>? domainId = null, bool? kbsExactMode = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(msg))
                throw new Exception("请输入内容");
            await SendAnyMsgAsync(msg, aiApp, ChatContentType.Text, null, msgGroup, domainId, kbsExactMode, cancellationToken);
        }

        public async Task SendMsgAsync(IBrowserFile file, AiAppInfo? aiApp = null, ChatMsgGroupItemInfo? msgGroup = null, List<string>? domainId = null, bool? kbsExactMode = null, CancellationToken cancellationToken = default)
        {
            var ext = Path.GetExtension(file.Name.ToLower()).Trim('.');
            var fileType = ext switch
            {
                string image when "jpg/jpeg/png/bmp/gif".Contains(image) => ChatContentType.Image,
                //string audio when "pcm/wav/amr/m4a/aac".Contains(audio) => ChatContentType.Audio,
                //string doc when "doc/docx/pdf/txt".Contains(doc) => ChatContentType.File,
                _ => throw new Exception($"不支持的文件类型({ext})")
            };
            await SendAnyMsgAsync("", aiApp, fileType, file, msgGroup, domainId, kbsExactMode, cancellationToken);
        }

        public Task<bool> UpdateMsgGroup(ChatMsgGroupItemInfo msgGroup)
        {
            var tmsgGroup = _msgCaches.FirstOrDefault(x => x.MsgGroup.Id == msgGroup.Id)?.MsgGroup;
            if (tmsgGroup == null)
                return Task.FromResult(false);
            tmsgGroup.Title = msgGroup.Title;
            return Task.FromResult(true);
        }
    }
}
