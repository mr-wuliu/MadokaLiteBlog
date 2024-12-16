using MadokaLiteBlog.Api.Models;
using Npgsql;

namespace MadokaLiteBlog.Api.Mapper;

public class VisitRecordMapper : BaseMapper<VisitRecord>
{
    public VisitRecordMapper(NpgsqlConnection dbContext) : base(dbContext)
    {

    }
}
