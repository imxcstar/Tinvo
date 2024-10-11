using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace TrustInnova.Abstractions
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
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public FunctionParametersInfo Parameters { get; set; } = null!;
    }

    public class FunctionParametersInfo
    {
        public string Type { get; set; } = null!;

        public Dictionary<string, FunctionParametersProperties> Properties { get; set; } = new Dictionary<string, FunctionParametersProperties>();

        public List<string> Required { get; set; } = new List<string>();
    }

    public class FunctionParametersProperties
    {
        public string Type { get; set; } = "string";

        public Type? RawType { get; set; }

        public string Description { get; set; } = null!;

        public List<string> Enum { get; set; } = null!;
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

        public object? Call(object?[]? parameters)
        {
            if (IsCustomFunction)
                throw new NotSupportedException("Not Support Custom Function Call");
            var or = Activator.CreateInstance(SourceCls!, SourceArgs) ?? throw new NotImplementedException();
            return MethodInfo!.Invoke(or, parameters);
        }
    }

    public class FunctionManager : IFunctionManager
    {
        public Dictionary<string, FunctionMetaInfo> _functions;

        public Dictionary<string, FunctionMetaInfo> Functions => _functions;

        public List<FunctionInfo> FunctionInfos => _functions.Values.Select(x => x.FunctionInfo).ToList();

        private object? _options;
        public object? Options => _options;

        public FunctionManager(object? options = null)
        {
            _functions = new Dictionary<string, FunctionMetaInfo>();
            _options = options;
        }

        public void AddFunction(Type cls, string name, string? customName = null, object?[]? clsArgs = null)
        {
            var function = cls.GetMethod(name) ?? throw new Exception($"function \"{name}\" not found");
            var desc = (function.GetCustomAttributes(typeof(DescriptionAttribute), false)?.FirstOrDefault() as DescriptionAttribute)?.Description ?? "";
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
                    Parameters = new FunctionParametersInfo()
                    {
                        Properties = properties.ToDictionary(x => x.Name!, x => new FunctionParametersProperties()
                        {
                            Description = (x.GetCustomAttributes(typeof(DescriptionAttribute), false)?.FirstOrDefault() as DescriptionAttribute)?.Description ?? "",
                            Type = x.ParameterType.ToJsonTypeString(),
                            RawType = x.ParameterType,
                            Enum = x.ParameterType.IsEnum ? x.ParameterType.GetEnumValues().OfType<string>().ToList() : new List<string>()
                        }),
                        Type = function.ReturnType.ToJsonTypeString(),
                        Required = properties.Where(x => x.GetCustomAttribute(typeof(RequiredAttribute)) != null).Select(x => x.Name!).ToList()
                    }
                }
            };
            _functions.Add(customName ?? name, info);
        }

        public void AddCustomFunction(string name, string desc, FunctionParametersInfo parameters, object?[]? clsArgs = null)
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
    }
}
