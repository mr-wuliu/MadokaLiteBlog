using MadokaLiteBlog.Api.Models;
using Npgsql;

namespace MadokaLiteBlog.Api.Data;

public class VisitRecordMapper : BaseMapper<VisitRecord>
{
    public VisitRecordMapper(NpgsqlConnection dbContext) : base(dbContext)
    {

    }
}
