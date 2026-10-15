using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bing.Tests.Samples
{
    /// <summary>
    /// 用于识别测试样例类型的标记契约。
    /// </summary>
    public interface ISample
    {
    }

    /// <summary>
    /// 用于验证数据注解、属性转换及对象映射的测试样例。
    /// </summary>
    [Description("测试样例")]
    public class Sample : ISample
    {
        /// <summary>
        /// 初始化一个 <see cref="Sample"/> 类型的实例。
        /// </summary>
        public Sample()
        {
            StringList = new List<string>();
            StringArray = StringList.ToArray();
        }

        /// <summary>
        /// 获取或设置用于组合数据注解验证的样例文本。
        /// </summary>
        [Display(Name = "测试值")]
        [Required(ErrorMessage = "测试值不能为空")]
        [StringLength(20, ErrorMessage = "测试值长度不能超过20")]
        [EmailAddress]
        [Url]
        public string TestValue { get; set; }
        /// <summary>
        /// 获取或设置用于电子邮件格式验证的样例文本。
        /// </summary>
        [EmailAddress(ErrorMessage = "电子邮件不正确")]
        public string Email { get; set; }
        /// <summary>
        /// 获取或设置用于网址格式验证的样例文本。
        /// </summary>
        [Url(ErrorMessage = "网址不正确")]
        public string Url { get; set; }
        /// <summary>
        /// 获取或设置用于最大长度验证的样例文本。
        /// </summary>
        [MaxLength(2, ErrorMessage = "最大长度是2")]
        public string MaxLengthValue { get; set; }
        /// <summary>
        /// 获取或设置用于长度范围验证的样例文本。
        /// </summary>
        [StringLength(3, MinimumLength = 2, ErrorMessage = "最大长度是3,最小长度是2")]
        public string StringLengthValue { get; set; }
        /// <summary>
        /// 获取或设置带有显示名称特性的样例文本。
        /// </summary>
        [DisplayName("DisplayNameValue值")]
        public string DisplayNameValue { get; set; }
        /// <summary>
        /// 获取或设置带有显示特性的样例文本。
        /// </summary>
        [Display(Name = "DisplayValue值")]
        public string DisplayValue { get; set; }
        /// <summary>
        /// 获取或设置用于非空及长度验证的字符串值。
        /// </summary>
        [Display(Name = "字符串值")]
        [StringLength(20, ErrorMessage = "长度不能超过20")]
        [Required(ErrorMessage = "不能为空")]
        public string StringValue { get; set; }
        /// <summary>
        /// 获取或设置样例十进制数值。
        /// </summary>
        public decimal DecimalValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例十进制数值。
        /// </summary>
        public decimal? NullableDecimalValue { get; set; }
        /// <summary>
        /// 获取或设置样例单精度浮点数值。
        /// </summary>
        public float FloatValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例单精度浮点数值。
        /// </summary>
        public float? NullableFloatValue { get; set; }
        /// <summary>
        /// 获取或设置样例双精度浮点数值。
        /// </summary>
        public double DoubleValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例双精度浮点数值。
        /// </summary>
        public double? NullableDoubleValue { get; set; }
        /// <summary>
        /// 获取或设置样例布尔值。
        /// </summary>
        public bool BoolValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例布尔值。
        /// </summary>
        public bool? NullableBoolValue { get; set; }
        /// <summary>
        /// 获取或设置样例枚举值。
        /// </summary>
        public EnumSample EnumValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例枚举值。
        /// </summary>
        public EnumSample? NullableEnumValue { get; set; }
        /// <summary>
        /// 获取或设置样例日期时间值。
        /// </summary>
        public DateTime DateValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例日期时间值。
        /// </summary>
        public DateTime? NullableDateValue { get; set; }
        /// <summary>
        /// 获取或设置样例整数值。
        /// </summary>
        [Description("IntValue")]
        public int IntValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例整数值。
        /// </summary>
        [Description("TestNullableIntValue")]
        public int? NullableIntValue { get; set; }
        /// <summary>
        /// 获取或设置样例短整数值。
        /// </summary>
        public short ShortValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例短整数值。
        /// </summary>
        public short? NullableShortValue { get; set; }
        /// <summary>
        /// 获取或设置样例长整数值。
        /// </summary>
        public long LongValue { get; set; }
        /// <summary>
        /// 获取或设置可空的样例长整数值。
        /// </summary>
        public long? NullableLongValue { get; set; }
        /// <summary>
        /// 获取或设置样例字符串列表。
        /// </summary>
        public List<string> StringList { get; set; }
        /// <summary>
        /// 获取或设置样例字符串数组。
        /// </summary>
        public string[] StringArray { get; set; }
        /// <summary>
        /// 获取或设置样例全局唯一标识符。
        /// </summary>
        public Guid GuidValue { get; set; }

        /// <summary>
        /// 获取或设置用于嵌套对象映射的第二种样例。
        /// </summary>
        public Sample2 Test2 { get; set; }

        /// <summary>
        /// 获取或设置用于嵌套对象映射的第三种样例。
        /// </summary>
        public Sample3Copy Test3 { get; set; }

        /// <summary>
        /// 获取或设置用于集合映射的嵌套样例列表。
        /// </summary>
        public List<Sample3Copy> TestList { get; set; }

        /// <summary>
        /// 获取用于静态属性反射的固定文本 <c>TestStaticString</c>。
        /// </summary>
        public static string StaticString => "TestStaticString";

        /// <summary>
        /// 获取字符串值为 <c>TestStaticSample</c> 的新样例。
        /// </summary>
        public static Sample2 StaticSample => new() { StringValue = "TestStaticSample" };

        /// <summary>
        /// 创建包含指定测试字符串的样例实例。
        /// </summary>
        /// <returns>字符串值为 <c>A</c> 的新样例实例。</returns>
        public static Sample Create1() => new() { StringValue = "A" };

        /// <summary>
        /// 创建包含指定测试字符串的样例实例。
        /// </summary>
        /// <returns>字符串值为 <c>B</c> 的新样例实例。</returns>
        public static Sample Create2() => new() { StringValue = "B" };
    }
}
