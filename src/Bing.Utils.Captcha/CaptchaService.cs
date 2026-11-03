using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bing.Drawing;
using Bing.Security.Authentication;
using Bing.Security.Randomness;

namespace Bing.Captcha;

/// <summary>验证码配置；服务构造时复制并验证。</summary>
public sealed class CaptchaServiceOptions
{
    /// <summary>获取或设置字符数，范围 4～8。</summary>
    public int Length { get; set; } = 5;
    /// <summary>获取或设置字符类型。</summary>
    public CaptchaType Type { get; set; } = CaptchaType.NumberAndLetter;
    /// <summary>获取或设置有效期，范围 30～300 秒。</summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromSeconds(120);
    /// <summary>获取或设置是否忽略 ASCII 大小写。</summary>
    public bool IgnoreAsciiCase { get; set; } = true;
    /// <summary>获取或设置图片字节上限，范围 1～1048576，默认 65536。</summary>
    public int MaxImageBytes { get; set; } = 65536;
    /// <summary>获取或设置允许的用途；用途也用作有界指标标签。</summary>
    public string[] AllowedPurposes { get; set; } = new[] { "login", "register", "password-reset" };
    /// <summary>获取或设置指标中的固定后端名称，长度不超过 32；不得使用用户输入。</summary>
    public string BackendName { get; set; } = "custom";
    internal CaptchaServiceOptions Snapshot()
    {
        if (Length < 4 || Length > 8) throw new ArgumentOutOfRangeException(nameof(Length));
        if (Lifetime < TimeSpan.FromSeconds(30) || Lifetime > TimeSpan.FromSeconds(300)) throw new ArgumentOutOfRangeException(nameof(Lifetime));
        if (MaxImageBytes < 1 || MaxImageBytes > 1048576) throw new ArgumentOutOfRangeException(nameof(MaxImageBytes));
        if (!Enum.IsDefined(typeof(CaptchaType), Type)) throw new ArgumentOutOfRangeException(nameof(Type));
        if (AllowedPurposes == null || AllowedPurposes.Length == 0 || AllowedPurposes.Length > 32) throw new ArgumentException("必须配置 1～32 个用途。", nameof(AllowedPurposes));
        return new CaptchaServiceOptions { Length = Length, Type = Type, Lifetime = Lifetime, IgnoreAsciiCase = IgnoreAsciiCase, MaxImageBytes = MaxImageBytes, BackendName = CaptchaContext.Check(BackendName, 32, nameof(BackendName)),
            AllowedPurposes = AllowedPurposes.Select(p => CaptchaContext.Check(p, 64, nameof(AllowedPurposes))).Distinct(StringComparer.Ordinal).ToArray() };
    }
}

