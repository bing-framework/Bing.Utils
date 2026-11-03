using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Bing.Ocr.PaddleServing;

/// <summary>PaddleOCR 单图 HTTP 适配器；借用 HttpClient，不重试，不下载模型。</summary>
public sealed class PaddleServingOcrProvider : IOcrProvider
{
    private readonly HttpClient _client;
    private readonly PaddleServingOptions _options;
    /// <summary>创建适配器；传入客户端的生命周期由宿主管理。</summary>
    public PaddleServingOcrProvider(HttpClient client, PaddleServingOptions? options = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = (options ?? new PaddleServingOptions()).Snapshot();
    }
    /// <inheritdoc />
    public string Name => "PaddleServing";
    /// <inheritdoc />
    public void ValidateOptions(OcrOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (!_options.SupportedLanguageHints.Contains(options.LanguageHint, StringComparer.Ordinal))
            throw new OcrCapabilityException("已部署模型不支持请求的语言提示。");
    }
    /// <inheritdoc />
    public async Task<OcrProviderResult> RecognizeAsync(PreparedOcrImage image, OcrOptions options, CancellationToken cancellationToken = default)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        ValidateOptions(options); cancellationToken.ThrowIfCancellationRequested();
        if (image.ByteLength > _options.MaxImageBytes) throw new OcrInputException("prepared_image_too_large", "Provider 输入超过限制。");
        try
        {
            if (_options.ExpectedModelProfileId != null)
                await ValidateIdentityAsync(cancellationToken).ConfigureAwait(false);
            var body = JsonSerializer.SerializeToUtf8Bytes(new
            {
                file = Convert.ToBase64String(image.CopyBytes()), fileType = 1,
                useDocOrientationClassify = false, useDocUnwarping = false,
                useTextlineOrientation = true, textRecScoreThresh = 0.0, visualize = false
            });
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.Endpoint, "ocr"));
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            CheckStatus(response.StatusCode);
            var bytes = await ReadResponseAsync(response, _options.MaxResponseBytes, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var result = PaddleResponseParser.Parse(bytes, image.Width, image.Height, _options.ExpectedModelProfileId);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested) { throw new OcrTimeoutException(ex); }
        catch (HttpRequestException ex) { throw new OcrProviderException("无法访问 OCR 服务。", ex); }
        catch (IOException ex) { throw new OcrProviderException("读取 OCR 响应失败。", ex); }
    }

    private async Task ValidateIdentityAsync(CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.Endpoint, "health/ready"));
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        CheckStatus(response.StatusCode);
        var bytes = await ReadResponseAsync(response, Math.Min(_options.MaxResponseBytes, 65536), token).ConfigureAwait(false);
        try
        {
            using var doc = JsonDocument.Parse(bytes);
            if (!doc.RootElement.GetProperty("ready").GetBoolean() ||
                doc.RootElement.GetProperty("modelProfileId").GetString() != _options.ExpectedModelProfileId)
                throw new OcrProviderException("就绪探针与预期模型配置不一致。");
        }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException)
        { throw new OcrProviderException("就绪探针响应无效。", ex); }
    }

    private static void CheckStatus(HttpStatusCode status)
    {
        if ((int)status == 429) throw new OcrBusyException();
        if ((int)status == 408 || (int)status == 504) throw new OcrTimeoutException();
        if ((int)status < 200 || (int)status >= 300)
            throw new OcrProviderException("OCR 服务返回 HTTP " + (int)status + "。");
    }

    private static async Task<byte[]> ReadResponseAsync(HttpResponseMessage response, int limit, CancellationToken token)
    {
        if (response.Content == null) throw new OcrProviderException("OCR 响应缺少正文。");
        if (response.Content.Headers.ContentLength > limit) throw new OcrProviderException("OCR 响应超过大小限制。");
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, (long)limit - output.Length + 1), token).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length + read > limit) throw new OcrProviderException("OCR 响应超过大小限制。");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }
}
