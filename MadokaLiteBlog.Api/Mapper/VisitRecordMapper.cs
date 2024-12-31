using MadokaLiteBlog.Api.Models.DTO;
using Npgsql;

namespace MadokaLiteBlog.Api.Mapper;

public class VisitRecordMapper : BaseMapper<VisitRecord>
{
    public VisitRecordMapper(NpgsqlConnection dbContext, ILogger<VisitRecordMapper> logger) : base(dbContext, logger)
    {

    }
}
