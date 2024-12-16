using MadokaLiteBlog.Api.Models;
using Npgsql;
namespace MadokaLiteBlog.Api.Mapper;

public class PostMapper : BaseMapper<Post>
{
    public PostMapper(NpgsqlConnection dbContext) : base(dbContext)
    {

    }
}