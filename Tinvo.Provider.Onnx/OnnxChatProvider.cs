using Microsoft.ML.OnnxRuntimeGenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Provider.Onnx
{
    [TypeMetadataDisplayName("聊天配置")]
    public class OnnxChatConfig
    {
        public string ModelPath { get; set; } = "";

        public string Template { get; set; } = "";

        public List<string> Stop { get; set; } = [];
    }

    public class OnnxChatProviderLoadInfo : IDisposable
    {
        public Model Model { get; set; }
        public Tokenizer Tokenizer { get; set; }

        public OnnxChatProviderLoadInfo(Model model, Tokenizer tokenizer)
        {
            Model = model;
            Tokenizer = tokenizer;
        }

        public void Dispose()
        {
            Tokenizer.Dispose();
            Model.Dispose();
        }
    }

    public class OnnxChatProviderLoader : IDisposable
    {
        private Dictionary<string, OnnxChatProviderLoadInfo> _loadinfo = [];

        public OnnxChatProviderLoadInfo Load(string modelPath)
        {
            if (_loadinfo.TryGetValue(modelPath, out var ret))
                return ret;
            var model = new Model(modelPath);
            var tokenizer = new Tokenizer(model);
            ret = new OnnxChatProviderLoadInfo(model, tokenizer);
            _loadinfo[modelPath] = ret;
            return ret;
        }

        public void Dispose()
        {
            foreach (var item in _loadinfo)
            {
                item.Value.Dispose();
            }
            _loadinfo.Clear();
        }
    }

    [ProviderTask("OnnxChat", "Onnx")]
    public class OnnxChatProvider : IAIChatTask
    {
        private OnnxChatConfig _config;
        private OnnxChatProviderLoadInfo _modelInfo;

        public OnnxChatProvider(OnnxChatConfig config, OnnxChatProviderLoader loader)
        {
            _config = config;
            _modelInfo = loader.Load(config.ModelPath);
        }

        public async IAsyncEnumerable<IAIChatHandleMessage> ChatAsync(ChatHistory chat, ChatSettings? chatSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string prompt = _config.Template.Replace("{user}", (chat.Last(x => x.Role == AuthorRole.User).Contents.FirstOrDefault() as AIProviderHandleTextMessageResponse)?.Message ?? "");
            var sequences = _modelInfo.Tokenizer.Encode(prompt);

            using GeneratorParams generatorParams = new GeneratorParams(_modelInfo.Model);

            using var tokenizerStream = _modelInfo.Tokenizer.CreateStream();
            using var generator = new Generator(_modelInfo.Model, generatorParams);
            generator.AppendTokenSequences(sequences);
            while (!generator.IsDone())
            {
                generator.GenerateNextToken();
                var msg = tokenizerStream.Decode(generator.GetSequence(0)[^1]);
                if (_config.Stop.Count > 0 && _config.Stop.Contains(msg))
                    break;
                yield return new AIProviderHandleTextMessageResponse()
                {
                    Message = msg
                };
                await Task.Delay(10, cancellationToken);
            }
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            var ret = new ChatHistory();
            if (instructions == null)
                ret.AddMessage(AuthorRole.User, [new AIProviderHandleTextMessageResponse() { Message = $@"现在的时间为：{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}" }]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(AuthorRole.User, [new AIProviderHandleTextMessageResponse() { Message = instructions }]);
            return ret;
        }
    }
}
