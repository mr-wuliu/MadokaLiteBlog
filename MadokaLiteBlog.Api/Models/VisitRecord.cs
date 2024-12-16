
namespace MadokaLiteBlog.Api.Models;
[AutoBuild]
[Table("VisitRecord")]
public class VisitRecord : BaseEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Ip { get; set; }
    public required string RequestPath { get; set; }
    public string? RequestQueryParams { get; set; }
    public required string RequestMethod {get; set;}
    public string? UserAgent { get; set; }
}
