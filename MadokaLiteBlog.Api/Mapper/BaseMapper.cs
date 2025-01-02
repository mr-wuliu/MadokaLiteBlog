using Dapper;
using Npgsql;
using System.Text.Json;
using System.Reflection;
using MadokaLiteBlog.Api.Common;
using System.Linq.Expressions;
using System.Collections;

namespace MadokaLiteBlog.Api.Mapper;
public abstract class BaseMapper<T> where T : class
{
    protected readonly NpgsqlConnection _dbContext;
    protected readonly ILogger<BaseMapper<T>> _logger;
    protected BaseMapper(NpgsqlConnection dbContext, ILogger<BaseMapper<T>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, object>>? orderBy = null, bool isDesc = false)
    {
        var orderByColumn = "";
        if (orderBy != null)
        {
            var body = orderBy.Body;
            if (body is UnaryExpression unaryExpression)
            {
                body = unaryExpression.Operand;
            }
            if (body is MemberExpression memberExpression)
            {
                orderByColumn = memberExpression.Member.Name;
            }
        } else {
            // 默认使用Key排序
            orderByColumn = typeof(T).GetProperties().FirstOrDefault(
                p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0)
                ?.Name;
        }

        var result = new List<T>();
        var query = $@"
            SELECT * FROM ""{typeof(T).Name}""
            ORDER BY ""{orderByColumn}"" {(isDesc ? "DESC" : "ASC")}
        ";
        
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
    public async Task<IEnumerable<T>> GetAllAsync(
        int page, int pageSize,
        Expression<Func<T, object>>? selector = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDesc = false
    )
    {
        if (page <= 0 || pageSize <= 0)
        {
            throw new ArgumentException("Page and pageSize must be greater than 0");
        }
        // 利用泛型
        var entityType = typeof(T);
        var tableName = entityType.GetCustomAttribute<TableAttribute>()?.Name ?? entityType.Name;
        var orderByColumn = "";
        if (orderBy != null)
        {
            var body = orderBy.Body;
            if (body is UnaryExpression unaryExpression) // 处理类型转换的情况
            {
                body = unaryExpression.Operand;
            }
            if (body is MemberExpression memberExpression)
            {
                orderByColumn = memberExpression.Member.Name;
            }
        } else {
            // 默认使用Key排序
            orderByColumn = entityType.GetProperties().FirstOrDefault(
                p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0)
                ?.Name;
        }
        var offset = (page - 1) * pageSize;

        var columns = "*";
        if (selector != null)
        {
            var body = selector.Body;
            if (body is NewExpression newExpression && newExpression.Members != null)
            {
                columns = string.Join(", ", newExpression.Members.Select(m => $"\"{m.Name}\""));
            }
            else if (body is MemberExpression memberExpression)
            {
                columns = $"\"{memberExpression.Member.Name}\"";
            }

        }
        var query = $@"
            SELECT {columns} FROM ""{tableName}""
            ORDER BY ""{orderByColumn}"" {(isDesc ? "DESC" : "ASC")}
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
        var entityType = typeof(T);
        var keyProperty = entityType.GetProperties().FirstOrDefault(
            p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0)
            ?? throw new Exception("类中没有KeyAttribute属性");
        var keyName = keyProperty.GetCustomAttribute<KeyAttribute>()?.Name 
            ?? keyProperty.Name;
        // cast id to KeyType
        var keyvalue = Convert.ChangeType(id, keyProperty.PropertyType);
        return await _dbContext.QueryFirstOrDefaultAsync<T>($"SELECT * FROM \"{typeof(T).Name}\" WHERE \"{keyName}\" = @Key", new { Key = keyvalue });
    }
    /// <summary>
    /// 根据属性值获取实体
    /// </summary>
    /// <param name="value"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    public async Task<IEnumerable<T>> GetByPropertyAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>? selector = null)
    {
        var entityType = typeof(T);
        var tableName = entityType.GetCustomAttribute<TableAttribute>()?.Name ?? entityType.Name;
        
        // 处理选择的列
        var columns = "*";
        if (selector != null)
        {
            var body = selector.Body;
            if (body is NewExpression newExpression && newExpression.Members != null)
            {
                columns = string.Join(", ", newExpression.Members.Select(m => $"\"{m.Name}\""));
            }
            else if (body is MemberExpression memberExpression)
            {
                columns = $"\"{memberExpression.Member.Name}\"";
            }
        }

        var whereClause = TranslatePredicateToSql(predicate);
        var parameters = new DynamicParameters();
        
        var query = $@"
            SELECT {columns} 
            FROM ""{tableName}""
            WHERE {whereClause.Sql}
        ";

        var result = new List<T>();
        using var reader = await _dbContext.ExecuteReaderAsync(query, whereClause.Parameters);
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
    public async Task<long> InsertAsync(T entity)
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

        return await _dbContext.ExecuteScalarAsync<long>(sql, parameters);
    }
    /// <summary>
    /// 更新实体        
    /// </summary>
    /// <param name="entity"></param>
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
        // !! FIXME
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
            else
            {
                if (value?.GetType().IsArray == true && prop.PropertyType.IsGenericType)
                {
                // 处理数组到List的转换
                    var elementType = prop.PropertyType.GetGenericArguments()[0];
                    var array = (Array)value;
                    var list = Array.ConvertAll(array.Cast<object>().ToArray(), 
                        item => Convert.ChangeType(item, elementType));
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var typedList = Activator.CreateInstance(listType);
                    foreach (var item in list)
                    {
                        ((IList)typedList!).Add(item);
                    }
                    prop.SetValue(entity, typedList);
                    continue;
                }
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var convertedValue = Convert.ChangeType(value, targetType);
                prop.SetValue(entity, convertedValue);
            }
        }

        return entity;
    }
    private class SqlWhereClause
    {
        public string Sql { get; set; } = string.Empty;
        public DynamicParameters Parameters { get; set; } = new DynamicParameters();
    }

    private SqlWhereClause TranslatePredicateToSql(Expression<Func<T, bool>> predicate)
    {
        var result = new SqlWhereClause();
        var parameterCounter = 0;

        void VisitBinary(BinaryExpression binary)
        {
            if (binary.Left is MemberExpression member)
            {
                var paramName = $"p{parameterCounter++}";
                var propertyName = member.Member.Name;
                
                // 处理不同的比较操作符
                string operation = binary.NodeType switch
                {
                    ExpressionType.Equal => "=",
                    ExpressionType.NotEqual => "!=",
                    ExpressionType.GreaterThan => ">",
                    ExpressionType.GreaterThanOrEqual => ">=",
                    ExpressionType.LessThan => "<",
                    ExpressionType.LessThanOrEqual => "<=",
                    _ => throw new NotSupportedException($"不支持的操作符: {binary.NodeType}")
                };

                var value = Expression.Lambda(binary.Right).Compile().DynamicInvoke();
                result.Sql = $"\"{propertyName}\" {operation} @{paramName}";
                result.Parameters.Add(paramName, value);
            }
        }

        if (predicate.Body is BinaryExpression binaryExpression)
        {
            VisitBinary(binaryExpression);
        }

        return result;
    }
}
