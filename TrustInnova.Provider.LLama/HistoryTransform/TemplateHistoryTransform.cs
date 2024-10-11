using LLama.Abstractions;
using LLama.Common;
using LLama.Transformers;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Provider.LLama.HistoryTransform
{
    public class TemplateHistoryTransform : IHistoryTransform
    {
        private readonly string defaultUserName = "User";
        private readonly string defaultAssistantName = "Assistant";
        private readonly string defaultSystemName = "System";
        private readonly string defaultUnknownName = "??";

        private string _template;
        private string _userName;
        private string _assistantName;
        private string _systemName;
        private string _unknownName;

        private Template _templateRender;

        public TemplateHistoryTransform(string template, string? userName = null, string? assistantName = null,
            string? systemName = null, string? unknownName = null)
        {
            _template = template;
            _templateRender = Template.Parse(_template);
            _userName = userName ?? defaultUserName;
            _assistantName = assistantName ?? defaultAssistantName;
            _systemName = systemName ?? defaultSystemName;
            _unknownName = unknownName ?? defaultUnknownName;
        }

        public IHistoryTransform Clone()
        {
            return new TemplateHistoryTransform(_template, _userName, _assistantName, _systemName, _unknownName);
        }

        public virtual string HistoryToText(ChatHistory history)
        {
            var system = history.Messages.Where(x => x.AuthorRole == AuthorRole.System).Select(x => x.Content).ToList();
            var user = history.Messages.Where(x => x.AuthorRole == AuthorRole.User).Select(x => x.Content).ToList();
            var assistant = history.Messages.Where(x => x.AuthorRole == AuthorRole.Assistant).Select(x => x.Content).ToList();
            var ret = _templateRender.Render(new
            {
                SystemsHas = system.Count > 0,
                UsersHas = user.Count > 0,
                AssistantsHas = assistant.Count > 0,
                Systems = system,
                Users = user,
                Assistants = assistant
            });
            return ret;
        }

        public ChatHistory TextToHistory(AuthorRole role, string text)
        {
            return new ChatHistory(
            [
                new ChatHistory.Message(role, text)
            ]);
        }
    }
}
