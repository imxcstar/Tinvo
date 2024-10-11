using Mapster;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TrustInnova.Abstractions;

namespace TrustInnova.Application.Utils
{
    public static class TypeMetadataExtend
    {
        public static TypeMetadata GetMetadata(this Type type)
        {
            var isBaseType = CheckTypeIsBaseType(type);
            return new TypeMetadata()
            {
                ID = type.FullName!,
                Name = type.Name,
                DisplayName = type.GetCustomAttribute<TypeMetadataDisplayNameAttribute>()?.Name,
                DisplayStyles = type.GetCustomAttribute<TypeMetadataDisplayStylesAttribute>()?.Adapt<TypeMetadataDisplayStyles>() ?? new TypeMetadataDisplayStyles(),
                Desc = type.GetCustomAttribute<DescriptionAttribute>()?.Description,
                Type = type,
                IsBaseType = isBaseType,
                IsEnumType = type.IsEnum,
                Value = type.GetCustomAttribute<DefaultValueAttribute>()?.Value ?? (isBaseType ? GetBaseTypeDefaultValue(type) : Activator.CreateInstance(type)),
                SimpleType = GetSimpleType(type),
                AllowNull = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>),
                Enums = type.IsEnum ? EnumToList(type) : null,
                SubTypeMetadatas = isBaseType ? null : type.GetProperties().Select(x4 => new TypeMetadata()
                {
                    ID = $"{type.FullName}@{x4.Name}",
                    Name = x4.Name,
                    DisplayName = x4.Name,
                    DisplayStyles = x4.GetCustomAttribute<TypeMetadataDisplayStylesAttribute>()?.Adapt<TypeMetadataDisplayStyles>() ?? new TypeMetadataDisplayStyles(),
                    Desc = x4.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    Type = x4.PropertyType,
                    IsBaseType = CheckTypeIsBaseType(x4.PropertyType),
                    IsEnumType = x4.PropertyType.IsEnum,
                    Value = x4.GetCustomAttribute<DefaultValueAttribute>()?.Value ?? (CheckTypeIsBaseType(x4.PropertyType) ? GetBaseTypeDefaultValue(x4.PropertyType) : Activator.CreateInstance(x4.PropertyType)),
                    SimpleType = GetSimpleType(x4.PropertyType),
                    AllowNull = x4.GetCustomAttribute<TypeMetadataAllowNullAttribute>() != null,
                    Enums = x4.PropertyType.IsEnum ? EnumToList(x4.PropertyType) : null,
                }).OrderBy(o => o.AllowNull).ToList()
            };
        }

