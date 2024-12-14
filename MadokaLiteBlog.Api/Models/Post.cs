namespace MadokaLiteBlog.Api.Models;

/// <summary>
/// 文章
/// </summary>
[Table("Post")]
public class Post
{
    [Key("Id")]
    public required long Id { get; set; }
    public string? Title { get; set; }
    /// <summary>
    /// 文章链接 
    /// </summary>
    public string? Slug { get; set; }
    public string? Status { get; set; }
    public bool? IsPublished { get; set; }
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
    /// 文章的创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// 文章的更新时间
    /// </summary>
    public DateTime UpdateAt { get; set; }
    /// <summary>
    /// 文章的分类
    /// </summary>
    public Category? Category { get; set; }


} 