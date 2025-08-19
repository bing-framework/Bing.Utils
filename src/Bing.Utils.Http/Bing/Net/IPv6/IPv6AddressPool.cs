namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址池
/// </summary>
public class IPv6AddressPool
{
    /// <summary>
    /// 地址列表
    /// </summary>
    public List<string> Addresses { get; set; } = new();

    /// <summary>
    /// 地址类型
    /// </summary>
    public IPv6AddressType AddressType { get; set; }

    /// <summary>
    /// 前缀
    /// </summary>
    public string Prefix { get; set; }

    /// <summary>
    /// 前缀长度
    /// </summary>
    public int PrefixLength { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 已使用的地址索引
    /// </summary>
    private int _usedIndex = 0;

    /// <summary>
    /// 获取下一个可用地址
    /// </summary>
    /// <returns>下一个可用地址</returns>
    public string GetNext()
    {
        if (_usedIndex >= Addresses.Count)
            throw new InvalidOperationException("地址池已耗尽");
        return Addresses[_usedIndex++];
    }

    /// <summary>
    /// 重置地址池
    /// </summary>
    public void Reset() => _usedIndex = 0;

    /// <summary>
    /// 获取剩余地址数量
    /// </summary>
    public int RemainingCount => Math.Max(0, Addresses.Count - _usedIndex);

    /// <summary>
    /// 是否已耗尽
    /// </summary>
    public bool IsExhausted => _usedIndex >= Addresses.Count;
}