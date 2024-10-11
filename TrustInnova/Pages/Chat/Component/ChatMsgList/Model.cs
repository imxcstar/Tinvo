using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Abstractions.ImageAnalysis;
using TrustInnova.Application.AIAssistant.Entities;
using TrustInnova.Application.Provider;

namespace TrustInnova.Pages.Chat.Component.ChatMsgList
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
            var imageAnalysisSkill = Assistant.Skills.FirstOrDefault(x => x.SupportType == AssistantSupportSkillType.ImageAnalysis);
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
        public string Content { get; set; }
        public string HeadIconURL { get; set; }
        public ChatUserType UserType { get; set; }
        public ChatContentType ContentType { get; set; }
        public AiAppInfo? AiApp { get; set; }
        public DateTime CreateTime { get; set; }
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
        File
    }
}
