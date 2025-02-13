using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT","TypeReflections.GetDisplayName")]
public class DisplayNameGettingTest
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
    public DisplayNameGettingTest()
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
    /// 测试 - 类级别 - 获取显示名称
    /// </summary>
    [Fact]
    public void Test_ClassLevel_Description_Getting()
    {
        var desc1 = TypeReflections.GetDisplayName(EntryOne);
        var desc2 = TypeReflections.GetDisplayName(EntryTwo);
        var desc3 = TypeReflections.GetDisplayName(EntryThree);
        var desc4 = TypeReflections.GetDisplayName(Wrapper);
        var desc5 = TypeReflections.GetDisplayName(Wrapper, ReflectionOptions.Inherit);

        desc1.ShouldBe("NormalClassDisplayNameOne");
        desc2.ShouldBe("NormalClassDisplayNameTwo");
        desc3.ShouldBe(EntryThree.Name);
        desc4.ShouldBe(Wrapper.Name);
        desc5.ShouldBe("NormalClassDisplayNameOne");

        desc1 = TypeReflections.GetDisplayNameOr(EntryOne, "OrMe");
        desc2 = TypeReflections.GetDisplayNameOr(EntryTwo, "OrMe");
        desc3 = TypeReflections.GetDisplayNameOr(EntryThree, "OrMe");
        desc4 = TypeReflections.GetDisplayNameOr(Wrapper, "OrMe");
        desc5 = TypeReflections.GetDisplayNameOr(Wrapper, "OrMe", ReflectionOptions.Inherit);

        desc1.ShouldBe("NormalClassDisplayNameOne");
        desc2.ShouldBe("NormalClassDisplayNameTwo");
        desc3.ShouldBe("OrMe");
        desc4.ShouldBe("OrMe");
        desc5.ShouldBe("NormalClassDisplayNameOne");
    }

    /// <summary>
    /// 测试 - 属性级别 - 获取显示名称
    /// </summary>
    [Fact]
    public void Test_PropertyLevel_Description_Getting()
    {
        var desc1 = TypeReflections.GetDisplayName(PropertyOne);
        var desc2 = TypeReflections.GetDisplayName(PropertyTwo);
        var desc3 = TypeReflections.GetDisplayName(PropertyThree);

        desc1.ShouldBe("PropertyDisplayNameOne");
        desc2.ShouldBe("PropertyDisplayNameTwo");
        desc3.ShouldBe(PropertyThree.Name);

        desc1 = TypeReflections.GetDisplayNameOr(PropertyOne, "OrMe");
        desc2 = TypeReflections.GetDisplayNameOr(PropertyTwo, "OrMe");
        desc3 = TypeReflections.GetDisplayNameOr(PropertyThree, "OrMe");

        desc1.ShouldBe("PropertyDisplayNameOne");
        desc2.ShouldBe("PropertyDisplayNameTwo");
        desc3.ShouldBe("OrMe");
    }

    /// <summary>
    /// 测试 - 字段级别 - 获取显示名称
    /// </summary>
    [Fact]
    public void Test_FieldLevel_Description_Getting()
    {
        var desc1 = TypeReflections.GetDisplayName(FieldOne);
        var desc2 = TypeReflections.GetDisplayName(FieldTwo);
        var desc3 = TypeReflections.GetDisplayName(FieldThree);

        desc1.ShouldBe("FieldOne");
        desc2.ShouldBe("FieldTwo");
        desc3.ShouldBe(FieldThree.Name);
            
        desc1 = TypeReflections.GetDisplayNameOr(FieldOne, "OrMe");
        desc2 = TypeReflections.GetDisplayNameOr(FieldTwo, "OrMe");
        desc3 = TypeReflections.GetDisplayNameOr(FieldThree, "OrMe");

        desc1.ShouldBe("FieldOne");
        desc2.ShouldBe("FieldTwo");
        desc3.ShouldBe("OrMe");
    }

    /// <summary>
    /// 测试 - 方法级别 - 获取显示名称
    /// </summary>
    [Fact]
    public void Test_MethodLevel_Description_Getting()
    {
        var desc1 = TypeReflections.GetDisplayName(MethodOne);
        var desc2 = TypeReflections.GetDisplayName(MethodTwo);
        var desc3 = TypeReflections.GetDisplayName(MethodThree);

        desc1.ShouldBe("MethodDisplayNameOne");
        desc2.ShouldBe("MethodDisplayNameTwo");
        desc3.ShouldBe(MethodThree.Name);
            
        desc1 = TypeReflections.GetDisplayNameOr(MethodOne, "OrMe");
        desc2 = TypeReflections.GetDisplayNameOr(MethodTwo, "OrMe");
        desc3 = TypeReflections.GetDisplayNameOr(MethodThree, "OrMe");

        desc1.ShouldBe("MethodDisplayNameOne");
        desc2.ShouldBe("MethodDisplayNameTwo");
        desc3.ShouldBe("OrMe");
    }

    /// <summary>
    /// 测试 - 参数级别 - 获取显示名称
    /// </summary>
    [Fact]
    public void Test_ParameterLevel_Description_Getting()
    {
        var desc1 = TypeReflections.GetDisplayName(ParameterOne);
        var desc2 = TypeReflections.GetDisplayName(ParameterTwo);
        var desc3 = TypeReflections.GetDisplayName(ParameterThree);

        desc1.ShouldBe("ParamName");
        desc2.ShouldBe("ParamName");
        desc3.ShouldBe(ParameterThree.Name);
            
        desc1 = TypeReflections.GetDisplayNameOr(ParameterOne, "OrMe");
        desc2 = TypeReflections.GetDisplayNameOr(ParameterTwo, "OrMe");
        desc3 = TypeReflections.GetDisplayNameOr(ParameterThree, "OrMe");

        desc1.ShouldBe("ParamName");
        desc2.ShouldBe("ParamName");
        desc3.ShouldBe("OrMe");
    }
}