        public static TypeMetadata GetMetadata(this ParameterInfo parameterInfo)
        {
            var type = parameterInfo.ParameterType;
            var isBaseType = CheckTypeIsBaseType(type);
            return new TypeMetadata()
            {
                ID = type.FullName!,
                Name = type.Name,
                ParameterName = parameterInfo.Name,
                DisplayName = type.GetCustomAttribute<TypeMetadataDisplayNameAttribute>()?.Name ?? parameterInfo.GetCustomAttribute<TypeMetadataDisplayNameAttribute>()?.Name,
                DisplayStyles = (type.GetCustomAttribute<TypeMetadataDisplayStylesAttribute>() ?? parameterInfo.GetCustomAttribute<TypeMetadataDisplayStylesAttribute>())?.Adapt<TypeMetadataDisplayStyles>() ?? new TypeMetadataDisplayStyles(),
                Desc = parameterInfo.GetCustomAttribute<DescriptionAttribute>()?.Description,
                Type = type,
                IsBaseType = isBaseType,
                IsEnumType = type.IsEnum,
                Value = parameterInfo.DefaultValue ?? parameterInfo.GetCustomAttribute<DefaultValueAttribute>()?.Value ?? (isBaseType ? GetBaseTypeDefaultValue(type) : Activator.CreateInstance(type)),
                SimpleType = GetSimpleType(type),
                AllowNull = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>),
                Enums = type.IsEnum ? EnumToList(type) : null,
                SubTypeMetadatas = isBaseType ? null : type.GetProperties().Select(x4 => new TypeMetadata()
                {
                    ID = $"{type.FullName}@{x4.Name}",
                    Name = x4.Name,
                    DisplayName = x4.Name,
                    DisplayStyles = x4.GetCustomAttribute<TypeMetadataDisplayStylesAttribute>()?.Adapt<TypeMetadataDisplayStyles>() ?? new TypeMetadataDisplayStyles(),
                    Desc = x4.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    Type = x4.PropertyType,
                    IsBaseType = CheckTypeIsBaseType(x4.PropertyType),
                    IsEnumType = x4.PropertyType.IsEnum,
                    Value = x4.GetCustomAttribute<DefaultValueAttribute>()?.Value ?? (CheckTypeIsBaseType(x4.PropertyType) ? GetBaseTypeDefaultValue(x4.PropertyType) : Activator.CreateInstance(x4.PropertyType)),
                    SimpleType = GetSimpleType(x4.PropertyType),
                    AllowNull = x4.GetCustomAttribute<TypeMetadataAllowNullAttribute>() != null,
                    Enums = x4.PropertyType.IsEnum ? EnumToList(x4.PropertyType) : null,
                }).OrderBy(o => o.AllowNull).ToList()
            };
        }

        public static List<TypeMetadataEnum>? EnumToList(Type enumType)
        {
            if (!enumType.IsEnum)
                return null;

            var values = Enum.GetValues(enumType);
            var result = new List<TypeMetadataEnum>();

            foreach (var value in values)
            {
                result.Add(new TypeMetadataEnum()
                {
                    Name = value.ToString()!,
                    Value = (int)value
                });
            }

            return result;
        }

        public static bool CheckTypeIsBaseType(Type type)
        {
            if (type == typeof(byte))
                return true;
            if (type == typeof(sbyte))
                return true;
            if (type == typeof(Int16))
                return true;
            if (type == typeof(Int32))
                return true;
            if (type == typeof(Int64))
                return true;
            if (type == typeof(Int128))
                return true;
            if (type == typeof(UInt16))
                return true;
            if (type == typeof(UInt32))
                return true;
            if (type == typeof(UInt64))
                return true;
            if (type == typeof(UInt128))
                return true;
            if (type == typeof(float))
                return true;
            if (type == typeof(double))
                return true;
            if (type == typeof(decimal))
                return true;
            if (type == typeof(string))
                return true;
            if (type == typeof(char))
                return true;
            return false;
        }

        public static object GetBaseTypeDefaultValue(Type type)
        {
            if (type == typeof(byte))
                return (byte)0;
            if (type == typeof(sbyte))
                return (sbyte)0;
            if (type == typeof(Int16))
                return (Int16)0;
            if (type == typeof(Int32))
                return (Int32)0;
            if (type == typeof(Int64))
                return (Int64)0;
            if (type == typeof(Int128))
                return (Int128)0;
            if (type == typeof(UInt16))
                return (UInt16)0;
            if (type == typeof(UInt32))
                return (UInt32)0;
            if (type == typeof(UInt64))
                return (UInt64)0;
            if (type == typeof(UInt128))
                return (UInt128)0;
            if (type == typeof(float))
                return (float)0;
            if (type == typeof(double))
                return (double)0;
            if (type == typeof(decimal))
                return (decimal)0;
            return "";
        }

        public static TypeMetadataSimpleType GetSimpleType(Type type)
        {
            if (type.IsEnum)
                return TypeMetadataSimpleType.Enum;
            if (type == typeof(byte))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(sbyte))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(Int16))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(Int32))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(Int64))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(Int128))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(UInt16))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(UInt32))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(UInt64))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(UInt128))
                return TypeMetadataSimpleType.Integer;
            if (type == typeof(float))
                return TypeMetadataSimpleType.Float;
            if (type == typeof(double))
                return TypeMetadataSimpleType.Float;
            if (type == typeof(decimal))
                return TypeMetadataSimpleType.Float;
            if (type == typeof(string))
                return TypeMetadataSimpleType.Text;
            if (type == typeof(char))
                return TypeMetadataSimpleType.Text;
            return TypeMetadataSimpleType.Other;
        }

        public static object ConvertStrToBaseType(Type type, string value)
        {
            if (type == typeof(byte))
                return byte.Parse(value);
            if (type == typeof(sbyte))
                return sbyte.Parse(value);
            if (type == typeof(Int16))
                return Int16.Parse(value);
            if (type == typeof(Int32))
                return Int32.Parse(value);
            if (type == typeof(Int64))
                return Int64.Parse(value);
            if (type == typeof(Int128))
                return Int128.Parse(value);
            if (type == typeof(UInt16))
                return UInt16.Parse(value);
            if (type == typeof(UInt32))
                return UInt32.Parse(value);
            if (type == typeof(UInt64))
                return UInt64.Parse(value);
            if (type == typeof(UInt128))
                return UInt128.Parse(value);
            if (type == typeof(float))
                return float.Parse(value);
            if (type == typeof(double))
                return double.Parse(value);
            if (type == typeof(decimal))
                return decimal.Parse(value);
            if (type == typeof(string))
                return value;
            if (type == typeof(char))
                return char.Parse(value);
            return value;
        }

        public static object? ConvertToBaseTypeValueByJsonValueKind(JsonElement? jsonElement, Type type)
        {
            if (jsonElement == null)
                return null;
            var value = jsonElement.Value;
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    var retStr = value.GetString();
                    if (retStr == null)
                        return null;
                    if (type == typeof(char))
                        return char.Parse(retStr);
                    return value.GetString();
                case JsonValueKind.Number:
                    if (type == typeof(byte))
                        return value.GetByte();
                    if (type == typeof(sbyte))
                        return value.GetSByte();
                    if (type == typeof(Int16))
                        return value.GetInt16();
                    if (type == typeof(Int32))
                        return value.GetInt32();
                    if (type == typeof(Int64))
                        return value.GetInt64();
                    if (type == typeof(UInt16))
                        return value.GetUInt16();
                    if (type == typeof(UInt32))
                        return value.GetUInt32();
                    if (type == typeof(UInt64))
                        return value.GetUInt64();
                    if (type == typeof(float))
                        return value.GetSingle();
                    if (type == typeof(double))
                        return value.GetDouble();
                    if (type == typeof(decimal))
                        return value.GetDecimal();
                    return value.GetInt64();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return value.GetBoolean();
                case JsonValueKind.Null:
                    return null;
                default:
                    throw new NotSupportedException("ConvertToBaseTypeByJsonValueKind NotSupportedException");
            }
        }
    }

    public class TypeMetadataDisplayStyles
    {
        public int LineNumber { get; set; } = 1;
    }

    public class TypeMetadata
    {
        public string ID { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ParameterName { get; set; }
        public string? DisplayName { get; set; }
        public TypeMetadataDisplayStyles DisplayStyles { get; set; } = null!;
        public Type Type { get; set; } = null!;
        public bool IsBaseType { get; set; }
        public bool IsEnumType { get; set; }
        public object? Value { get; set; } = null!;
        public string ValueStr
        {
            get => IsBaseType || Type.IsEnum ? (Value?.ToString() ?? "") : JsonSerializer.Serialize(Value);
            set
            {
                try
                {
                    if (Type.IsEnum)
                        Value = Enum.Parse(Type, value);
                    else if (IsBaseType)
                        Value = TypeMetadataExtend.ConvertStrToBaseType(Type, value);
                    else
                        Value = JsonSerializer.Deserialize(value, Type)!;
                }
                catch (Exception)
                {
                }
            }
        }

        public int ValueEnum
        {
            get => (int)(Value ?? 0);
            set => Value = value;
        }

        public TypeMetadataSimpleType SimpleType { get; set; }
        public string? Desc { get; set; }
        public bool AllowNull { get; set; }
        public List<TypeMetadata>? SubTypeMetadatas { get; set; }
        public List<TypeMetadataEnum>? Enums { get; set; }

        public override string ToString()
        {
            if (IsBaseType && SubTypeMetadatas != null)
                return Desc ?? DisplayName ?? Name;
            else
                return (DisplayName ?? Name) + (Desc == null ? "" : $"({Desc})");
        }

        public object? ToInstanceMetadata()
        {
            object? value;
            if (!IsBaseType)
            {
                value = SubTypeMetadatas?.ToDictionary(x => x.ParameterName ?? x.Name, x => x.Value);
            }
            else
            {
                value = Value;
            }

            return value;
        }

        public void LoadInstanceMetadata(JsonElement? value)
        {
            if (value == null)
            {
                Value = null;
                return;
            }

            if (IsBaseType)
            {
                Value = TypeMetadataExtend.ConvertToBaseTypeValueByJsonValueKind(value, Type);
            }
            else if (IsEnumType)
            {
                ValueEnum = value.Value.GetInt32();
            }
            else if (SubTypeMetadatas != null)
            {
                var cvalues = value.Value.Deserialize<Dictionary<string, JsonElement?>>()!;
                foreach (var subTypeMetadata in SubTypeMetadatas)
                {
                    if (!cvalues.TryGetValue(subTypeMetadata.ParameterName ?? subTypeMetadata.Name, out var cvalue))
                        continue;
                    if (subTypeMetadata.IsBaseType)
                        subTypeMetadata.Value = TypeMetadataExtend.ConvertToBaseTypeValueByJsonValueKind(cvalue, subTypeMetadata.Type);
                    else if (IsEnumType && cvalue != null)
                        subTypeMetadata.ValueEnum = cvalue.Value.GetInt32();
                    else
                        subTypeMetadata.Value = cvalue == null ? 0 : cvalue.Value.Deserialize(subTypeMetadata.Type)!;
                }
            }
        }
    }

    public class TypeMetadataEnum
    {
        public string Name { get; set; } = null!;
        public int Value { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum TypeMetadataSimpleType
    {
        Other,
        Integer,
        Float,
        Text,
        Enum
    }
}
