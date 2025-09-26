using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using Tinvo.Abstractions;
using Tinvo.Application;
using Tinvo.Pages.Chat.Component.ChatMsgList;

namespace Tinvo.Pages.Chat;

public partial class Chat
{
    private MudMessageBox setTitleBox { get; set; }

    private string searchChatMsgGroupName = "";

    private bool isKBSWait = false;

    private bool isLoadingMsgGroupList = true;
    private bool isLoadingMsgList = true;
    private bool isChatMsgGenerating = false;
    private bool enableSend = true;
    private bool isShowLoadGroupLoading = false;

    private string _mr = "";
    private bool _isShowChatHistory = false;

    private ChatMsgGroupItemInfo? selectMsgGroup;
    private AiAppInfo? selectAiApp;

    private AiAppInfo? chatSetSelectAiApp;

    private CancellationTokenSource? chatMsgGenerateCancellationTokenSource;

    private List<IBrowserFile> selectedFiles = new List<IBrowserFile>();

    private void ShowSettings()
    {
        _navigationManager.NavigateTo("/settings");
    }

    private void ShowAssistantManage()
    {
        _navigationManager.NavigateTo("/assistantManage");
    }

    private void ShowChatHistory()
    {
        _isShowChatHistory = !_isShowChatHistory;
        StateHasChanged();
    }

    private async Task<string> GetInputMsgValue()
    {
        return await _js.InvokeAsync<string>("blazorHelper.GetElementValueById", "input-msg");
    }

    private async Task SetInputMsgValue(string value)
    {
        await _js.InvokeVoidAsync("blazorHelper.SetElementValueById", "input-msg", value);
    }

    [JSInvokable]
    public void SendInfoMessage(string value)
    {
        _snackbar.Add(value, Severity.Info);
    }

    [JSInvokable]
    public void SendSuccessMessage(string value)
    {
        _snackbar.Add(value, Severity.Success);
    }

    [JSInvokable]
    public void SendErrorMessage(string value)
    {
        _snackbar.Add(value, Severity.Error);
    }

    [JSInvokable]
    public void SendWarnMessage(string value)
    {
        _snackbar.Add(value, Severity.Warning);
    }

    [JSInvokable]
    public bool OnMsgInputKeyDownBefore(string value)
    {
        return enableSend;
    }

    [JSInvokable]
    public async Task OnMsgInputKeyDownAfter(string value)
    {
        if (enableSend)
            await Send(value);
    }

    private async Task ChatSetting()
    {
        try
        {
            var oldName = selectMsgGroup?.Title;
            chatSetSelectAiApp = selectAiApp;
            bool? result = await setTitleBox.ShowAsync();
            if (result == null || !result.Value)
            {
                if (selectMsgGroup != null && oldName != null)
                    selectMsgGroup.Title = oldName;
            }
            else
            {
                if (selectMsgGroup != null && oldName != null)
                {
                    if (string.IsNullOrWhiteSpace(selectMsgGroup.Title))
                    {
                        selectMsgGroup.Title = oldName;
                        _snackbar.Add("标题不能为空", Severity.Warning);
                        return;
                    }

                    if (selectMsgGroup.Title != oldName)
                        if (!await _chatService.UpdateMsgGroup(selectMsgGroup))
                            selectMsgGroup.Title = oldName;
                }

                selectAiApp = chatSetSelectAiApp;
            }
        }
        catch (Exception ex)
        {
            _snackbar.Add(ex.Message, Severity.Error, config => { config.RequireInteraction = true; });
        }

        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (OperatingSystem.IsMacCatalyst())
                _mr = "margin-right: 8px;";
            try
            {
                await Task.Delay(500);
                _chatService.OnStateHasChange = EventCallback.Factory.Create(this, StateHasChanged);
                await _chatService.LoadAiAppListAsync();
                selectAiApp = _chatService.AiAppList.FirstOrDefault();
                isLoadingMsgGroupList = true;
                await _chatService.LoadMsgGroupListAsync();
                isLoadingMsgGroupList = false;
                var fMsgGroup = _chatService.MsgGroupList.FirstOrDefault();
                if (fMsgGroup != null)
                    await GetMsgList(fMsgGroup);
                else
                    await NewMsgGroup();
                await _js.InvokeVoidAsync("blazorHelper.InitScrollEndListener", "msg-history-group", DotNetObjectReference.Create(this), nameof(OnMsgGroupScrollToEnd));
                await _js.InvokeVoidAsync("blazorHelper.OnKeyDownListen", "input-msg", "!shift + !ctrl + Enter", DotNetObjectReference.Create(this), nameof(OnMsgInputKeyDownBefore), nameof(OnMsgInputKeyDownAfter));

                await _js.InvokeVoidAsync("blazorHelper.initializePasteHandler", "input-msg", "paste-file-input", DotNetObjectReference.Create(this), nameof(OnPasteFile));

                await _js.InvokeVoidAsync("blazorHelper.RegisterInfoMessageFun", DotNetObjectReference.Create(this), nameof(SendInfoMessage));
                await _js.InvokeVoidAsync("blazorHelper.RegisterSuccessMessageFun", DotNetObjectReference.Create(this), nameof(SendSuccessMessage));
                await _js.InvokeVoidAsync("blazorHelper.RegisterErrorMessageFun", DotNetObjectReference.Create(this), nameof(SendErrorMessage));
                await _js.InvokeVoidAsync("blazorHelper.RegisterWarnMessageFun", DotNetObjectReference.Create(this), nameof(SendWarnMessage));

                isLoadingMsgList = false;
            }
            catch (Exception ex)
            {
                _snackbar.Add(ex.Message, Severity.Error);
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public class PasteBrowserFile : IBrowserFile
    {
        private readonly Stream _stream;

        public PasteBrowserFile(string name, long size, Stream stream)
        {
            Name = name;
            Size = size;
            _stream = stream;
        }

        public string Name { get; }

        public DateTimeOffset LastModified => DateTimeOffset.Now;

        public long Size { get; }

        public string ContentType => "application/octet-stream";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            return _stream;
        }
    }

    [JSInvokable]
    public async Task<bool> OnPasteFile()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsMacCatalyst() && _platform.Type != PlatformType.Maui)
            return true;

