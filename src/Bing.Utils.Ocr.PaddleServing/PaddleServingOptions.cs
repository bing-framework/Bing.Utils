namespace Bing.Ocr.PaddleServing;

/// <summary>私有化服务传输配置；构造适配器时复制。</summary>
public sealed class PaddleServingOptions
{
    /// <summary>可信服务根地址；应使用禁止自动重定向的 HttpClient。</summary>
    public Uri Endpoint { get; set; } = new("http://127.0.0.1:8080/");
    /// <summary>最大响应字节数。</summary>
    public int MaxResponseBytes { get; set; } = 4 * 1024 * 1024;
    /// <summary>最大请求图片编码字节数，防止直接调用 Provider 时绕过限制。</summary>
    public int MaxImageBytes { get; set; } = 20 * 1024 * 1024;
    /// <summary>部署的语言提示列表，不触发模型切换。</summary>
    public string[] SupportedLanguageHints { get; set; } = new[] { "zh-Hans+en", "zh-Hans", "en" };
    /// <summary>预期模型标识；设置后必须通过就绪探针及识别响应双重核对。</summary>
    public string? ExpectedModelProfileId { get; set; }

    internal PaddleServingOptions Snapshot()
    {
        var copy = new PaddleServingOptions { Endpoint = Endpoint, MaxResponseBytes = MaxResponseBytes, MaxImageBytes = MaxImageBytes,
            SupportedLanguageHints = SupportedLanguageHints == null ? Array.Empty<string>() : (string[])SupportedLanguageHints.Clone(),
            ExpectedModelProfileId = ExpectedModelProfileId };
        if (copy.Endpoint == null || !copy.Endpoint.IsAbsoluteUri ||
            (copy.Endpoint.Scheme != Uri.UriSchemeHttp && copy.Endpoint.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(copy.Endpoint.UserInfo) || !string.IsNullOrEmpty(copy.Endpoint.Query) || !string.IsNullOrEmpty(copy.Endpoint.Fragment))
            throw new ArgumentException("OCR 服务地址必须为不含凭据、查询及片段的 HTTP(S) 根地址。");
        copy.Endpoint = new Uri(copy.Endpoint.AbsoluteUri.TrimEnd('/') + "/");
        if (copy.MaxResponseBytes <= 0 || copy.MaxImageBytes <= 0 || copy.SupportedLanguageHints.Length == 0 ||
            copy.SupportedLanguageHints.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("OCR 传输配置无效。");
        if (copy.ExpectedModelProfileId != null && string.IsNullOrWhiteSpace(copy.ExpectedModelProfileId))
            throw new ArgumentException("预期模型标识不能为空白。");
        return copy;
    }
}
