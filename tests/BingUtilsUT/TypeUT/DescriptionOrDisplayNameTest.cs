using Bing.Reflection;
namespace BingUtilsUT.TypeUT;

/// <summary>
/// 验证反射读取 Description 或 Display 元数据的优先级和回退行为。
/// </summary>
[Trait("TypeUT", "TypeReflections.GetDescriptionOrDisplay")]
public class DescriptionOrDisplayNameTest
{
    /// <summary>
    /// 获取提供 Description 值的样例类型。
    /// </summary>
    public Type EntryOne = typeof(NormalDescriptionClass);
    /// <summary>
    /// 获取以 Display 元数据提供描述回退值的样例类型。
    /// </summary>
    public Type EntryTwo = typeof(NormalDescriptionOrClass);
    /// <summary>
    /// 获取以 Display 元数据提供显示回退值的样例类型。
    /// </summary>
    public Type EntryThree = typeof(NormalDisplayNameOrClass);
    /// <summary>
    /// 获取用于测试继承描述回退值的包装类型。
    /// </summary>
    public Type Wrapper = typeof(NormalDescriptionWrapper);
    /// <summary>
    /// 获取第一个属性的反射信息。
    /// </summary>
    public PropertyInfo PropertyOne;
    /// <summary>
    /// 获取第二个属性的反射信息。
    /// </summary>
    public PropertyInfo PropertyTwo;
    /// <summary>
    /// 获取第三个属性的反射信息。
    /// </summary>
    public PropertyInfo PropertyThree;
    /// <summary>
    /// 获取第一个字段的反射信息。
    /// </summary>
    public FieldInfo FieldOne;
    /// <summary>
    /// 获取第二个字段的反射信息。
    /// </summary>
    public FieldInfo FieldTwo;
    /// <summary>
    /// 获取第三个字段的反射信息。
    /// </summary>
    public FieldInfo FieldThree;
    /// <summary>
    /// 获取第一个方法的反射信息。
    /// </summary>
    public MethodInfo MethodOne;
    /// <summary>
    /// 获取第二个方法的反射信息。
    /// </summary>
    public MethodInfo MethodTwo;
    /// <summary>
    /// 获取第三个方法的反射信息。
    /// </summary>
    public MethodInfo MethodThree;
    /// <summary>
    /// 获取第一个方法参数的反射信息。
    /// </summary>
    public ParameterInfo ParameterOne;
    /// <summary>
    /// 获取第二个方法参数的反射信息。
    /// </summary>
    public ParameterInfo ParameterTwo;
    /// <summary>
    /// 获取第三个方法参数的反射信息。
    /// </summary>
    public ParameterInfo ParameterThree;
    /// <summary>
    /// 初始化一个 <see cref="DescriptionOrDisplayNameTest"/> 类型的实例。
    /// </summary>
    public DescriptionOrDisplayNameTest()
    {
        var members1 = EntryOne.GetMembers();
        var members2 = EntryTwo.GetMembers();
        var members3 = EntryThree.GetMembers();
        PropertyOne = members1.Single(s => s.Name == "PropertyOne") as PropertyInfo;
        PropertyTwo = members2.Single(s => s.Name == "PropertyTwo") as PropertyInfo;
        PropertyThree = members3.Single(s => s.Name == "PropertyThree") as PropertyInfo;
        FieldOne = members1.Single(s => s.Name == "FieldOne") as FieldInfo;
        FieldTwo = members2.Single(s => s.Name == "FieldTwo") as FieldInfo;
        FieldThree = members3.Single(s => s.Name == "FieldThree") as FieldInfo;
        MethodOne = members1.Single(s => s.Name == "MethodOne") as MethodInfo;
        MethodTwo = members2.Single(s => s.Name == "MethodTwo") as MethodInfo;
        MethodThree = members3.Single(s => s.Name == "MethodThree") as MethodInfo;
        ParameterOne = MethodOne!.GetParameters()[0];
        ParameterTwo = MethodTwo!.GetParameters()[0];
        ParameterThree = MethodThree!.GetParameters()[0];
    }
    /// <summary>
    /// 测试 - 类级别 - 获取描述或显示名称
    /// </summary>
    [Fact]
    public void Test_ClassLevel_DescriptionOrDisplayName_Getting()
    {
        var desc1 = TypeReflections.GetDescriptionOrDisplayName(EntryOne);
        var desc2 = TypeReflections.GetDescriptionOrDisplayName(EntryTwo);
        var desc3 = TypeReflections.GetDescriptionOrDisplayName(EntryThree);
        var desc4 = TypeReflections.GetDescriptionOrDisplayName(Wrapper);
        var desc5 = TypeReflections.GetDescriptionOrDisplayName(Wrapper, ReflectionOptions.Inherit);
        desc1.ShouldBe("NormalClassOne");
        desc2.ShouldBe("NormalClassDisplayTwo");
        desc3.ShouldBe("NormalClassThree");
        desc4.ShouldBe(Wrapper.Name);
        desc5.ShouldBe("NormalClassOne");
    }
    /// <summary>
    /// 测试 - 属性级别 - 获取描述或显示名称
    /// </summary>
    [Fact]
    public void Test_PropertyLevel_DescriptionOrDisplayName_Getting()
    {
        var desc1 = TypeReflections.GetDescriptionOrDisplayName(PropertyOne);
        var desc2 = TypeReflections.GetDescriptionOrDisplayName(PropertyTwo);
        var desc3 = TypeReflections.GetDescriptionOrDisplayName(PropertyThree);
        desc1.ShouldBe("PropertyOne");
        desc2.ShouldBe("PropertyDisplayTwo");
        desc3.ShouldBe("PropertyThree");
    }
    /// <summary>
    /// 测试 - 字段级别 - 获取描述或显示名称
    /// </summary>
    [Fact]
    public void Test_FieldLevel_DescriptionOrDisplayName_Getting()
    {
        var desc1 = TypeReflections.GetDescriptionOrDisplayName(FieldOne);
        var desc2 = TypeReflections.GetDescriptionOrDisplayName(FieldTwo);
        var desc3 = TypeReflections.GetDescriptionOrDisplayName(FieldThree);
        desc1.ShouldBe("FieldOne");
        desc2.ShouldBe("FieldDisplayTwo");
        desc3.ShouldBe("FieldThree");
    }
    /// <summary>
    /// 测试 - 方法级别 - 获取描述或显示名称
    /// </summary>
    [Fact]
    public void Test_MethodLevel_DescriptionOrDisplayName_Getting()
    {
        var desc1 = TypeReflections.GetDescriptionOrDisplayName(MethodOne);
        var desc2 = TypeReflections.GetDescriptionOrDisplayName(MethodTwo);
        var desc3 = TypeReflections.GetDescriptionOrDisplayName(MethodThree);
        desc1.ShouldBe("MethodOne");
        desc2.ShouldBe("MethodDisplayTwo");
        desc3.ShouldBe("MethodThree");
    }
    /// <summary>
    /// 测试 - 参数级别 - 获取描述或显示名称
    /// </summary>
    [Fact]
    public void Test_ParameterLevel_DescriptionOrDisplayName_Getting()
    {
        var desc1 = TypeReflections.GetDescriptionOrDisplayName(ParameterOne);
        var desc2 = TypeReflections.GetDescriptionOrDisplayName(ParameterTwo);
        var desc3 = TypeReflections.GetDescriptionOrDisplayName(ParameterThree);
        desc1.ShouldBe("ArgOne");
        desc2.ShouldBe("ParamDesc");
        desc3.ShouldBe("ArgThree");
    }
}
