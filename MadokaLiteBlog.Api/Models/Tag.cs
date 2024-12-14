namespace MadokaLiteBlog.Api.Models;

/// <summary>
/// 标签
/// </summary>
[Table("Tag")]
public class Tag
{
    [Key("Id")]
    public required long Id { get; set; }
    public string? Name { get; set; }
}