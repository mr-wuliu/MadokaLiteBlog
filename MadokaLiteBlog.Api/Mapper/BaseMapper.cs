using Dapper;
using Npgsql;
using System.Text.Json;
using System.Reflection;
using MadokaLiteBlog.Api.Models;
using MadokaLiteBlog.Api.Common;

namespace MadokaLiteBlog.Api.Mapper;
public abstract class BaseMapper<T> where T : class
{
    protected readonly NpgsqlConnection _dbContext;

    protected BaseMapper(NpgsqlConnection dbContext)
    {
        _dbContext = dbContext;
    }

    protected virtual T? MapFromReader(IDictionary<string, object> data)
    {
        var entity = Activator.CreateInstance<T>();
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (!data.ContainsKey(prop.Name)) continue;

            var value = data[prop.Name];
            if (value == null || value is DBNull) continue;

            if (DataUtils.GetSqlType(prop) == "JSONB")
            {
                if (value is string json)
                {
                    var deserializedValue = JsonSerializer.Deserialize(json, prop.PropertyType);
                    prop.SetValue(entity, deserializedValue);
                }
            }
            // TODO: 对于继承于BaseEntity的实体, 在保存/读取的时候, 可以考虑使用ID来获取实体
            else
            {
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var convertedValue = Convert.ChangeType(value, targetType);
                prop.SetValue(entity, convertedValue);
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
                var entity = MapFromReader(data);
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
                var entity = MapFromReader(data);
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
                return MapFromReader(data);
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

            if (DataUtils.GetSqlType(prop) == "JSONB")
            {
                values.Add($"@{prop.Name}::jsonb");
                parameters.Add(prop.Name, JsonSerializer.Serialize(value, JsonSerializerOptions.Default));
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
        var keyProperty = properties.FirstOrDefault(
            p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0)
            ?? throw new Exception("类中没有KeyAttribute属性");
        var keyName = keyProperty.GetCustomAttribute<KeyAttribute>()?.Name 
            ?? keyProperty.Name;
        var keyValue = keyProperty.GetValue(entity) 
            ?? throw new Exception("Key 属性值不能为空");
        
        var parameters = new DynamicParameters();
        var updatePairs = new List<string>();

        foreach (var prop in properties)
        {
            // 跳过主键属性
            if (prop.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0) continue;
            
            var value = prop.GetValue(entity);
            if (value == null) continue;

            string paramName = $"@{prop.Name}";
            
            if (DataUtils.GetSqlType(prop) == "JSONB")
            {
                updatePairs.Add($"\"{prop.Name}\" = {paramName}::jsonb");
                parameters.Add(prop.Name, JsonSerializer.Serialize(value));
            }
            else
            {
                updatePairs.Add($"\"{prop.Name}\" = {paramName}");
                parameters.Add(prop.Name, value);
            }
        }

        var tableName = typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name;
        parameters.Add("Key", keyValue);

        var sql = $@"
            UPDATE ""{tableName}"" 
            SET {string.Join(", ", updatePairs)} 
            WHERE ""{keyName}"" = @Key";

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
