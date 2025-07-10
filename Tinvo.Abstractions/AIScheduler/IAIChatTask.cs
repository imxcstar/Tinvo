using System.Text;

namespace Tinvo.Abstractions.AIScheduler
{
    public interface IAIChatTask : IProvider
    {
        public ChatHistory CreateNewChat(string? instructions = null);
        public IAsyncEnumerable<IAIChatHandleMessage> ChatAsync(ChatHistory chat, ChatSettings? chatSettings = null, CancellationToken cancellationToken = default);

        public async Task<string> ChatReturnTextAsync(ChatHistory chat, ChatSettings? chatSettings = null, CancellationToken cancellationToken = default)
        {
            var ret = new StringBuilder();
            var data = ChatAsync(chat, chatSettings, cancellationToken);
            await foreach (var item in data)
            {
                if (item.Type == AIChatHandleMessageType.TextMessage && item is AIProviderHandleTextMessageResponse messageResponse)
                {
                    ret.Append(messageResponse.Message);
                }
            }
            return ret.ToString();
        }
    }

    public class ChatSettings
    {
        public string? SessionId { get; set; }

        public double? Temperature { get; set; }

        public double? TopP { get; set; }

        public double? PresencePenalty { get; set; }

        public double? FrequencyPenalty { get; set; }

        public IList<string> StopSequences { get; set; } = Array.Empty<string>();

        public int ResultsPerPrompt { get; set; } = 1;

        public int? MaxOutputTokens { get; set; }

        public IFunctionManager? FunctionManager { get; set; }

        private List<IFunctionManager> _functionManagers = [];
        public List<IFunctionManager> FunctionManagers
        {
            get => _functionManagers;
            set
            {
                _functionManagers = value;
                FunctionManager = new MultiFunctionManager(_functionManagers.ToList());
            }
        }
    }

    public enum AuthorRole
    {
        System,
        Assistant,
        User,
        Tool
    }

    public class ChatMessage
    {
        public AuthorRole Role { get; set; }

        public List<IAIChatHandleMessage> Contents { get; set; }

        public ChatMessage(AuthorRole role, List<IAIChatHandleMessage> contents)
        {
            Role = role;
            Contents = contents;
        }
    }

    public class ChatHistory : List<ChatMessage>
    {
        public void AddMessage(AuthorRole authorRole, List<IAIChatHandleMessage> contents)
        {
            Add(new ChatMessage(authorRole, contents));
        }

        public ChatHistory ShallowClone()
        {
            var ret = new ChatHistory();
            foreach (var item in this)
            {
                ret.Add(item);
            }
            return ret;
        }
    }
}
