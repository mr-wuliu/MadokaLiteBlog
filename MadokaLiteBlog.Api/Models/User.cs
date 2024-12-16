namespace MadokaLiteBlog.Api.Models;
[AutoBuild]
[Table("User")]
public class User : BaseEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}
