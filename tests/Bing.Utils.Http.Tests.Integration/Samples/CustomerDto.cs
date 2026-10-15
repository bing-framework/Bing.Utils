namespace Bing.Utils.Http.Tests.Integration.Samples;

/// <summary>
/// 保存客户接口集成测试使用的数据。
/// </summary>
public class CustomerDto
{
    /// <summary>
    /// 获取或设置客户标识。
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 获取或设置长度不超过 100 个字符的客户编码。
    /// </summary>
    [Description("编码")]
    [MaxLength(100)]
    public string Code { get; set; }

    /// <summary>
    /// 获取或设置长度不超过 200 个字符的客户姓名。
    /// </summary>
    [Description("姓名")]
    [MaxLength(200)]
    public string Name { get; set; }

    /// <summary>
    /// 获取或设置长度不超过 50 个字符的客户昵称。
    /// </summary>
    [Description("昵称")]
    [MaxLength(50)]
    public string Nickname { get; set; }

    /// <summary>
    /// 获取或设置客户出生日期；未知时为 null。
    /// </summary>
    [Description("出生日期")]
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// 获取或设置长度不超过 50 个字符的客户手机号。
    /// </summary>
    [Description("手机号")]
    [MaxLength(50)]
    public string Phone { get; set; }

    /// <summary>
    /// 获取或设置长度不超过 500 个字符的客户电子邮件地址。
    /// </summary>
    [Description("电子邮件")]
    [MaxLength(500)]
    public string Email { get; set; }
}