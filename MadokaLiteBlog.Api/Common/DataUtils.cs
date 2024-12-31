using System.Reflection;
namespace MadokaLiteBlog.Api.Common;

public class DataUtils
{
    /// <summary>
    ///  适用于Postgres的映射规则   
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    public static string GetSqlType(PropertyInfo property)
    {
        if (property.GetCustomAttributes(typeof(JsonbAttribute), false).Any())
        {
            return "JSONB";
        }
        // 获取实际类型
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (IsEnumerableType(type))
        {
            var elementType = GetElementType(type);
            if (elementType != null)
            {
                var elementSqlType = GetBasicSqlType(elementType);
                if (elementSqlType != "JSONB")
                {
                    return $"{elementSqlType}[]";
                }
            }
        }
        return GetBasicSqlType(type);
    }
    private static bool IsEnumerableType(Type type)
    {
        return type != typeof(string)
            && (type.IsArray
                || (type.IsGenericType
                    && type.GetInterfaces()
                        .Any(i => i.IsGenericType
                            && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                )
            );
    }

    private static Type? GetElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }
        if (type.IsGenericType)
        {
            return type.GetGenericArguments().FirstOrDefault();
        }
        return null;
    }
    private static string GetBasicSqlType(Type type)
    {
        if (!type.IsPrimitive 
            && type != typeof(string) 
            && type != typeof(DateTime) 
            && type != typeof(decimal) 
            && !type.IsEnum)
        {
            return "JSONB";
        }

        return type switch
        {
            Type t when t == typeof(long) || t == typeof(ulong) => "BIGINT",
            Type t when t == typeof(string) => "TEXT",
            Type t when t == typeof(DateTime) => "TIMESTAMP",
            Type t when t == typeof(DateTimeOffset) => "TIMESTAMPTZ",
            Type t when t == typeof(bool) => "BOOLEAN",
            Type t when t == typeof(int) || t == typeof(uint) => "INTEGER",
            Type t when t == typeof(decimal) => "NUMERIC",
            Type t when t == typeof(double) => "DOUBLE PRECISION",
            Type t when t == typeof(float) => "REAL",
            Type t when t == typeof(Guid) => "UUID",
            Type t when t == typeof(byte[]) => "BYTEA",
            Type t when t == typeof(DateOnly) => "DATE",
            Type t when t == typeof(TimeOnly) => "TIME",
            Type t when t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) => "SMALLINT",
            Type t when t == typeof(char) => "CHAR",
            _ => "JSONB"
        };
    }
}
