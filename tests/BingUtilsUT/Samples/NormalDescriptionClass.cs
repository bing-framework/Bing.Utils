namespace BingUtilsUT.Samples;

/// <summary>
/// 提供带有多种显示元数据特性的类型、属性、字段和方法样例。
/// </summary>
[Description("NormalClassOne")]
[DisplayName("NormalClassDisplayNameOne")]
[Display(Name = "NormalDescription", Description = "NormalClassDisplayOne")]
public class NormalDescriptionClass
{
    /// <summary>
    /// 获取或设置用于读取显示元数据的属性样例。
    /// </summary>
    [Description("PropertyOne")]
    [DisplayName("PropertyDisplayNameOne")]
    [Display(Name = "PropertyOne", Description = "PropertyDisplayOne")]
    public string PropertyOne { get; set; }

    /// <summary>
    /// 表示用于读取显示元数据的字段样例。
    /// </summary>
    [Description("FieldOne")]
    [Display(Name = "FieldOne", Description = "FieldDisplayOne")]
    public string FieldOne;

    /// <summary>
    /// 返回用于读取显示元数据的方法样例值。
    /// </summary>
    /// <param name="argOne">传入方法的参数样例。</param>
    /// <returns>空字符串。</returns>
    [Description("MethodOne")]
    [DisplayName("MethodDisplayNameOne")]
    [Display(Name = "MethodOne", Description = "MethodDisplayOne")]
    public string MethodOne(
        [Description("ArgOne")] [Display(Name = "ParamName", Description = "ParamDesc")]
        string argOne
    ) => string.Empty;
}

/// <summary>
/// 提供继承显示元数据样例类型的包装类型。
/// </summary>
public class NormalDescriptionWrapper : NormalDescriptionClass { }

/// <summary>
/// 提供仅带有显示名称元数据特性的类型、属性、字段和方法样例。
/// </summary>
[DisplayName("NormalClassDisplayNameTwo")]
[Display(Name = "NormalDescription", Description = "NormalClassDisplayTwo")]
public class NormalDescriptionOrClass
{
    /// <summary>
    /// 获取或设置用于读取显示名称元数据的属性样例。
    /// </summary>
    [DisplayName("PropertyDisplayNameTwo")]
    [Display(Name = "PropertyTwo", Description = "PropertyDisplayTwo")]
    public string PropertyTwo { get; set; }

    /// <summary>
    /// 表示用于读取显示名称元数据的字段样例。
    /// </summary>
    [Display(Name = "FieldTwo", Description = "FieldDisplayTwo")]
    public string FieldTwo;

    /// <summary>
    /// 返回用于读取显示名称元数据的方法样例值。
    /// </summary>
    /// <param name="argTwo">传入方法的参数样例。</param>
    /// <returns>空字符串。</returns>
    [DisplayName("MethodDisplayNameTwo")]
    [Display(Name = "MethodTwo", Description = "MethodDisplayTwo")]
    public string MethodTwo(
        [Display(Name = "ParamName", Description = "ParamDesc")]
        string argTwo
    ) => string.Empty;
}

/// <summary>
/// 提供带有描述特性的类型、属性、字段和方法样例。
/// </summary>
[Description("NormalClassThree")]
public class NormalDisplayNameOrClass
{
    /// <summary>
    /// 获取或设置用于读取描述元数据的属性样例。
    /// </summary>
    [Description("PropertyThree")] public string PropertyThree { get; set; }

    /// <summary>
    /// 表示用于读取描述元数据的字段样例。
    /// </summary>
    [Description("FieldThree")]
    public string FieldThree;

    /// <summary>
    /// 返回用于读取描述元数据的方法样例值。
    /// </summary>
    /// <param name="argThree">传入方法的参数样例。</param>
    /// <returns>空字符串。</returns>
    [Description("MethodThree")]
    public string MethodThree([Description("ArgThree")] string argThree) => string.Empty;
}
