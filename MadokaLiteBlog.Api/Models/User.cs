namespace MadokaLiteBlog.Api.Models;

public class User : BaseEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}
