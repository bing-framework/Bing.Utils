using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "AttributeGetting")]
public class AttributeGettingTest
{
    /// <summary>
    /// 测试 - 类型 - 获取特性
    /// </summary>
    [Fact]
    public void Test_DirectType_GettingAttribute()
    {
        var type = typeof(NormalWithAttrClass);
        var one = typeof(ModelOneAttribute);
        var two = typeof(ModelTwoAttribute);
        var three = typeof(ModelThreeAttribute);

        TypeReflections.GetAttribute(type, one).ShouldNotBeNull();
        TypeReflections.GetAttribute(type, two).ShouldBeNull();
        TypeReflections.GetAttribute(type, three).ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - 泛型类型 - 获取特性
    /// </summary>
    [Fact]
    public void Test_GenericType_GettingAttribute()
    {
        var type = typeof(NormalWithAttrClass);

        TypeReflections.GetAttribute<ModelOneAttribute>(type).ShouldNotBeNull();
        TypeReflections.GetAttribute<ModelTwoAttribute>(type).ShouldBeNull();
        TypeReflections.GetAttribute<ModelThreeAttribute>(type).ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - 类型 - 获取特性 - 继承选项
    /// </summary>
    [Fact]
    public void Test_DirectType_GettingAttribute_Inherit()
    {
        var type = typeof(NormalWithAttrClassWrapper2);
        var one = typeof(ModelOneAttribute);
        var two = typeof(ModelTwoAttribute);
        var three = typeof(ModelThreeAttribute);
        var four = typeof(ModelFourAttribute);

        TypeReflections.GetAttribute(type, one).ShouldBeNull();
        TypeReflections.GetAttribute(type, two).ShouldBeNull();
        TypeReflections.GetAttribute(type, three).ShouldBeNull();
        TypeReflections.GetAttribute(type, four).ShouldNotBeNull();

        Assert.Throws<AmbiguousMatchException>(() => TypeReflections.GetAttribute(type, one, ReflectionOptions.Inherit));
        TypeReflections.GetAttribute(type, one, ReflectionOptions.Inherit, ReflectionAmbiguousOptions.IgnoreAmbiguous).ShouldNotBeNull();
        TypeReflections.GetAttribute(type, two, ReflectionOptions.Inherit).ShouldBeNull();
        TypeReflections.GetAttribute(type, three, ReflectionOptions.Inherit).ShouldNotBeNull();
        TypeReflections.GetAttribute(type, four, ReflectionOptions.Inherit).ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - 泛型类型 - 获取特性 - 继承选项
    /// </summary>
    [Fact]
    public void Test_GenericType_GettingAttribute_Inherit()
    {
        var type = typeof(NormalWithAttrClassWrapper2);

        TypeReflections.GetAttribute<ModelOneAttribute>(type).ShouldBeNull();
        TypeReflections.GetAttribute<ModelTwoAttribute>(type).ShouldBeNull();
        TypeReflections.GetAttribute<ModelThreeAttribute>(type).ShouldBeNull();
        TypeReflections.GetAttribute<ModelFourAttribute>(type).ShouldNotBeNull();

        Assert.Throws<AmbiguousMatchException>(() => TypeReflections.GetAttribute<ModelOneAttribute>(type, ReflectionOptions.Inherit));
        TypeReflections.GetAttribute<ModelOneAttribute>(type, ReflectionOptions.Inherit, ReflectionAmbiguousOptions.IgnoreAmbiguous).ShouldNotBeNull();
        TypeReflections.GetAttribute<ModelTwoAttribute>(type, ReflectionOptions.Inherit).ShouldBeNull();
        TypeReflections.GetAttribute<ModelThreeAttribute>(type, ReflectionOptions.Inherit).ShouldNotBeNull();
        TypeReflections.GetAttribute<ModelFourAttribute>(type, ReflectionOptions.Inherit).ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - 类型 - 获取特性列表
    /// </summary>
    [Fact]
    public void Test_DirectType_GettingAttributes()
    {
        var type = typeof(NormalWithAttrClass);
        var one = typeof(ModelOneAttribute);
        var two = typeof(ModelTwoAttribute);
        var three = typeof(ModelThreeAttribute);

        var val1 = TypeReflections.GetAttributes(type, one);
        var val2 = TypeReflections.GetAttributes(type, two);
        var val3 = TypeReflections.GetAttributes(type, three);

        val1.ShouldNotBeEmpty();
        val2.ShouldBeEmpty();
        val3.ShouldNotBeEmpty();

        val1.Count().ShouldBe(2);
        val3.Count().ShouldBe(1);
    }

    /// <summary>
    /// 测试 - 泛型类型 - 获取特性列表
    /// </summary>
    [Fact]
    public void Test_GenericType_GettingAttributes()
    {
        var type = typeof(NormalWithAttrClass);

        var val1 = TypeReflections.GetAttributes<ModelOneAttribute>(type);
        var val2 = TypeReflections.GetAttributes<ModelTwoAttribute>(type);
        var val3 = TypeReflections.GetAttributes<ModelThreeAttribute>(type);

        val1.ShouldNotBeEmpty();
        val2.ShouldBeEmpty();
        val3.ShouldNotBeEmpty();

        val1.Count().ShouldBe(2);
        val3.Count().ShouldBe(1);
    }

    /// <summary>
    /// 测试 - 类型 - 获取特性列表 - 继承选项
    /// </summary>
    [Fact]
    public void Test_DirectType_GettingAttributes_Inherit()
    {
        var type = typeof(NormalWithAttrClassWrapper2);
        var one = typeof(ModelOneAttribute);
        var two = typeof(ModelTwoAttribute);
        var three = typeof(ModelThreeAttribute);
        var four = typeof(ModelFourAttribute);

        var val1 = TypeReflections.GetAttributes(type, one);
        var val2 = TypeReflections.GetAttributes(type, two);
        var val3 = TypeReflections.GetAttributes(type, three);
        var val4 = TypeReflections.GetAttributes(type, four);

        val1.ShouldBeEmpty();
        val2.ShouldBeEmpty();
        val3.ShouldBeEmpty();
        val4.ShouldNotBeEmpty();

        val4.Count().ShouldBe(1);

        val1 = TypeReflections.GetAttributes(type, one, ReflectionOptions.Inherit);
        val2 = TypeReflections.GetAttributes(type, two, ReflectionOptions.Inherit);
        val3 = TypeReflections.GetAttributes(type, three, ReflectionOptions.Inherit);
        val4 = TypeReflections.GetAttributes(type, four, ReflectionOptions.Inherit);

        val1.ShouldNotBeEmpty();
        val2.ShouldBeEmpty();
        val3.ShouldNotBeEmpty();
        val4.ShouldNotBeEmpty();

        val1.Count().ShouldBe(2);
        val3.Count().ShouldBe(1);
        val4.Count().ShouldBe(1);
    }

    /// <summary>
    /// 测试 - 泛型类型 - 获取特性列表 - 继承选项
    /// </summary>
    [Fact]
    public void Test_GenericType_GettingAttributes_Inherit()
    {
        var type = typeof(NormalWithAttrClassWrapper2);

        var val1 = TypeReflections.GetAttributes<ModelOneAttribute>(type);
        var val2 = TypeReflections.GetAttributes<ModelTwoAttribute>(type);
        var val3 = TypeReflections.GetAttributes<ModelThreeAttribute>(type);
        var val4 = TypeReflections.GetAttributes<ModelFourAttribute>(type);

        val1.ShouldBeEmpty();
        val2.ShouldBeEmpty();
        val3.ShouldBeEmpty();
        val4.ShouldNotBeEmpty();

        val4.Count().ShouldBe(1);

        val1 = TypeReflections.GetAttributes<ModelOneAttribute>(type, ReflectionOptions.Inherit);
        val2 = TypeReflections.GetAttributes<ModelTwoAttribute>(type, ReflectionOptions.Inherit);
        val3 = TypeReflections.GetAttributes<ModelThreeAttribute>(type, ReflectionOptions.Inherit);
        val4 = TypeReflections.GetAttributes<ModelFourAttribute>(type, ReflectionOptions.Inherit);

        val1.ShouldNotBeEmpty();
        val2.ShouldBeEmpty();
        val3.ShouldNotBeEmpty();
        val4.ShouldNotBeEmpty();

        val1.Count().ShouldBe(2);
        val3.Count().ShouldBe(1);
        val4.Count().ShouldBe(1);
    }

    /// <summary>
    /// 测试 - 类型 - 获取特性 - 必需
    /// </summary>
    [Fact]
    public void Test_DirectType_GettingAttribute_Required()
    {
        var type = typeof(NormalWithAttrClass);
        var one = typeof(ModelOneAttribute);
        var two = typeof(ModelTwoAttribute);
        var three = typeof(ModelThreeAttribute);

        TypeReflections.GetAttributeRequired(type, one).ShouldNotBeNull();
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributeRequired(type, two).ShouldBeNull());
        TypeReflections.GetAttributeRequired(type, three).ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - 泛型类型 - 获取特性 - 必需
    /// </summary>
    [Fact]
    public void Test_GenericType_GettingAttribute_Required()
    {
        var type = typeof(NormalWithAttrClass);

        TypeReflections.GetAttributeRequired<ModelOneAttribute>(type).ShouldNotBeNull();
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributeRequired<ModelTwoAttribute>(type));
        TypeReflections.GetAttributeRequired<ModelThreeAttribute>(type).ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - 类型 - 获取特性 - 必需 - 继承选项
    /// </summary>
    [Fact]
    public void Test_DirectType_GettingAttribute_Required_Inherit()
    {
        var type = typeof(NormalWithAttrClassWrapper2);
        var one = typeof(ModelOneAttribute);
        var two = typeof(ModelTwoAttribute);
        var three = typeof(ModelThreeAttribute);
        var four = typeof(ModelFourAttribute);

        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributeRequired(type, one));
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributeRequired(type, two).ShouldBeNull());
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributeRequired(type, three).ShouldBeNull());
        Assert.Throws<AmbiguousMatchException>(() => TypeReflections.GetAttributeRequired(type, one, ReflectionOptions.Inherit));
        TypeReflections.GetAttributeRequired(type, one, ReflectionOptions.Inherit, ReflectionAmbiguousOptions.IgnoreAmbiguous).ShouldNotBeNull();
        TypeReflections.GetAttributeRequired(type, three, ReflectionOptions.Inherit).ShouldNotBeNull();
        TypeReflections.GetAttributeRequired(type, four).ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - 泛型类型 - 获取特性 - 必需 - 继承选项
    /// </summary>
    [Fact]
    public void Test_GenericType_GettingAttribute_Required_Inherit()
    {
        var type = typeof(NormalWithAttrClassWrapper2);

        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributeRequired<ModelOneAttribute>(type));
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributeRequired<ModelTwoAttribute>(type));
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributeRequired<ModelThreeAttribute>(type));
        Assert.Throws<AmbiguousMatchException>(() => TypeReflections.GetAttributeRequired<ModelOneAttribute>(type, ReflectionOptions.Inherit).ShouldNotBeNull());
        TypeReflections.GetAttributeRequired<ModelOneAttribute>(type, ReflectionOptions.Inherit, ReflectionAmbiguousOptions.IgnoreAmbiguous).ShouldNotBeNull();
        TypeReflections.GetAttributeRequired<ModelThreeAttribute>(type, ReflectionOptions.Inherit).ShouldNotBeNull();
        TypeReflections.GetAttributeRequired<ModelFourAttribute>(type).ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - 类型 - 获取特性列表 - 必需
    /// </summary>
    [Fact]
    public void Test_DirectType_GettingAttributes_Required()
    {
        var type = typeof(NormalWithAttrClass);
        var one = typeof(ModelOneAttribute);
        var two = typeof(ModelTwoAttribute);
        var three = typeof(ModelThreeAttribute);

        var val1 = TypeReflections.GetAttributesRequired(type, one);
        var val3 = TypeReflections.GetAttributesRequired(type, three);

        val1.ShouldNotBeEmpty();
        val3.ShouldNotBeEmpty();

        val1.Count().ShouldBe(2);
        val3.Count().ShouldBe(1);

        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired(type, two));
    }

    /// <summary>
    /// 测试 - 泛型类型 - 获取特性列表 - 必需
    /// </summary>
    [Fact]
    public void Test_GenericType_GettingAttributes_Required()
    {
        var type = typeof(NormalWithAttrClass);

        var val1 = TypeReflections.GetAttributesRequired<ModelOneAttribute>(type);
        var val3 = TypeReflections.GetAttributesRequired<ModelThreeAttribute>(type);

        val1.ShouldNotBeEmpty();
        val3.ShouldNotBeEmpty();

        val1.Count().ShouldBe(2);
        val3.Count().ShouldBe(1);

        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired<ModelTwoAttribute>(type));
    }

    /// <summary>
    /// 测试 - 类型 - 获取特性列表 - 必需 - 继承选项
    /// </summary>
    [Fact]
    public void Test_DirectType_GettingAttributes_Required_Inherit()
    {
        var type = typeof(NormalWithAttrClassWrapper2);
        var one = typeof(ModelOneAttribute);
        var two = typeof(ModelTwoAttribute);
        var three = typeof(ModelThreeAttribute);
        var four = typeof(ModelFourAttribute);

        var val1 = TypeReflections.GetAttributesRequired(type, four);

        val1.ShouldNotBeEmpty();
        val1.Count().ShouldBe(1);

        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired(type, one));
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired(type, two));
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired(type, three));

        var val2 = TypeReflections.GetAttributesRequired(type, one, ReflectionOptions.Inherit);
        var val3 = TypeReflections.GetAttributesRequired(type, three, ReflectionOptions.Inherit);

        val2.ShouldNotBeEmpty();
        val3.ShouldNotBeEmpty();

        val2.Count().ShouldBe(2);
        val3.Count().ShouldBe(1);

        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired(type, two, ReflectionOptions.Inherit));
    }

    /// <summary>
    /// 测试 - 泛型类型 - 获取特性列表 - 必需 - 继承选项
    /// </summary>
    [Fact]
    public void Test_GenericType_GettingAttributes_Required_Inherit()
    {
        var type = typeof(NormalWithAttrClassWrapper2);

        var val1 = TypeReflections.GetAttributesRequired<ModelFourAttribute>(type);

        val1.ShouldNotBeEmpty();
        val1.Count().ShouldBe(1);

        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired<ModelOneAttribute>(type));
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired<ModelTwoAttribute>(type));
        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired<ModelThreeAttribute>(type));

        var val2 = TypeReflections.GetAttributesRequired<ModelOneAttribute>(type, ReflectionOptions.Inherit);
        var val3 = TypeReflections.GetAttributesRequired<ModelThreeAttribute>(type, ReflectionOptions.Inherit);

        val2.ShouldNotBeEmpty();
        val3.ShouldNotBeEmpty();

        val2.Count().ShouldBe(2);
        val3.Count().ShouldBe(1);

        Assert.Throws<ArgumentException>(() => TypeReflections.GetAttributesRequired<ModelTwoAttribute>(type, ReflectionOptions.Inherit));
    }

    /// <summary>
    /// 测试 - 获取全部特性
    /// </summary>
    [Fact]
    public void Test_AllAttributeGetting()
    {
        var val = TypeReflections.GetAttributes(typeof(NormalWithAttrClass));

        val.ShouldNotBeNull();
        val.ShouldNotBeEmpty();
        val.Count().ShouldBe(3);
    }

    /// <summary>
    /// 测试 - 获取全部特性 - 继承选项
    /// </summary>
    [Fact]
    public void Test_AllAttributeGetting_Inherit()
    {
        var val1 = TypeReflections.GetAttributes(typeof(NormalWithAttrClassWrapper2));
        var val2 = TypeReflections.GetAttributes(typeof(NormalWithAttrClassWrapper2), ReflectionOptions.Inherit);

        val1.ShouldNotBeNull();
        val1.ShouldNotBeEmpty();
        val1.Count().ShouldBe(1);

        val2.ShouldNotBeNull();
        val2.ShouldNotBeEmpty();
        val2.Count().ShouldBe(4);
    }
}