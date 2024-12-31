using MadokaLiteBlog.Api.Mapper;
using MadokaLiteBlog.Api.Models.VO;
using MadokaLiteBlog.Api.Models.DTO;

namespace MadokaLiteBlog.Api.Service;
public class CategoryServer
{
    private readonly CategoryMapper _categoryMapper;
    private readonly ILogger<CategoryServer> _logger;
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
    public async Task<CategoryVo> GetCategoryByName(string name)
    {
        var categories = await _categoryMapper.GetByPropertyAsync(c => c.Name == name);
        if (!categories.Any())
        {
            throw new Exception("分类不存在");
        }
        var category = categories.First();
        return new CategoryVo
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
        };
    }
    public async Task<bool> IsCategoryExist(string name)
    {
        var category = await _categoryMapper.GetByPropertyAsync(
            c => c.Name == name
            );
        if (category.Count() > 1)
        {
            throw new Exception("分类重复");
        }
        return category.Any();
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
