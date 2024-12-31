using MadokaLiteBlog.Api.Mapper;
using MadokaLiteBlog.Api.Models.DTO;
using MadokaLiteBlog.Api.Models.VO;
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
    public async Task<IEnumerable<PostVo>> GetAllPosts()
    {
        var posts = await _postMapper.GetAllAsync(orderBy: p => p.Id, isDesc: true);
        return posts.Select(p => new PostVo
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            Status = p.Status,
            IsPublished = p.IsPublished,
            Tags = p.Tags,
            Summary = p.Summary,
            Content = p.Content,
            Path = p.Path,
            CategoryId = p.CategoryId,
        });
    }
    public async Task<IEnumerable<PostVo>> GetAllPosts(int page, int pageSize)
    {
        var posts = await _postMapper.GetAllAsync(page, pageSize, null, p => p.Id, true);
        _logger.LogInformation("Get all posts page: {page}, pageSize: {pageSize}, count: {count}", page, pageSize, posts.Count());
        return posts.Select(p => new PostVo
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            Status = p.Status,
            IsPublished = p.IsPublished,
            Tags = p.Tags,
            Summary = p.Summary,
            Content = p.Content,
            Path = p.Path,
            CategoryId = p.CategoryId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            CreatedBy = p.CreatedBy,
            UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted,
        });
    }
    public async Task<IEnumerable<PostVo>> GetPostsSummaryByPage(int page, int pageSize)
    {
        var posts = await _postMapper.GetAllAsync(page, pageSize,
            p => new { p.Id, p.Title, p.Summary },
            p => p.Id, true
        );
        _logger.LogInformation("Get posts summary by page: {page}, pageSize: {pageSize}, count: {count}", page, pageSize, posts.Count());
        return posts.Select(p => new PostVo
        {
            Id = p.Id,
            Title = p.Title,
            Summary = p.Summary,
        });
    }
    public async Task<PostVo?> GetPost(PostVo post)
    {
        if (post.Id == null || post.Id <= 0)
        {
            throw new ArgumentException("Invalid post ID");
        }
        Post queryPost = new() { Id = post.Id ?? 0};
        var p = await _postMapper.GetByPropertyAsync(queryPost);
        if (p == null)
        {
            _logger.LogInformation("Post not found: {Id}", post.Id);
            return null;
        }
        _logger.LogInformation("Get post by id: {Id}", post.Id);
        return new PostVo
        {
            Id = p.Id ,
            Title = p.Title,
            Slug = p.Slug,
            Status = p.Status,
            IsPublished = p.IsPublished,
            Tags = p.Tags,
            Summary = p.Summary,
            Content = p.Content,
            Path = p.Path,
            CategoryId = p.CategoryId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            CreatedBy = p.CreatedBy,
            UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted,
        };
    }
    public async Task<int> AddPost(PostVo post)
    {
        _logger.LogDebug("Inserting post: {post}", post.Title);
        Post postEntity = new()
        {
            Title = post.Title,
            Slug = post.Slug,
            Status = post.Status,
            IsPublished = post.IsPublished,
            Tags = post.Tags,
            Summary = post.Summary,
            Content = post.Content,
            Path = post.Path,
            CategoryId = post.CategoryId,
            CreatedAt = DateTime.Now,
            // TODO: 获取当前用户id
            CreatedBy = 1,
        };
        var postId = await _postMapper.InsertAsync(postEntity);
        _logger.LogDebug("Post operation completed. Action: {Action}, Id: {Id}, Title: {Title}", "insert", postId, post.Title);
        return postId;
    }
    public async Task<int> DeletePost(long id)
    {
        var result = await _postMapper.DeleteAsync(id);
        _logger.LogInformation("Post operation completed. Action: {Action}, Id: {Id}, Result: {Result}", "delete", id, result);
        return result;
    }
    public async Task<int> UpdatePost(PostVo post)
    {
        if (post.Id == null || post.Id <= 0)
        {
            _logger.LogError("Invalid post ID: {Id}", post.Id);
            return 0;
        }
        Post postEntity = new()
        {
            Id = (long)post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Status = post.Status,
            IsPublished = post.IsPublished,
            Tags = post.Tags,
            Summary = post.Summary,
            Content = post.Content,
            Path = post.Path,
            CategoryId = post.CategoryId,
            UpdatedAt = DateTime.Now,
            // TODO: 获取当前用户id
            UpdatedBy = 1,
        };
        var result = await _postMapper.UpdateAsync(postEntity);
        _logger.LogInformation("Post operation completed. Action: {Action}, Id: {Id}, Result: {Result}", "update", post.Id, result);
        return result;
    }

}
