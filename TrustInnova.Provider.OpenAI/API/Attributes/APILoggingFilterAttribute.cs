using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApiClientCore.Attributes;
using WebApiClientCore;

namespace TrustInnova.Provider.OpenAI.API
{
    internal class APILoggingFilterAttribute : LoggingFilterAttribute
    {
        private readonly ILogger _logger;

        public APILoggingFilterAttribute()
        {
            _logger = Log.ForContext<APILoggingFilterAttribute>();
        }

        protected override Task WriteLogAsync(ApiResponseContext context, LogMessage logMessage)
        {
            _logger.Debug(logMessage.ToIndentedString(spaceCount: 4));
            return Task.CompletedTask;
        }
    }
}
