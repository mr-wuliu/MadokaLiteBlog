namespace MadokaLiteBlog.Api.Models.DTO;

/// <summary>
/// 实体基类
/// </summary>
public abstract class BaseDtoEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public long CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}