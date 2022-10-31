using System.Collections.Generic;
using System.Reflection;
using Bing.Reflection;
using Bing.Tests.Samples;
using Xunit;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Reflection;

/// <summary>
/// 测试反射操作
/// </summary>
public class ReflectionsTest : TestBase
{
    /// <summary>
    /// 测试样例
    /// </summary>
    private readonly Sample _sample;

    /// <summary>
    /// 初始化一个<see cref="TestBase"/>类型的实例
    /// </summary>
    public ReflectionsTest(ITestOutputHelper output) : base(output)
    {
        _sample = new Sample();
    }

    /// <summary>
    /// 测试 - 获取类成员描述
    /// </summary>
    [Fact]
    public void Test_GetDescription()
    {
        Assert.Equal("", Reflections.GetDescription<EnumSample>("X"));
        Assert.Equal("A", Reflections.GetDescription<EnumSample>("A"));
        Assert.Equal("B2", Reflections.GetDescription<EnumSample>("B"));
        Assert.Equal("IntValue", Reflections.GetDescription<Sample>("IntValue"));
    }

    /// <summary>
    /// 测试获取类描述
    /// </summary>
    [Fact]
    public void TestGetDescription_Class()
    {
        Assert.Equal("测试样例", Reflections.GetDescription<Sample>());
        Assert.Equal("Sample2", Reflections.GetDescription<Sample2>());
    }

    /// <summary>
    /// 测试显示名
    /// </summary>
    [Fact]
    public void TestGetDisplayName()
    {
        Assert.Equal("", Reflections.GetDisplayName<Sample>());
        Assert.Equal("测试样例2", Reflections.GetDisplayName<Sample2>());
    }

    /// <summary>
    /// 测试获取类描述或显示名
    /// </summary>
    [Fact]
    public void TestGetDescriptionOrDisplayName()
    {
        Assert.Equal("测试样例", Reflections.GetDisplayNameOrDescription<Sample>());
        Assert.Equal("测试样例2",Reflections.GetDisplayNameOrDescription<Sample2>());
        Assert.Equal("测试样例", Reflections.GetDisplayNameOrDescription<Sample>());
    }

    /// <summary>
    /// 测试是否布尔类型
    /// </summary>
    [Fact]
    public void TestIsBool()
    {
        Assert.True(Reflections.IsBool(_sample.BoolValue.GetType().GetTypeInfo()), "BoolValue GetType");
        Assert.True(Reflections.IsBool(_sample.GetType().GetMember("BoolValue")[0]), "BoolValue");
        Assert.True(Reflections.IsBool(_sample.GetType().GetMember("NullableBoolValue")[0]), "NullableBoolValue");
        Assert.False(Reflections.IsBool(_sample.GetType().GetMember("EnumValue")[0]), "EnumValue");
    }

    /// <summary>
    /// 测试是否枚举类型
    /// </summary>
    [Fact]
    public void TestIsEnum()
    {
        Assert.True(Reflections.IsEnum(_sample.EnumValue.GetType().GetTypeInfo()), "EnumValue GetType");
        Assert.True(Reflections.IsEnum(_sample.GetType().GetMember("EnumValue")[0]), "EnumValue");
        Assert.True(Reflections.IsEnum(_sample.GetType().GetMember("NullableEnumValue")[0]), "NullableEnumValue");
        Assert.False(Reflections.IsEnum(_sample.GetType().GetMember("BoolValue")[0]), "BoolValue");
        Assert.False(Reflections.IsEnum(_sample.GetType().GetMember("NullableBoolValue")[0]), "NullableBoolValue");
    }

    /// <summary>
    /// 测试是否日期类型
    /// </summary>
    [Fact]
    public void TestIsDate()
    {
        Assert.True(Reflections.IsDate(_sample.DateValue.GetType().GetTypeInfo()), "DateValue GetType");
        Assert.True(Reflections.IsDate(_sample.GetType().GetMember("DateValue")[0]), "DateValue");
        Assert.True(Reflections.IsDate(_sample.GetType().GetMember("NullableDateValue")[0]), "NullableDateValue");
        Assert.False(Reflections.IsDate(_sample.GetType().GetMember("EnumValue")[0]), "EnumValue");
    }

