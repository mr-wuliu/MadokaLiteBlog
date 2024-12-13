using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
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
    public IActionResult Get(int id)
    {
        var post = new Post { Id = id, Title = "第" + id + "篇文章", Content = "这是测试内容" + id };
        return Ok(post);
    }
} 