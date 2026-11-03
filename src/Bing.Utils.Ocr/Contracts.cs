namespace Bing.Ocr;

/// <summary>识别单张图片；输入流从当前位置读取，调用结束后保持打开。</summary>
public interface IOcrService
{
    /// <summary>识别图片并返回完整显示图坐标的结果。</summary>
    Task<OcrResult> RecognizeAsync(Stream image, OcrOptions? options = null, CancellationToken cancellationToken = default);
}
/// <summary>无共享图像状态的预处理器；所有原生临时资源由实现释放。</summary>
public interface IOcrImagePreprocessor
{
    /// <summary>检查图片并预处理；不得修改传入字节和配置；返回结果不包含原生资源。</summary>
    PreparedOcrImage Prepare(byte[] encodedImage, OcrOptions options, OcrServiceOptions limits, CancellationToken cancellationToken = default);
}
/// <summary>线程安全的识别引擎；只处理准备好的单图，返回处理图坐标。</summary>
public interface IOcrProvider
{
    /// <summary>固定引擎名称。</summary>
    string Name { get; }
    /// <summary>在读取图片之前检查语言及请求能力，不执行网络或模型下载。</summary>
    void ValidateOptions(OcrOptions options);
    /// <summary>识别图片；实现应遵守取消令牌，停止使用输入后才完成任务。</summary>
    Task<OcrProviderResult> RecognizeAsync(PreparedOcrImage image, OcrOptions options, CancellationToken cancellationToken = default);
}
/// <summary>文件路径便捷方法；自行管理文件流。</summary>
public static class OcrServiceExtensions
{
    /// <summary>打开文件并识别，成功或失败均关闭文件。</summary>
    public static async Task<OcrResult> RecognizeFileAsync(this IOcrService service, string path, OcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("路径不能为空。", nameof(path));
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = File.OpenRead(path);
        return await service.RecognizeAsync(stream, options, cancellationToken).ConfigureAwait(false);
    }
}
/// <summary>图片输入错误，提供稳定错误码。</summary>
public sealed class OcrInputException : Exception
{
    /// <summary>创建输入错误。</summary>
    public OcrInputException(string code, string message, Exception? inner = null) : base(message, inner) { Code = code; }
    /// <summary>稳定错误码。</summary>
    public string Code { get; }
}
/// <summary>所选引擎无法满足请求能力。</summary>
public sealed class OcrCapabilityException : Exception
{
    /// <summary>创建能力错误。</summary>
    public OcrCapabilityException(string message) : base(message) { }
}
/// <summary>识别服务通信、推理或响应协议故障。</summary>
public sealed class OcrProviderException : Exception
{
    /// <summary>创建 Provider 故障。</summary>
    public OcrProviderException(string message, Exception? inner = null) : base(message, inner) { }
}
/// <summary>客户端或模型服务容量已满。</summary>
public sealed class OcrBusyException : Exception
{
    /// <summary>创建繁忙错误。</summary>
    public OcrBusyException() : base("OCR 服务繁忙，请稍后重试。") { }
}
/// <summary>识别期限已到；远端或原生推理可能仍在执行。</summary>
public sealed class OcrTimeoutException : TimeoutException
{
    /// <summary>创建超时错误。</summary>
    public OcrTimeoutException(Exception? inner = null) : base("OCR 识别超过配置的期限。", inner) { }
}
