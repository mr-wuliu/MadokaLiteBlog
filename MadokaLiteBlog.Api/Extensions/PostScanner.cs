namespace MadokaLiteBlog.Api.Extensions;

public class PostScanResult
{
    public string MarkdownPath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> ImagePaths { get; set; } = new();
    public DateTime LastModified { get; set; }
}

public class PostScanner
{
    private readonly ILogger<PostScanner> _logger;
    public PostScanner(ILogger<PostScanner> logger)
    {
        _logger = logger;
    }

    public IEnumerable<PostScanResult> ScanPosts(string basePath)
    {
        var results = new List<PostScanResult>();
        if (!Directory.Exists(basePath))
        {
            _logger.LogError("扫描路径不存在: {Path}", basePath);
            return results;
        }
        var mdFiles = Directory.GetFiles(basePath, "*.md", SearchOption.AllDirectories);
        _logger.LogInformation("找到 {Count} 个 Markdown 文件", mdFiles.Length);

        foreach (var mdFile in mdFiles)
        {
            try
            {
                var result = ScanSinglePost(mdFile);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理文件时出错: {File}", mdFile);
            }
        }

        return results;
    }

    private PostScanResult? ScanSinglePost(string mdFilePath)
    {
        var fileInfo = new FileInfo(mdFilePath);
        if (!fileInfo.Exists)
        {
            return null;
        }

        var result = new PostScanResult
        {
            MarkdownPath = mdFilePath,
            Title = Path.GetFileNameWithoutExtension(mdFilePath),
            LastModified = fileInfo.LastWriteTime
        };

        // 查找同名图片文件夹
        var imageFolder = Path.Combine(
            Path.GetDirectoryName(mdFilePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(mdFilePath)
        );

        if (Directory.Exists(imageFolder))
        {
            // 获取所有图片文件
            var imageFiles = Directory.GetFiles(imageFolder, "*.*", SearchOption.AllDirectories)
                .Where(file => IsImageFile(file));

            result.ImagePaths.AddRange(imageFiles);
            
            
            _logger.LogInformation("文章 {Title} 找到 {Count} 个图片文件", 
                result.Title, result.ImagePaths.Count);
        }

        return result;
    }

    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => true,
            _ => false
        };
    }
}