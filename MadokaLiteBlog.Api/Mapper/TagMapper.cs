using MadokaLiteBlog.Api.Models.DTO;
using Npgsql;

namespace MadokaLiteBlog.Api.Mapper;

public class TagMapper : BaseMapper<Tag>
{
    public TagMapper(NpgsqlConnection dbContext, ILogger<TagMapper> logger) : base(dbContext, logger)
    {

    }
}
