namespace Bing.Ocr;

/// <summary>预处理预设；标准模式保留颜色，扫描模式增加灰度化。</summary>
public enum OcrPreprocessingPreset { Standard, Scan }

/// <summary>单次识别选项；服务在调用开始时复制配置。</summary>
public sealed class OcrOptions
{
    /// <summary>语言提示，由 Provider 检查已部署模型是否支持。</summary>
    public string LanguageHint { get; set; } = "zh-Hans+en";
    /// <summary>EXIF 校正后的完整图片中的矩形区域；为空时使用整图。</summary>
    public OcrRectangle? Region { get; set; }
    /// <summary>区域裁切后顺时针旋转角度，仅接受 0、90、180、270。</summary>
    public int RotationDegrees { get; set; }
    /// <summary>旋转后缩放比例；默认保持尺寸，结果仍受像素限制约束。</summary>
    public double Scale { get; set; } = 1;
    /// <summary>图像预处理预设。</summary>
    public OcrPreprocessingPreset Preprocessing { get; set; }
    /// <summary>低于此分数时标记低置信度；只作显示提示，不过滤文字。</summary>
    public double LowConfidenceThreshold { get; set; } = 0.6;

    internal OcrOptions Snapshot()
    {
        var copy = new OcrOptions { LanguageHint = LanguageHint, Region = Region, RotationDegrees = RotationDegrees,
            Scale = Scale, Preprocessing = Preprocessing, LowConfidenceThreshold = LowConfidenceThreshold };
        if (string.IsNullOrWhiteSpace(copy.LanguageHint)) throw new ArgumentException("语言提示不能为空。", nameof(LanguageHint));
        if (copy.RotationDegrees != 0 && copy.RotationDegrees != 90 && copy.RotationDegrees != 180 && copy.RotationDegrees != 270)
            throw new ArgumentOutOfRangeException(nameof(RotationDegrees));
        if (!OcrPoint.IsFinite(copy.Scale) || copy.Scale <= 0) throw new ArgumentOutOfRangeException(nameof(Scale));
        if (copy.Preprocessing != OcrPreprocessingPreset.Standard && copy.Preprocessing != OcrPreprocessingPreset.Scan)
            throw new ArgumentOutOfRangeException(nameof(Preprocessing));
        if (!OcrPoint.IsFinite(copy.LowConfidenceThreshold) || copy.LowConfidenceThreshold < 0 || copy.LowConfidenceThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(LowConfidenceThreshold));
        return copy;
    }
}

/// <summary>共享服务的资源上限；不代表原生进程的内存硬上限。</summary>
public sealed class OcrServiceOptions
{
    /// <summary>编码输入的最大字节数。</summary>
    public int MaxInputBytes { get; set; } = 10 * 1024 * 1024;
    /// <summary>完整图片及处理中间图片允许的最大像素数。</summary>
    public long MaxPixels { get; set; } = 20_000_000;
    /// <summary>图片允许的最大单边尺寸。</summary>
    public int MaxDimension { get; set; } = 8192;
    /// <summary>预处理后编码图片最大字节数。</summary>
    public int MaxPreparedBytes { get; set; } = 20 * 1024 * 1024;
    /// <summary>允许同时执行的识别数。</summary>
    public int MaxConcurrency { get; set; } = 1;
    /// <summary>允许等待执行的请求数；满载时立即拒绝。</summary>
    public int MaxQueuedRequests { get; set; } = 8;
    /// <summary>覆盖排队、读取及识别的合作取消期限；不可中断原生调用需由进程隔离限制。</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    internal OcrServiceOptions Snapshot()
    {
        var copy = new OcrServiceOptions { MaxInputBytes = MaxInputBytes, MaxPixels = MaxPixels, MaxDimension = MaxDimension,
            MaxPreparedBytes = MaxPreparedBytes, MaxConcurrency = MaxConcurrency, MaxQueuedRequests = MaxQueuedRequests, Timeout = Timeout };
        if (copy.MaxInputBytes <= 0 || copy.MaxPixels <= 0 || copy.MaxDimension <= 0 || copy.MaxPreparedBytes <= 0 ||
            copy.MaxConcurrency <= 0 || copy.MaxQueuedRequests < 0 || (long)copy.MaxConcurrency + copy.MaxQueuedRequests > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(OcrServiceOptions), "资源限制无效。");
        if (copy.Timeout <= TimeSpan.Zero || copy.Timeout.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(Timeout));
        return copy;
    }
    internal void CheckSize(int width, int height)
    {
        if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension || (long)width * height > MaxPixels)
            throw new OcrInputException("image_dimensions_exceeded", "图片尺寸超过限制。");
    }
}
