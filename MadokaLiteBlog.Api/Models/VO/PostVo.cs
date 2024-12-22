namespace MadokaLiteBlog.Api.Models.VO;

public class PostVo
{
    public long Id { get; set; }
    public string? Title {get; set; }
    public string? Slug { get; set; }
    public string? Status { get; set; }
    public bool? IsPublished { get; set; }
    public List<long>? Tags { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? Path { get; set; }
    public long? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
} 