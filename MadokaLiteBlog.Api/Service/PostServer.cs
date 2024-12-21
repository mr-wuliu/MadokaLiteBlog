using MadokaLiteBlog.Api.Mapper;
using MadokaLiteBlog.Api.Models;

namespace MadokaLiteBlog.Api.Service;

public class PostServer
{
    private readonly PostMapper _postMapper;
    private readonly ILogger _logger;
    public PostServer(PostMapper postMapper, ILogger<PostServer> logger)
    {
        _postMapper = postMapper;
        _logger = logger;
    }
    public async Task<IEnumerable<Post>> GetAllPosts()
    {
        return await _postMapper.GetAllAsync();
    }
    public async Task<IEnumerable<Post>> GetAllPosts(int page, int pageSize)
    {
        return await _postMapper.GetAllAsync( page, pageSize);
    }
    public async Task<Post?> GetPost(Post post)
    {
        return await _postMapper.GetByPropertyAsync(post);
    }
    public async Task<int> AddPost(Post post)
    {
        var postId = await _postMapper.InsertAsync(post);
        _logger.LogInformation("Post operation completed. Action: {Action}, Id: {Id}, Title: {Title}", "insert", postId, post.Title);
        return postId;
    }
    public async Task<int> DeletePost(long id)
    {
        var result = await _postMapper.DeleteAsync(id);
        _logger.LogInformation("Post operation completed. Action: {Action}, Id: {Id}, Result: {Result}", "delete", id, result);
        return result;
    }
    public async Task<int> UpdatePost(Post post)
    {
        var result = await _postMapper.UpdateAsync(post);
        _logger.LogInformation("Post operation completed. Action: {Action}, Id: {Id}, Result: {Result}", "update", post.Id, result);
        return result;
    }

}
