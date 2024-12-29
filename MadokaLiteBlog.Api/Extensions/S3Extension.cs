using System.Security.Cryptography;
using System.Text;      
using System.Net.Http.Headers;

namespace MadokaLiteBlog.Api.Extensions;
public static class S3Extension
{
    private static string CalculateHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static byte[] HmacSHA256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return hmac.ComputeHash(dataBytes);
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        byte[] kSecret = Encoding.UTF8.GetBytes($"AWS4{key}");
        byte[] kDate = HmacSHA256(kSecret, dateStamp);
        byte[] kRegion = HmacSHA256(kDate, regionName);
        byte[] kService = HmacSHA256(kRegion, serviceName);
        byte[] kSigning = HmacSHA256(kService, "aws4_request");
        return kSigning;
    }

    private static string HexEncode(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", "").ToLower();
    }
    /// <summary>
    /// 生成临时访问URL
    /// </summary>
    public static string GeneratePresignedUrl(
        string bucketName, 
        string region, 
        string accessKey, 
        string secretKey, 
        string objectKey, 
        int expiresInMinutes = 60)
    {
        var date = DateTime.UtcNow;
        var dateStamp = date.ToString("yyyyMMdd");
        var amzDate = date.ToString("yyyyMMddTHHmmssZ");

        var canonicalUri = $"/{objectKey}";
        var canonicalQueryString = $"X-Amz-Algorithm=AWS4-HMAC-SHA256" +
                                 $"&X-Amz-Credential={Uri.EscapeDataString($"{accessKey}/{dateStamp}/{region}/s3/aws4_request")}" +
                                 $"&X-Amz-Date={amzDate}" +
                                 $"&X-Amz-Expires={expiresInMinutes * 60}" +
                                 $"&X-Amz-SignedHeaders=host";

        var canonicalHeaders = $"host:{bucketName}.s3.{region}.amazonaws.com\n";
        var signedHeaders = "host";

        var canonicalRequest = $"GET\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\nUNSIGNED-PAYLOAD";

        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{dateStamp}/{region}/s3/aws4_request\n{CalculateHash(canonicalRequest)}";
        var signingKey = GetSignatureKey(secretKey, dateStamp, region, "s3");
        var signature = HexEncode(HmacSHA256(signingKey, stringToSign));

        return $"https://{bucketName}.s3.{region}.amazonaws.com/{objectKey}?" +
               $"{canonicalQueryString}&X-Amz-Signature={signature}";
    }

    /// <summary>
    /// 上传文件到S3
    /// </summary>
    public static async Task<string> UploadFileAsync(
        HttpClient httpClient,
        Stream fileStream,
        string fileName,
        string contentType,
        string bucketName,
        string region,
        string accessKey,
        string secretKey)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var datestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        
        var canonicalUri = $"/{fileName}";
        var canonicalQueryString = "";
        var canonicalHeaders = 
            $"host:{bucketName}.s3.{region}.amazonaws.com\n" +
            $"x-amz-content-sha256:UNSIGNED-PAYLOAD\n" +
            $"x-amz-date:{date}\n";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        
        var canonicalRequest = 
            $"PUT\n" +
            $"{canonicalUri}\n" +
            $"{canonicalQueryString}\n" +
            $"{canonicalHeaders}\n" +
            $"{signedHeaders}\n" +
            "UNSIGNED-PAYLOAD";
        
        var algorithm = "AWS4-HMAC-SHA256";
        var credentialScope = $"{datestamp}/{region}/s3/aws4_request";
        var stringToSign = $"{algorithm}\n{date}\n{credentialScope}\n{CalculateHash(canonicalRequest)}";
        
        var signingKey = GetSignatureKey(secretKey, datestamp, region, "s3");
        var signature = HexEncode(HmacSHA256(signingKey, stringToSign));

        var authorization = $"{algorithm} Credential={accessKey}/{credentialScope},SignedHeaders={signedHeaders},Signature={signature}";

        var url = $"https://{bucketName}.s3.{region}.amazonaws.com/{fileName}";
        
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.TryAddWithoutValidation("host", $"{bucketName}.s3.{region}.amazonaws.com");
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", "UNSIGNED-PAYLOAD");
        request.Headers.TryAddWithoutValidation("x-amz-date", date);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        
        request.Content = new StreamContent(fileStream);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error Response: {errorContent}");
        }
        
        response.EnsureSuccessStatusCode();

        return fileName;
    }

}