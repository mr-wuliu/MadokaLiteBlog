using MadokaLiteBlog.Api.Models;
using Npgsql;
namespace MadokaLiteBlog.Api.Mapper;

public class CategoryMapper : BaseMapper<Category>
{
    public CategoryMapper(NpgsqlConnection dbContext) : base(dbContext) 
    {
        
    }
}
