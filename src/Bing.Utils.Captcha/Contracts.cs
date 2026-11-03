using System.Text;

namespace Bing.Captcha;

/// <summary>验证码用途、服务端会话绑定和表单流程的不可变上下文。</summary>
public sealed class CaptchaContext
{
    /// <summary>创建上下文；字段须为非空、无控制字符的文本，长度分别不超过 64、256、128。</summary>
    /// <param name="purpose">服务端固定且列入允许清单的用途，区分大小写。</param>
    /// <param name="bindingId">从可信会话取得的绑定值，不接受客户端自报身份。</param>
    /// <param name="flowId">同一会话内的表单或标签页标识，不用作限流身份。</param>
    /// <exception cref="ArgumentException">字段为空、超长或包含控制字符、代理项。</exception>
    public CaptchaContext(string purpose, string bindingId, string flowId)
    {
        Purpose = Check(purpose, 64, nameof(purpose));
        BindingId = Check(bindingId, 256, nameof(bindingId));
        FlowId = Check(flowId, 128, nameof(flowId));
    }
    /// <summary>获取业务用途。</summary>
    public string Purpose { get; }
    /// <summary>获取可信的会话绑定标识。</summary>
    public string BindingId { get; }
    /// <summary>获取表单流程标识。</summary>
    public string FlowId { get; }
    /// <summary>获取长度前缀 UTF-8 字段的 SHA-256 摘要，不暴露原始绑定值。</summary>
    /// <returns>用于存储定位的 64 位小写十六进制文本，不承担身份认证职责。</returns>
    public string GetStorageKey()
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encode(Purpose, BindingId, FlowId))).Replace("-", "").ToLowerInvariant();
    }
    internal static byte[] Encode(params string[] fields)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        foreach (var field in fields)
        {
            var bytes = Encoding.UTF8.GetBytes(field);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
        return stream.ToArray();
    }
    internal static string Check(string value, int max, string name)
    {
        if (value == null) throw new ArgumentNullException(name);
        if (string.IsNullOrWhiteSpace(value) || value.Length > max || value.Any(char.IsControl) || value.Any(char.IsSurrogate))
            throw new ArgumentException("字段为空、过长或包含不支持的字符。", name);
        return value;
    }
}

/// <summary>已编码的 PNG 图片；构造和访问均复制字节，调用方无需释放原生资源。</summary>
public sealed class CaptchaImage
{
    private readonly byte[] _bytes;
    /// <summary>创建图片结果，尺寸不得超过 512×256。</summary>
    /// <param name="bytes">PNG 编码数据；复制输入，校验签名，具体解码正确性由渲染器负责。</param>
    /// <param name="width">像素宽度，范围 1～512。</param>
    /// <param name="height">像素高度，范围 1～256。</param>
    public CaptchaImage(byte[] bytes, int width, int height)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length < 8 || !bytes.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}))
            throw new ArgumentException("必须返回 PNG 数据。", nameof(bytes));
        if (width < 1 || width > 512) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1 || height > 256) throw new ArgumentOutOfRangeException(nameof(height));
        _bytes = (byte[])bytes.Clone(); Width = width; Height = height;
    }
    /// <summary>获取独立的 PNG 字节副本。</summary>
    public byte[] Bytes => (byte[])_bytes.Clone();
    /// <summary>获取编码长度。</summary>
    public int ByteLength => _bytes.Length;
    /// <summary>获取媒体类型。</summary>
    public string ContentType => "image/png";
    /// <summary>获取像素宽度。</summary>
    public int Width { get; }
    /// <summary>获取像素高度。</summary>
    public int Height { get; }
}

/// <summary>可向客户端返回的挑战，包含 ID、图片和 UTC 过期时间。</summary>
public sealed class CaptchaChallenge
{
    internal CaptchaChallenge(string id, CaptchaImage image, DateTimeOffset expiresAt) { CaptchaId = id; Image = image; ExpiresAt = expiresAt; }
    /// <summary>获取随机挑战 ID。</summary>
    public string CaptchaId { get; }
    /// <summary>获取图片。</summary>
    public CaptchaImage Image { get; }
    /// <summary>获取绝对过期时间。</summary>
    public DateTimeOffset ExpiresAt { get; }
}

