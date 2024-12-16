using MadokaLiteBlog.Api.Mapper;
using MadokaLiteBlog.Api.Models;

namespace MadokaLiteBlog.Api.Service;

public class PostServer
{
    private readonly PostMapper _postMapper;
    public PostServer(PostMapper postMapper)
    {
        _postMapper = postMapper;
    }
    public async Task<IEnumerable<Post>> GetAllPosts()
    {
        return await _postMapper.GetAllAsync();
    }
    public async Task<Post?> GetPost(Post post)
    {
        return await _postMapper.GetByPropertyAsync(post);
    }
    public async Task AddPost(Post post)
    {
        await _postMapper.InsertAsync(post);
    }

}
