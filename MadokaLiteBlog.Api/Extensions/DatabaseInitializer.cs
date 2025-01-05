using System.Reflection;
using Dapper;
using Npgsql;
using MadokaLiteBlog.Api.Common;
using MadokaLiteBlog.Api.Models.DTO;

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
            CreateTable(type);
        }
        InitializeData();
        _logger.LogInformation("DatabaseInitializer initialized successfully");
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
        SELECT a.attname AS column_name, pg_catalog.format_type(a.atttypid, a.atttypmod) AS data_type
        FROM pg_catalog.pg_attribute a
        JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
        JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
        WHERE n.nspname = 'public'
            AND c.relname = '{tableName}'
            AND a.attnum > 0
            AND NOT a.attisdropped;
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
            var modelColumnType = DataUtils.GetSqlType(property);

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
    private void InitializeData()
    {
        // 检查user表是否存在数据
        var user = _dbContext.QueryFirstOrDefault<User>("SELECT * FROM \"User\"");
        if (user == null)
        {
            _logger.LogInformation("Initializing data...");
            var adminUser = new User
            {
                Username = "admin",
                Password = "admin",
                Email = "admin@admin.com",
                AvatarUrl = "https://www.gravatar.com/avatar/205e460b479e2e5b48aec07710c08d50",
                Motto = "初始管理员"
            };
            var sql = $@"INSERT INTO ""User"" 
                (""Username"", ""Password"", ""Email"", ""AvatarUrl"", ""Motto"", ""CreatedAt"", ""CreatedBy"", ""IsDeleted"") 
                VALUES ('{adminUser.Username}', '{adminUser.Password}', '{adminUser.Email}', '{adminUser.AvatarUrl}', '{adminUser.Motto}', now(), 1, false)";
            _dbContext.Execute(sql);
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
            _logger.LogInformation("Table {tableName} already exists", tableName);
            return;
        }
        _logger.LogInformation("Table {tableName} does not exist, creating...", tableName);
        var properties = type.GetProperties();
        // 遍历所有的字段
        var columns = properties.Select(p => {
            // Key属性可以自定义名称:[Key("XX")]
            var keyAttribute = p.GetCustomAttributes(typeof(KeyAttribute), false).FirstOrDefault() as KeyAttribute;
            var columnName = $"\"{keyAttribute?.Name ?? p.Name}\"";
            var columnType = DataUtils.GetSqlType(p);
            var isKey = p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0;
            var primaryKey = isKey ? "PRIMARY KEY" : "";
            var autoIncrement = isKey ? (columnType == "BIGINT" ? "BIGSERIAL" : "SERIAL") : columnType; // 使用SERIAL或BIGSERIAL
            // 检查是否为可空类型
            var isNullable = !p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) != null;
            var nullable = isNullable ? "" : "NOT NULL";
            return $"{columnName} {autoIncrement} {primaryKey} {nullable}".Trim();
        });
        var createTableQuery = $@"
            CREATE TABLE IF NOT EXISTS ""{tableName}"" (
                {string.Join(",", columns)}
            )
        ";
        _logger.LogInformation("Creating table {tableName} with query: {query}", tableName, createTableQuery);
        _dbContext.Execute(createTableQuery);
        _logger.LogInformation("Table {tableName} created successfully", tableName);
    }

}
