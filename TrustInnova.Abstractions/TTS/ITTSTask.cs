using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TrustInnova.Abstractions.TTS
{
    public enum TTSTaskType
    {
        XunFei
    }

    public enum TTSTaskPlayType
    {
        Unknown,
        PCM,
        MP3,
    }

    public interface ITTSTask
    {
        public bool EnableStream { get; set; }
        public bool SupportStream { get; }
        public DateTime HeartBeatTime { get; set; }
        public string ID { get; set; }

        public Task CloseAsync();
        public Task ConnectAsync();

        public IAsyncEnumerable<TTSTaskReceiveMessageResponse> Listening(CancellationToken cancellationToken = default);
        public Task SynthesizeAsync(string value, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
        public IAsyncEnumerable<TTSTaskReceiveMessageResponse> SynthesizeAndListeningAsync(string value, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
        public Task<TTSTaskReceiveMessageResponse> SynthesizeAndReturnAsync(string value, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
    }

    public abstract class TTSStreamTask : ITTSTask
    {
        public virtual bool EnableStream { get; set; } = true;
        public virtual DateTime HeartBeatTime { get; set; } = DateTime.Now;

        public abstract bool SupportStream { get; }

        public virtual string ID { get; set; } = Guid.NewGuid().ToString();

        public virtual Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task ConnectAsync()
        {
            return Task.CompletedTask;
        }

        public virtual async IAsyncEnumerable<TTSTaskReceiveMessageResponse> SynthesizeAndListeningAsync(string value, Dictionary<string, object>? metadata = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await SynthesizeAsync(value, metadata, cancellationToken);
            await foreach (var item in Listening(cancellationToken))
            {
                yield return item;
            }
        }

        public virtual async Task<TTSTaskReceiveMessageResponse> SynthesizeAndReturnAsync(string value, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            var old = EnableStream;
            EnableStream = false;
            var ret = SynthesizeAndListeningAsync(value, metadata, cancellationToken);
            var retBuffer = new List<byte>();
            await foreach (var item in ret)
            {
                if (item.Success && item.Data != null)
                    retBuffer.AddRange(item.Data);
            }
            EnableStream = old;
            return new TTSTaskReceiveMessageResponse()
            {
                Success = true,
                Data = retBuffer.ToArray()
            };
        }

        public abstract IAsyncEnumerable<TTSTaskReceiveMessageResponse> Listening(CancellationToken cancellationToken = default);
        public abstract Task SynthesizeAsync(string value, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
    }

    public class TTSTaskReceiveMessageResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public byte[]? Data { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("end")]
        public bool End { get; set; }
    }
}
