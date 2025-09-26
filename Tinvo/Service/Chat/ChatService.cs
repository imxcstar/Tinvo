using DeepCloner.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Serilog;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.AISkill;
using Tinvo.Application.DataStorage;
using Tinvo.Application.Provider;
using Tinvo.Pages.Chat;
using Tinvo.Pages.Chat.Component.ChatMsgList;
using static MudBlazor.CategoryTypes;
using static System.Net.Mime.MediaTypeNames;

namespace Tinvo.Service.Chat
{
    public class MsgCacheInfo
    {
        public ChatMsgGroupItemInfo MsgGroup { get; set; }
        public List<ChatMsgItemInfo> MsgList { get; set; }
    }

    public class ChatService
        : IChatService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IDataStorageServiceFactory _dataStorageServiceFactory;
        private readonly ProviderRegisterer _providerRegisterer;
        private readonly AIAssistantService _aiAssistantService;
        private readonly ProviderService _providerService;
        private readonly AITaskState _aiTaskState;

        public ChatService(IDataStorageServiceFactory dataStorageServiceFactory, ProviderRegisterer providerRegisterer,
            AIAssistantService aiAssistantService, ProviderService providerService, AITaskState aiTaskState)
        {
            _logger = Log.ForContext<ChatService>();
            _dataStorageServiceFactory = dataStorageServiceFactory;
            _providerRegisterer = providerRegisterer;
            _aiAssistantService = aiAssistantService;
            _providerService = providerService;
            _aiTaskState = aiTaskState;
        }

        private List<MsgCacheInfo> _msgCaches;

        public List<ChatMsgGroupItemInfo> MsgGroupList { get; set; } = [];
        public List<ChatMsgItemInfo> MsgList { get; set; } = [];
        public List<AiAppInfo> AiAppList { get; set; } = [];
        public EventCallback OnStateHasChange { get; set; }


        public async Task LoadAiAppListAsync()
        {
            await _aiAssistantService.InitAsync();
            List<AiAppInfo> aiAppList = (await _aiAssistantService.GetAssistantsAsync()).Select(x => new AiAppInfo()
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
            var ret = await (await _dataStorageServiceFactory.CreateAsync()).GetItemAsync<List<MsgCacheInfo>>("msgCache");
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
            var dataStorageService = await _dataStorageServiceFactory.CreateAsync();
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

                _aiTaskState.TaskID = tmsgGroup.Id;
                _aiTaskState.CancellationToken = cancellationToken;
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
                        if ("jpg/jpeg/png/bmp/gif".Contains(ext))
                        {
                            using var fileStream = file.OpenReadStream(30 * 1024 * 1024);
                            var fileCustomID = Guid.NewGuid().ToString();
                            await dataStorageService.SetItemAsStreamAsync(fileCustomID, fileStream, false, cancellationToken);
                            newMsg.Contents.Add(new AIProviderHandleCustomFileMessageResponse()
                            {
                                Type = AIChatHandleMessageType.ImageMessage,
                                FileCustomID = fileCustomID,
                            });
                        }
                        else if ("pdf".Contains(ext))
                        {
                            using var fileStream = file.OpenReadStream(30 * 1024 * 1024);
                            var fileCustomID = Guid.NewGuid().ToString();
                            await dataStorageService.SetItemAsStreamAsync(fileCustomID, fileStream, false, cancellationToken);
                            newMsg.Contents.Add(new AIProviderHandleCustomFileMessageResponse()
                            {
                                Type = AIChatHandleMessageType.FileMessage,
                                FileCustomID = fileCustomID,
                                FileOriginalName = file.Name,
                                FileOriginalMediaType = "application/pdf"
                            });
                        }
                        else
                        {
                            throw new Exception($"不支持的文件类型({ext})");
                        }
                    }
                }

                newMsg.Contents.Add(new AIProviderHandleTextMessageResponse()
                {
                    Message = msg
                });

                var newRetMsg = new ChatMsgItemInfo()
                {
                    Id = Guid.NewGuid().ToString(),
                    AiApp = aiApp,
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
                await dataStorageService.SetItemAsync("msgCache", _msgCaches, cancellationToken);
                await OnStateHasChange.InvokeAsync();
                var ai = await aiApp.GetAIProviderAsync(_providerService);
                var mcpServices = await aiApp.GetMCPServicesAsync(_providerService);
                var msgChat = ai.CreateNewChat(aiApp.Assistant.Prompt);
                var defaultMsgHistory = aiApp.Assistant.HistoryMsg.Where(x => !string.IsNullOrWhiteSpace(x.Name));
                foreach (var tmsg in defaultMsgHistory)
                {
                    msgChat.AddMessage(tmsg.Name.ToLower() switch
                    {
                        "user" => AuthorRole.User,
                        "system" => AuthorRole.System,
                        "assistant" => AuthorRole.Assistant,
                        "tool" => AuthorRole.Tool,
                        _ => AuthorRole.User
                    }, [
                        new AIProviderHandleTextMessageResponse()
                        {
                            Message = tmsg.Content
                        }
                    ]);
                }

                var msgHistory = msgCache.MsgList[..^1];
                ConvertChatHistory(msgHistory, msgChat);

                var functionManagers = new List<IFunctionManager>();
                var customFunctionManager = new DefaultFunctionManager();
                customFunctionManager.AddFunction(typeof(AIUtilsSkill), nameof(AIUtilsSkill.QueryNowDate), clsArgs: [dataStorageService]);
                foreach (var mcpService in mcpServices)
                {
                    var functionManager = await mcpService.GetIFunctionManager(cancellationToken);
                    functionManagers.Add(functionManager);
                }
                functionManagers.Add(customFunctionManager);

                var chatSettings = new ChatSettings()
                {
                    FunctionManagers = functionManagers ?? [],
                    SessionId = tmsgGroup.Id
                };

                _aiTaskState.ChatTask = ai;
                _aiTaskState.ChatSettings = chatSettings;
                _aiTaskState.ChatHistory = msgChat;
                var chatRet = ai.ChatAsync(msgChat, chatSettings, cancellationToken);

                await HandleMessage(_aiTaskState, chatRet, msgHistory, newRetMsg.Contents);
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error("SendAnyMsgAsync: {error}\r\n{stackTrace}", ex.Message, ex.StackTrace);
                if (!cancellationToken.IsCancellationRequested)
                    throw;
            }

