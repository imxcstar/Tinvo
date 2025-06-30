using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Abstractions
{
    public static class JsonPropertyTypeConvert
    {
        private const string String = "string";
        private const string Bool = "boolean";
        private const string Single = "number";
        private const string Double = "number";
        private const string Int16 = "integer";
        private const string Int32 = "integer";
        private const string Int64 = "integer";
        private const string Byte = "integer";
        private const string Array = "array";
        private const string Object = "object";

        public static string ToJsonTypeString(this Type type)
        {
            if (type == typeof(string))
                return String;
            else if (type == typeof(bool))
                return Bool;
            else if (type == typeof(float))
                return Single;
            else if (type == typeof(double))
                return Double;
            else if (type == typeof(short))
                return Int16;
            else if (type == typeof(int))
                return Int32;
            else if (type == typeof(long))
                return Int64;
            else if (type == typeof(byte))
                return Byte;
            else if (type.GetInterface(typeof(IEnumerable<>).FullName!) != null)
                return Array;
            else
                return Object;
        }
    }

    public class FunctionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("parameters")]
        public FunctionParametersInfo Parameters { get; set; } = null!;
    }

    public class FunctionParametersInfo
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, FunctionParametersProperties> Properties { get; set; } =
            new Dictionary<string, FunctionParametersProperties>();

        [JsonPropertyName("required")]
        public List<string> Required { get; set; } = new List<string>();
    }

    public class FunctionParametersProperties
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";

        [JsonIgnore]
        public Type? RawType { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("enum")]
        public List<string>? Enum { get; set; }

        [JsonPropertyName("default")]
        public object? Default { get; set; }
    }

    public class FunctionMetaInfo
    {
        public string Name { get; set; } = null!;
        public string? CustomName { get; set; }
        public Type? SourceCls { get; set; }
        public object?[]? SourceArgs { get; set; }
        public MethodInfo? MethodInfo { get; set; }
        public FunctionInfo FunctionInfo { get; set; } = null!;
        public bool IsCustomFunction { get; set; } = false;

        public IAIChatHandleMessage? Call(object?[]? parameters)
        {
            if (IsCustomFunction)
                throw new NotSupportedException("Not Support Custom Function Call");

            var instance = Activator.CreateInstance(SourceCls!, SourceArgs) ?? throw new NotImplementedException();
            var methodParams = MethodInfo!.GetParameters();

            if (parameters == null || parameters.Length == 0)
            {
                return MethodInfo.Invoke(instance, null) as IAIChatHandleMessage;
            }

            var convertedParams = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var expectedType = methodParams[i].ParameterType;
                var paramValue = parameters[i];

                if (paramValue == null)
                {
                    convertedParams[i] = null;
                }
                else if (expectedType.IsInstanceOfType(paramValue))
                {
                    convertedParams[i] = paramValue;
                }
                else if (paramValue is JsonElement jsonElement)
                {
                    convertedParams[i] = JsonSerializer.Deserialize(jsonElement.GetRawText(), expectedType);
                }
                else
                {
                    convertedParams[i] = Convert.ChangeType(paramValue, expectedType);
                }
            }

            return MethodInfo.Invoke(instance, convertedParams) as IAIChatHandleMessage;
        }
    }

    public class DefaultFunctionManager : IFunctionManager
    {
        public Dictionary<string, FunctionMetaInfo> _functions;

        public Dictionary<string, FunctionMetaInfo> Functions => _functions;

        private object? _options;
        public object? Options => _options;

        public DefaultFunctionManager(object? options = null)
        {
            _functions = new Dictionary<string, FunctionMetaInfo>();
            _options = options;
        }

        public void AddFunction(Type cls, string name, string? customName = null, object?[]? clsArgs = null)
        {
            var function = cls.GetMethod(name) ?? throw new Exception($"function \"{name}\" not found");
            var desc =
                (function.GetCustomAttributes(typeof(DescriptionAttribute), false)?.FirstOrDefault() as
                    DescriptionAttribute)?.Description ?? "";
            var properties = function.GetParameters().Where(x => !string.IsNullOrWhiteSpace(x.Name));
            var info = new FunctionMetaInfo()
            {
                Name = name,
                CustomName = customName,
                SourceCls = cls,
                SourceArgs = clsArgs,
                MethodInfo = function,
                FunctionInfo = new FunctionInfo()
                {
                    Name = customName ?? name,
                    Description = desc,
                    Type = function.ReturnType.ToJsonTypeString(),
                    Parameters = new FunctionParametersInfo()
                    {
                        Type = function.ReturnType.ToJsonTypeString(),
                        Properties = properties.ToDictionary(x => x.Name!, x => new FunctionParametersProperties()
                        {
                            Description =
                                (x.GetCustomAttributes(typeof(DescriptionAttribute), false)?.FirstOrDefault() as
                                    DescriptionAttribute)?.Description ?? "",
                            Type = x.ParameterType.ToJsonTypeString(),
                            RawType = x.ParameterType,
                            Enum = x.ParameterType.IsEnum
                                ? x.ParameterType.GetEnumValues().OfType<string>().ToList()
                                : new List<string>()
                        }),
                        Required = properties.Where(x => x.GetCustomAttribute(typeof(RequiredAttribute)) != null)
                            .Select(x => x.Name!).ToList()
                    }
                }
            };
            _functions.Add(customName ?? name, info);
        }

        public void AddCustomFunction(string name, string desc, string type, FunctionParametersInfo parameters,
            object?[]? clsArgs = null)
        {
            var info = new FunctionMetaInfo()
            {
                Name = name,
                CustomName = name,
                SourceArgs = clsArgs,
                FunctionInfo = new FunctionInfo()
                {
                    Name = name,
                    Description = desc,
                    Type = type,
                    Parameters = parameters
                }
            };
            _functions.Add(name, info);
        }

        public FunctionMetaInfo GetFnctionMetaInfo(string name)
        {
            if (!_functions.TryGetValue(name, out var func))
                throw new InvalidOperationException();
            return func;
        }

        public virtual List<FunctionInfo> GetFunctionInfos()
        {
            return _functions.Values.Select(x => x.FunctionInfo).ToList();
        }

        public virtual async IAsyncEnumerable<IAIChatHandleMessage> CallFunctionAsync(
            string name,
            Dictionary<string, object?>? parameters,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var function = GetFnctionMetaInfo(name);
            var methodParams = function.MethodInfo!.GetParameters();

            var orderedParams = methodParams.Select(p =>
                parameters != null && parameters.TryGetValue(p.Name!, out var value) ? value : GetDefault(p.ParameterType)
            ).ToArray();

            var ret = function.Call(orderedParams) ?? new AIProviderHandleTextMessageResponse()
            {
                Message = "调用成功"
            };

            yield return ret;
        }

        private static object? GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }

    public class MultiFunctionManager : IFunctionManager
    {
        private List<IFunctionManager> _functionManagers;
        private Dictionary<IFunctionManager, List<FunctionInfo>> _functionManagerInfos;

        public MultiFunctionManager(List<IFunctionManager> functionManagers)
        {
            _functionManagers = functionManagers;
            _functionManagerInfos = _functionManagers.Distinct().ToDictionary(x => x, x => x.GetFunctionInfos());
        }

        public IAsyncEnumerable<IAIChatHandleMessage> CallFunctionAsync(string name, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default)
        {

            foreach (var functionManagerInfo in _functionManagerInfos)
            {
                if (functionManagerInfo.Value.Any(x => x.Name == name))
                {
                    return functionManagerInfo.Key.CallFunctionAsync(name, parameters);
                }
            }
            throw new InvalidOperationException();
        }

        public List<FunctionInfo> GetFunctionInfos()
        {
            return _functionManagerInfos.Values.SelectMany(x => x).ToList();
        }
    }
}