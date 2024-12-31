namespace MadokaLiteBlog.Api.Models.DTO;
[AutoBuild]
[Table("Post")]
public class Post : BaseDtoEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    /// <summary>
    /// 文章链接 
    /// </summary>
    public string? Slug { get; set; }
    public string? Status { get; set; }
    public bool? IsPublished { get; set; }
    /// <summary>
    /// 文章的标签          
    /// </summary>
    public List<long>? Tags { get; set; }
    public string? Summary { get; set; }
    [Jsonb]
    public string? Content { get; set; }
    /// <summary>
    /// 文章的本地路径
    /// </summary>
    public string? Path { get; set; }
    /// <summary>
    /// 文章的分类
    /// </summary>
    public List<long>? CategoryId { get; set; }
} 