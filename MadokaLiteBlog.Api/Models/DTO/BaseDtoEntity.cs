namespace MadokaLiteBlog.Api.Models;

/// <summary>
/// 实体基类
/// </summary>
public abstract class BaseDtoEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}