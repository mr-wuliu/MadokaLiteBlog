namespace MadokaLiteBlog.Api.Models.DTO;
[AutoBuild]
[Table("VisitRecord")]
public class VisitRecord : BaseDtoEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Ip { get; set; }
    public required string RequestPath { get; set; }
    public string? RequestQueryParams { get; set; }
    public required string RequestMethod {get; set;}
    public string? UserAgent { get; set; }
}
