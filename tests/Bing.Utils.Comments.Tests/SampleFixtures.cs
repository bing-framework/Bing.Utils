namespace Bing.Utils.Comments.Tests;

/// <summary>
/// 为枚举注释读取测试提供颜色成员及其描述特性样例。
/// </summary>
public enum SampleColor
{
    /// <summary>
    /// 红色
    /// </summary>
    Red,

    /// <summary>
    /// 绿色
    /// </summary>
    [Description("绿")]
    Green,

    /// <summary>
    /// 蓝色
    /// </summary>
    Blue
}
