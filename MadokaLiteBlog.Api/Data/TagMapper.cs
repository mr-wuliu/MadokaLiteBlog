using MadokaLiteBlog.Api.Models;
using Npgsql;

namespace MadokaLiteBlog.Api.Data;

public class TagMapper : BaseMapper<Tag>
{
    public TagMapper(NpgsqlConnection dbContext) : base(dbContext)
    {

    }
}
