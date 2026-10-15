namespace BingUtilsUT.Samples;

/// <summary>
/// 定义用于测试值类型、可空类型和集合类型反射信息的样例。
/// </summary>
public class NormalValueTypeClass
{
    /// <summary>
    /// 获取或设置用于反射测试的非空整数属性。
    /// </summary>
    public int Int16V1 { get; set; }

    /// <summary>
    /// 表示用于反射测试的非空整数字段。
    /// </summary>
    public int Int16V2;

    /// <summary>
    /// 获取或设置用于反射测试的可空整数属性。
    /// </summary>
    public int? Int16V3 { get; set; }

    /// <summary>
    /// 表示用于反射测试的可空整数字段。
    /// </summary>
    public int? Int16V4;

    /// <summary>
    /// 获取或设置用于反射测试的布尔属性。
    /// </summary>
    public bool BooleanV1 { get; set; }

    /// <summary>
    /// 表示用于反射测试的布尔字段。
    /// </summary>
    public bool BooleanV2;

    /// <summary>
    /// 获取或设置用于反射测试的可空布尔属性。
    /// </summary>
    public bool? BooleanV3 { get; set; }

    /// <summary>
    /// 表示用于反射测试的可空布尔字段。
    /// </summary>
    public bool? BooleanV4;

    /// <summary>
    /// 获取或设置用于反射测试的日期时间属性。
    /// </summary>
    public DateTime DateTimeV1 { get; set; }

    /// <summary>
    /// 表示用于反射测试的日期时间字段。
    /// </summary>
    public DateTime DateTimeV2;

    /// <summary>
    /// 获取或设置用于反射测试的可空日期时间属性。
    /// </summary>
    public DateTime? DateTimeV3 { get; set; }

    /// <summary>
    /// 表示用于反射测试的可空日期时间字段。
    /// </summary>
    public DateTime? DateTimeV4;

    /// <summary>
    /// 获取或设置用于反射测试的字符串属性。
    /// </summary>
    public string Str { get; set; }

    /// <summary>
    /// 获取或设置用于反射测试的 <see cref="Int16Enum"/> 属性。
    /// </summary>
    public Int16Enum Int99V1 { get; set; }

    /// <summary>
    /// 表示用于反射测试的 <see cref="Int16Enum"/> 字段。
    /// </summary>
    public Int16Enum Int99V2;

    /// <summary>
    /// 获取或设置用于反射测试的可空 <see cref="Int16Enum"/> 属性。
    /// </summary>
    public Int16Enum? Int99V3 { get; set; }

    /// <summary>
    /// 表示用于反射测试的可空 <see cref="Int16Enum"/> 字段。
    /// </summary>
    public Int16Enum? Int99V4;

    /// <summary>
    /// 获取或设置用于反射测试的 <see cref="Int32Enum"/> 列表属性。
    /// </summary>
    public List<Int32Enum> Int32EnumV1 { get; set; }

    /// <summary>
    /// 表示用于反射测试的 <see cref="Int32Enum"/> 列表字段。
    /// </summary>
    public List<Int32Enum> Int32EnumV2;

    /// <summary>
    /// 获取或设置元素可空的 <see cref="Int32Enum"/> 列表属性。
    /// </summary>
    public List<Int32Enum?> Int32EnumV3 { get; set; }

    /// <summary>
    /// 表示元素可空的 <see cref="Int32Enum"/> 列表字段。
    /// </summary>
    public List<Int32Enum?> Int32EnumV4;

    /// <summary>
    /// 获取或设置以字符串为键、以 <see cref="Int32Enum"/> 为值的字典属性。
    /// </summary>
    public Dictionary<string, Int32Enum> Int32EnumD1 { get; set; }

    /// <summary>
    /// 表示以字符串为键、以 <see cref="Int32Enum"/> 为值的字典字段。
    /// </summary>
    public Dictionary<string, Int32Enum> Int32EnumD2;

    /// <summary>
    /// 获取或设置包含 <see cref="Int32Enum"/> 元素的数组属性。
    /// </summary>
    public Int32Enum[] Int32EnumA1 { get; set; }

    /// <summary>
    /// 表示包含 <see cref="Int32Enum"/> 元素的数组字段。
    /// </summary>
    public Int32Enum[] Int32EnumA2;

    /// <summary>
    /// 获取或设置包含可空 <see cref="Int32Enum"/> 元素的数组属性。
    /// </summary>
    public Int32Enum?[] Int32EnumA3 { get; set; }

    /// <summary>
    /// 表示包含可空 <see cref="Int32Enum"/> 元素的数组字段。
    /// </summary>
    public Int32Enum?[] Int32EnumA4;
}
