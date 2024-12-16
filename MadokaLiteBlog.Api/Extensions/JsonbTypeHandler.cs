using System.Data;
using Dapper;
using System.Text.Json;
using Npgsql;

namespace MadokaLiteBlog.Api.Mapper;

public class JsonbTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override T Parse(object? value)
    {
        if (value == null || value is DBNull)
        {
            // 如果是集合类型，返回空集合
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
            {
                var listType = typeof(T).GetGenericArguments()[0];
                var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                return (T)list!;
            }
            return default!;
        }

        // 如果是普通字符串且类型就是 string，直接返回
        if (typeof(T) == typeof(string) && value is string strValue)
        {
            return (T)(object)strValue;
        }

        // 处理 JSONB 类型
        if (value is string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                // 如果是空字符串且是集合类型，返回空集合
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = typeof(T).GetGenericArguments()[0];
                    var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                    return (T)list!;
                }
                return default!;
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(json, Options);
                if (result == null && typeof(T).IsGenericType && 
                    typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = typeof(T).GetGenericArguments()[0];
                    var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                    return (T)list!;
                }
                return result ?? default!;
            }
            catch
            {
                // 如果解析失败且是集合类型，返回空集合
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = typeof(T).GetGenericArguments()[0];
                    var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                    return (T)list!;
                }
                return default!;
            }
        }

        return default!;
    }

    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        if (value == null)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        // 如果是字符串类型，直接设置值
        if (typeof(T) == typeof(string))
        {
            parameter.Value = value;
            return;
        }

        // 其他类型序列化为 JSON
        parameter.Value = JsonSerializer.Serialize(value, Options);
        
        // 设置 Npgsql JSONB 类型
        if (parameter is NpgsqlParameter npgsqlParameter)
        {
            npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
        }
    }
} 