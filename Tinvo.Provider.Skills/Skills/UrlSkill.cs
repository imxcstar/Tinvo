using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.DataStorage;

namespace Tinvo.Provider.Skills
{
    public class UrlSkill
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly IDataStorageService _dataStorageService;

        public UrlSkill(HttpClient httpClient, IDataStorageService dataStorageService)
        {
            _logger = Log.ForContext<UrlSkill>();
            _httpClient = httpClient;
            _dataStorageService = dataStorageService;
        }

        [Description("下载指定URL的文本内容")]
        public async Task<IAIChatHandleMessage> DownloadTextAsync(
            [Description("URL地址"), Required] string url)
        {
            _logger.Debug("AIUrlUtilsSkill: DownloadTextAsync {Url}", url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                return new AIProviderHandleTextMessageResponse()
                {
                    Message = content
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下载URL文本内容失败: {Url}", url);
                return new AIProviderHandleTextMessageResponse()
                {
                    Message = $"下载失败: {ex.Message}"
                };
            }
        }

        [Description("下载指定URL的图片内容")]
        public async Task<IAIChatHandleMessage> DownloadImageAsync([Description("URL地址"), Required] string url)
        {
            _logger.Debug("AIUrlUtilsSkill: DownloadImageAsync {Url}", url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStreamAsync();
                var fileCustomId = Guid.NewGuid().ToString();
                await _dataStorageService.SetItemAsStreamAsync(fileCustomId, content);

                return new AIProviderHandleCustomFileMessageResponse()
                {
                    Type = AIChatHandleMessageType.ImageMessage,
                    FileCustomID = fileCustomId,
                    FileOriginalURL = url
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下载URL图片内容失败: {Url}", url);
                return new AIProviderHandleTextMessageResponse()
                {
                    Message = $"下载失败: {ex.Message}"
                };
            }
        }

        [Description("检查URL是否可用")]
        public async Task<IAIChatHandleMessage> CheckUrlAvailableAsync(
            [Description("URL地址"), Required] string url)
        {
            _logger.Debug("AIUrlUtilsSkill: CheckUrlAvailableAsync {Url}", url);
            try
            {
                var response = await _httpClient.GetAsync(url);
                bool isAvailable = response.IsSuccessStatusCode;

                return new AIProviderHandleTextMessageResponse()
                {
                    Message = isAvailable ? "可访问" : $"不可访问: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检测URL可用性失败: {Url}", url);
                return new AIProviderHandleTextMessageResponse()
                {
                    Message = $"检测失败: {ex.Message}"
                };
            }
        }

        [Description("获取URL的响应头")]
        public async Task<IAIChatHandleMessage> GetUrlHeadersAsync(
            [Description("URL地址"), Required] string url)
        {
            _logger.Debug("AIUrlUtilsSkill: GetUrlHeadersAsync {Url}", url);
            try
            {
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                response.EnsureSuccessStatusCode();
                var headers = string.Join(Environment.NewLine, response.Headers);

                return new AIProviderHandleTextMessageResponse()
                {
                    Message = headers
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取URL头信息失败: {Url}", url);
                return new AIProviderHandleTextMessageResponse()
                {
                    Message = $"获取失败: {ex.Message}"
                };
            }
        }
    }
}