/// <summary>仅供服务端保存的不可变校验记录。</summary>
public sealed class CaptchaEntry
{
    private readonly byte[] _hash;
    /// <summary>创建校验记录；摘要必须为 32 字节，规范化版本必须为 1。</summary>
    /// <param name="captchaId">非空挑战 ID，最多 64 个字符。</param>
    /// <param name="answerHash">HMAC-SHA256 摘要，构造时复制。</param>
    /// <param name="keyVersion">密钥快照版本，最多 64 个字符。</param>
    /// <param name="expiresAt">绝对过期时间，转换为 UTC；持久化使用毫秒精度。</param>
    /// <param name="ignoreAsciiCase">生成时固定的 ASCII 大小写策略。</param>
    /// <param name="normalizationVersion">规范化协议，当前仅支持版本 1。</param>
    public CaptchaEntry(string captchaId, byte[] answerHash, string keyVersion, DateTimeOffset expiresAt, bool ignoreAsciiCase, int normalizationVersion = 1)
    {
        CaptchaId = CaptchaContext.Check(captchaId, 64, nameof(captchaId));
        KeyVersion = CaptchaContext.Check(keyVersion, 64, nameof(keyVersion));
        if (answerHash == null) throw new ArgumentNullException(nameof(answerHash));
        if (answerHash.Length != 32) throw new ArgumentException("摘要必须为 32 字节。", nameof(answerHash));
        if (normalizationVersion != 1) throw new ArgumentOutOfRangeException(nameof(normalizationVersion));
        _hash = (byte[])answerHash.Clone(); ExpiresAt = expiresAt.ToUniversalTime(); IgnoreAsciiCase = ignoreAsciiCase; NormalizationVersion = normalizationVersion;
    }
    /// <summary>获取挑战 ID。</summary>
    public string CaptchaId { get; }
    /// <summary>获取摘要的独立副本。</summary>
    public byte[] AnswerHash => (byte[])_hash.Clone();
    /// <summary>获取密钥版本。</summary>
    public string KeyVersion { get; }
    /// <summary>获取绝对过期时间。</summary>
    public DateTimeOffset ExpiresAt { get; }
    /// <summary>获取 ASCII 大小写规则。</summary>
    public bool IgnoreAsciiCase { get; }
    /// <summary>获取规范化协议版本。</summary>
    public int NormalizationVersion { get; }
}

