using OpenAI.Chat;
using OpenAI.Responses;
using Serilog;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.DataStorage;

namespace Tinvo.Provider.OpenAI.AIScheduler
{
    public class OpenAIProviderResponsetParser : IAIChatParser
    {
        private readonly IDataStorageService _storageService;
        private string _handleFunctionName = "";
        private StringBuilder _functionContentBuilder = new();
        private Serilog.ILogger _logger;
        private bool _isInReasoning = false;
        private bool _isThinkHandle = false;

        public OpenAIProviderResponsetParser(IDataStorageService storageService, bool isThinkHandle)
        {
            _storageService = storageService;
            _logger = Log.ForContext<OpenAIProviderParser>();
            _isThinkHandle = isThinkHandle;
        }

        public void ResetHandleState()
        {
        }

        public async IAsyncEnumerable<IAIChatHandleMessage> Handle(object msg, IFunctionManager? functionManager)
        {
            if (msg is ClientResult<OpenAIResponse> tmsg)
            {
                foreach (var item in tmsg.Value.OutputItems)
                {
                    if (item is MessageResponseItem message)
                    {
                    }
                }
            }
            yield return new AIProviderHandleTextMessageResponse() { Message = "" };
        }
    }
}
