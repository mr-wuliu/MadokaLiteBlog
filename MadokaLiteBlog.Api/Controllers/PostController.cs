using Microsoft.AspNetCore.Mvc;
using MadokaLiteBlog.Api.Models;
using MadokaLiteBlog.Api.Service;

[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private readonly PostServer _postServer;
    private readonly ILogger<PostController> _logger;
    public PostController(PostServer postServer, ILogger<PostController> logger)
    {
        _postServer = postServer;
        _logger = logger;
    }
    // 文章嘛, 无非增删改查
    [HttpPost("insert")]
    public async Task<IActionResult> InsertPost(Post post)
    {
        var postId = await _postServer.AddPost(post);
        return Ok(new
        {
            Id = postId,
            Title = post.Title,
        });
    }
    [HttpPost("delete")]
    public async Task<IActionResult> DeletePost(long id)
    {
        var result = await _postServer.DeletePost(id);
        return Ok(result);
    }
    [HttpPost("update")]
    public async Task<IActionResult> UpdatePost(Post post)
    {
        var result = await _postServer.UpdatePost(post);
        return Ok(result);
    }
    [HttpPost("get")]
    public async Task<IActionResult> GetPost(Post post)
    {
        var result = await _postServer.GetPost(post);
        return Ok(result);
    }
    // 分页查询
    [HttpGet("get")]
    public async Task<IActionResult> GetPostFromDatabase(int page, int pageSize)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest("Page and pageSize must be greater than 0");
        }
        if (pageSize > 20){
            return BadRequest("pageSize must be less than 20");
        }
        var post = await _postServer.GetAllPosts(page, pageSize);
        return Ok(post);
    }
} 