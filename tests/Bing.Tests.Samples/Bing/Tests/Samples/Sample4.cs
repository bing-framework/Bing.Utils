namespace Bing.Tests.Samples;

/// <summary>
/// 用于验证字符串转换和浅复制的测试样例。
/// </summary>
public class Sample4
{
    /// <summary>
    /// 获取或设置用于复制测试的字符串值。
    /// </summary>
    public string StringValue { get; set; }

    /// <inheritdoc />
    /// <remarks>此样例始终返回空字符串。</remarks>
    public override string ToString() => "";

    /// <summary>
    /// 创建当前样例的浅副本。
    /// </summary>
    /// <returns>复制当前实例字段所得的浅副本。</returns>
    public object GetClone() => MemberwiseClone();
}
