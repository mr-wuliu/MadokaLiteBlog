using MadokaLiteBlog.Api.Models;
using Npgsql;
namespace MadokaLiteBlog.Api.Data;

public class CategoryMapper : BaseMapper<Category>
{
    public CategoryMapper(NpgsqlConnection dbContext) : base(dbContext) 
    {
        
    }
}
