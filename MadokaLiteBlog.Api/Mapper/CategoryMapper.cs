using MadokaLiteBlog.Api.Models.DTO;
using Npgsql;
namespace MadokaLiteBlog.Api.Mapper;

public class CategoryMapper : BaseMapper<Category>
{
    public CategoryMapper(NpgsqlConnection dbContext, ILogger<CategoryMapper> logger) : base(dbContext, logger) 
    {
        
    }
}
