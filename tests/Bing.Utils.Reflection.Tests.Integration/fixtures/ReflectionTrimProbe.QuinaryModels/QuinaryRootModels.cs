using ReflectionTrimProbe.QuaternaryModels;
using ReflectionTrimProbe.TertiaryModels;

namespace ReflectionTrimProbe.QuinaryModels;

/// <summary>
/// 第五个外部程序集中的跨分支嵌套 root 夹具。
/// </summary>
public sealed class CrossBranchRootModel
{
    /// <summary>
    /// 同时持有第四和第三程序集类型并混合字段、方法 root 的嵌套类型。
    /// </summary>
    public sealed class PreservedNested
    {
        /// <summary>
        /// 获取第四个外部程序集中的字段根类型实例。
        /// </summary>
        public QuaternaryFieldsRootModel PreservedQuaternaryPayload = new();

        /// <summary>
        /// 获取第三个外部程序集中的字段根类型实例。
        /// </summary>
        public TertiaryFieldsRootModel PreservedTertiaryPayload = new();

        /// <summary>
        /// 获取由跨分支 root 显式保留的字段值。
        /// </summary>
        public int PreservedNestedField = 1;

        /// <summary>
        /// 执行由完整签名 root 显式保留的整数转换。
        /// </summary>
        /// <param name="value">待转换的整数值。</param>
        /// <returns>输入值加一后的结果。</returns>
        public int PreservedMethod(int value) => value + 1;

        /// <summary>
        /// 用于确认完整签名 root 不会按方法名称保留同名字符串重载。
        /// </summary>
        /// <param name="value">待转换的字符串。</param>
        /// <returns>字符串长度加一后的结果。</returns>
        public int PreservedMethod(string value) => value.Length + 1;

        /// <summary>
        /// 用于确认未声明字段会被裁剪的字段值。
        /// </summary>
        public int TrimmedNestedField = 1;

        /// <summary>
        /// 用于确认未声明方法会被裁剪的整数方法。
        /// </summary>
        /// <param name="value">待转换的整数值。</param>
        /// <returns>输入值加二后的结果。</returns>
        public int TrimmedMethod(int value) => value + 2;
    }

    /// <summary>
    /// 用于确认 nothing root 只保留嵌套类型身份的嵌套类型。
    /// </summary>
    public sealed class TrimmedNested
    {
        /// <summary>
        /// 用于确认 nothing root 会裁剪字段的字段值。
        /// </summary>
        public int TrimmedField = 1;

        /// <summary>
        /// 用于确认 nothing root 会裁剪方法的整数方法。
        /// </summary>
        /// <param name="value">待转换的整数值。</param>
        /// <returns>输入值加三后的结果。</returns>
        public int TrimmedMethod(int value) => value + 3;
    }
}
