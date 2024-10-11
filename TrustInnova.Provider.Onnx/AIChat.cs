using Microsoft.ML.OnnxRuntimeGenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Provider.Onnx
{
    public class ChatMessage
    {
        public string UserMessage { get; set; } = null!;
        public string AIMessage { get; set; } = null!;
    }


    public class AIChat
    {
        private string _modelPath;

        private Model _model;
        private MultiModalProcessor _multiModel;
        private Tokenizer _tokenizer;

        public AIChat(string modelPath)
        {
            _modelPath = modelPath;
            _model = new Model(_modelPath);
            _multiModel = new MultiModalProcessor(_model);
            _tokenizer = new Tokenizer(_model);
        }

        public async IAsyncEnumerable<string> ChatAsync(string message, IList<ChatMessage> chatHistory)
        {
            var chatHistoryStr = string.Join('\n', chatHistory.Select(x => @$"<|user|>
{x.UserMessage}<|end|>
<|assistant|>
{x.AIMessage}<|end|>
"));
            if (!string.IsNullOrWhiteSpace(chatHistoryStr))
                chatHistoryStr += "\n";
            string prompt = @$"{chatHistoryStr}<|user|>
{message}<|end|>
<|assistant|>
";
            var sequences = _tokenizer.Encode(prompt);

            using GeneratorParams generatorParams = new GeneratorParams(_model);
            generatorParams.SetInputSequences(sequences);

            using var tokenizerStream = _tokenizer.CreateStream();
            using var generator = new Generator(_model, generatorParams);
            while (!generator.IsDone())
            {
                generator.ComputeLogits();
                generator.GenerateNextToken();
                var ret = tokenizerStream.Decode(generator.GetSequence(0)[^1]);
                if (!string.IsNullOrEmpty(ret))
                    yield return ret;
                await Task.Delay(1);
            }
        }

        public async IAsyncEnumerable<string> ChatImageAsync(string message, string imagePath, IList<ChatMessage> chatHistory)
        {
            var chatHistoryStr = string.Join('\n', chatHistory.Select(x => @$"<|user|>
{x.UserMessage}<|end|>
<|assistant|>
{x.AIMessage}<|end|>
"));
            if (!string.IsNullOrWhiteSpace(chatHistoryStr))
                chatHistoryStr += "\n";
            string prompt = @$"{chatHistoryStr}<|user|>
<|image_1|>
{message}<|end|>
<|assistant|>
";
            var image = Images.Load(imagePath);
            var inputs = _multiModel.ProcessImages(prompt, image);

            using GeneratorParams generatorParams = new GeneratorParams(_model);
            generatorParams.SetInputs(inputs);

            using var tokenizerStream = _tokenizer.CreateStream();
            using var generator = new Generator(_model, generatorParams);
            while (!generator.IsDone())
            {
                generator.ComputeLogits();
                generator.GenerateNextToken();
                var ret = tokenizerStream.Decode(generator.GetSequence(0)[^1]);
                if (!string.IsNullOrEmpty(ret))
                    yield return ret;
                await Task.Delay(1);
            }
        }
    }
}
