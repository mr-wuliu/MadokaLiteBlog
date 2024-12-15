namespace MadokaLiteBlog.Api.Models;

/// <summary>
/// 标签
/// </summary>
[Table("Tag")]
public class Tag : BaseEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Name { get; set; }
}