using MadokaLiteBlog.Api.Mapper;
using MadokaLiteBlog.Api.Models.VO;
using MadokaLiteBlog.Api.Models.DTO;
namespace MadokaLiteBlog.Api.Servic;

public class CategoryServer
{
    private readonly CategoryMapper _categoryMapper;
    private readonly ILogger _logger;
    public CategoryServer(CategoryMapper categoryMapper, ILogger<CategoryServer> logger)
    {
        _categoryMapper = categoryMapper;
        _logger = logger;
    }
    public async Task<IEnumerable<CategoryVo>> GetAllCategories()
    {
        var categories = await _categoryMapper.GetAllAsync();
        return categories.Select(c => new CategoryVo
        {
            Id = c.Id,
            Name = c.Name
        });
    }
    public async Task<CategoryVo> GetCategoryById(long id)
    {
        var category = await _categoryMapper.GetByIdAsync(id) 
            ?? throw new Exception("分类不存在");
        return new CategoryVo
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
        };
    }
    public async Task<long> CreateCategory(CategoryVo categoryVo)
    {
        var category = new Category
        {
            Name = categoryVo.Name,
            Description = categoryVo.Description,
        };
        await _categoryMapper.InsertAsync(category);
        return category.Id;
    }
}