/// <summary>校验结果；默认值为失效。</summary>
public enum CaptchaVerifyStatus
{
    /// <summary>记录不存在、已消费、过期或密钥版本不可用。</summary>
    InvalidOrExpired = 0,
    /// <summary>答案匹配且当前记录已消费。</summary>
    Success = 1,
    /// <summary>答案不匹配且当前记录已消费。</summary>
    Mismatch = 2
}
/// <summary>普通校验结果，不承载基础设施故障。</summary>
public sealed class CaptchaVerifyResult
{
    internal CaptchaVerifyResult(CaptchaVerifyStatus status) { Status = status; }
    /// <summary>获取状态。</summary>
    public CaptchaVerifyStatus Status { get; }
    /// <summary>获取是否成功。</summary>
    public bool Succeeded => Status == CaptchaVerifyStatus.Success;
}
/// <summary>验证码生命周期入口。</summary>
public interface ICaptchaService
{
    /// <summary>渲染成功后替换当前挑战；失败时不返回挑战。</summary>
    /// <param name="context">用途、可信会话及流程上下文。</param>
    /// <param name="cancellationToken">取消令牌；存储派发后取消不保证状态回滚。</param>
    /// <returns>只含 ID、PNG 及绝对过期时间的公开挑战。</returns>
    /// <exception cref="CaptchaRendererException">图片渲染或输出大小检查失败，尚未替换旧记录。</exception>
    /// <exception cref="CaptchaStoreException">存储失败；网络派发后的结果可能未知。</exception>
    /// <exception cref="OperationCanceledException">操作取消，不应当作答案错误。</exception>
    Task<CaptchaChallenge> CreateAsync(CaptchaContext context, CancellationToken cancellationToken = default);
    /// <summary>原子消费当前挑战并校验；错误答案也消费，格式错误不消费。</summary>
    /// <param name="context">必须与生成时三个字段完全一致。</param>
    /// <param name="captchaId">当前挑战 ID，非空且最多 64 个字符。</param>
    /// <param name="answer">原始答案及 FormKC 规范化结果均最多 64 个字符；允许首尾普通空格。</param>
    /// <param name="cancellationToken">取消令牌；消费派发后不能保证恢复记录。</param>
    /// <returns>成功、失效或不匹配；默认状态为失效。</returns>
    /// <exception cref="ArgumentException">输入格式非法；此时不会调用存储。</exception>
    /// <exception cref="CaptchaStoreException">消费操作失败，业务必须拒绝放行。</exception>
    /// <exception cref="OperationCanceledException">取消等待；已经完成的消费不会回滚。</exception>
    Task<CaptchaVerifyResult> VerifyAsync(CaptchaContext context, string captchaId, string answer, CancellationToken cancellationToken = default);
}
/// <summary>线程安全的 PNG 渲染接口；原生临时资源由实现释放。</summary>
public interface ICaptchaRenderer
{
    /// <summary>渲染指定文本，不得返回答案元数据。</summary>
    /// <param name="text">服务生成的受控文本。</param>
    /// <param name="cancellationToken">取消令牌，实现应在分配原生图片前检查。</param>
    /// <returns>独立拥有托管字节的 PNG 结果。</returns>
    CaptchaImage Render(string text, CancellationToken cancellationToken = default);
}
/// <summary>可选的渲染能力校验接口，在服务构造阶段拒绝不兼容的字符集，不生成图片。</summary>
/// <remarks>具有字符集限制的自定义渲染器应实现此接口；未实现的渲染器由宿主保证兼容。</remarks>
public interface ICaptchaRendererValidator
{
    /// <summary>验证生成器可能产生的全部字符均受当前渲染配置支持。</summary>
    /// <param name="alphabet">去重后的实际生成字符集，保留生成时的大小写。</param>
    /// <exception cref="ArgumentException">字符集为空或包含渲染器不支持的字符。</exception>
    void ValidateAlphabet(string alphabet);
}
/// <summary>原子替换和消费存储。网络操作派发后的取消或超时不保证回滚。</summary>
public interface ICaptchaStore
{
    /// <summary>原子替换同一上下文，并设置绝对过期。</summary>
    /// <param name="context">存储作用域，三个字段共同构成键。</param>
    /// <param name="entry">不可变记录，不能保存明文答案或图片。</param>
    /// <param name="cancellationToken">调用前取消不派发；网络派发后不保证回滚。</param>
    /// <returns>写入完成任务，同一上下文以最后提交的写入为准。</returns>
    Task ReplaceAsync(CaptchaContext context, CaptchaEntry entry, CancellationToken cancellationToken = default);
    /// <summary>仅当 ID 匹配时取出并删除；不匹配不得删除新挑战。</summary>
    /// <param name="context">存储作用域。</param>
    /// <param name="captchaId">待消费的 ID，以序号方式精确比较。</param>
    /// <param name="cancellationToken">取消等待令牌，不代表已消费记录可以恢复。</param>
    /// <returns>匹配的记录或 null；服务仍须检查返回记录的绝对过期时间。</returns>
    Task<CaptchaEntry?> TryTakeAsync(CaptchaContext context, string captchaId, CancellationToken cancellationToken = default);
}
/// <summary>可替换的 UTC 时钟。</summary>
public interface ICaptchaClock { /// <summary>获取当前 UTC 时间。</summary>
    DateTimeOffset UtcNow { get; } }
/// <summary>系统 UTC 时钟。</summary>
public sealed class SystemCaptchaClock : ICaptchaClock { /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
/// <summary>验证码存储故障，业务必须拒绝放行。</summary>
public sealed class CaptchaStoreException : Exception { /// <summary>创建存储故障。</summary>
    public CaptchaStoreException(string message, Exception? inner = null) : base(message, inner) {} }
/// <summary>验证码渲染故障。</summary>
public sealed class CaptchaRendererException : Exception { /// <summary>创建渲染故障。</summary>
    public CaptchaRendererException(string message, Exception? inner = null) : base(message, inner) {} }
