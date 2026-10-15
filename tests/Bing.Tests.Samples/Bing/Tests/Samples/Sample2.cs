using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bing.Tests.Samples
{
    /// <summary>
    /// 用于验证属性元数据读取及嵌套对象映射的测试样例。
    /// </summary>
    [DisplayName("测试样例2")]
    public class Sample2
    {
        /// <summary>
        /// 获取或设置带有描述特性的样例文本。
        /// </summary>
        [Description("描述")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置带有显示名称特性的样例文本。
        /// </summary>
        [DisplayName("显示名")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 获取或设置通过显示特性声明描述的样例文本。
        /// </summary>
        [Display(Description = "显示名2")]
        public string DisplayName2 { get; set; }

        /// <summary>
        /// 获取或设置用于读取显示描述的样例文本。
        /// </summary>
        [Display(Description = "显示描述")]
        public string Display { get; set; }

        /// <summary>
        /// 获取或设置样例字符串值。
        /// </summary>
        public string StringValue { get; set; }

        /// <summary>
        /// 获取或设置样例整数值。
        /// </summary>
        public int IntValue { get; set; }

        /// <summary>
        /// 获取或设置样例布尔值。
        /// </summary>
        public bool BoolValue { get; set; }

        /// <summary>
        /// 获取或设置可空的样例布尔值。
        /// </summary>
        public bool? NullableBoolValue { get; set; }

        /// <summary>
        /// 获取或设置用于嵌套对象映射的样例。
        /// </summary>
        public Sample3 Test3 { get; set; }

        /// <summary>
        /// 获取或设置用于集合映射的嵌套样例列表。
        /// </summary>
        public List<Sample3> TestList { get; set; }
    }
}
