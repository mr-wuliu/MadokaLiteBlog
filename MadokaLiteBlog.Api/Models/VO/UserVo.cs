namespace MadokaLiteBlog.Api.Models.VO;

public class UserVo
{
    public long? Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Motto { get; set; }
}