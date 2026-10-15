using System.ComponentModel;

namespace Bing.Tests.Samples
{
    /// <summary>
    /// 用于验证枚举转换及描述读取的测试枚举。
    /// </summary>
    public enum EnumSample
    {
        /// <summary>
        /// 表示测试场景中的 <c>A</c> 枚举值。
        /// </summary>
        A = 1,
        /// <summary>
        /// 表示测试场景中的 <c>B</c> 枚举值，数值为 <c>2</c>。
        /// </summary>
        [Description("B2")]
        B = 2,
        /// <summary>
        /// 表示测试场景中的 <c>C</c> 枚举值，数值为 <c>3</c>。
        /// </summary>
        [Description("C3")]
        C = 3,
        /// <summary>
        /// 表示测试场景中的 <c>D</c> 枚举值，数值为 <c>4</c>。
        /// </summary>
        [Description("D4")]
        D = 4,
        /// <summary>
        /// 表示测试场景中的 <c>E</c> 枚举值，数值为 <c>5</c>。
        /// </summary>
        [Description("E5")]
        E = 5
    }
}
