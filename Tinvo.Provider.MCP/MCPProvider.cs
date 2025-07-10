using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.MCP;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using Tinvo.Abstractions.AIScheduler;
using System.Text.Json;
using Tinvo.Application.DataStorage;

namespace Tinvo.Provider.MCP
{
    [TypeMetadataDisplayName("MCP配置")]
    public class MCPConfig
    {
        [Description("传输类型")]
        [DefaultValue(MCPTransportType.SSE)]
        public MCPTransportType TransportType { get; set; } = MCPTransportType.SSE;

        [Description("超时时间(秒)")]
        [DefaultValue(30)]
        public int Timeout { get; set; } = 30;

        [Description("[SSE]服务地址")]
        [DefaultValue("http://localhost:8080")]
        public string BaseURL { get; set; } = "http://localhost:8080";

        [Description("[SSE]传输模式")]
        [DefaultValue(HttpTransportMode.AutoDetect)]
        public HttpTransportMode TransportMode { get; set; } = HttpTransportMode.AutoDetect;

        [Description("[SSE]Headers参数")]
        [TypeMetadataAllowNull]
        public Dictionary<string, string>? AdditionalHeaders { get; set; } = [];

        [Description("[Stdio]命令")]
        [TypeMetadataAllowNull]
        public string? Command { get; set; }

        [Description("[Stdio]参数")]
        [TypeMetadataAllowNull]
        public List<string>? Arguments { get; set; } = [];

        [Description("[Stdio]环境变量")]
        public Dictionary<string, string?> EnvironmentVariables { get; set; } = [];

        [Description("[Stdio]工作目录")]
        [TypeMetadataAllowNull]
        public string? WorkingDirectory { get; set; }
    }

    public enum MCPTransportType
    {
        SSE,
        Stdio
    }

    [ProviderTask("MCP.Default", "MCP服务")]
    public class MCPProvider : MCPStreamService
    {
        private readonly MCPConfig _config;
        private readonly IDataStorageServiceFactory _dataStorageServiceFactory;

        private IDataStorageService _storageService;

        public override async Task InitAsync()
        {
            _storageService = await _dataStorageServiceFactory.CreateAsync();
        }

        public MCPProvider(IDataStorageServiceFactory storageServiceFactory, MCPConfig config)
        {
            _dataStorageServiceFactory = storageServiceFactory;
            _config = config;
        }

        private FunctionInfo ConvertToMCPTool(McpClientTool tool)
        {
            return new FunctionInfo
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = tool.JsonSchema.Deserialize<FunctionParametersInfo>(tool.JsonSerializerOptions) ?? new FunctionParametersInfo()
            };
        }

        public override async Task<IFunctionManager> GetIFunctionManager(CancellationToken cancellationToken = default)
        {
            IClientTransport transport = _config.TransportType switch
            {
                MCPTransportType.SSE => new SseClientTransport(new SseClientTransportOptions()
                {
                    Name = "MCP",
                    Endpoint = new Uri(_config.BaseURL),
                    ConnectionTimeout = TimeSpan.FromSeconds(_config.Timeout),
                    AdditionalHeaders = _config.AdditionalHeaders,
                    TransportMode = _config.TransportMode
                }),
                MCPTransportType.Stdio => new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "MCP",
                    Command = _config.Command ?? throw new ArgumentException("Command is required for Stdio transport"),
                    Arguments = _config.Arguments?.ToArray(),
                    ShutdownTimeout = TimeSpan.FromSeconds(_config.Timeout),
                    EnvironmentVariables = _config.EnvironmentVariables,
                    WorkingDirectory = _config.WorkingDirectory
                }),
                _ => throw new ArgumentException($"Unsupported transport type: {_config.TransportType}")
            };

            var mcpClient = await McpClientFactory.CreateAsync(transport, cancellationToken: cancellationToken);
            var tools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

            return new MCPFunctionManager(_storageService, mcpClient, tools.Select(ConvertToMCPTool).ToList());
        }
    }

    public class MCPFunctionManager : IFunctionManager
    {
        private readonly IDataStorageService _storageService;
        private IMcpClient _mcpClient;
        private List<FunctionInfo> _functionInfos;

        public MCPFunctionManager(IDataStorageService storageService, IMcpClient mcpClient, List<FunctionInfo> functionInfos)
        {
            _storageService = storageService;
            _mcpClient = mcpClient;
            _functionInfos = functionInfos;
        }

        public async IAsyncEnumerable<IAIChatHandleMessage> CallFunctionAsync(string name, Dictionary<string, object?>? parameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var ret = await _mcpClient.CallToolAsync(name, parameters ?? new Dictionary<string, object?>(), cancellationToken: cancellationToken);
            foreach (var content in ret.Content)
            {
                var customFileID = Guid.NewGuid().ToString();
                switch (content.Type)
                {
                    case "text":
                        yield return new AIProviderHandleTextMessageResponse()
                        {
                            Message = content.Text!
                        };
                        break;
                    case "image":
                        await _storageService.SetItemAsBinaryAsync(customFileID, Convert.FromBase64String(content.Data!), cancellationToken);
                        yield return new AIProviderHandleCustomFileMessageResponse()
                        {
                            Type = AIChatHandleMessageType.ImageMessage,
                            FileCustomID = customFileID,
                        };
                        break;
                    case "audio":
                        await _storageService.SetItemAsBinaryAsync(customFileID, Convert.FromBase64String(content.Data!), cancellationToken);
                        yield return new AIProviderHandleCustomFileMessageResponse()
                        {
                            Type = AIChatHandleMessageType.AudioMessage,
                            FileCustomID = customFileID,
                        };
                        break;
                    case "resource":
                        yield return new AIProviderHandleTextMessageResponse()
                        {
                            Message = content.Resource!.Uri
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        public List<FunctionInfo> GetFunctionInfos()
        {
            return _functionInfos;
        }
    }
}