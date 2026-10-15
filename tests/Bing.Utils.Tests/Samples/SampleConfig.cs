namespace Bing.Utils.Tests.Samples;

/// <summary>
/// 保存配置绑定测试使用的示例选项。
/// </summary>
public class SampleConfig
{
    /// <summary>
    /// 获取或设置用于验证字符串配置绑定的值。
    /// </summary>
    public string StringValue { get; set; }

    /// <summary>
    /// 获取或设置用于验证十进制配置绑定的值。
    /// </summary>
    public decimal DecimalValue { get; set; }

    /// <summary>
    /// 获取或设置用于验证整数配置绑定的值。
    /// </summary>
    public int IntValue { get; set; }
}