namespace Bing.Utils.Http.Tests.Integration.Samples;

/// <summary>
/// 保存客户查询测试使用的查询条件。
/// </summary>
public class CustomerQuery
{
    /// <summary>
    /// 获取或设置可选的客户标识。
    /// </summary>
    [Description("客户标识")]
    public int? CustomerId { get; set; }
    /// <summary>
    /// 获取或设置客户编码。
    /// </summary>
    [Description("编码")]
    public string Code { get; set; }
    /// <summary>
    /// 获取或设置客户姓名。
    /// </summary>
    [Description("姓名")]
    public string Name { get; set; }
    /// <summary>
    /// 获取或设置客户昵称。
    /// </summary>
    [Description("昵称")]
    public string Nickname { get; set; }
    /// <summary>
    /// 获取或设置客户手机号。
    /// </summary>
    [Description("手机号")]
    public string Phone { get; set; }
    /// <summary>
    /// 获取或设置客户电子邮件地址。
    /// </summary>
    [Description("电子邮件")]
    public string Email { get; set; }
}
