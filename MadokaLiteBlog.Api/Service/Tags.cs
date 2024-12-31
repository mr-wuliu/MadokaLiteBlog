using MadokaLiteBlog.Api.Mapper;
using MadokaLiteBlog.Api.Models.DTO;
using MadokaLiteBlog.Api.Models.VO;

namespace MadokaLiteBlog.Api.Service;

public class TagsServer
{
    private readonly TagMapper _tagMapper;
    private readonly ILogger<TagsServer> _logger;
    public TagsServer(TagMapper tagMapper, ILogger<TagsServer> logger)
    {
        _tagMapper = tagMapper;
        _logger = logger;
    }
    public async Task<IEnumerable<TagVo>> GetAllTags()
    {
        var tags = await _tagMapper.GetAllAsync();
        return tags.Select(t => new TagVo
        {
            Id = t.Id,
            Name = t.Name
        });
    }
    public async Task<TagVo> GetTagById(long id)
    {
        var tag = await _tagMapper.GetByIdAsync(id) ?? throw new Exception("标签不存在");
        return new TagVo
        {
            Id = tag.Id,
            Name = tag.Name
        };
    }
    public async Task<bool> IsTagExist(string name)
    {
        var tag = await _tagMapper.GetByPropertyAsync(t => t.Name == name);
        return tag.Any();
    }
    public async Task<long> CreateTag(TagVo tagVo)
    {
        var tag = new Tag
        {
            Name = tagVo.Name
        };
        await _tagMapper.InsertAsync(tag);
        return tag.Id;
    }
}   