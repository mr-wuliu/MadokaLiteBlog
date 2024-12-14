using MadokaLiteBlog.Api.Data;
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

}