    /// <summary>
    /// 测试是否整型
    /// </summary>
    [Fact]
    public void TestIsInt()
    {
        Assert.True(Reflections.IsInt(_sample.IntValue.GetType().GetTypeInfo()), "IntValue GetType");
        Assert.True(Reflections.IsInt(_sample.GetType().GetMember("IntValue")[0]), "IntValue");
        Assert.True(Reflections.IsInt(_sample.GetType().GetMember("NullableIntValue")[0]), "NullableIntValue");

        Assert.True(Reflections.IsInt(_sample.ShortValue.GetType().GetTypeInfo()), "ShortValue GetType");
        Assert.True(Reflections.IsInt(_sample.GetType().GetMember("ShortValue")[0]), "ShortValue");
        Assert.True(Reflections.IsInt(_sample.GetType().GetMember("NullableShortValue")[0]), "NullableShortValue");

        Assert.True(Reflections.IsInt(_sample.LongValue.GetType().GetTypeInfo()), "LongValue GetType");
        Assert.True(Reflections.IsInt(_sample.GetType().GetMember("LongValue")[0]), "LongValue");
        Assert.True(Reflections.IsInt(_sample.GetType().GetMember("NullableLongValue")[0]), "NullableLongValue");
    }

    /// <summary>
    /// 测试是否数值类型
    /// </summary>
    [Fact]
    public void TestIsNumber()
    {
        Assert.True(Reflections.IsNumber(_sample.DoubleValue.GetType().GetTypeInfo()), "DoubleValue GetType");
        Assert.True(Reflections.IsNumber(_sample.GetType().GetMember("DoubleValue")[0]), "DoubleValue");
        Assert.True(Reflections.IsNumber(_sample.GetType().GetMember("NullableDoubleValue")[0]), "NullableDoubleValue");

        Assert.True(Reflections.IsNumber(_sample.DecimalValue.GetType().GetTypeInfo()), "DecimalValue GetType");
        Assert.True(Reflections.IsNumber(_sample.GetType().GetMember("DecimalValue")[0]), "DecimalValue");
        Assert.True(Reflections.IsNumber(_sample.GetType().GetMember("NullableDecimalValue")[0]), "NullableDecimalValue");

        Assert.True(Reflections.IsNumber(_sample.FloatValue.GetType().GetTypeInfo()), "FloatValue GetType");
        Assert.True(Reflections.IsNumber(_sample.GetType().GetMember("FloatValue")[0]), "FloatValue");
        Assert.True(Reflections.IsNumber(_sample.GetType().GetMember("NullableFloatValue")[0]), "NullableFloatValue");

        Assert.True(Reflections.IsNumber(_sample.IntValue.GetType().GetTypeInfo()), "IntValue GetType");
        Assert.True(Reflections.IsNumber(_sample.GetType().GetMember("IntValue")[0]), "IntValue");
        Assert.True(Reflections.IsNumber(_sample.GetType().GetMember("NullableIntValue")[0]), "NullableIntValue");
    }

    /// <summary>
    /// 测试是否集合
    /// </summary>
    [Fact]
    public void TestIsCollection()
    {
        Assert.True(Reflections.IsCollection(_sample.StringArray.GetType()));
        Assert.True(TypeReflections.IsCollection(_sample.StringArray.GetType()));
    }

    /// <summary>
    /// 测试是否泛型集合
    /// </summary>
    [Fact]
    public void TestIsGenericCollection()
    {
        Assert.True(Reflections.IsGenericCollection(_sample.StringList.GetType()));
    }

    /// <summary>
    /// 测试获取公共属性列表
    /// </summary>
    [Fact]
    public void TestGetPublicProperties()
    {
        Sample4 sample = new Sample4
        {
            A = "1",
            B = "2"
        };
        var items = Reflections.GetPublicProperties(sample);
        Assert.Equal(2, items.Count);
        Assert.Equal("A", items[0].Text);
        Assert.Equal("1", items[0].Value);
        Assert.Equal("B", items[1].Text);
        Assert.Equal("2", items[1].Value);
    }

    /// <summary>
    /// 获取顶级基类
    /// </summary>
    [Fact]
    public void TestGetTopBaseType()
    {
        Assert.Null(Reflections.GetTopBaseType(null));
    }

    /// <summary>
    /// 获取元素类型
    /// </summary>
    [Fact]
    public void TestGetElementType_1()
    {
        Sample sample = new Sample();
        Assert.Equal(typeof(Sample), Reflections.GetElementType(sample.GetType()));
    }

    /// <summary>
    /// 获取元素类型 - 数组
    /// </summary>
    [Fact]
    public void TestGetElementType_2()
    {
        var list = new[] { new Sample() };
        var type = list.GetType();
        Assert.Equal(typeof(Sample), Reflections.GetElementType(type));
    }

    /// <summary>
    /// 获取元素类型 - 集合
    /// </summary>
    [Fact]
    public void TestGetElementType_3()
    {
        var list = new List<Sample> { new Sample() };
        var type = list.GetType();
        Assert.Equal(typeof(Sample), Reflections.GetElementType(type));
    }
}