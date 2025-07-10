using Microsoft.Extensions.DependencyInjection;
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

        public async Task<IAIChatTask> GetAIProviderAsync(ProviderService providerService)
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
            var task = (await metadata.InstanceAsync(config)) as IAIChatTask;
            if (task == null)
                throw new NotSupportedException("此助手的聊天实例化错误");
            return task;
        }

        public async Task<List<IMCPService>> GetMCPServicesAsync(ProviderService providerService)
        {
            var ret = new List<IMCPService>();
            var data =
                Assistant.Skills
                .Where(x =>
                    x.SupportType == AssistantSupportSkillType.MCP &&
                    !string.IsNullOrWhiteSpace(x.Content)
                );
            foreach (var item in data)
            {
                var config = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement?>>>(item.Content!);
                if (config == null)
                    continue;
                var metadata = providerService.GetProviderTaskParameterMetadataById(item.Id);
                if (metadata == null)
                    continue;
                var task = (await metadata.InstanceAsync(config)) as IMCPService;
                if (task == null)
                    continue;
                ret.Add(task);
            }
            return ret;
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
        public required string Id { get; set; }
        public string? Name { get; set; }
        public string? HeadIconURL { get; set; }
        public List<IAIChatHandleMessage> Contents { get; set; } = new List<IAIChatHandleMessage>();
        public required ChatUserType UserType { get; set; }
        public AiAppInfo? AiApp { get; set; }
        public DateTime? CreateTime { get; set; }
    }

    public enum ChatUserType
    {
        Sender,
        Receiver
    }
}