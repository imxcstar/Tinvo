﻿using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Abstractions.ImageAnalysis;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.Provider;

namespace Tinvo.Pages.Chat.Component.ChatMsgList
{
    public enum AiAppAIProviderType
    {
        OpenAI,
        XFSpark
    }

    public class AiAppInfo
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int OrderIndex { get; set; }

        public AssistantEntity Assistant { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public IAIChatTask GetAIProvider(ProviderService providerService)
        {
            var chatSkill = Assistant.Skills.FirstOrDefault(x => x.SupportType == AssistantSupportSkillType.Chat);
            if (chatSkill == null)
                throw new NotSupportedException("此助手没有聊天技能");
            if (string.IsNullOrWhiteSpace(chatSkill.Content))
                throw new NotSupportedException("此助手的聊天配置错误");
            var config = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement?>>>(chatSkill.Content);
            if (config == null)
                throw new NotSupportedException("此助手的聊天配置异常");
            var metadata = providerService.GetProviderTaskParameterMetadataById(chatSkill.Id);
            if (metadata == null)
                throw new NotSupportedException("请重新配置此助手的聊天技能");
            var task = metadata.Instance(config) as IAIChatTask;
            if (task == null)
                throw new NotSupportedException("此助手的聊天实例化错误");
            return task;
        }

        public IImageAnalysisTask? GetImageAnalysis(ProviderService providerService)
        {
            var imageAnalysisSkill =
                Assistant.Skills.FirstOrDefault(x => x.SupportType == AssistantSupportSkillType.ImageAnalysis);
            if (imageAnalysisSkill == null)
                return null;
            if (string.IsNullOrWhiteSpace(imageAnalysisSkill.Content))
                throw new NotSupportedException("此助手的图片识别配置错误");
            var config = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement?>>>(imageAnalysisSkill.Content);
            if (config == null)
                throw new NotSupportedException("此助手的图片识别配置异常");
            var metadata = providerService.GetProviderTaskParameterMetadataById(imageAnalysisSkill.Id);
            if (metadata == null)
                throw new NotSupportedException("请重新配置此助手的图片识别技能");
            var task = metadata.Instance(config) as IImageAnalysisTask;
            if (task == null)
                throw new NotSupportedException("此助手的图片识别实例化错误");
            return task;
        }

        public List<IMCPService> GetMCPServices(ProviderService providerService)
        {
            return
                Assistant.Skills
                .Where(x =>
                    x.SupportType == AssistantSupportSkillType.MCP &&
                    !string.IsNullOrWhiteSpace(x.Content)
                )
                .Select(x =>
                {
                    var config = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement?>>>(x.Content!);
                    if (config == null)
                        return null;
                    var metadata = providerService.GetProviderTaskParameterMetadataById(x.Id);
                    if (metadata == null)
                        return null;
                    var task = metadata.Instance(config) as IMCPService;
                    if (task == null)
                        return null;
                    return task;
                })
                .Where(x => x != null)
                .ToList()!;
        }
    }

    public class ChatMsgGroupItemInfo
    {
        public string? Id { get; set; }

        public string Title { get; set; } = null!;

        public DateTime CreateTime { get; set; }
    }

    public class ChatMsgItemInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string HeadIconURL { get; set; }
        public List<ChatMsgItemContentInfo> Contents { get; set; } = new List<ChatMsgItemContentInfo>();
        public ChatUserType UserType { get; set; }
        public AiAppInfo? AiApp { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class ChatMsgItemContentInfo
    {
        public string? Title { get; set; }

        public string Content { get; set; } = "";

        public ChatContentType ContentType { get; set; } = ChatContentType.Default;
    }

    public enum ChatUserType
    {
        Sender,
        Receiver
    }

    public enum ChatContentType
    {
        Text,
        Video,
        Audio,
        Image,
        File,
        Default = 99,
        ErrorInfo = 100,
        BrowserURL = 101,
        BrowserHTML = 102,
    }
}