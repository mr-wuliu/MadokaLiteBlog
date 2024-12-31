using MadokaLiteBlog.Api.Models.DTO;
using Npgsql;
namespace MadokaLiteBlog.Api.Mapper;

public class PostMapper : BaseMapper<Post>
{
    public PostMapper(NpgsqlConnection dbContext, ILogger<PostMapper> logger) : base(dbContext, logger)
    {

    }
}