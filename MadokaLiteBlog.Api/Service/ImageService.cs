using System.Text.RegularExpressions;
using MadokaLiteBlog.Api.Extensions;
public class ImageService
{
    private readonly HttpClient _httpClient;
    private readonly string? _bucketName;
    private readonly string? _accessKey;
    private readonly string? _secretKey;
    private readonly string? _region;
    private readonly bool _isEnabled;
    
    // 用于匹配 [s3://filename.jpg] 格式的图片标记
    private static readonly Regex S3UrlPattern = new(@"\[s3://([^\]]+)\]", RegexOptions.Compiled);
    
    // 允许的图片类型
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const int MaxFileSizeInMB = 10;
    private const int DefaultUrlExpirationMinutes = 60;

    public ImageService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _bucketName = configuration["AWS:BucketName"];
        _accessKey = configuration["AWS:AccessKey"];
        _secretKey = configuration["AWS:SecretKey"];
        _region = configuration["AWS:Region"];
        
        _isEnabled = !string.IsNullOrEmpty(_bucketName) && 
                    !string.IsNullOrEmpty(_accessKey) && 
                    !string.IsNullOrEmpty(_secretKey) && 
                    !string.IsNullOrEmpty(_region);
    }

    /// <summary>
    /// 上传图片并返回标记格式的图片ID
    /// </summary>
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Image storage service is not enabled");
        }

        // 验证文件类型
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Invalid file type. Only images are allowed.");
        }

        // 验证文件大小
        if (file.Length > MaxFileSizeInMB * 1024 * 1024)
        {
            throw new InvalidOperationException($"File size exceeds {MaxFileSizeInMB}MB limit.");
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        using var stream = file.OpenReadStream();
        
        // 上传文件并获取标记格式的返回值
        return await S3Extension.UploadFileAsync(
            _httpClient,
            stream,
            fileName,
            file.ContentType,
            _bucketName!,
            _region!,
            _accessKey!,
            _secretKey!
        );
    }

    /// <summary>
    /// 获取单个图片的临时访问URL
    /// </summary>
    public string GetImageUrl(string imageId)
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Image storage service is not enabled");
        }

        if (imageId.StartsWith("[s3://") && imageId.EndsWith("]"))
        {
            imageId = imageId[6..^1]; // 移除 [s3:// 和 ]
        }

        return S3Extension.GeneratePresignedUrl(
            _bucketName!,
            _region!,
            _accessKey!,
            _secretKey!,
            imageId,
            DefaultUrlExpirationMinutes
        );
    }

    /// <summary>
    /// 处理文本内容中的所有图片标记，将其替换为临时访问URL
    /// </summary>
    public string ProcessContent(string content)
    {
        if (!_isEnabled)
        {
            return content;
        }

        return S3UrlPattern.Replace(content, match =>
        {
            var imageId = match.Groups[1].Value;
            return S3Extension.GeneratePresignedUrl(
                _bucketName!,
                _region!,
                _accessKey!,
                _secretKey!,
                imageId,
                DefaultUrlExpirationMinutes
            );
        });
    }

    /// <summary>
    /// 检查服务是否启用
    /// </summary>
    public bool IsEnabled() => _isEnabled;
}