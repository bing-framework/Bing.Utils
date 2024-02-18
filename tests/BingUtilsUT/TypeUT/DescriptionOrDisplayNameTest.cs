using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT","TypeReflections.GetDescriptionOrDisplay")]
public class DescriptionOrDisplayNameTest
{
    public Type EntryOne = typeof(NormalDescriptionClass);
    public Type EntryTwo = typeof(NormalDescriptionOrClass);
    public Type EntryThree = typeof(NormalDisplayNameOrClass);
    public Type Wrapper = typeof(NormalDescriptionWrapper);

    public PropertyInfo PropertyOne;
    public PropertyInfo PropertyTwo;
    public PropertyInfo PropertyThree;

    public FieldInfo FieldOne;
    public FieldInfo FieldTwo;
    public FieldInfo FieldThree;

    public MethodInfo MethodOne;
    public MethodInfo MethodTwo;
    public MethodInfo MethodThree;

    public ParameterInfo ParameterOne;
    public ParameterInfo ParameterTwo;
    public ParameterInfo ParameterThree;

    /// <summary>
    /// 测试初始化
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