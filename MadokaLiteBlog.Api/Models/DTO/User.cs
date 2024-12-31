namespace MadokaLiteBlog.Api.Models.DTO;
[AutoBuild]
[Table("User")]
public class User : BaseDtoEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Motto { get; set; }
}
