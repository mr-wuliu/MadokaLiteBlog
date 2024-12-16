using System.Reflection;
using Dapper;
using MadokaLiteBlog.Api.Models;
using Npgsql;

namespace MadokaLiteBlog.Api.Extensions;

public class DatabaseInitializer
{
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly NpgsqlConnection _dbContext;
    public DatabaseInitializer(NpgsqlConnection dbContext, ILogger<DatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    public void Initialize()
    {
        _logger.LogInformation("DatabaseInitializer initialized");
        // 利用反射
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(AutoBuildAttribute), false).Length > 0);

        foreach (var type in types)
        {
            _logger.LogInformation("Creating table for type: {type}", type.Name);
            CreateTable(type);
        }
    }
    private static bool IsCompatibleType(string existingType, string modelType)
    {
        // 将类型转换为小写进行比较
        existingType = existingType.ToLowerInvariant();
        modelType = modelType.ToLowerInvariant();

        if (existingType == modelType)
            return true;
        if (existingType == "double precision" && modelType == "real")
            return true;
        if (existingType == "numeric" && modelType == "real")
            return true;
        if (existingType == "timestamp without time zone" && modelType == "timestamp")
            return true;
        if (existingType == "integer" && modelType == "bigint")
            return true;

        // TODO: 其他兼容
        return false;
    }
    private Dictionary<string, string> GetExistingTableColumns(string tableName)
    {
        var query = $@"
        SELECT column_name, data_type
        FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = '{tableName}'
    ";

        return _dbContext.Query(query)
            .ToDictionary(row => (string)row.column_name, row => (string)row.data_type);
    }
    private void ValidateTableStructure(Type type, string tableName)
    {
        var existingColumns = GetExistingTableColumns(tableName);
        var modelProperties = type.GetProperties();

        foreach (var property in modelProperties)
        {
            var columnName = property.Name;
            var modelColumnType = GetSqlType(property);

            if (existingColumns.TryGetValue(columnName, out var existingColumnType))
            {
                if (!IsCompatibleType(existingColumnType, modelColumnType))
                {
                    throw new InvalidOperationException($"Incompatible column type for '{columnName}': existing type '{existingColumnType}', model type '{modelColumnType}'.");
                }
            }
            else
            {
                throw new InvalidOperationException($"Missing column '{columnName}' in existing table '{tableName}'.");
            }
        }
    }
    private bool TableExists(string tableNaem)
    {
        var query = $@"
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_name = '{tableNaem}'
            )
        ";
        return _dbContext.ExecuteScalar<bool>(query);
    }
    private void CreateTable(Type type)
    {
        var tableName = type.GetCustomAttribute<TableAttribute>()?.Name;
        // 断言tableName不可能不存在
        if (tableName == null)
        {
            throw new InvalidOperationException("Table name is null");
        }
        if (TableExists(tableName))
        {
            ValidateTableStructure(type, tableName);
        }
        var properties = type.GetProperties();
        // 遍历所有的字段
        var columns = properties.Select(p => {
            var columnName = $"\"{p.Name}\"";
            var columnType = GetSqlType(p);
            var isKey = p.GetCustomAttributes(typeof(KeyAttribute), false).Any();
            var primaryKey = isKey ? "PRIMARY KEY" : "";
            var autoIncrement = isKey ? (columnType == "BIGINT" ? "BIGSERIAL" : "SERIAL") : columnType; // 使用SERIAL或BIGSERIAL
            return $"{columnName} {autoIncrement} {primaryKey}".Trim();
        });
        var createTableQuery = $@"
            CREATE TABLE IF NOT EXISTS ""{tableName}"" (
                {string.Join(",", columns)}
            )
        ";
        _dbContext.Execute(createTableQuery);
    }
    /// <summary>
    ///  适用于Postgres的映射规则   
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    private string GetSqlType(PropertyInfo property)
    {
        if (property.GetCustomAttributes(typeof(JsonbAttribute), false).Any())
        {
            return "JSONB";
        }

        var type = property.PropertyType;

        if (!type.IsPrimitive && type != typeof(string) && type != typeof(DateTime) && type != typeof(decimal) && !type.IsEnum)
        {
            return "JSONB";
        }

        return type switch
        {
            Type t when t == typeof(long) || t == typeof(ulong) => "BIGINT",
            Type t when t == typeof(string) => "TEXT",
            Type t when t == typeof(DateTime) => "TIMESTAMP",
            Type t when t == typeof(DateTimeOffset) => "TIMESTAMPTZ",
            Type t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) => "JSONB",
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
            _ => "TEXT"
        };
    }
}
