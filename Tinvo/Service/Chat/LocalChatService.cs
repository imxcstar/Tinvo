using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Serilog;
using System.Text.Json;
using Tinvo.Pages.Chat;
using Tinvo.Pages.Chat.Component.ChatMsgList;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Tinvo.Application.DataStorage;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Abstractions;
using Tinvo.Application.Provider;
using Tinvo.Application.AIAssistant;
using System.Text;
using static MudBlazor.CategoryTypes;
using System.Net.Mime;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.AIAssistant.Entities;

namespace Tinvo.Service.Chat
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
        private readonly ProviderRegisterer _providerRegisterer;
        private readonly AIAssistantService _aiAssistantService;
        private readonly ProviderService _providerService;

        public LocalChatService(IDataStorageService dataStorageService, ProviderRegisterer providerRegisterer,
            AIAssistantService aiAssistantService, ProviderService providerService)
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
            var msgList = msgGroup == null
                ? null
                : _msgCaches.FirstOrDefault(x => x.MsgGroup.Id == msgGroup.Id)?.MsgList;
            if (msgList == null)
                MsgList = [];
            else
                MsgList = msgList;
            await OnStateHasChange.InvokeAsync();
        }

        private async Task SendAnyMsgAsync(string msg, AiAppInfo? aiApp, List<IBrowserFile>? files = null,
            ChatMsgGroupItemInfo? msgGroup = null, List<string>? domainId = null, bool? kbsExactMode = null,
            CancellationToken cancellationToken = default)
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

                var newMsg = new ChatMsgItemInfo()
                {
                    Id = Guid.NewGuid().ToString(),
                    AiApp = aiApp,
                    UserType = ChatUserType.Sender,
                    CreateTime = DateTime.Now
                };

                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var ext = Path.GetExtension(file.Name.ToLower()).Trim('.');
                        var fileType = ext switch
                        {
                            string image when "jpg/jpeg/png/bmp/gif".Contains(image) => ChatContentType.Image,
                            //string audio when "pcm/wav/amr/m4a/aac".Contains(audio) => ChatContentType.Audio,
                            //string doc when "doc/docx/pdf/txt".Contains(doc) => ChatContentType.File,
                            _ => throw new Exception($"不支持的文件类型({ext})")
                        };
                        switch (fileType)
                        {
                            case ChatContentType.Image:
                                {
                                    using var imageStream = new MemoryStream();
                                    using var fileStream = file.OpenReadStream(30 * 1024 * 1024);
                                    await fileStream.CopyToAsync(imageStream);

                                    newMsg.Contents.Add(new ChatMsgItemContentInfo()
                                    {
                                        ContentType = ChatContentType.Image,
                                        Content = Convert.ToBase64String(imageStream.ToArray())
                                    });
                                    break;
                                }
                        }
                    }
                }

                newMsg.Contents.Add(new ChatMsgItemContentInfo()
                {
                    ContentType = ChatContentType.Text,
                    Content = msg
                });

                var newRetMsg = new ChatMsgItemInfo()
                {
                    Id = Guid.NewGuid().ToString(),
                    AiApp = aiApp,
                    UserType = ChatUserType.Receiver,
                    CreateTime = DateTime.Now
                };
                var defaultRetContent = new ChatMsgItemContentInfo();
                newRetMsg.Contents.Add(defaultRetContent);

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
                var mcpServices = aiApp.GetMCPServices(_providerService);
                var msgChat = ai.CreateNewChat(aiApp.Assistant.Prompt);
                var defaultMsgHistory = aiApp.Assistant.HistoryMsg.Where(x => !string.IsNullOrWhiteSpace(x.Name));
                foreach (var tmsg in defaultMsgHistory)
                {
                    msgChat.AddMessage(tmsg.Name.ToLower() switch
                    {
                        "user" => AuthorRole.User,
                        "system" => AuthorRole.System,
                        "assistant" => AuthorRole.Assistant,
                        _ => AuthorRole.User
                    }, [new(Guid.NewGuid().ToString(), tmsg.Content, ChatMessageContentType.Text)]);
                }

                var msgHistory = msgCache.MsgList[..^1];
                foreach (var item in msgHistory)
                {
                    foreach (var content in item.Contents)
                    {
                        var contentType = content.ContentType switch
                        {
                            ChatContentType.Text => ChatMessageContentType.Text,
                            ChatContentType.Image => ChatMessageContentType.ImageBase64,
                            _ => throw new NotSupportedException("不支持的内容类型")
                        };
                        switch (item.UserType)
                        {
                            case ChatUserType.Sender:
                                msgChat.AddMessage(AuthorRole.User, [new(item.Id, content.Content, contentType)]);
                                break;
                            case ChatUserType.Receiver:
                                msgChat.AddMessage(AuthorRole.Assistant, [new(item.Id, content.Content, contentType)]);
                                break;
                            default:
                                break;
                        }
                    }
                }

                var functionManagers = new List<IFunctionManager>();
                foreach (var mcpService in mcpServices)
                {
                    var functionManager = await mcpService.GetIFunctionManager(cancellationToken);
                    functionManagers.Add(functionManager);
                }

                var chatRet = ai.ChatAsync(msgChat, new ChatSettings()
                {
                    FunctionManagers = functionManagers ?? [],
                    SessionId = tmsgGroup.Id
                }, cancellationToken);

                await HandleOutMessage(ai, msgChat, chatRet, newRetMsg.Contents, defaultRetContent, cancellationToken);
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

        private ChatMsgItemContentInfo GetOrNewContent(ChatMsgItemContentInfo? currentContent,
            ChatContentType newContentType,
            List<ChatMsgItemContentInfo> outContents,
            string? newTitle = null)
        {
            var ret = currentContent;
            if (ret != null && (ret.ContentType == ChatContentType.Default ||
                                ret.ContentType == newContentType ||
                                (!string.IsNullOrWhiteSpace(newTitle) && ret.Title == newTitle)))
                return ret;
            ret = new ChatMsgItemContentInfo();
            outContents.Add(ret);
            return ret;
        }

        private async Task HandleOutMessage(
            IAIChatTask aiChatTask,
            ChatHistory chatMessages,
            IAsyncEnumerable<IAIChatHandleResponse?> responses,
            List<ChatMsgItemContentInfo> OutContents,
            ChatMsgItemContentInfo? defaultContent,
            CancellationToken cancellationToken)
        {
            ChatMsgItemContentInfo? content = defaultContent;
            ChatMsgItemContentInfo? reasoningcontent = null;
            var isReasoning = false;
            await foreach (var response in responses.WithCancellation(cancellationToken))
            {
                if (cancellationToken != CancellationToken.None && cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();
                if (response == null)
                    continue;
                switch (response.Type)
                {
                    case AIChatHandleResponseType.TextMessage:
                        content = GetOrNewContent(content, ChatContentType.Text, OutContents);
                        var messageResponse = response as AIProviderHandleTextMessageResponse;
                        if (messageResponse == null)
                        {
                            content.ContentType = ChatContentType.ErrorInfo;
                            content.Title = "错误";
                            content.Content = $"AI执行返回解释错误：{response}";
                            await OnStateHasChange.InvokeAsync();
                            break;
                        }

                        if (isReasoning && reasoningcontent != null)
                        {
                            reasoningcontent.Content += messageResponse.Message;
                        }
                        else
                        {
                            if (content == reasoningcontent)
                                content = GetOrNewContent(null, ChatContentType.Text, OutContents);
                            content.ContentType = ChatContentType.Text;
                            content.Content += messageResponse.Message;
                        }

                        await OnStateHasChange.InvokeAsync();
                        break;
                    case AIChatHandleResponseType.ReasoningStart:
                        if (reasoningcontent == null)
                            reasoningcontent = GetOrNewContent(content, ChatContentType.Text, OutContents, "思考");
                        else
                            reasoningcontent = GetOrNewContent(reasoningcontent, ChatContentType.Text, OutContents, "思考");
                        reasoningcontent.ContentType = ChatContentType.Text;
                        reasoningcontent.Title = "思考";
                        isReasoning = true;

                        await OnStateHasChange.InvokeAsync();
                        break;
                    case AIChatHandleResponseType.ReasoningEnd:
                        isReasoning = false;

                        break;
                    case AIChatHandleResponseType.ImageMessage:
                        content = GetOrNewContent(content, ChatContentType.Image, OutContents);
                        var imageMessageResponse = response as AIProviderHandleImageMessageResponse;
                        if (imageMessageResponse == null)
                        {
                            content.ContentType = ChatContentType.ErrorInfo;
                            content.Title = "错误";
                            content.Content = $"AI执行返回解释错误：{response}";
                            await OnStateHasChange.InvokeAsync();
                            break;
                        }

                        content.ContentType = ChatContentType.Image;
                        content.Content =
                            Convert.ToBase64String((imageMessageResponse.Image as MemoryStream)!.ToArray());

                        await OnStateHasChange.InvokeAsync();
                        break;
                    case AIChatHandleResponseType.AudioMessage:
                        content = GetOrNewContent(content, ChatContentType.Image, OutContents);
                        var audioMessageResponse = response as AIProviderHandleAudioMessageResponse;
                        if (audioMessageResponse == null)
                        {
                            content.ContentType = ChatContentType.ErrorInfo;
                            content.Title = "错误";
                            content.Content = $"AI执行返回解释错误：{response}";
                            await OnStateHasChange.InvokeAsync();
                            break;
                        }

                        content.ContentType = ChatContentType.Audio;
                        content.Content =
                            Convert.ToBase64String((audioMessageResponse.Audio as MemoryStream)!.ToArray());

                        await OnStateHasChange.InvokeAsync();
                        break;
                    case AIChatHandleResponseType.FileMessage:
                        content = GetOrNewContent(content, ChatContentType.Image, OutContents);
                        var fileMessageResponse = response as AIProviderHandleFileMessageResponse;
                        if (fileMessageResponse == null)
                        {
                            content.ContentType = ChatContentType.ErrorInfo;
                            content.Title = "错误";
                            content.Content = $"AI执行返回解释错误：{response}";
                            await OnStateHasChange.InvokeAsync();
                            break;
                        }

                        content.ContentType = ChatContentType.File;
                        content.Content =
                            Convert.ToBase64String((fileMessageResponse.File as MemoryStream)!.ToArray());

                        await OnStateHasChange.InvokeAsync();
                        break;
                    case AIChatHandleResponseType.FunctionStart:
                        content = GetOrNewContent(content, ChatContentType.Default, OutContents);

                        var funStartResponse = response as AIProviderHandleFunctionStartResponse;
                        if (funStartResponse == null)
                        {
                            content.ContentType = ChatContentType.ErrorInfo;
                            content.Title = "错误";
                            content.Content = $"AI执行返回解释错误：{response}";
                            await OnStateHasChange.InvokeAsync();
                            break;
                        }

                        content.Title = $"工具调用({funStartResponse.FunctionName})";
                        await OnStateHasChange.InvokeAsync();
                        break;
                    case AIChatHandleResponseType.FunctionCall:
                        content = GetOrNewContent(content, ChatContentType.Default, OutContents);

                        var funHandleResponse = response as AIProviderHandleFunctionCallResponse;
                        if (funHandleResponse == null)
                        {
                            content.ContentType = ChatContentType.ErrorInfo;
                            content.Title = "错误";
                            content.Content = $"AI执行返回解释错误：{response}";
                            await OnStateHasChange.InvokeAsync();
                            break;
                        }

                        content.Title = $"工具调用({funHandleResponse.FunctionName})";
                        content.ContentType = ChatContentType.Text;
                        content.Content += $"{JsonSerializer.Serialize(funHandleResponse.Arguments)}";
                        var oldContent = content;
                        await OnStateHasChange.InvokeAsync();

                        content = GetOrNewContent(null, ChatContentType.Default, OutContents);
                        await OnStateHasChange.InvokeAsync();

                        var funCallRet = funHandleResponse.FunctionManager.CallFunctionAsync(
                            funHandleResponse.FunctionName,
                            funHandleResponse.Arguments?.ToDictionary(x => x.Key, x => (object?)x.Value)
                        );

                        await foreach (var item in funCallRet)
                        {
                            if (item is AIProviderHandleTextMessageResponse textMessageResponse)
                            {
                                oldContent.Content += $"\n\n{textMessageResponse.Message}";
                                await OnStateHasChange.InvokeAsync();

                                var cloneChatMessages = chatMessages.ShallowClone();
                                cloneChatMessages.Add(
                                    new ChatMessage(
                                        AuthorRole.Assistant,
                                        [
                                            new(Guid.NewGuid().ToString(), textMessageResponse.Message, ChatMessageContentType.Text)
                                        ]
                                    )
                                );
                                content.ContentType = ChatContentType.Text;
                                content.Content = await aiChatTask.ChatReturnTextAsync(cloneChatMessages, new ChatSettings(), cancellationToken);
                                await OnStateHasChange.InvokeAsync();
                            }
                        }

                        break;
                    default:
                        break;
                }

                await Task.Delay(1, cancellationToken);
            }
        }

        public async Task SendMsgAsync(string? msg, List<IBrowserFile>? files = null, AiAppInfo? aiApp = null,
            ChatMsgGroupItemInfo? msgGroup = null, List<string>? domainId = null, bool? kbsExactMode = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(msg))
                throw new Exception("请输入内容");
            await SendAnyMsgAsync(msg, aiApp, files, msgGroup, domainId, kbsExactMode, cancellationToken);
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