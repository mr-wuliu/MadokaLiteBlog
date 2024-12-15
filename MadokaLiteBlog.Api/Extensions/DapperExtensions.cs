using System.Reflection;
using Dapper;
using MadokaLiteBlog.Api.Data;

namespace MadokaLiteBlog.Api.Extensions;

public static class DapperExtensions
{
    public static void RegisterJsonbTypeHandlers(this Assembly assembly)
    {
        // 获取所有类型
        var types = assembly.GetTypes();
        
        // 获取所有带有 JsonbAttribute 的属性的类型
        var properties = types
            .SelectMany(t => t.GetProperties())
            .Where(p => p.GetCustomAttributes(typeof(JsonbAttribute), false).Length > 0);

        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;
            
            // 如果是泛型类型（如 List<T>），注册泛型类型处理器
            if (propertyType.IsGenericType)
            {
                var genericTypeDefinition = propertyType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(List<>))
                {
                    // 为 List<T> 注册处理器
                    var handlerType = typeof(JsonbTypeHandler<>).MakeGenericType(propertyType);
                    var handler = Activator.CreateInstance(handlerType);
                    
                    // 使用 SqlMapper.TypeHandler<TValue> 注册
                    var method = typeof(SqlMapper).GetMethods()
                        .First(m => m.Name == "AddTypeHandler" && m.IsGenericMethod)
                        .MakeGenericMethod(propertyType);
                    
                    method.Invoke(null, new[] { handler });
                }
            }
            else
            {
                // 为非泛型类型注册处理器
                var handlerType = typeof(JsonbTypeHandler<>).MakeGenericType(propertyType);
                var handler = Activator.CreateInstance(handlerType);
                SqlMapper.AddTypeHandler(propertyType, (SqlMapper.ITypeHandler)handler!);
            }
        }
    }
} 