namespace Bing.Utils.Comments.Tests;

/// <summary>
/// 用于测试枚举注释的辅助枚举
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