        var systemClipboard = _services.GetService<ISystemClipboard>();
        if (systemClipboard == null)
            return false;

        var files = systemClipboard.GetFiles();
        if (files == null || files.Count == 0)
            return false;
            
        foreach (var file in files)
        {
            try
            {
                var stream = file.Stream;
                var browserFile = new PasteBrowserFile(file.Name ?? "clipboard-file", stream.Length, stream);
                selectedFiles.Add(browserFile);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"处理剪贴板文件 {file.Name} 时出错: {ex.Message}", Severity.Error);
            }
        }
        StateHasChanged();
        
        return false;
    }

    [JSInvokable]
    public async Task OnMsgGroupScrollToEnd()
    {
        if (isShowLoadGroupLoading)
            return;

        isShowLoadGroupLoading = true;
        StateHasChanged();
        await Task.Delay(300);

        try
        {
            await _chatService.LoadMoreMsgGroupListAsync();
        }
        catch (Exception ex)
        {
            _snackbar.Add(ex.Message, Severity.Error);
        }

        isShowLoadGroupLoading = false;
        StateHasChanged();
    }

    private async Task GetMsgList(ChatMsgGroupItemInfo? msgGroup)
    {
        try
        {
            if (selectMsgGroup == msgGroup)
                return;
            if (chatMsgGenerateCancellationTokenSource != null)
                await chatMsgGenerateCancellationTokenSource.CancelAsync();
            selectMsgGroup = msgGroup;
            isLoadingMsgList = true;
            StateHasChanged();
            await Task.Delay(100);
            await _chatService.LoadMsgListAsync(selectMsgGroup);
            var uAiapp = _chatService.MsgList.LastOrDefault()?.AiApp;
            if (uAiapp != null && chatSetSelectAiApp == null) // This condition for chatSetSelectAiApp might need review based on its intended reset logic
            {
                selectAiApp = _chatService.AiAppList.FirstOrDefault(x => x.Id == uAiapp.Id);
            }
            else if (selectAiApp == null) // Ensure an AI app is selected if possible
            {
                selectAiApp = _chatService.AiAppList.FirstOrDefault();
            }


            isLoadingMsgList = false;
            if (_isShowChatHistory)
                _isShowChatHistory = false;
            StateHasChanged();
            await Task.Delay(100);
            await _js.InvokeVoidAsyncIgnoreErrors("ChatContainerToBottom", true, false);
        }
        catch (Exception ex)
        {
            _snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task OnSettingNewMsgGroup()
    {
        setTitleBox.Close();
        await NewMsgGroup();
    }

    private async Task NewMsgGroup()
    {
        var fMsgGroup = _chatService.MsgGroupList.FirstOrDefault();
        if (fMsgGroup == null || !string.IsNullOrWhiteSpace(fMsgGroup.Id))
        {
            fMsgGroup = new ChatMsgGroupItemInfo()
            {
                CreateTime = DateTime.Now,
                Title = "新的聊天",
            };
            _chatService.MsgGroupList.Insert(0, fMsgGroup);
        }

        selectAiApp = _chatService.AiAppList.FirstOrDefault(); // Reset to default AI for new chat
        await GetMsgList(fMsgGroup);
    }

    private async Task ClearSelectFiles()
    {
        selectedFiles.Clear();
        await _js.InvokeVoidAsync("blazorHelper.clearPasteFile", "paste-file-input");
    }

    private async Task HandleFileSelection(IReadOnlyList<IBrowserFile>? files)
    {
        if (files == null)
            return;
        selectedFiles.Clear();
        selectedFiles.AddRange(files);
        StateHasChanged();
    }

    private async Task RemoveSelectedFile(IBrowserFile file)
    {
        selectedFiles.Remove(file);
        await _js.InvokeVoidAsync("blazorHelper.removePasteFile", "paste-file-input", file.Name);
        StateHasChanged();
    }

    private async Task SendMsgFromButtonClick()
    {
        await Send(await GetInputMsgValue());
    }

    private async Task Send(string? sendMsgContent)
    {
        var trimmedMsgContent = sendMsgContent?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedMsgContent) && !selectedFiles.Any())
        {
            _snackbar.Add("请输入消息或选择文件", Severity.Warning);
            return;
        }

        try
        {
            if (selectAiApp == null || !_chatService.AiAppList.Any(x => x.Id == selectAiApp.Id))
            {
                selectAiApp = _chatService.AiAppList.FirstOrDefault();
                if (selectAiApp == null)
                {
                    _snackbar.Add("没有可用的AI应用,请先配置", Severity.Error); // No AI app configured
                    return;
                }
            }

            var msgGroup = selectMsgGroup;
            if (!string.IsNullOrWhiteSpace(searchChatMsgGroupName))
            {
                var isReturn = true;
                foreach (var chatHistory in _chatService.MsgGroupList)
                {
                    if (chatHistory.Title.Contains(searchChatMsgGroupName) && chatHistory.Id == msgGroup?.Id)
                    {
                        isReturn = false;
                        break;
                    }
                }

                if (isReturn)
                {
                    _snackbar.Add("搜索中只能使用搜索的聊天组", Severity.Warning);
                    return;
                }
            }

            if (chatMsgGenerateCancellationTokenSource != null)
                await chatMsgGenerateCancellationTokenSource.CancelAsync();
            chatMsgGenerateCancellationTokenSource = new CancellationTokenSource();
            isChatMsgGenerating = true;
            enableSend = false;
            StateHasChanged(); // Update UI to reflect disabled send button

            await SetInputMsgValue("");
            await _js.InvokeVoidAsyncIgnoreErrors("ChatContainerToBottom");
            await _js.InvokeVoidAsyncIgnoreErrors("StartChatContainerCheck");

            List<IBrowserFile> tempFiles = [..selectedFiles];

            await ClearSelectFiles();

            await _chatService.SendMsgAsync(trimmedMsgContent, tempFiles, selectAiApp, msgGroup, null, null,
                chatMsgGenerateCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _snackbar.Add("消息发送已取消", Severity.Info);
        }
        catch (Exception ex)
        {
            _snackbar.Add(ex.Message, Severity.Error, config => { config.RequireInteraction = true; });
        }
        finally
        {
            await _js.InvokeVoidAsyncIgnoreErrors("StopChatContainerCheck");
            await _js.InvokeVoidAsyncIgnoreErrors("ChatContainerToBottom", false);
            isChatMsgGenerating = false;
            enableSend = true;
            chatMsgGenerateCancellationTokenSource?.Dispose();
            chatMsgGenerateCancellationTokenSource = null;
            StateHasChanged(); // Update UI
        }
    }

    private async Task StopChatMsgGenerate()
    {
        if (chatMsgGenerateCancellationTokenSource != null)
        {
            await chatMsgGenerateCancellationTokenSource.CancelAsync();
            _snackbar.Add("停止指令已发送", Severity.Info);
        }
    }

    public void Dispose()
    {
        _js.InvokeVoidAsyncIgnoreErrors("StopChatContainerCheck");
        if (chatMsgGenerateCancellationTokenSource != null)
        {
            chatMsgGenerateCancellationTokenSource.Cancel();
            chatMsgGenerateCancellationTokenSource.Dispose();
        }
    }
}