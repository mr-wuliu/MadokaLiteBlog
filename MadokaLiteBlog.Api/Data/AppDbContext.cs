using System.Data;
using Dapper;
using Npgsql;

namespace MadokaLiteBlog.Api.Data
{
    public class PostgresDbContext
    {
        private readonly string _connectionString;
        private NpgsqlConnection? _connection;

        public PostgresDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        public NpgsqlConnection CreateConnection()
        {
            if (_connection == null)
            {
                _connection = new NpgsqlConnection(_connectionString);
            }
            return _connection;
        }
    }
    public static class DapperExtensions
    {
        public static async Task<int> InsertAsync<T>(this PostgresDbContext dbContext, T entity)
        {
            using var conn = dbContext.CreateConnection();
            return await conn.ExecuteAsync(
                "INSERT INTO TABLEName (...) VALUES (...)", entity
            );
        }
        public static async Task<int> UpdateAsync<T>(this PostgresDbContext dbContext, T entity)
        {
            using var conn = dbContext.CreateConnection();
            return await conn.ExecuteAsync(
                "UPDATE TableName SET ... WHERE Id = @Id", entity
            );
        }

    }
}
