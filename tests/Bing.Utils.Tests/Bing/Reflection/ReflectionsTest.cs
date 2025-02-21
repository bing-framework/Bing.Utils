﻿using Bing.Tests.Samples;
using Bing.Utils.Tests;

namespace Bing.Reflection;

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
    /// 测试初始化
    /// </summary>
    public ReflectionsTest(ITestOutputHelper output) : base(output)
    {
        _sample = new Sample();
    }

    #region GetDescription

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
    /// 测试 - 获取类描述
    /// </summary>
    [Fact]
    public void Test_GetDescription_Class()
    {
        Assert.Equal("测试样例", Reflections.GetDescription<Sample>());
        Assert.Equal("Sample2", Reflections.GetDescription<Sample2>());
    }

    #endregion

    #region GetDisplayName

    /// <summary>
    /// 测试 - 显示名
    /// </summary>
    [Fact]
    public void Test_GetDisplayName()
    {
        Assert.Equal("", Reflections.GetDisplayName<Sample>());
        Assert.Equal("测试样例2", Reflections.GetDisplayName<Sample2>());
    }

    #endregion

    #region GetDisplayNameOrDescription

    /// <summary>
    /// 测试 - 获取类描述或显示名
    /// </summary>
    [Fact]
    public void Test_GetDescriptionOrDisplayName()
    {
        Assert.Equal("测试样例", Reflections.GetDisplayNameOrDescription<Sample>());
        Assert.Equal("测试样例2", Reflections.GetDisplayNameOrDescription<Sample2>());
        Assert.Equal("测试样例", Reflections.GetDisplayNameOrDescription<Sample>());
    }

    #endregion

    #region FindImplementTypes

    /// <summary>
    /// 测试 - 查找实现类型列表 - 测试类型中只有一个类实现IA接口
    /// </summary>
    [Fact]
    public void Test_FindImplementTypes_1()
    {
        var types = Reflections.FindImplementTypes<IA>(this.GetType().Assembly);
        Assert.Single(types);
        Assert.Equal(typeof(A), types[0]);
    }

    /// <summary>
    /// 测试 - 查找实现类型列表 - 抽象类和接口被排除
    /// </summary>
    [Fact]
    public void Test_FindImplementTypes_2()
    {
        var types = Reflections.FindImplementTypes<IB>(this.GetType().Assembly);
        Assert.Equal(2, types.Count);
        Assert.Equal(typeof(A), types[0]);
        Assert.Equal(typeof(B), types[1]);
    }

    /// <summary>
    /// 测试 - 查找实现类型列表 - 泛型实现 
    /// </summary>
    [Fact]
    public void Test_FindImplementTypes_3()
    {
        var types = Reflections.FindImplementTypes<IC>(this.GetType().Assembly);
        Assert.Equal(2, types.Count);
        Assert.Equal(typeof(B), types[0]);
        Assert.Equal(typeof(D<>), types[1]);
    }

    /// <summary>
    /// 测试 - 查找实现类型列表 - 泛型参数为E
    /// </summary>
    [Fact]
    public void Test_FindImplementTypes_4()
    {
        var types = Reflections.FindImplementTypes<IG<E>>(this.GetType().Assembly);
        Assert.Single(types);
        Assert.Equal(typeof(E), types[0]);
    }

    /// <summary>
    /// 测试 - 查找实现类型列表 - 泛型参数为空
    /// </summary>
    [Fact]
    public void Test_FindImplementTypes_5()
    {
        var types = Reflections.FindImplementTypes(typeof(IG<>), this.GetType().Assembly);
        Assert.Equal(2, types.Count);
        Assert.Equal(typeof(E), types[0]);
        Assert.Equal(typeof(F<>), types[1]);
    }

    /// <summary>
    /// 测试 - 查找实现类型列表 - 测试并发
    /// </summary>
    [Fact]
    public void Test_FindImplementTypes_6()
    {
        Bing.Helpers.Thread.ParallelExecute(() =>
        {
            var types = Reflections.FindImplementTypes<IA>(this.GetType().Assembly);
            Assert.Single(types);
            Assert.Equal(typeof(A), types[0]);
        }, () =>
        {
            var types = Reflections.FindImplementTypes<IB>(this.GetType().Assembly);
            Assert.Equal(2, types.Count);
            Assert.Equal(typeof(A), types[0]);
            Assert.Equal(typeof(B), types[1]);
        }, () =>
        {
            var types = Reflections.FindImplementTypes<IC>(this.GetType().Assembly);
            Assert.Equal(2, types.Count);
            Assert.Equal(typeof(B), types[0]);
            Assert.Equal(typeof(D<>), types[1]);
        });
    }

    #endregion

    #region GetDirectInterfaceTypes

    /// <summary>
    /// 测试 - 获取直接接口列表
    /// </summary>
    [Fact]
    public void Test_GetDirectInterfaces()
    {
        var interfaceTypes = Reflections.GetDirectInterfaceTypes<TestSample>();
        Assert.Equal(2, interfaceTypes.Count);
        Assert.True(interfaceTypes.Exists(t => t.Name == "ITestSample"));
        Assert.True(interfaceTypes.Exists(t => t.Name == "ITestSample5"));
    }

    /// <summary>
    /// 测试 - 获取直接接口列表 - 一个基接口
    /// </summary>
    [Fact]
    public void Test_GetDirectInterfaces_2()
    {
        var interfaceTypes = Reflections.GetDirectInterfaceTypes<TestSample>(typeof(ITestSample3));
        Assert.Single(interfaceTypes);
        Assert.True(interfaceTypes.Exists(t => t.Name == "ITestSample"));
    }

    /// <summary>
    /// 测试 - 获取直接接口列表 - 两个基接口
    /// </summary>
    [Fact]
    public void Test_GetDirectInterfaces_3()
    {
        var interfaceTypes = Reflections.GetDirectInterfaceTypes<TestSample>(typeof(ITestSample4), typeof(ITestSample3));
        Assert.Equal(2, interfaceTypes.Count);
        Assert.True(interfaceTypes.Exists(t => t.Name == "ITestSample"));
        Assert.True(interfaceTypes.Exists(t => t.Name == "ITestSample5"));
    }

    #endregion

    #region GetInterfaceTypes

    /// <summary>
    /// 测试 - 获取接口列表
    /// </summary>
    [Fact]
    public void Test_GetInterfaces_1()
    {
        var interfaceTypes = Reflections.GetInterfaceTypes<TestSample>();
        Assert.Equal(5, interfaceTypes.Count);
    }

    /// <summary>
    /// 测试 - 获取接口列表 - 一个基接口
    /// </summary>
    [Fact]
    public void Test_GetInterfaces_2()
    {
        var interfaceTypes = Reflections.GetInterfaceTypes<TestSample>(typeof(ITestSample3));
        Assert.Equal(2, interfaceTypes.Count);
        Assert.True(interfaceTypes.Exists(t => t.Name == "ITestSample"));
        Assert.True(interfaceTypes.Exists(t => t.Name == "ITestSample2"));
    }

    /// <summary>
    /// 测试 - 获取接口列表 - 两个基接口
    /// </summary>
    [Fact]
    public void Test_GetInterfaces_3()
    {
        var interfaceTypes = Reflections.GetInterfaceTypes<TestSample>(typeof(ITestSample4), typeof(ITestSample3));
        Assert.Equal(3, interfaceTypes.Count);
    }

    #endregion

    #region IsBool

    /// <summary>
    /// 测试 - 是否布尔类型
    /// </summary>
    [Fact]
    public void Test_IsBool()
    {
        Assert.True(Reflections.IsBool(_sample.BoolValue.GetType().GetTypeInfo()), "BoolValue GetType");
        Assert.True(Reflections.IsBool(_sample.GetType().GetMember("BoolValue")[0]), "BoolValue");
        Assert.True(Reflections.IsBool(_sample.GetType().GetMember("NullableBoolValue")[0]), "NullableBoolValue");
        Assert.False(Reflections.IsBool(_sample.GetType().GetMember("EnumValue")[0]), "EnumValue");
    }

    #endregion

    #region IsEnum

    /// <summary>
    /// 测试 - 是否枚举类型
    /// </summary>
    [Fact]
    public void Test_IsEnum()
    {
        Assert.True(Reflections.IsEnum(_sample.EnumValue.GetType().GetTypeInfo()), "EnumValue GetType");
        Assert.True(Reflections.IsEnum(_sample.GetType().GetMember("EnumValue")[0]), "EnumValue");
        Assert.True(Reflections.IsEnum(_sample.GetType().GetMember("NullableEnumValue")[0]), "NullableEnumValue");
        Assert.False(Reflections.IsEnum(_sample.GetType().GetMember("BoolValue")[0]), "BoolValue");
        Assert.False(Reflections.IsEnum(_sample.GetType().GetMember("NullableBoolValue")[0]), "NullableBoolValue");
    }

    #endregion

    #region IsDate

    /// <summary>
    /// 测试 - 是否日期类型
    /// </summary>
    [Fact]
    public void Test_IsDate()
    {
        Assert.True(Reflections.IsDate(_sample.DateValue.GetType().GetTypeInfo()), "DateValue GetType");
        Assert.True(Reflections.IsDate(_sample.GetType().GetMember("DateValue")[0]), "DateValue");
        Assert.True(Reflections.IsDate(_sample.GetType().GetMember("NullableDateValue")[0]), "NullableDateValue");
        Assert.False(Reflections.IsDate(_sample.GetType().GetMember("EnumValue")[0]), "EnumValue");
    }

    #endregion

    #region IsInt

    /// <summary>
    /// 测试 - 是否整型
    /// </summary>
    [Fact]
    public void Test_IsInt()
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

    #endregion

    #region IsNumber

    /// <summary>
    /// 测试 - 是否数值类型
    /// </summary>
    [Fact]
    public void Test_IsNumber()
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

    #endregion

    #region IsCollection

    /// <summary>
    /// 测试 - 是否集合
    /// </summary>
    [Fact]
    public void Test_IsCollection()
    {
        Assert.True(Reflections.IsCollection(_sample.StringArray.GetType()));
        Assert.True(TypeReflections.IsCollection(_sample.StringArray.GetType()));
    }

    #endregion

    #region IsGenericCollection

    /// <summary>
    /// 测试 - 是否泛型集合
    /// </summary>
    [Fact]
    public void Test_IsGenericCollection()
    {
        Assert.True(Reflections.IsGenericCollection(_sample.StringList.GetType()));
    }

    #endregion

    #region GetPublicProperties

    /// <summary>
    /// 测试 - 获取公共属性列表
    /// </summary>
    [Fact]
    public void Test_GetPublicProperties()
    {
        ReflectionsSample sample = new ReflectionsSample
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

    #endregion

    #region GetTopBaseType

    /// <summary>
    /// 测试 - 获取顶级基类
    /// </summary>
    [Fact]
    public void Test_GetTopBaseType()
    {
        Assert.Null(Reflections.GetTopBaseType(null));
    }

    #endregion

    #region GetElementType

    /// <summary>
    /// 测试 - 获取元素类型
    /// </summary>
    [Fact]
    public void Test_GetElementType_1()
    {
        Sample sample = new Sample();
        Assert.Equal(typeof(Sample), Reflections.GetElementType(sample.GetType()));
    }

    /// <summary>
    /// 测试 - 获取元素类型 - 数组
    /// </summary>
    [Fact]
    public void Test_GetElementType_2()
    {
        var list = new[] { new Sample() };
        var type = list.GetType();
        Assert.Equal(typeof(Sample), Reflections.GetElementType(type));
    }

    /// <summary>
    /// 测试 - 获取元素类型 - 集合
    /// </summary>
    [Fact]
    public void Test_GetElementType_3()
    {
        var list = new List<Sample> { new() };
        var type = list.GetType();
        Assert.Equal(typeof(Sample), Reflections.GetElementType(type));
    }

    #endregion

    #region Get/Set PropertyValueByPath

    /// <summary>
    /// 测试 - 通过指定对象的属性路径获取属性值
    /// </summary>
    [Fact]
    public void Test_GetPropertyValueByPath()
    {
        var value = new GetPropertyValueByPathTestClass
        {
            Name = "test",
            Count = 8,
            Time = DateTime.Parse("2023-01-01"),
            Children = new GetPropertyValueByPathTestChildrenClass
            {
                Name = "test-children",
                Count = 9
            }
        };

        Reflections.GetPropertyValueByPath(value, value.GetType(), "Name").ShouldBe("test");
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Count").ShouldBe(8);
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Time").ShouldBe(DateTime.Parse("2023-01-01"));
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Bing.Reflection.ReflectionsTest+GetPropertyValueByPathTestClass.Name").ShouldBe("test");
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Children.Name").ShouldBe("test-children");
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Children.Count").ShouldBe(9);
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Bing.Reflection.ReflectionsTest+GetPropertyValueByPathTestClass.Children.Name").ShouldBe("test-children");

        Reflections.GetPropertyValueByPath(value, value.GetType(), "Children.NotExists").ShouldBeNull();
        Reflections.GetPropertyValueByPath(value, value.GetType(), "NotExists").ShouldBeNull();
    }

    /// <summary>
    /// 测试 - 通过指定对象的属性路径设置属性值
    /// </summary>
    [Fact]
    public void Test_SetPropertyValueByPath()
    {
        var value = new GetPropertyValueByPathTestClass
        {
            Name = "test",
            Count = 8,
            Time = DateTime.Parse("2023-01-01"),
            Children = new GetPropertyValueByPathTestChildrenClass
            {
                Name = "test-children",
                Count = 9
            }
        };

        Reflections.SetPropertyValueByPath(value, value.GetType(), "Name", "test-set");
        Reflections.SetPropertyValueByPath(value, value.GetType(), "Count", 80);
        Reflections.SetPropertyValueByPath(value, value.GetType(), "Time", DateTime.Parse("2022-01-01"));
        Reflections.SetPropertyValueByPath(value, value.GetType(), "Children.Name", "test-children-set");
        Reflections.SetPropertyValueByPath(value, value.GetType(), "Children.Count", 90);

        Reflections.GetPropertyValueByPath(value, value.GetType(), "Name").ShouldBe("test-set");
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Count").ShouldBe(80);
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Time").ShouldBe(DateTime.Parse("2022-01-01"));
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Children.Name").ShouldBe("test-children-set");
        Reflections.GetPropertyValueByPath(value, value.GetType(), "Children.Count").ShouldBe(90);
    }

    class GetPropertyValueByPathTestClass
    {
        public string Name { get; set; }

        public int Count { get; set; }

        public DateTime Time { get; set; }

        public GetPropertyValueByPathTestChildrenClass Children { get; set; }
    }

    class GetPropertyValueByPathTestChildrenClass
    {
        public string Name { get; set; }

        public int Count { get; set; }
    }

    #endregion

    #region GetPublicConstantsRecursively

    /// <summary>
    /// 测试 - 获取指定类型的所有公共常量值
    /// </summary>
    [Fact]
    public void Test_GetPublicConstantsRecursively()
    {
        var constants = Reflections.GetPublicConstantsRecursively(typeof(BaseRole));

        constants.ShouldNotBeEmpty();
        constants.Length.ShouldBe(1);
        constants.ShouldContain(x => x == "DefaultBaseRoleName");
    }

    /// <summary>
    /// 测试 - 获取指定类型的所有公共常量值 - 继承
    /// </summary>
    [Fact]
    public void Test_GetPublicConstantsRecursively_Inherit()
    {
        var constants = Reflections.GetPublicConstantsRecursively(typeof(Roles));

        constants.ShouldNotBeEmpty();
        constants.Length.ShouldBe(2);
        constants.ShouldContain(x => x == "DefaultBaseRoleName");
        constants.ShouldContain(x => x == "DefaultRoleName");
    }

    /// <summary>
    /// 测试 - 获取指定类型的所有公共常量值 - 嵌套类型
    /// </summary>
    [Fact]
    public void Test_GetPublicConstantsRecursively_NestedTypes()
    {
        var constants = Reflections.GetPublicConstantsRecursively(typeof(IdentityPermissions));

        constants.ShouldNotBeEmpty();
        constants.Except(IdentityPermissions.GetAll()).Count().ShouldBe(0);
    }

    #endregion

    #region GetBaseClasses

    /// <summary>
    /// 测试 - 获取指定类型的所有基类型 - 排除object对象
    /// </summary>
    [Fact]
    public void Test_GetBaseClasses_Excluding_Object()
    {
        var baseClasses = Reflections.GetBaseClasses(typeof(MyClass), false);
        baseClasses.Length.ShouldBe(2);
        baseClasses[0].ShouldBe(typeof(MyBaseClass1));
        baseClasses[1].ShouldBe(typeof(MyBaseClass2));
    }

    /// <summary>
    /// 测试 - 获取指定类型的所有基类型 - 指定停止类型
    /// </summary>
    [Fact]
    public void Test_GetBaseClasses_With_StoppingType()
    {
        var baseClasses = Reflections.GetBaseClasses(typeof(MyClass), typeof(MyBaseClass1));
        baseClasses.Length.ShouldBe(1);
        baseClasses[0].ShouldBe(typeof(MyBaseClass2));
    }

    public abstract class MyBaseClass1
    {
    }

    public class MyBaseClass2 : MyBaseClass1
    {
    }

    public class MyClass : MyBaseClass2
    {
    }

    #endregion

}

public class BaseRole
{
    public const string BaseRoleName = "DefaultBaseRoleName";
}

public class Roles : BaseRole
{
    public const string RoleName = "DefaultRoleName";
}

public static class IdentityPermissions
{
    public const string GroupName = "AbpIdentity";

    public static class Roles
    {
        public const string Default = GroupName + ".Roles";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManagePermissions = Default + ".ManagePermissions";
    }

    public static class Users
    {
        public const string Default = GroupName + ".Users";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManagePermissions = Default + ".ManagePermissions";
    }

    public static class UserLookup
    {
        public const string Default = GroupName + ".UserLookup";
    }

    public static string[] GetAll()
    {
        return
        [
            GroupName,
            Roles.Default,
            Roles.Create,
            Roles.Update,
            Roles.Delete,
            Roles.ManagePermissions,
            Users.Default,
            Users.Create,
            Users.Update,
            Users.Delete,
            Users.ManagePermissions,
            UserLookup.Default
        ];
    }
}
