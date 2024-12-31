namespace MadokaLiteBlog.Api.Models.DTO;

/// <summary>
/// 标签
/// </summary>
[AutoBuild]
[Table("Tag")]
public class Tag : BaseDtoEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Name { get; set; }
}