            await dataStorageService.SetItemAsync("msgCache", _msgCaches);
        }

        private void ConvertChatHistory(List<ChatMsgItemInfo> historyMessages, ChatHistory chatHistory)
        {
            foreach (var item in historyMessages)
            {
                switch (item.UserType)
                {
                    case ChatUserType.Sender:
                        chatHistory.AddMessage(AuthorRole.User, item.Contents);
                        break;
                    case ChatUserType.Receiver:
                        if (item.Name == "tool")
                            chatHistory.AddMessage(AuthorRole.Tool, item.Contents);
                        if (item.Name == "system")
                            chatHistory.AddMessage(AuthorRole.System, item.Contents);
                        else
                            chatHistory.AddMessage(AuthorRole.Assistant, item.Contents);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task<List<IAIChatHandleMessage>> HandleMessage(
            AITaskState aiTaskState,
            IAsyncEnumerable<IAIChatHandleMessage> receiveMessages,
            List<ChatMsgItemInfo> uiHistoryMessages, List<IAIChatHandleMessage> uiNewResultMessages)
        {
            var ret = new List<IAIChatHandleMessage>();
            IAIChatHandleMessage? oldResponse = null;
            await foreach (var response in receiveMessages.WithCancellation(aiTaskState.CancellationToken))
            {
                if (aiTaskState.CancellationToken != CancellationToken.None && aiTaskState.CancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();
                if (response == null)
                    continue;
                if (response is AIProviderHandleTextMessageResponse textMessageResponse && oldResponse != null && oldResponse is AIProviderHandleTextMessageResponse oldTextMessageResponse)
                {
                    oldTextMessageResponse.Message += textMessageResponse.Message;
                }
                else if (response is AIProviderHandleReasoningMessageResponse reasoningMessageResponse && oldResponse != null && oldResponse is AIProviderHandleReasoningMessageResponse oldReasoningMessageResponse)
                {
                    oldReasoningMessageResponse.Message += reasoningMessageResponse.Message;
                }
                else if (response is AIProviderHandleRefusalMessageResponse refusalMessageResponse && oldResponse != null && oldResponse is AIProviderHandleRefusalMessageResponse oldRefusalMessageResponse)
                {
                    oldRefusalMessageResponse.Refusal += refusalMessageResponse.Refusal;
                }
                else if (response is AIProviderHandleAudioStreamMessageResponse audioStreamMessageResponse && oldResponse != null && oldResponse is AIProviderHandleAudioStreamMessageResponse oldAudioStreamMessageResponse)
                {
                    using var reader = new BinaryReader(audioStreamMessageResponse.Stream);
                    using var writer = new BinaryWriter(oldAudioStreamMessageResponse.Stream);
                    var buffer = new byte[4096];
                    int bytesRead;

                    while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                        writer.Flush();
                        await OnStateHasChange.InvokeAsync();
                    }
                }
                else
                {
                    uiNewResultMessages.Add(response);
                    ret.Add(response);
                    oldResponse = response;
                }
                await OnStateHasChange.InvokeAsync();
                if (response is AIProviderHandleFunctionCallResponse functionCallMessage)
                {
                    if (functionCallMessage.Result == null)
                    {
                        var funCallRet = functionCallMessage.FunctionManager?.CallFunctionAsync(
                            functionCallMessage.FunctionName,
                            functionCallMessage.Arguments?.ToDictionary(x => x.Key, x => (object?)x.Value),
                            aiTaskState.CancellationToken
                        );
                        if (funCallRet != null)
                        {
                            var cloneChatMessages = uiHistoryMessages.ToList();
                            var newMessages = ret.ToList();
                            cloneChatMessages.Add(
                                new ChatMsgItemInfo()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    UserType = ChatUserType.Receiver,
                                    Contents = newMessages,
                                }
                            );
                            functionCallMessage.Result = await HandleMessage(aiTaskState, funCallRet, cloneChatMessages, []);
                            var chatHistory = new ChatHistory();
                            ConvertChatHistory(cloneChatMessages, chatHistory);
                            aiTaskState.ChatHistory = chatHistory;
                            var functionCallResultAISummaryResult = aiTaskState.ChatTask!.ChatAsync(chatHistory, aiTaskState.ChatSettings, aiTaskState.CancellationToken);
                            await HandleMessage(aiTaskState, functionCallResultAISummaryResult, cloneChatMessages, uiNewResultMessages);
                            break;
                        }
                    }
                }
            }

            return ret;
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