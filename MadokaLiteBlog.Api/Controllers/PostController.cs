using Microsoft.AspNetCore.Mvc;
using MadokaLiteBlog.Api.Models;
using MadokaLiteBlog.Api.Service;

[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private readonly PostServer _postServer;
    public PostController(PostServer postServer)
    {
        _postServer = postServer;
    }
    [HttpGet]
    public IActionResult Get()
    {
        var posts = new List<Post>
        {
            new() { Id = 1, Title = "第一篇文章", Content = "这是测试内容1" },
            new() { Id = 2, Title = "第二篇文章", Content = "这是测试内容2" }
        };
        
        return Ok(posts);
    }
    [HttpGet("{id}")]
    public IActionResult Get(long id)
    {
        var post = new Post { Id = id, Title = "第" + id + "篇文章", Content = "这是测试内容" + id };
        return Ok(post);
    }
    [HttpPost]
    public async Task<IActionResult> GetPostFromDatabase()
    {
        var post = await _postServer.GetAllPosts();
        return Ok(post);
    }

    [HttpPost("query")]
    public async Task<IActionResult> QueryPostFromDatabase()
    {
        var post = new Post { Id = 1 };
        post = await _postServer.GetPost(post);
        return Ok(post);
    }
    [HttpPost("add")]
    public async Task<IActionResult> AddPostToDatabase()
    {
        var post = new Post 
        { 
            Title = "第1篇文章",
            Content = "这是测试内容1",
            Summary = "这是测试摘要1",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
        };
        await _postServer.AddPost(post);
        return Ok();
    }
} 