using System.Security.Cryptography;
using System.Text.Json.Nodes;
namespace MadokaLiteBlog.Api.Extensions;

public class RsaHelper
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    public RsaHelper(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<(string publicKey, string privateKey)> GenerateAndSaveKeys()
    {
        var (publicKey, privateKey) = GenerateKeys();
        var configPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        var jsonString = await File.ReadAllTextAsync(configPath);
        var jsonNode = JsonNode.Parse(jsonString);
        if (jsonNode is JsonObject root)
        {
            var rsaObject = new JsonObject
            {
                ["PublicKey"] = publicKey,
                ["PrivateKey"] = privateKey
            };
            root["Rsa"] = rsaObject;
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(configPath, jsonNode.ToJsonString(options));
        }
        return (publicKey, privateKey);
    }

    private static (string publicKey, string privateKey) GenerateKeys()
    {
        using var rsa = RSA.Create(2048);
        
        // 使用 PEM 格式导出
        var publicKey = rsa.ExportRSAPublicKeyPem();
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        
        return (publicKey, privateKey);
    }

    public static string Encrypt(string content, string publicKey)
    {
        try 
        {
            using var rsa = RSA.Create();
            
            try 
            {
                rsa.ImportFromPem(publicKey);
            }
            catch 
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
            }
            
            var data = System.Text.Encoding.UTF8.GetBytes(content);
            var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            throw new Exception($"加密失败。公钥长度：{publicKey?.Length}，内容长度：{content?.Length}", ex);
        }
    }

    public static string Decrypt(string content, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        
        var data = Convert.FromBase64String(content);
        var decrypted = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }
}