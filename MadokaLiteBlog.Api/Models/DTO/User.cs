namespace MadokaLiteBlog.Api.Models;
[AutoBuild]
[Table("User")]
public class User : BaseDtoEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
