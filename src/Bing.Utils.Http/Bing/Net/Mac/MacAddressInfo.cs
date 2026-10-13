namespace Bing.Net.Mac;

/// <summary>
/// MAC地址信息
/// </summary>
public class MacAddressInfo
{
    /// <summary>
    /// 获取或设置标准化的MAC地址
    /// </summary>
    public string MacAddress { get; set; }

    /// <summary>
    /// 获取或设置是否为全球唯一地址（Universal）
    /// </summary>
    public bool IsUniversal { get; set; }

    /// <summary>
    /// 获取或设置是否为本地管理地址（Locally Administered）
    /// </summary>
    public bool IsLocallyAdministered { get; set; }

    /// <summary>
    /// 获取或设置是否为组播地址（Multicast）
    /// </summary>
    public bool IsMulticast { get; set; }

    /// <summary>
    /// 获取或设置是否为单播地址（Unicast）
    /// </summary>
    public bool IsUnicast { get; set; }

    /// <summary>
    /// 获取或设置组织唯一标识符（OUI - Organizationally Unique Identifier）
    /// </summary>
    public string OUI { get; set; }

    /// <summary>
    /// 获取或设置网络接口控制器标识符（NIC - Network Interface Controller）
    /// </summary>
    public string NIC { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"MAC: {MacAddress}, Type: {(IsUniversal ? "Universal" : "Local")}, " +
               $"Cast: {(IsUnicast ? "Unicast" : "Multicast")}, OUI: {OUI}";
    }
}
