using Dapper;
using Npgsql;
using System.Text.Json;
using System.Reflection;
using MadokaLiteBlog.Api.Models;

namespace MadokaLiteBlog.Api.Mapper;
public abstract class BaseMapper<T> where T : class
{
    protected readonly NpgsqlConnection _dbContext;

    protected BaseMapper(NpgsqlConnection dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 是否应该序列化为 JSONB 类型
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    protected virtual bool ShouldSerializeAsJson(PropertyInfo property)
    {
        // 1. 显式标记了 JsonbAttribute 的属性
        if (property.GetCustomAttributes(typeof(JsonbAttribute), false).Length > 0)
            return true;

        var type = property.PropertyType;

        // 2. 集合类型
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return true;

        // 3. 复杂类型（不是基本类型且不是字符串）
        if (!type.IsPrimitive && type != typeof(string) && type != typeof(DateTime) 
            && type != typeof(decimal) && !type.IsEnum)
        {
            // 如果是自定义类型，检查是否继承自 BaseEntity
            if (typeof(BaseEntity).IsAssignableFrom(type))
            {
                // BaseEntity 的子类使用 ID 引用
                return false;
            }
            return true;
        }

        return false;
    }

    protected virtual async Task<T?> MapFromReader(IDictionary<string, object> data)
    {
        var entity = Activator.CreateInstance<T>();
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (!data.ContainsKey(prop.Name)) continue;

            var value = data[prop.Name];
            if (value == null || value is DBNull) continue;

            if (ShouldSerializeAsJson(prop))
            {
                if (value is string json)
                {
                    var deserializedValue = JsonSerializer.Deserialize(json, prop.PropertyType);
                    prop.SetValue(entity, deserializedValue);
                }
            }
            else if (typeof(BaseEntity).IsAssignableFrom(prop.PropertyType))
            {
                // 如果是 BaseEntity 的子类，通过 ID 加载实体
                if (value is long id)
                {
                    var entityType = prop.PropertyType;
                    var mapperType = typeof(BaseMapper<>).MakeGenericType(entityType);
                    var mapper = Activator.CreateInstance(mapperType, _dbContext);
                    
                    if (mapper != null)
                    {
                        var method = mapperType.GetMethod("GetByIdAsync");
                        var task = method?.Invoke(mapper, new object[] { id }) as Task;
                        if (task != null)
                        {
                            await task.ConfigureAwait(false);
                            var resultProperty = task.GetType().GetProperty("Result");
                            var result = resultProperty?.GetValue(task);
                            prop.SetValue(entity, result);
                        }
                    }
                }
            }
            else
            {
                // 基本类型直接设置
                prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
            }
        }

        return entity;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var result = new List<T>();
        var query = $"SELECT * FROM \"{typeof(T).Name}\"";
        
        using var reader = await _dbContext.ExecuteReaderAsync(query);
        var parser = reader.GetRowParser<dynamic>();
        
        while (await reader.ReadAsync())
        {
            if (parser(reader) is IDictionary<string, object> data)
            {
                var entity = await MapFromReader(data);
                if (entity != null)
                {
                    result.Add(entity);
                }
            }
        }
        return result;
    }
    public async Task<IEnumerable<T>> GetAllAsync(int page, int pageSize)
    {
        if (page <= 0 || pageSize <= 0)
        {
            throw new ArgumentException("Page and pageSize must be greater than 0");
        }
        // 利用泛型
        var entityType = typeof(T);
        var tableName = entityType.GetCustomAttribute<TableAttribute>()?.Name ?? entityType.Name;
        var keyColumn = entityType.GetProperties().FirstOrDefault(
            p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0)
            ?.Name;
        var offset = (page - 1) * pageSize;
        var query = $@"
            SELECT * FROM ""{tableName}""
            ORDER BY ""{keyColumn}"" ASC
            LIMIT {pageSize} OFFSET {offset}
        ";
        var result = new List<T>();

        using var reader = await _dbContext.ExecuteReaderAsync(query);
        var parser = reader.GetRowParser<dynamic>();
        
        while (await reader.ReadAsync())
        {
            if (parser(reader) is IDictionary<string, object> data)
            {
                var entity = await MapFromReader(data);
                if (entity != null)
                {
                    result.Add(entity);
                }
            }
        }
        return result;
    }
    public async Task<T?> GetByIdAsync(object id)
    {
        return await _dbContext.QueryFirstOrDefaultAsync<T>($"SELECT * FROM \"{typeof(T).Name}\" WHERE Id = @Id", new { Id = id });
    }
    public async Task<T?> GetByPropertyAsync(object value)
    {
        var keyProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)))
            ?? throw new Exception("类中没有KeyAttribute属性");
        var keyName = keyProperty.GetCustomAttribute<KeyAttribute>()?.Name 
            ?? keyProperty.Name;

        var keyValue = keyProperty.GetValue(value) ?? throw new Exception("Key 属性值不能为空");    
        var tableName = typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name;

        var query = $"SELECT * FROM \"{tableName}\" WHERE \"{keyName}\" = @Value";
        using var reader = await _dbContext.ExecuteReaderAsync(query, new { Value = keyValue });
        
        if (await reader.ReadAsync())
        {
            var parser = reader.GetRowParser<dynamic>();
            if (parser(reader) is IDictionary<string, object> data)
            {
                return await MapFromReader(data);
            }
        }

        return null;
    }
    public async Task<int> InsertAsync(T entity)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length == 0);

        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new DynamicParameters();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(entity);
            if (value == null) continue;

            columns.Add($"\"{prop.Name}\"");

            if (ShouldSerializeAsJson(prop))
            {
                values.Add($"@{prop.Name}::jsonb");
                parameters.Add(prop.Name, JsonSerializer.Serialize(value, JsonSerializerOptions.Default));
            }
            else if (typeof(BaseEntity).IsAssignableFrom(prop.PropertyType))
            {
                // 如果是 BaseEntity 的子类，只存储 ID
                var idProp = prop.PropertyType.GetProperty("Id");
                if (idProp != null)
                {
                    values.Add($"@{prop.Name}");
                    parameters.Add(prop.Name, idProp.GetValue(value));
                }
            }
            else
            {
                values.Add($"@{prop.Name}");
                parameters.Add(prop.Name, value);
            }
        }

        var tableAttribute = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault(); 
        var tableName = tableAttribute != null ? (tableAttribute as TableAttribute)?.Name : typeof(T).Name;

        var sql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)}) RETURNING \"Id\"";

        return await _dbContext.ExecuteScalarAsync<int>(sql, parameters);
    }
    /// <summary>
    /// 更新实体        
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<int> UpdateAsync(T entity)
    {
        var properties = typeof(T).GetProperties();
        var keyProperty = properties.FirstOrDefault(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0);
        var keyValue = (keyProperty?.GetValue(entity)) ?? throw new Exception("Key 属性值不能为空");
        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new DynamicParameters();

        foreach (var prop in properties)
        {
            if (prop.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0) continue;
            var value = prop.GetValue(entity);
            if (value == null) continue;

            columns.Add($"\"{prop.Name}\"");

            if (ShouldSerializeAsJson(prop))
            {
                values.Add($"@{prop.Name}::jsonb");
                parameters.Add(prop.Name, JsonSerializer.Serialize(value, JsonSerializerOptions.Default));
            }
            else if (typeof(BaseEntity).IsAssignableFrom(prop.PropertyType))
            {
                // 如果是 BaseEntity 的子类，只存储 ID
                var idProp = prop.PropertyType.GetProperty("Id");
                if (idProp != null)
                {
                    values.Add($"@{prop.Name}");
                    parameters.Add(prop.Name, idProp.GetValue(value));
                }
            }
            else
            {
                values.Add($"@{prop.Name}");
                parameters.Add(prop.Name, value);
            }
        }

        var tableAttribute = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault(); 
        var tableName = tableAttribute != null ? (tableAttribute as TableAttribute)?.Name : typeof(T).Name;

        var sql = $"UPDATE \"{tableName}\" SET {string.Join(", ", columns)} WHERE Id = {keyValue}";
        return await _dbContext.ExecuteAsync(sql, parameters);
    }
    public async Task<int> DeleteAsync(object id)
    {
        var tableAttribute = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault(); 
        var tableName = tableAttribute != null ? (tableAttribute as TableAttribute)?.Name : typeof(T).Name;
        return await _dbContext.ExecuteAsync($"DELETE FROM \"{tableName}\" WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 验证标识符
    /// </summary>
    private static string ValidateIdentifier(string identifier)
    {
        // 只允许字母、数字和下划线
        if (!System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[a-zA-Z0-9_]+$"))
        {
            throw new ArgumentException($"Invalid identifier: {identifier}");
        }
        return identifier;
    }
}
