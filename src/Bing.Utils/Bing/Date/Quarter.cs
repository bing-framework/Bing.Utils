using System.ComponentModel;

namespace Bing.Date;

/// <summary>
/// 季度
/// </summary>
public enum Quarter
{
    /// <summary>
    /// 第一季度
    /// </summary>
    [Description("第一季度")]
    Q1 = 1,

    /// <summary>
    /// 第二季度
    /// </summary>
    [Description("第二季度")]
    Q2 = 2,

    /// <summary>
    /// 第三季度
    /// </summary>
    [Description("第三季度")]
    Q3 = 3,

    /// <summary>
    /// 第四季度
    /// </summary>
    [Description("第四季度")]
    Q4 = 4
}