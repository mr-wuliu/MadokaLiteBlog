namespace MadokaLiteBlog.Api.Models.DTO;
[AutoBuild]
[Table("Category")]
public class Category : BaseDtoEntity
{
    [Key("Id")]
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
