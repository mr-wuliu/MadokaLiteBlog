namespace MadokaLiteBlog.Api.Models;

[Table("Post")]
public class Post : BaseEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Title { get; set; }
    
    /// <summary>
    /// 文章链接 
    /// </summary>
    public string? Slug { get; set; }
    public string? Status { get; set; }
    public bool IsPublished { get; set; }
    /// <summary>
    /// 文章的标签
    /// </summary>
    [Jsonb]
    public List<Tag>? Tags { get; set; }
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
    public Category? Category { get; set; }
} 