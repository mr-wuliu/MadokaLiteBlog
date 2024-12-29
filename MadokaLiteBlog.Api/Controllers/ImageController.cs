
using Microsoft.AspNetCore.Mvc;
/// <summary>
/// 图床管理, 上传图片使用开放的公共接口
/// 获取token使用内部的私有接口, 需要登录验证
/// </summary>
[ApiController]
[Route("api/image")]
public class ImageController : ControllerBase
{
    private readonly ImageService _imageService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(ImageService imageService, ILogger<ImageController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (!_imageService.IsEnabled())
        {
            return BadRequest("Image storage service is not enabled");
        }

        try
        {
            var imageId = await _imageService.UploadImageAsync(file);
            // 返回特殊格式的标记
            return Ok(new { 
                marker = $"[s3://{imageId}]"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    /// <summary>
    /// 开发模式, 将图片标记替换为临时访问URL
    /// </summary>
    [HttpPost("get-url")]
    public IActionResult GetUrl(string imageId)
    {
        return Ok(_imageService.GetImageUrl(imageId));
    }
}