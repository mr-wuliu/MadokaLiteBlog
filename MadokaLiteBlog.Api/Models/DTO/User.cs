namespace MadokaLiteBlog.Api.Models;
[AutoBuild]
[Table("User")]
public class User : BaseDtoEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}
