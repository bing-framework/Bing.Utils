namespace Bing.Ocr;

/// <summary>图片文字检测结果；异常通过异常类型表达。</summary>
public enum OcrStatus { NoTextDetected, TextDetected }

/// <summary>只读文字行，四点框统一为顺时针排列。</summary>
public sealed class OcrLine
{
    /// <summary>创建文字行；复制四点框并拒绝退化或自交的框。</summary>
    public OcrLine(string text, IEnumerable<OcrPoint> polygon, double? confidence = null, bool isLowConfidence = false)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        if (polygon == null) throw new ArgumentNullException(nameof(polygon));
        var points = polygon.Take(5).ToArray();
        if (points.Length != 4) throw new ArgumentException("文字框必须包含四点。", nameof(polygon));
        double sign = 0;
        for (var i = 0; i < 4; i++)
        {
            var a = points[i]; var b = points[(i + 1) % 4]; var c = points[(i + 2) % 4];
            var cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
            if (!OcrPoint.IsFinite(cross) || cross == 0 || (sign != 0 && Math.Sign(sign) != Math.Sign(cross)))
                throw new ArgumentException("文字框必须为非退化凸四边形。", nameof(polygon));
            sign = cross;
        }
        if (sign < 0) Array.Reverse(points);
        if (confidence.HasValue && (!OcrPoint.IsFinite(confidence.Value) || confidence < 0 || confidence > 1))
            throw new ArgumentOutOfRangeException(nameof(confidence));
        Polygon = Array.AsReadOnly(points); Confidence = confidence; IsLowConfidence = isLowConfidence;
    }
    /// <summary>识别原文，不进行纠错或空白归一化。</summary>
    public string Text { get; }
    /// <summary>顺时针四点框；允许边界坐标等于图片宽高。</summary>
    public IReadOnlyList<OcrPoint> Polygon { get; }
    /// <summary>引擎识别分数，缺失时为空；不代表实际准确率。</summary>
    public double? Confidence { get; }
    /// <summary>是否低于请求设置的显示阈值。</summary>
    public bool IsLowConfidence { get; }
}

/// <summary>预处理图片及从处理图回到完整显示图的变换；图像字节由对象独立持有。</summary>
public sealed class PreparedOcrImage
{
    private readonly byte[] _bytes;
    /// <summary>创建处理结果；复制编码字节与警告集合。</summary>
    public PreparedOcrImage(byte[] bytes, int width, int height, int originalWidth, int originalHeight,
        OcrTransform toOriginal, IEnumerable<string>? warnings = null)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length == 0) throw new ArgumentException("编码图片不能为空。", nameof(bytes));
        if (width <= 0 || height <= 0 || originalWidth <= 0 || originalHeight <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        _bytes = (byte[])bytes.Clone(); Width = width; Height = height; OriginalWidth = originalWidth; OriginalHeight = originalHeight;
        ToOriginal = toOriginal ?? throw new ArgumentNullException(nameof(toOriginal));
        Warnings = Array.AsReadOnly((warnings ?? Array.Empty<string>()).ToArray());
    }
    /// <summary>处理图宽度。</summary>
    public int Width { get; }
    /// <summary>处理图高度。</summary>
    public int Height { get; }
    /// <summary>完整显示图宽度。</summary>
    public int OriginalWidth { get; }
    /// <summary>完整显示图高度。</summary>
    public int OriginalHeight { get; }
    /// <summary>编码字节数。</summary>
    public int ByteLength => _bytes.Length;
    /// <summary>回到完整显示图的逆变换。</summary>
    public OcrTransform ToOriginal { get; }
    /// <summary>结构化处理提示。</summary>
    public IReadOnlyList<string> Warnings { get; }
    /// <summary>获取独立的编码字节副本。</summary>
    public byte[] CopyBytes() => (byte[])_bytes.Clone();
    /// <summary>创建借用内部编码数据的只读流，调用方负责关闭返回流。</summary>
    public Stream OpenRead() => new MemoryStream(_bytes, false);
}

/// <summary>引擎返回的处理图坐标结果。</summary>
public sealed class OcrProviderResult
{
    /// <summary>创建结果，复制文字行集合；身份已验证时必须有模型配置标识。</summary>
    public OcrProviderResult(IEnumerable<OcrLine> lines, string? modelProfileId = null, bool modelIdentityVerified = false)
    {
        if (lines == null) throw new ArgumentNullException(nameof(lines));
        var copy = lines.ToArray();
        if (copy.Any(x => x == null)) throw new ArgumentException("文字行不能为 null。", nameof(lines));
        if (modelIdentityVerified && string.IsNullOrWhiteSpace(modelProfileId)) throw new ArgumentException("缺少已验证的模型身份。");
        Lines = Array.AsReadOnly(copy); ModelProfileId = modelProfileId; ModelIdentityVerified = modelIdentityVerified;
    }
    /// <summary>按引擎阅读顺序排列的文字行。</summary>
    public IReadOnlyList<OcrLine> Lines { get; }
    /// <summary>上游报告的模型配置标识；没有报告时为空。</summary>
    public string? ModelProfileId { get; }
    /// <summary>是否已与就绪探针及预期配置核对身份。</summary>
    public bool ModelIdentityVerified { get; }
}

/// <summary>可定位到完整显示图的只读识别结果。</summary>
public sealed class OcrResult
{
    internal OcrResult(PreparedOcrImage image, IEnumerable<OcrLine> lines, string provider, OcrProviderResult result, TimeSpan elapsed)
    {
        Lines = Array.AsReadOnly(lines.ToArray());
        Text = string.Join("\n", Lines.Select(x => x.Text));
        Status = Lines.Count == 0 ? OcrStatus.NoTextDetected : OcrStatus.TextDetected;
        ImageWidth = image.OriginalWidth; ImageHeight = image.OriginalHeight;
        Provider = provider; ModelProfileId = result.ModelProfileId; ModelIdentityVerified = result.ModelIdentityVerified;
        Elapsed = elapsed; Warnings = Array.AsReadOnly(image.Warnings.ToArray());
    }
    /// <summary>统一结果版本。</summary>
    public int SchemaVersion => 1;
    /// <summary>识别状态。</summary>
    public OcrStatus Status { get; }
    /// <summary>以 LF 拼接的全文。</summary>
    public string Text { get; }
    /// <summary>完整显示图宽度。</summary>
    public int ImageWidth { get; }
    /// <summary>完整显示图高度。</summary>
    public int ImageHeight { get; }
    /// <summary>坐标基准为 EXIF 校正后的完整图像素。</summary>
    public string CoordinateSystem => "UprightImagePixels";
    /// <summary>阅读顺序不变的只读文字行。</summary>
    public IReadOnlyList<OcrLine> Lines { get; }
    /// <summary>识别引擎名称。</summary>
    public string Provider { get; }
    /// <summary>上游报告的模型配置标识。</summary>
    public string? ModelProfileId { get; }
    /// <summary>模型身份是否已核对。</summary>
    public bool ModelIdentityVerified { get; }
    /// <summary>包含等待的总耗时。</summary>
    public TimeSpan Elapsed { get; }
    /// <summary>处理提示。</summary>
    public IReadOnlyList<string> Warnings { get; }
}
