using System.Text;
using System.Text.RegularExpressions;
using MadokaLiteBlog.Api.Models.DTO;
using MadokaLiteBlog.Api.Models.VO;
using MadokaLiteBlog.Api.Service;
using YamlDotNet.Serialization;
using MadokaLiteBlog.Api.Mapper;
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
    private readonly CategoryServer _categoryServer;
    private readonly TagServer _tagsServer;
    private readonly ImageService _imageService;
    private readonly PostMapper _postMapper;
    private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder().Build();

    public PostScanner(ILogger<PostScanner> logger, CategoryServer categoryServer, TagServer tagsServer, PostMapper postMapper, ImageService imageService)
    {
        _logger = logger;
        _categoryServer = categoryServer;
        _tagsServer = tagsServer;
        _postMapper = postMapper;
        _imageService = imageService;
    }

    public async Task<bool> ScanPosts(string basePath)
    {
        if (!Directory.Exists(basePath))
        {
            _logger.LogError("扫描路径不存在: {Path}", basePath);
            return false;
        }
        var mdFiles = Directory.GetFiles(basePath, "*.md", SearchOption.AllDirectories);
        _logger.LogInformation("找到 {Count} 个 Markdown 文件", mdFiles.Length);

        foreach (var mdFile in mdFiles)
        {
            try
            {
                await ScanSinglePost(mdFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理文件时出错: {File}", mdFile);
                return false;
            }
        }
        return true;
    }

    private async Task<PostScanResult?> ScanSinglePost(string mdFilePath)
    {
        var fileInfo = new FileInfo(mdFilePath);
        if (!fileInfo.Exists)
        {
            return null;
        }
        var content = File.ReadAllText(mdFilePath);
        var match = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", 
            RegexOptions.Singleline);
        if (!match.Success)
        {
            _logger.LogWarning("文件 {File} 缺少YAML前置数据", mdFilePath);
            return null;
        }
        var yamlContent = match.Groups[1].Value;
        var metadata = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlContent);

        var title = metadata["title"]?.ToString() ?? "";
        var author = metadata.GetValueOrDefault("author", "")?.ToString() ?? "Mr Wuliu";
        var createdAt = DateTime.TryParse(metadata["date"]?.ToString(), out DateTime parsedDate) ? parsedDate : DateTime.Now;

        // 处理分类
        var categoryIds = new List<long>();
        if (metadata.TryGetValue("categories", out var categories) && categories is List<string> categoryList)
        {
            _logger.LogInformation("开始处理分类: {Categories}", categoryList);
            foreach (var category in categoryList)
            {
                if (await _categoryServer.IsCategoryExist(category)) {
                    var categoryVo = await _categoryServer.GetCategoryByName(category);
                    categoryIds.Add(categoryVo.Id);
                } else {
                    var categoryId = await _categoryServer.CreateCategory(new CategoryVo
                    {
                        Name = category,
                        Description = ""
                    });
                    categoryIds.Add(categoryId);
                }
            }
        }
        var tags = new List<long>();
        if (metadata.TryGetValue("tags", out var tagsList) && tagsList is List<string> tagList)
        {
            foreach (var tag in tagList)
            {
                if (await _tagsServer.IsTagExist(tag)) 
                {
                    var tagId = await _tagsServer.GetTagByName(tag);
                    tags.Add(tagId.Id);
                } 
                else
                {
                    var tagId = await _tagsServer.CreateTag(new TagVo
                    {
                        Name = tag,
                    });
                    tags.Add(tagId);
                }
            }
        }
        // YAML结束, 开始处理正文
        var mainContent = content.Substring(match.Index + match.Length);
        var parts = mainContent.Split(new[] { "<!--more-->" }, 
            StringSplitOptions.RemoveEmptyEntries);
        var summary = "";
        var postContent = "";
        if(parts.Length > 1) 
        {
            summary = parts[0].Trim();
            postContent = parts[1].Trim();
        }
        else
        {
            postContent = mainContent.Replace("<!--more-->", "");
        }

        // 替换图片
        var imageFolder = Path.Combine(
            Path.GetDirectoryName(mdFilePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(mdFilePath)
        );
        var contentWithImages = await ProcessImages(
            postContent, imageFolder);

        // 构造完整的Post

        Post post = new Post
        {
            Id = 0,
            Title = title,
            Author = author,
            Content = contentWithImages,
            Summary = summary,
            CategoryId = categoryIds,
            Tags = tags,
            CreatedAt = createdAt,
            Status = "draft",
            IsPublished = false,
        };
        var postId = await _postMapper.InsertAsync(post);
        _logger.LogInformation("插入文章 {Title} 成功, ID: {Id}", title, postId);
        var result = new PostScanResult
        {
            MarkdownPath = mdFilePath,
            Title = Path.GetFileNameWithoutExtension(mdFilePath),
            LastModified = fileInfo.LastWriteTime
        };

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
    private string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
    private async Task<string> ProcessImages(string content, string imageFolder)
    {
        var imgPattern = new Regex(@"<img[^>]+src=""([^""]+)""[^>]*>", RegexOptions.Singleline);
        return await ReplaceAsync(imgPattern, content, async match =>
        {
            var imgPath = match.Groups[1].Value;
            if (imgPath.StartsWith("./"))
            {
                var fullPath = Path.Combine(imageFolder, imgPath[2..]);
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    var fileStream = File.OpenRead(fullPath);
                    var fileName = Path.GetFileName(fullPath);
                    var contentType = GetContentType(fileName);
                    // 创建 FormFile 对象，确保设置所有必要的属性
                    var formFile = new FormFile(
                        baseStream: fileStream,
                        baseStreamOffset: 0,
                        length: fileInfo.Length,
                        name: "image",
                        fileName: fileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = contentType
                    };
                    try 
                    {
                        var filename = await _imageService.UploadImageAsync(formFile);
                        return match.Value.Replace(imgPath, $"[s3://{filename}]");
                    }
                    finally
                    {
                        // 确保释放文件流
                        await fileStream.DisposeAsync();
                    }
                }
            }
            return match.Value;
        });
    }
    // 辅助扩展方法
    public static async Task<string> ReplaceAsync(
        Regex regex, 
        string input, 
        Func<Match, Task<string>> replacementFn)
    {
        var sb = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in regex.Matches(input))
        {
            sb.Append(input, lastIndex, match.Index - lastIndex);
            sb.Append(await replacementFn(match));
            lastIndex = match.Index + match.Length;
        }

        sb.Append(input, lastIndex, input.Length - lastIndex);
        return sb.ToString();
    }
}