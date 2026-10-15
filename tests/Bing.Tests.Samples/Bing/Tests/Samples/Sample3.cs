using System.ComponentModel;

namespace Bing.Tests.Samples
{
    /// <summary>
    /// 用于验证显示名称特性及嵌套属性的测试样例。
    /// </summary>
    [DisplayName("测试样例3")]
    public class Sample3
    {
        /// <summary>
        /// 获取或设置用于对象映射的字符串值。
        /// </summary>
        public string StringValue { get; set; }
    }

    /// <summary>
    /// 用于验证同结构对象映射的目标样例。
    /// </summary>
    public class Sample3Copy
    {
        /// <summary>
        /// 获取或设置用于对象映射的字符串值。
        /// </summary>
        public string StringValue { get; set; }
    }
}
