namespace Bing.Captcha;

/// <summary>不可变密钥快照，不自动输出密钥数据。</summary>
public sealed class CaptchaKey
{
    private readonly byte[] _material;
    /// <summary>创建至少 32 字节的密钥快照，复制输入字节。</summary>
    /// <param name="version">唯一版本，非空且最多 64 个字符。</param>
    /// <param name="material">32～1024 字节宿主密钥，不在服务内生成生产密钥。</param>
    public CaptchaKey(string version, byte[] material)
    {
        Version = CaptchaContext.Check(version, 64, nameof(version));
        if (material == null) throw new ArgumentNullException(nameof(material));
        if (material.Length < 32 || material.Length > 1024) throw new ArgumentOutOfRangeException(nameof(material));
        _material = (byte[])material.Clone();
    }
    /// <summary>获取版本。</summary>
    public string Version { get; }
    /// <summary>获取密钥副本；使用后应清零。</summary>
    public byte[] GetBytes() => (byte[])_material.Clone();
}
/// <summary>提供原子的版本和密钥快照，旧版本应保留到挑战全部过期。</summary>
public interface ICaptchaKeyProvider
{
    /// <summary>获取当前完整密钥快照。</summary>
    CaptchaKey GetCurrentKey();
    /// <summary>查找保留的历史密钥，未知版本返回 null。</summary>
    /// <param name="version">生成记录中保存的密钥版本。</param>
    /// <returns>版本与材料一致的不可变快照，或 null。</returns>
    CaptchaKey? FindKey(string version);
}
/// <summary>从宿主提供的密钥构建只读密钥环；轮换时部署包含新旧版本的配置。</summary>
public sealed class StaticCaptchaKeyProvider : ICaptchaKeyProvider
{
    private readonly Dictionary<string, CaptchaKey> _keys;
    private readonly CaptchaKey _current;
    /// <summary>创建密钥环；版本必须唯一且当前版本必须存在。</summary>
    /// <param name="currentVersion">用于新挑战的版本。</param>
    /// <param name="keys">当前和保留的历史密钥；枚举一次并复制索引。</param>
    public StaticCaptchaKeyProvider(string currentVersion, IEnumerable<CaptchaKey> keys)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        _keys = keys.ToDictionary(k => k.Version, StringComparer.Ordinal);
        if (!_keys.TryGetValue(currentVersion, out var current)) throw new ArgumentException("当前密钥版本不存在。", nameof(currentVersion));
        _current = current;
    }
    /// <inheritdoc />
    public CaptchaKey GetCurrentKey() => _current;
    /// <inheritdoc />
    public CaptchaKey? FindKey(string version) => _keys.TryGetValue(version, out var key) ? key : null;
}