/// <summary>公开受控字符集，供渲染器启动时检查字形覆盖。</summary>
public static class CaptchaAlphabet
{
    /// <summary>获取去重字符集；中文使用现有共享白名单。</summary>
    /// <param name="type">数字、字母数字或中文类型。</param>
    /// <param name="ignoreAsciiCase">为 true 时折叠为大写并去重。</param>
    /// <returns>可用于字形覆盖检查的完整字符集。</returns>
    public static string GetCharacters(CaptchaType type, bool ignoreAsciiCase = true)
    {
        string chars;
        switch (type)
        {
            case CaptchaType.Number: chars = "0123456789"; break;
            case CaptchaType.NumberAndLetter: chars = "23456789ABCDEFGHJKMNPQRSTUVWXYZabcdefghkmnpqrstuvwxyz"; break;
            case CaptchaType.ChineseChar: chars = Bing.Drawing.Internal.ChineseCaptchaGlyphSet.Chars; break;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
        return new string(chars.Select(c => ignoreAsciiCase && c >= 'a' && c <= 'z' ? (char)(c - 32) : c).Distinct().ToArray());
    }
    internal static string Generate(string alphabet, int length)
    {
        // 拒绝采样避免取模偏差；一次生成复用随机源和缓冲区。
        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[64]; var output = new char[length]; var written = 0;
        var limit = 65536 - 65536 % alphabet.Length;
        while (written < length)
        {
            rng.GetBytes(buffer);
            for (var i = 0; i < buffer.Length && written < length; i += 2)
            {
                var value = buffer[i] | (buffer[i + 1] << 8);
                if (value < limit) output[written++] = alphabet[value % alphabet.Length];
            }
        }
        Array.Clear(buffer, 0, buffer.Length);
        return new string(output);
    }
}

/// <summary>线程安全的验证码服务；不拥有注入的存储、渲染器和密钥提供器。</summary>
public sealed class CaptchaService : ICaptchaService
{
    private static readonly Meter Meter = new("Bing.Utils.Captcha", "1.0");
    private static readonly Counter<long> Events = Meter.CreateCounter<long>("captcha.events");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("captcha.duration", "ms");
    private static readonly Histogram<long> ImageSize = Meter.CreateHistogram<long>("captcha.image.bytes", "By");
    private readonly ICaptchaStore _store;
    private readonly ICaptchaRenderer _renderer;
    private readonly ICaptchaKeyProvider _keys;
    private readonly ICaptchaClock _clock;
    private readonly CaptchaServiceOptions _options;
    private readonly string _alphabet;
    /// <summary>创建服务并验证配置和当前密钥。</summary>
    /// <param name="store">线程安全的存储，实现原子替换和消费。</param>
    /// <param name="renderer">线程安全的 PNG 渲染器。</param>
    /// <param name="keys">提供原子密钥快照的提供器。</param>
    /// <param name="options">构造时复制的配置；null 使用默认值。</param>
    /// <param name="clock">可注入的 UTC 时钟；null 使用系统时间。</param>
    public CaptchaService(ICaptchaStore store, ICaptchaRenderer renderer, ICaptchaKeyProvider keys, CaptchaServiceOptions? options = null, ICaptchaClock? clock = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store)); _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys)); _clock = clock ?? new SystemCaptchaClock();
        _options = (options ?? new CaptchaServiceOptions()).Snapshot(); _alphabet = CaptchaAlphabet.GetCharacters(_options.Type, _options.IgnoreAsciiCase);
        if (_renderer is ICaptchaRendererValidator validator) validator.ValidateAlphabet(_alphabet);
        if (_keys.GetCurrentKey() == null) throw new ArgumentException("必须配置当前密钥。", nameof(keys));
    }
    /// <inheritdoc />
    public async Task<CaptchaChallenge> CreateAsync(CaptchaContext context, CancellationToken cancellationToken = default)
    {
        Validate(context); cancellationToken.ThrowIfCancellationRequested();
        var code = CaptchaAlphabet.Generate(_alphabet, _options.Length);
        var id = SecurityRandom.GetBase64Url(16);
        CaptchaImage image;
        var timer = Stopwatch.StartNew();
        try { image = _renderer.Render(code, cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception e) { Count(context, "render_failed"); throw new CaptchaRendererException("验证码渲染失败。", e); }
        finally { RecordDuration(context, timer, "render"); }
        if (image == null || image.ByteLength > _options.MaxImageBytes)
        {
            Count(context, "render_failed");
            throw new CaptchaRendererException("图片为空或超出大小限制。");
        }
        cancellationToken.ThrowIfCancellationRequested();
        var key = _keys.GetCurrentKey();
        // 毫秒精度与 Redis PXAT 一致，渲染耗时不占用有效期。
        var expires = DateTimeOffset.FromUnixTimeMilliseconds(_clock.UtcNow.Add(_options.Lifetime).ToUnixTimeMilliseconds());
        var hash = Compute(context, id, expires, _options.IgnoreAsciiCase, 1, Normalize(code, _options.IgnoreAsciiCase), key);
        var entry = new CaptchaEntry(id, hash, key.Version, expires, _options.IgnoreAsciiCase);
        Array.Clear(hash, 0, hash.Length);
        timer.Restart();
        try { await _store.ReplaceAsync(context, entry, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception e) { Count(context, "store_failed"); throw new CaptchaStoreException("验证码存储失败。", e); }
        finally { RecordDuration(context, timer, "replace"); }
        Count(context, "created"); ImageSize.Record(image.ByteLength, Tags(context, "created"));
        return new CaptchaChallenge(id, image, expires);
    }
    /// <inheritdoc />
    public async Task<CaptchaVerifyResult> VerifyAsync(CaptchaContext context, string captchaId, string answer, CancellationToken cancellationToken = default)
    {
        Validate(context); CaptchaContext.Check(captchaId, 64, nameof(captchaId));
        // 在消费前完成两种规范化，拒绝非法 Unicode 与过长输入。
        var normalized = Normalize(answer, false); var upper = Normalize(answer, true);
        cancellationToken.ThrowIfCancellationRequested();
        CaptchaEntry? entry;
        var timer = Stopwatch.StartNew();
        try { entry = await _store.TryTakeAsync(context, captchaId, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception e) { Count(context, "store_failed"); throw new CaptchaStoreException("验证码消费失败，状态可能已改变。", e); }
        finally { RecordDuration(context, timer, "take"); }
        var status = CaptchaVerifyStatus.InvalidOrExpired;
        if (entry != null && string.Equals(entry.CaptchaId, captchaId, StringComparison.Ordinal) && _clock.UtcNow < entry.ExpiresAt)
        {
            var key = _keys.FindKey(entry.KeyVersion);
            if (key != null)
            {
                var hash = Compute(context, captchaId, entry.ExpiresAt, entry.IgnoreAsciiCase, entry.NormalizationVersion, entry.IgnoreAsciiCase ? upper : normalized, key);
                status = Bing.Security.SecureComparison.FixedTimeEquals(hash, entry.AnswerHash) ? CaptchaVerifyStatus.Success : CaptchaVerifyStatus.Mismatch;
                Array.Clear(hash, 0, hash.Length);
            }
        }
        Count(context, status.ToString()); return new CaptchaVerifyResult(status);
    }
    private void Validate(CaptchaContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (!_options.AllowedPurposes.Contains(context.Purpose, StringComparer.Ordinal)) throw new ArgumentException("未配置此验证码用途。", nameof(context));
    }
    private static string Normalize(string answer, bool ignoreCase)
    {
        CaptchaContext.Check(answer, 64, nameof(answer));
        var normalized = answer.Normalize(NormalizationForm.FormKC).Trim();
        CaptchaContext.Check(normalized, 64, nameof(answer));
        return ignoreCase ? new string(normalized.Select(c => c >= 'a' && c <= 'z' ? (char)(c - 32) : c).ToArray()) : normalized;
    }
    private static byte[] Compute(CaptchaContext context, string id, DateTimeOffset expires, bool ignoreCase, int version, string answer, CaptchaKey key)
    {
        var bytes = CaptchaContext.Encode("captcha-answer-v1", context.Purpose, context.BindingId, context.FlowId, id,
            expires.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture), version.ToString(CultureInfo.InvariantCulture), ignoreCase ? "1" : "0", key.Version, answer);
        var material = key.GetBytes();
        try { return Hmac.Compute(bytes, material); }
        finally { Array.Clear(material, 0, material.Length); Array.Clear(bytes, 0, bytes.Length); }
    }
    private KeyValuePair<string, object?>[] Tags(CaptchaContext context, string result) => new[]
    {
        new KeyValuePair<string, object?>("purpose", context.Purpose),
        new KeyValuePair<string, object?>("backend", _options.BackendName),
        new KeyValuePair<string, object?>("result", result)
    };
    private void Count(CaptchaContext context, string result) => Events.Add(1, Tags(context, result));
    private void RecordDuration(CaptchaContext context, Stopwatch timer, string stage) => Duration.Record(timer.Elapsed.TotalMilliseconds, Tags(context, stage));
}
