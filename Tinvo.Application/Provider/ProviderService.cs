using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Abstractions.ASR;
using Tinvo.Abstractions.ImageAnalysis;
using Tinvo.Abstractions.ImageGeneration;
using Tinvo.Abstractions.KBS;
using Tinvo.Abstractions.MCP;
using Tinvo.Abstractions.TTS;
using Tinvo.Application.Utils;

namespace Tinvo.Application.Provider
{
    public class ProviderTaskInfo
    {
        public string ID { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Type Type { get; set; } = null!;

        public override string ToString()
        {
            return Name;
        }
    }

    public class ProviderTaskParameterMetadata
    {
        private readonly IServiceProvider _serviceProvider;

        public ProviderTaskParameterMetadata(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ProviderTaskInfo ProviderTask { get; set; } = null!;

        public List<TypeMetadata>? Parameters { get; set; }

        public List<Dictionary<string, object?>> GetAllParameterInstanceMetadata()
        {
            return Parameters?.Select(x => new Dictionary<string, object?>()
            {
                { x.ParameterName ?? x.Name, x.ToInstanceMetadata() }
            }).ToList() ?? [];
        }

        public void LoadAllParameterInstanceMetadata(List<Dictionary<string, JsonElement?>> data)
        {
            if (Parameters == null)
                return;
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (i >= data.Count)
                    return;
                Parameters[i].LoadInstanceMetadata(data[i].Values.First());
            }
        }

        public object? Instance(List<Dictionary<string, JsonElement?>> parameters)
        {
            var paramDict = parameters.SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);

            // Get the constructor with the most parameters
            ConstructorInfo? ctor = ProviderTask.Type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctor == null)
            {
                throw new InvalidOperationException("No suitable constructor found for type " + ProviderTask.Type.FullName);
            }

            // Get the parameters for the constructor
            ParameterInfo[] ctorParams = ctor.GetParameters();
            object?[] args = new object?[ctorParams.Length];

            for (int i = 0; i < ctorParams.Length; i++)
            {
                var param = ctorParams[i];
                if (paramDict.TryGetValue(param.Name!, out JsonElement? jsonElement) && jsonElement.HasValue)
                {
                    args[i] = JsonSerializer.Deserialize(jsonElement.Value.GetRawText(), param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    var service = _serviceProvider.GetService(param.ParameterType);
                    if (service != null)
                        args[i] = service;
                    else
                        throw new ArgumentException($"Missing parameter '{param.Name}' for constructor of type {ProviderTask.Type.FullName}");
                }
            }

            return ctor.Invoke(args);
        }
    }

    public class ProviderService
    {
        private readonly ProviderRegisterer _providerRegisterer;
        private readonly IServiceProvider _serviceProvider;

        public IServiceProvider ServiceProvider => _serviceProvider;

        public ProviderService(ProviderRegisterer providerRegisterer, IServiceProvider serviceProvider)
        {
            _providerRegisterer = providerRegisterer;
            _serviceProvider = serviceProvider;
        }

        public List<ProviderTaskInfo> GetAllProviderTasks()
        {
            return _providerRegisterer.Providers.SelectMany(x => x.AllTaskType)
                .Select(x => new ProviderTaskInfo()
                {
                    ID = x.GetCustomAttribute<ProviderTaskAttribute>()!.ID,
                    Name = x.GetCustomAttribute<ProviderTaskAttribute>()!.Name,
                    Type = x
                }).ToList();
        }

        public List<ProviderTaskInfo> GetProviderTasks<T>()
        {
            return _providerRegisterer.Providers.SelectMany(x => x.AllTaskType).Where(x => typeof(T).IsAssignableFrom(x))
                .Select(x => new ProviderTaskInfo()
                {
                    ID = x.GetCustomAttribute<ProviderTaskAttribute>()!.ID,
                    Name = x.GetCustomAttribute<ProviderTaskAttribute>()!.Name,
                    Type = x
                }).ToList();
        }

        public List<ProviderTaskInfo> GetTTSList()
        {
            return GetProviderTasks<ITTSTask>();
        }

        public List<ProviderTaskInfo> GetASRList()
        {
            return GetProviderTasks<IASRTask>();
        }

        public List<ProviderTaskInfo> GetChatList()
        {
            return GetProviderTasks<IAIChatTask>();
        }

        public List<ProviderTaskInfo> GetImageGenerationList()
        {
            return GetProviderTasks<IImageGenerationTask>();
        }

        public List<ProviderTaskInfo> GetImageAnalysisList()
        {
            return GetProviderTasks<IImageAnalysisTask>();
        }

        public List<ProviderTaskInfo> GetKBSList()
        {
            return GetProviderTasks<IKBSTask>();
        }

        public List<ProviderTaskInfo> GetMCPList()
        {
            return GetProviderTasks<IMCPService>();
        }

        public List<ProviderTaskInfo> GetTokenCalcList()
        {
            return GetProviderTasks<ITokenCalcTask>();
        }

        public List<Type> GetProviderTaskConfigs()
        {
            return _providerRegisterer.Providers.SelectMany(x => x.AllTaskConfigType).ToList();
        }

        public IEnumerable<ProviderTaskParameterMetadata> GetAllProviderTaskParameterMetadata()
        {
            var tasks = GetAllProviderTasks();
            var taskConfigs = GetProviderTaskConfigs();
            return tasks.Select(x =>
            {
                return new ProviderTaskParameterMetadata(ServiceProvider)
                {
                    ProviderTask = x,
                    Parameters = x.Type.GetConstructors().FirstOrDefault()?.GetParameters()
                                    .Where(x2 => taskConfigs.Contains(x2.ParameterType) || TypeMetadataExtend.CheckTypeIsBaseType(x2.ParameterType))
                                    .Select(x3 => x3.GetMetadata()).OrderBy(o => o.AllowNull).ToList()
                };
            });
        }

        public ProviderTaskParameterMetadata? GetProviderTaskParameterMetadataById(string id)
        {
            var task = GetAllProviderTasks().FirstOrDefault(x => x.ID == id);
            if (task == null)
                return null;
            var taskConfigs = GetProviderTaskConfigs();
            return new ProviderTaskParameterMetadata(ServiceProvider)
            {
                ProviderTask = task,
                Parameters = task.Type.GetConstructors().FirstOrDefault()?.GetParameters()
                                .Where(x2 => taskConfigs.Contains(x2.ParameterType) || TypeMetadataExtend.CheckTypeIsBaseType(x2.ParameterType))
                                .Select(x3 => x3.GetMetadata()).OrderBy(o => o.AllowNull).ToList()
            };
        }
    }
}
