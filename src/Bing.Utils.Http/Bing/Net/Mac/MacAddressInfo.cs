namespace Bing.Net.Mac;

/// <summary>
/// MAC地址信息
/// </summary>
public class MacAddressInfo
{
    /// <summary>
    /// 标准化的MAC地址
    /// </summary>
    public string MacAddress { get; set; }

    /// <summary>
    /// 是否为全球唯一地址（Universal）
    /// </summary>
    public bool IsUniversal { get; set; }

    /// <summary>
    /// 是否为本地管理地址（Locally Administered）
    /// </summary>
    public bool IsLocallyAdministered { get; set; }

    /// <summary>
    /// 是否为组播地址（Multicast）
    /// </summary>
    public bool IsMulticast { get; set; }

    /// <summary>
    /// 是否为单播地址（Unicast）
    /// </summary>
    public bool IsUnicast { get; set; }

    /// <summary>
    /// 组织唯一标识符（OUI - Organizationally Unique Identifier）
    /// </summary>
    public string OUI { get; set; }

    /// <summary>
    /// 网络接口控制器标识符（NIC - Network Interface Controller）
    /// </summary>
    public string NIC { get; set; }

    /// <summary>
    /// 返回MAC地址信息的字符串表示
    /// </summary>
    /// <returns>格式化的信息字符串</returns>
    public override string ToString()
    {
        return $"MAC: {MacAddress}, Type: {(IsUniversal ? "Universal" : "Local")}, " +
               $"Cast: {(IsUnicast ? "Unicast" : "Multicast")}, OUI: {OUI}";
    }
}