using Dapper;
using Npgsql;

namespace MadokaLiteBlog.Api.Data;
public abstract class BaseMapper<T> where T : class
{
    protected readonly NpgsqlConnection _dbContext;

    protected BaseMapper(NpgsqlConnection dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbContext.QueryAsync<T>(
            $"SELECT * FROM \"{typeof(T).Name}\""
        );
    }
    public async Task<T?> GetByIdAsync(object id)
    {
        return await _dbContext.QueryFirstOrDefaultAsync<T>($"SELECT * FROM \"{typeof(T).Name}\" WHERE Id = @Id", new { Id = id });
    }
    public async Task<T?> GetByPropertyAsync(object value)
    {
        var properties = typeof(T).GetProperties();
        var keyProperty = properties.FirstOrDefault(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0);
        if (keyProperty == null)
        {
            throw new Exception("类中没有KeyAttribute属性");
        }
        var keyAttribute = keyProperty.GetCustomAttributes(typeof(KeyAttribute), false).FirstOrDefault() as KeyAttribute;
        var keyName = keyAttribute?.Name ?? keyProperty.Name;
        var keyValue = keyProperty.GetValue(value);

        var tableAttribute = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault(); 
        var tableName = tableAttribute != null ? (tableAttribute as TableAttribute)?.Name : typeof(T).Name;

        return await _dbContext.QueryFirstOrDefaultAsync<T>(
            $"SELECT * FROM \"{tableName}\" WHERE \"{keyName}\" = @Value", 
            new { Value = keyValue }
        );
    }
    public async Task<int> InsertAsync(T entity)
    {
        return await _dbContext.ExecuteAsync(
            $"INSERT INTO \"{typeof(T).Name}\" ({string.Join(", ", typeof(T).GetProperties().Select(p => p.Name))}) VALUES ({string.Join(", ", typeof(T).GetProperties().Select(p => "@" + p.Name))})", entity
        );
    }
    public async Task<int> UpdateAsync(T entity)
    {
        var properties = typeof(T).GetProperties();
        var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
        return await _dbContext.ExecuteAsync($"UPDATE \"{typeof(T).Name}\" SET {setClause} WHERE Id = @Id", entity);
    }
    public async Task<int> DeleteAsync(object id)
    {
        return await _dbContext.ExecuteAsync($"DELETE FROM \"{typeof(T).Name}\" WHERE Id = @Id", new { Id = id });
    }
}
