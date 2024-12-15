namespace MadokaLiteBlog.Api.Models;
[Table("Category")]
public class Category : BaseEntity
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
