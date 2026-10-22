namespace ReflectionTrimProbe.QuaternaryModels;

/// <summary>
/// 第四个外部程序集中的字段根描述夹具。
/// </summary>
public sealed class QuaternaryFieldsRootModel
{
    public int PreservedField = 1;
}

/// <summary>
/// 第四个外部程序集中的显式方法根描述夹具。
/// </summary>
public sealed class QuaternaryMethodsRootModel
{
    public int PreservedMethod(int value) => value + 1;

    public int TrimmedMethod(int value) => value + 2;
}

/// <summary>
/// 第四个外部程序集中的嵌套字段根描述夹具。
/// </summary>
public sealed class QuaternaryNestedRootModel
{
    public sealed class PreservedNested
    {
        public int PreservedField = 1;

        public int TrimmedField = 1;
    }
}

/// <summary>
/// 第四个外部程序集中的跨第三程序集嵌套 root 夹具。
/// </summary>
public sealed class CrossAssemblyRootModel
{
    /// <summary>
    /// 通过显式 linker root 组合保留字段和签名方法的嵌套类型。
    /// </summary>
    public sealed class PreservedNested
    {
        /// <summary>
        /// 获取第三个外部程序集中的公开字段根类型实例。
        /// </summary>
        public ReflectionTrimProbe.TertiaryModels.TertiaryFieldsRootModel PreservedTertiaryPayload = new();

        /// <summary>
        /// 获取由跨程序集 root 显式保留的字段值。
        /// </summary>
        public int PreservedNestedField = 1;

        /// <summary>
        /// 执行由完整签名 root 显式保留的整数转换。
        /// </summary>
        /// <param name="value">待转换的整数值。</param>
        /// <returns>输入值加一后的结果。</returns>
        public int PreservedMethod(int value) => value + 1;

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

/// <summary>
/// 第四个外部程序集中的空根描述夹具。
/// </summary>
public sealed class QuaternaryNothingRootModel
{
    public int TrimmedField;

    public int TrimmedProperty { get; set; }

    public int TrimmedMethod(int value) => value + 1;
}
