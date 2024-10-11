using System.Text;

namespace TrustInnova.Abstractions.AIScheduler
{
    public interface IAIChatTask
    {
        public ChatHistory CreateNewChat(string? instructions = null);
        public IAsyncEnumerable<IAIChatHandleResponse> ChatAsync(ChatHistory chat, ChatSettings? chatSettings = null, CancellationToken cancellationToken = default);

        public async Task<string> ChatReturnTextAsync(ChatHistory chat, ChatSettings? chatSettings = null, CancellationToken cancellationToken = default)
        {
            var ret = new StringBuilder();
            var data = ChatAsync(chat, chatSettings, cancellationToken);
            await foreach (var item in data)
            {
                if (item.Type == AIChatHandleResponseType.TextMessage && item is AIProviderHandleTextMessageResponse messageResponse)
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

        public int? MaxTokens { get; set; }

        public IFunctionManager? FunctionManager { get; set; }
    }

    public static class ChatHistoryExtend
    {
        public static ChatHistory MaxTokenTruncation(this ChatHistory chats, ITokenCalcTask? tokenCalcService, long maxToken)
        {
            if (tokenCalcService == null)
                return chats;
            var ret = new ChatHistory();
            var count = 0L;
            for (int i = chats.Count - 1; i >= 0; i--)
            {
                var chat = chats[i];
                if (chat == null || chat.Contents.Count == 0)
                    continue;
                var fContent = chat.Contents.First();
                if (chat.Role == AuthorRole.System || chat.Contents.Count > 1 || fContent.ContentType != ChatMessageContentType.Text)
                {
                    ret.Insert(0, chat);
                    continue;
                }
                if (count >= maxToken)
                    continue;
                rcalc:
                var fContentStr = (string)fContent.Content;
                var chatTokens = tokenCalcService.GetTokens(fContentStr);
                if (count + chatTokens > maxToken)
                {
                    if (fContentStr.Length == 0)
                        break;
                    fContent.Content = fContentStr[1..];
                    goto rcalc;
                }
                else
                {
                    count += chatTokens;
                    ret.Insert(0, chat);
                }
            }
            if (ret.Count == 0)
                return chats;
            return ret;
        }
    }

    public enum AuthorRole
    {
        System,
        Assistant,
        User
    }

    public enum ChatMessageContentType
    {
        Text,
        ImageBase64,
        ImageURL,
        DocStream,
        DocURL
    }

    public class ChatMessageContent
    {
        public string ContentId { get; set; }

        public string ContentName { get; set; } = "";

        public object Content { get; set; }

        public ChatMessageContentType ContentType { get; set; }

        public ChatMessageContent()
        {
        }

        public ChatMessageContent(string contentId, object content, ChatMessageContentType contentType)
        {
            ContentId = contentId;
            Content = content;
            ContentType = contentType;
        }
    }

    public class ChatMessage
    {
        public AuthorRole Role { get; set; }

        public List<ChatMessageContent> Contents { get; set; }

        public ChatMessage(AuthorRole role, List<ChatMessageContent> contents)
        {
            Role = role;
            Contents = contents;
        }
    }

    public class ChatHistory : List<ChatMessage>
    {
        public void AddMessage(AuthorRole authorRole, List<ChatMessageContent> contents)
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
