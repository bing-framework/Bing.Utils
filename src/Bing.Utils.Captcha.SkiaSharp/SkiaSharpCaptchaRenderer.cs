using System.Threading;
using Bing.Captcha;
using Bing.Drawing;
using SkiaSharp;

namespace Bing.Captcha.SkiaSharp;

/// <summary>使用 SkiaSharp 输出受限大小 PNG 的验证码渲染器。</summary>
public sealed class SkiaSharpCaptchaRenderer : ICaptchaRenderer, ICaptchaRendererValidator, IDisposable
{
    private readonly SkiaSharpCaptchaRendererOptions _options;
    private readonly SKTypeface _typeface;
    private readonly string _alphabet;
    private readonly object _disposeGate = new();
    private readonly ReaderWriterLockSlim _lifetimeLock = new();
    private bool _disposed;

    /// <summary>创建渲染器并检查所选字符集是否有可用字形。</summary>
    /// <param name="options">可选渲染配置；中文字符集必须包含显式字体文件路径。</param>
    public SkiaSharpCaptchaRenderer(SkiaSharpCaptchaRendererOptions? options = null)
    {
        _options = (options ?? new SkiaSharpCaptchaRendererOptions()).Snapshot();
        _alphabet = CaptchaAlphabet.GetCharacters(_options.Type, _options.IgnoreAsciiCase);
        _typeface = LoadTypeface(_options.FontPath);
        try
        {
            ValidateGlyphCoverage(_typeface, _alphabet);
        }
        catch
        {
            _typeface.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void ValidateAlphabet(string alphabet)
    {
        if (alphabet == null)
            throw new ArgumentNullException(nameof(alphabet));
        if (alphabet.Length == 0 || alphabet.Any(character => _alphabet.IndexOf(character) < 0))
            throw new ArgumentException("生成器字符集与渲染配置不兼容，请检查 Type、IgnoreAsciiCase 和中文字体配置。", nameof(alphabet));
    }

    /// <inheritdoc />
    public CaptchaImage Render(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentNullException(nameof(text));
        if (text.Length < 4 || text.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(text), "验证码长度必须在 4 到 8 个字符之间。");

        foreach (var character in text)
        {
            var candidate = _options.IgnoreAsciiCase && character >= 'a' && character <= 'z'
                ? (char)(character - 'a' + 'A')
                : character;
            if (_alphabet.IndexOf(candidate) < 0)
                throw new ArgumentException("验证码含有当前字符集不支持的字符。", nameof(text));
        }

        lock (_disposeGate)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SkiaSharpCaptchaRenderer));
            _lifetimeLock.EnterReadLock();
        }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var image = SkiaSharpHelper.CreateCaptchaImage(text, _options.DrawingOptions, _typeface);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            if (data == null)
                throw new CaptchaRendererException("SkiaSharp 未能编码验证码图片。");
            if (data.Size > _options.MaxImageBytes)
                throw new CaptchaRendererException("编码后的验证码图片超出大小限制。");
            return new CaptchaImage(data.ToArray(), image.Width, image.Height);
        }
        catch (CaptchaRendererException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CaptchaRendererException("SkiaSharp 验证码渲染失败。", exception);
        }
        finally
        {
            _lifetimeLock.ExitReadLock();
        }
    }

    /// <summary>释放渲染器拥有的字体资源。</summary>
    public void Dispose()
    {
        lock (_disposeGate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _lifetimeLock.EnterWriteLock();
            try
            {
                _typeface.Dispose();
            }
            finally
            {
                _lifetimeLock.ExitWriteLock();
                _lifetimeLock.Dispose();
            }
        }
    }

    private static SKTypeface LoadTypeface(string? path)
    {
        if (path == null)
            return SKTypeface.FromFamilyName(null)
                ?? throw new CaptchaRendererException("SkiaSharp 未能创建默认字体实例。");
        if (!File.Exists(path))
            throw new FileNotFoundException("验证码字体文件不存在。", Path.GetFileName(path));

        try
        {
            return SKTypeface.FromFile(path)
                ?? throw new CaptchaRendererException("SkiaSharp 无法加载指定字体。");
        }
        catch (CaptchaRendererException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CaptchaRendererException("SkiaSharp 无法加载指定字体。", exception);
        }
    }

    private static void ValidateGlyphCoverage(SKTypeface typeface, string characters)
    {
        var glyphs = typeface.GetGlyphs(characters);
        if (glyphs.Length != characters.Length || glyphs.Any(glyph => glyph == 0))
            throw new CaptchaRendererException("配置字体未覆盖验证码字符集；中文验证码需要支持完整白名单的字体文件。");
    }
}
