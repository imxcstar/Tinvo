using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TrustInnova.Abstractions.ASR
{
    public enum ASRTaskAudioType
    {
        PCM,
        MP3,
        M4A,
        WAV,
        AMR,
        AAC
    }

    public enum ASRTaskType
    {
        XunFei,
        Baidu
    }

    public enum ASRFrameType
    {
        FirstFrame = 0,
        ContinueFrame = 1,
        LastFrame = 2
    }

    public interface IASRTask
    {
        public bool EnableStream { get; set; }
        public bool SupportStream { get; }
        public DateTime HeartBeatTime { get; set; }
        public string ID { get; set; }

        public Task CloseAsync();
        public Task ConnectAsync();
        public IAsyncEnumerable<ASRTaskReceiveMessageResponse> ReceiveMessages(CancellationToken cancellationToken = default);
        public Task SendAsync(Stream data, ASRTaskAudioType type, ASRFrameType? frameType = null, Dictionary<string, object>? metadata = null, bool autoSplitFrame = false, CancellationToken cancellationToken = default);
        public IAsyncEnumerable<ASRTaskReceiveMessageResponse> SendAndReceiveMessagesAsync(Stream data, ASRTaskAudioType type, Dictionary<string, object>? metadata = null, bool autoSplitFrame = false, CancellationToken cancellationToken = default);
        public Task<string> SendAndReturnAsync(Stream data, ASRTaskAudioType type, Dictionary<string, object>? metadata = null, bool autoSplitFrame = false, CancellationToken cancellationToken = default);
    }

    public abstract class ASRStreamTask : IASRTask
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

        public virtual async IAsyncEnumerable<ASRTaskReceiveMessageResponse> SendAndReceiveMessagesAsync(Stream data, ASRTaskAudioType type, Dictionary<string, object>? metadata = null, bool autoSplitFrame = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await SendAsync(data, type, null, metadata, autoSplitFrame, cancellationToken);
            await foreach (var item in ReceiveMessages(cancellationToken))
            {
                yield return item;
            }
        }

        public virtual async Task<string> SendAndReturnAsync(Stream data, ASRTaskAudioType type, Dictionary<string, object>? metadata = null, bool autoSplitFrame = false, CancellationToken cancellationToken = default)
        {
            var old = EnableStream;
            EnableStream = false;
            var ret = SendAndReceiveMessagesAsync(data, type, metadata, autoSplitFrame, cancellationToken);
            var retString = new StringBuilder();
            await foreach (var item in ret)
            {
                if (item.Success)
                    retString.Append(item.Msg);
            }
            EnableStream = old;
            return retString.ToString();
        }

        public abstract IAsyncEnumerable<ASRTaskReceiveMessageResponse> ReceiveMessages(CancellationToken cancellationToken = default);

        public abstract Task SendAsync(Stream data, ASRTaskAudioType type, ASRFrameType? frameType = null, Dictionary<string, object>? metadata = null, bool autoSplitFrame = false, CancellationToken cancellationToken = default);
    }

    public class ASRTaskReceiveMessageResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("end")]
        public bool End { get; set; }
    }
}
