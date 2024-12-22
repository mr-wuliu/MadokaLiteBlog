namespace MadokaLiteBlog.Api.Models;
[AutoBuild]
[Table("Category")]
public class Category : BaseDtoEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// 反向引用
    /// </summary>
    public List<Post>? Posts { get; set; }
}
