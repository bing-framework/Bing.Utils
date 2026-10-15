namespace BingUtilsUT.Samples;

/// <summary>
/// 定义可重复应用于任意程序元素的第一种测试特性。
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ModelOneAttribute : Attribute { }

/// <summary>
/// 定义不从基类继承且可重复应用的测试特性。
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
public class ModelTwoAttribute : Attribute { }

/// <summary>
/// 定义可重复应用于任意程序元素的第三种测试特性。
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ModelThreeAttribute : Attribute { }

/// <summary>
/// 定义可重复应用于任意程序元素的第四种测试特性。
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ModelFourAttribute : Attribute { }

/// <summary>
/// 提供用于验证特性应用位置、重复次数和继承行为的样例类型。
/// </summary>
[ModelOne]
[ModelOne]
[ModelThree]
public class NormalWithAttrClass
{
    /// <summary>
    /// 获取或设置用于验证属性特性的字符串样例。
    /// </summary>
    [ModelOne][ModelTwo] public string Nice { get; set; }

    /// <summary>
    /// 获取或设置用于验证属性反射的整数样例。
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 表示用于验证字段特性的字符串样例。
    /// </summary>
    [ModelOne]
    [ModelTwo]
    public string Good;

    /// <summary>
    /// 返回用于验证方法特性的字符串样例值。
    /// </summary>
    /// <returns>空字符串。</returns>
    [ModelOne]
    [ModelTwo]
    public string GetAwesome()
    {
        return string.Empty;
    }

    /// <summary>
    /// 初始化一个 <see cref="NormalWithAttrClass"/> 类型的实例。
    /// </summary>
    [ModelOne]
    [ModelTwo]
    public NormalWithAttrClass() { }

    /// <summary>
    /// 初始化一个 <see cref="NormalWithAttrClass"/> 类型的实例。
    /// </summary>
    /// <param name="value">用于初始化 <see cref="Nice"/> 属性的字符串。</param>
    public NormalWithAttrClass([ModelTwo][ModelThree] string value)
    {
        Nice = value;
    }

    /// <summary>
    /// 初始化一个 <see cref="NormalWithAttrClass"/> 类型的实例。
    /// </summary>
    /// <param name="value">用于初始化 <see cref="Nice"/> 属性的字符串。</param>
    /// <param name="index">用于初始化 <see cref="Index"/> 属性的整数。</param>
    public NormalWithAttrClass(string value, int index)
    {
        Nice = value;
        Index = index;
    }
}

/// <summary>
/// 提供不带特性的类型、属性、字段、方法和构造函数样例。
/// </summary>
public class NormalWithoutAttrClass
{
    /// <summary>
    /// 获取或设置字符串属性样例。
    /// </summary>
    public string Nice { get; set; }

    /// <summary>
    /// 表示字符串字段样例。
    /// </summary>
    public string Good;

    /// <summary>
    /// 返回方法测试使用的字符串样例值。
    /// </summary>
    /// <returns>空字符串。</returns>
    public string GetAwesome()
    {
        return string.Empty;
    }

    /// <summary>
    /// 初始化一个 <see cref="NormalWithoutAttrClass"/> 类型的实例。
    /// </summary>
    public NormalWithoutAttrClass() { }

    /// <summary>
    /// 初始化一个 <see cref="NormalWithoutAttrClass"/> 类型的实例。
    /// </summary>
    /// <param name="value">用于初始化 <see cref="Nice"/> 属性的字符串。</param>
    public NormalWithoutAttrClass(string value)
    {
        Nice = value;
    }
}

/// <summary>
/// 提供继承特性样例类型的包装类型。
/// </summary>
public class NormalWithAttrClassWrapper : NormalWithAttrClass
{
    /// <summary>
    /// 初始化一个 <see cref="NormalWithAttrClassWrapper"/> 类型的实例。
    /// </summary>
    public NormalWithAttrClassWrapper() : base() { }

    /// <summary>
    /// 初始化一个 <see cref="NormalWithAttrClassWrapper"/> 类型的实例。
    /// </summary>
    /// <param name="value">传递给基类构造函数的字符串。</param>
    public NormalWithAttrClassWrapper(string value) : base(value) { }
}

/// <summary>
/// 提供应用了多种类型级特性的样例类型。
/// </summary>
[ModelOne]
[ModelOne]
[ModelTwo]
[ModelThree]
public class NormalWithAttrClass2 { }

/// <summary>
/// 提供继承类型级特性样例的派生类型。
/// </summary>
[ModelFour]
public class NormalWithAttrClassWrapper2 : NormalWithAttrClass2 { }
