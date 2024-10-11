using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TrustInnova.Utils.Extend;

namespace TrustInnova.Provider.OpenAI.API
{
    public class OpenAIChatCompletionAPI
    {
        private readonly IOpenAIChatAPI _api;
        private readonly ILogger _logger;

        public OpenAIChatCompletionAPI(IOpenAIChatAPI chatAPI)
        {
            _api = chatAPI;
            _logger = Log.ForContext<OpenAIChatCompletionAPI>();
        }

        public async IAsyncEnumerable<OpenAIChatCompletionCreateResponse> SendChat(OpenAIChatCompletionCreateRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? response = null;

            try
            {
                response = await _api.ChatCompletionsStreamAsync(request, cancellationToken);
            }
            catch (Exception)
            {
                if (response != null)
                    _logger.Error("ChatCompletionStreamAsync: {response}", JsonSerializer.Serialize(response));
                throw;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                line = line.RemoveIfStartWith("data: ");

                if (line.StartsWith("[DONE]"))
                {
                    break;
                }

                OpenAIChatCompletionCreateResponse? block;
                try
                {
                    _logger.Debug("ChatCompletionStreamAsync: {value}", line);
                    block = JsonSerializer.Deserialize<OpenAIChatCompletionCreateResponse>(line);
                }
                catch (Exception)
                {
                    line += await reader.ReadToEndAsync();
                    _logger.Error("ChatCompletionStreamAsync-All: {value}", line);
                    block = JsonSerializer.Deserialize<OpenAIChatCompletionCreateResponse>(line);
                }

                if (null != block)
                {
                    yield return block;
                }
            }
        }
    }
}
