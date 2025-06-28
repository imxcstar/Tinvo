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
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.AIAssistant.Entities;
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
                        if ("jpg/jpeg/png/bmp/gif".Contains(ext))
                        {
                            using var fileStream = file.OpenReadStream(30 * 1024 * 1024);
                            var fileCustomID = Guid.NewGuid().ToString();
                            await _dataStorageService.SetItemAsStreamAsync(fileCustomID, fileStream, false, cancellationToken);
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
                            await _dataStorageService.SetItemAsStreamAsync(fileCustomID, fileStream, false, cancellationToken);
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
                AddChatHistory(msgHistory, msgChat);

                var functionManagers = new List<IFunctionManager>();
                foreach (var mcpService in mcpServices)
                {
                    var functionManager = await mcpService.GetIFunctionManager(cancellationToken);
                    functionManagers.Add(functionManager);
                }

                var chatSettings = new ChatSettings()
                {
                    FunctionManagers = functionManagers ?? [],
                    SessionId = tmsgGroup.Id
                };

                var chatRet = ai.ChatAsync(msgChat, chatSettings, cancellationToken);

                await HandleMessage(ai, chatSettings, chatRet, msgHistory, newRetMsg.Contents, cancellationToken);
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

        private void AddChatHistory(List<ChatMsgItemInfo> historyMessages, ChatHistory chatHistory)
        {
            foreach (var item in historyMessages)
            {
                if (item.Contents.Count == 0)
                    continue;
                switch (item.UserType)
                {
                    case ChatUserType.Sender:
                        chatHistory.AddMessage(AuthorRole.User, item.Contents);
                        break;
                    case ChatUserType.Receiver:
                        if (item.Name == "tool")
                            chatHistory.AddMessage(AuthorRole.Tool, item.Contents);
                        else
                            chatHistory.AddMessage(AuthorRole.Assistant, item.Contents);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task<List<IAIChatHandleMessage>> HandleMessage(IAIChatTask ai, ChatSettings chatSettings,
            IAsyncEnumerable<IAIChatHandleMessage> receiveMessages, List<ChatMsgItemInfo> historyMessages,
            List<IAIChatHandleMessage> newResultMessages, CancellationToken cancellationToken = default)
        {
            var ret = new List<IAIChatHandleMessage>();
            IAIChatHandleMessage? oldResponse = null;
            await foreach (var response in receiveMessages.WithCancellation(cancellationToken))
            {
                if (cancellationToken != CancellationToken.None && cancellationToken.IsCancellationRequested)
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
                    newResultMessages.Add(response);
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
                            functionCallMessage.Arguments?.ToDictionary(x => x.Key, x => (object?)x.Value)
                        );
                        if (funCallRet != null)
                        {
                            var cloneChatMessages = historyMessages.ToList();
                            var newMessages = newResultMessages.ToList();
                            newMessages.Remove(response);
                            cloneChatMessages.Add(
                                new ChatMsgItemInfo()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    UserType = ChatUserType.Receiver,
                                    Contents = newMessages,
                                }
                            );
                            var functionMsg = new ChatMsgItemInfo()
                            {
                                Id = Guid.NewGuid().ToString(),
                                UserType = ChatUserType.Receiver,
                                Name = "tool",
                                Contents = [
                                    functionCallMessage
                                ]
                            };
                            cloneChatMessages.Add(functionMsg);
                            cloneChatMessages.Add(new ChatMsgItemInfo()
                            {
                                Id = Guid.NewGuid().ToString(),
                                UserType = ChatUserType.Sender,
                                Contents = [
                                    new AIProviderHandleTextMessageResponse() { Message= "总结以上工具返回的内容" }
                                ]
                            });
                            functionCallMessage.Result = await HandleMessage(ai, chatSettings, funCallRet, cloneChatMessages, functionMsg.Contents, cancellationToken);
                            var chatHistory = new ChatHistory();
                            AddChatHistory(cloneChatMessages, chatHistory);
                            var functionCallResultAISummaryResult = ai.ChatAsync(chatHistory, chatSettings, cancellationToken);
                            await HandleMessage(ai, chatSettings, functionCallResultAISummaryResult, cloneChatMessages, newResultMessages, cancellationToken);
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