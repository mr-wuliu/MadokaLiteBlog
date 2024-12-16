using MadokaLiteBlog.Api.Models;
using Npgsql;

namespace MadokaLiteBlog.Api.Mapper;

public class TagMapper : BaseMapper<Tag>
{
    public TagMapper(NpgsqlConnection dbContext) : base(dbContext)
    {

    }
}
