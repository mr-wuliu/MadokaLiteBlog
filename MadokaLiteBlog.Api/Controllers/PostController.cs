using Microsoft.AspNetCore.Mvc;
using MadokaLiteBlog.Api.Service;
using MadokaLiteBlog.Api.Models.VO;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/post")]
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
    [Authorize]
    [HttpPost("insert")]
    public async Task<IActionResult> InsertPost(PostVo post)
    {
        _logger.LogInformation("Inserting post: {post}", post.Title);
        var postId = await _postServer.AddPost(post);
        return Ok(new
        {
            Id = postId,
            Title = post.Title,
        });
    }
    [Authorize]
    [HttpPost("delete")]
    public async Task<IActionResult> DeletePost([FromBody] long id)
    {
        var result = await _postServer.DeletePost(id);
        return Ok(result);
    }
    [Authorize]
    [HttpPost("update")]
    public async Task<IActionResult> UpdatePost(PostVo post)
    {
        var result = await _postServer.UpdatePost(post);
        return Ok(result);
    }
    [HttpPost("getbyid")]
    public async Task<IActionResult> GetPostById(long id)
    {
        _logger.LogInformation("Getting post by id: {id}", id);
        var result = await _postServer.GetPost(new PostVo { Id = id });
        return Ok(result);
    }
    // 分页查询
    [HttpPost("getAllpages")]
    public async Task<IActionResult> GetPostByPage(int page, int pageSize)
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
    [HttpPost("getsummary")]
    public async Task<IActionResult> GetPostsSummaryByPage(int page, int pageSize)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest("Page and pageSize must be greater than 0");
        }
        if (pageSize > 5){
            return BadRequest("pageSize must be less than 20");
        }
        var post = await _postServer.GetPostsSummaryByPage(page, pageSize);
        return Ok(post);
    }
} 