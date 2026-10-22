using Bing.Tests.Samples;
namespace Bing.Reflection;
/// <summary>
/// 验证反射描述、接口发现、成员访问及继承关系查询。
/// </summary>
public class ReflectionsTest : TestBase
{
    /// <summary>
    /// 类型判定测试共用的样例对象。
    /// </summary>
    private readonly Sample _sample;
    /// <summary>
    /// 初始化一个 <see cref="ReflectionsTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">测试输出助手。</param>
    public ReflectionsTest(ITestOutputHelper output) : base(output)
    {
        _sample = new Sample();
    }
    #region GetDescription
    /// <summary>
    /// 验证成员描述读取优先采用描述特性并对缺失成员返回空字符串。
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
    /// 验证类型描述在未声明描述特性时回退到类型名称。
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
    /// 验证显示名称读取使用显示名称特性并在缺失时返回空字符串。
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
    /// 验证显示名称与描述的组合读取结果。
    /// </summary>
    [Fact]
    public void Test_GetDescriptionOrDisplayName()
    {
        Assert.Equal("测试样例", Reflections.GetDisplayNameOrDescription<Sample>());
        Assert.Equal("测试样例2", Reflections.GetDisplayNameOrDescription<Sample2>());
        Assert.Equal("测试样例", Reflections.GetDisplayNameOrDescription<Sample>());
    }
    #endregion
    #region GetAssemblies
    /// <summary>
    /// 测试 - 按绝对路径加载目录中的程序集，并支持大小写不同的扩展名
    /// </summary>
    [Fact]
    public void Test_GetAssemblies_LoadsAssemblyFromPath()
    {
        var sourcePath = typeof(Reflections).Assembly.Location;
        Assert.False(string.IsNullOrWhiteSpace(sourcePath));

        var directoryPath = Path.Combine(Path.GetTempPath(), $"bing-utils-reflections-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        try
        {
            var targetPath = Path.Combine(directoryPath, "ReflectionCopy.DLL");
            File.Copy(sourcePath, targetPath);

            var assemblies = Reflections.GetAssemblies(directoryPath);

            Assert.Contains(assemblies, assembly =>
                assembly.GetName().Name == typeof(Reflections).Assembly.GetName().Name);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Test_GetAssemblies_EmptyDirectoryPath_ThrowsArgumentException(string directoryPath)
    {
        Assert.Throws<ArgumentException>(() => Reflections.GetAssemblies(directoryPath));
    }

    [Fact]
    public void Test_GetAssemblies_MissingDirectory_ThrowsDirectoryNotFoundException()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"bing-utils-reflections-missing-{Guid.NewGuid():N}");

        Assert.Throws<DirectoryNotFoundException>(() => Reflections.GetAssemblies(directoryPath));
    }
    #endregion
    #region GetAssembly
    /// <summary>
    /// 测试 - 程序集名称输入校验应返回稳定的参数异常。
    /// </summary>
    [Fact]
    public void Test_GetAssembly_NullName_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Reflections.GetAssembly(null!));

        Assert.Equal("assemblyName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("BadAssembly, Version=invalid")]
    public void Test_GetAssembly_InvalidName_ThrowsArgumentException(string assemblyName)
    {
        var exception = Assert.Throws<ArgumentException>(() => Reflections.GetAssembly(assemblyName));

        Assert.Equal("assemblyName", exception.ParamName);
    }

    /// <summary>
    /// 测试 - 有效程序集名称应解析到已加载程序集。
    /// </summary>
    [Fact]
    public void Test_GetAssembly_ValidName_LoadsAssembly()
    {
        var assemblyName = typeof(Reflections).Assembly.GetName().Name;

        var assembly = Reflections.GetAssembly(assemblyName);

        Assert.Equal(typeof(Reflections).Assembly.GetName().Name, assembly.GetName().Name);
    }
    #endregion
    #region FindImplementTypes
    /// <summary>
    /// 验证查找仅有一个实现类的接口返回该实现类型。
    /// </summary>
    [Fact]
    public void Test_FindImplementTypes_1()
    {
        var types = Reflections.FindImplementTypes<IA>(this.GetType().Assembly);
        Assert.Single(types);
        Assert.Equal(typeof(A), types[0]);
    }
    /// <summary>
    /// 验证接口实现类型查询排除抽象类与接口。
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
    /// 验证接口实现类型查询保留泛型类型定义。
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
    /// 验证闭合泛型接口查询匹配对应实现类型。
    /// </summary>
    [Fact]
    public void Test_FindImplementTypes_4()
    {
        var types = Reflections.FindImplementTypes<IG<E>>(this.GetType().Assembly);
        Assert.Single(types);
        Assert.Equal(typeof(E), types[0]);
    }
    /// <summary>
    /// 验证开放泛型接口查询包含普通实现与泛型实现。
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
    /// 验证并发接口实现类型查询返回各自预期结果。
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
    /// 验证直接接口查询返回未被其他接口继承的接口。
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
    /// 验证直接接口查询按单个基接口筛选。
    /// </summary>
    [Fact]
    public void Test_GetDirectInterfaces_2()
    {
        var interfaceTypes = Reflections.GetDirectInterfaceTypes<TestSample>(typeof(ITestSample3));
        Assert.Single(interfaceTypes);
        Assert.True(interfaceTypes.Exists(t => t.Name == "ITestSample"));
    }
    /// <summary>
    /// 验证直接接口查询按多个基接口筛选。
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
    /// 验证接口查询包含全部继承接口。
    /// </summary>
    [Fact]
    public void Test_GetInterfaces_1()
    {
        var interfaceTypes = Reflections.GetInterfaceTypes<TestSample>();
        Assert.Equal(5, interfaceTypes.Count);
    }
    /// <summary>
    /// 验证接口查询按单个基接口筛选并保留其派生接口。
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
    /// 验证接口查询按多个基接口筛选。
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
    /// 验证布尔类型及可空布尔成员被识别而枚举成员被排除。
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
    /// 验证枚举及可空枚举成员被识别而布尔成员被排除。
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
    /// 验证日期及可空日期成员被识别而枚举成员被排除。
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
    /// 验证短整数、整数和长整数及其可空成员均被识别。
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
    /// 验证整数与十进制数及其可空成员均被识别为数值。
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
    /// 验证数组类型满足集合判定。
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
    /// 验证字符串列表满足泛型集合判定。
    /// </summary>
    [Fact]
    public void Test_IsGenericCollection()
    {
        Assert.True(Reflections.IsGenericCollection(_sample.StringList.GetType()));
    }
    #endregion
    #region GetPublicProperties
    /// <summary>
    /// 验证公共属性枚举返回对应名称与值。
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
    /// 验证空类型的顶级基类查询返回空。
    /// </summary>
    [Fact]
    public void Test_GetTopBaseType()
    {
        Assert.Throws<ArgumentNullException>(() => Reflections.GetTopBaseType(null));
    }
    #endregion
    #region GetElementType
    /// <summary>
    /// 验证普通类型的元素类型查询返回自身。
    /// </summary>
    [Fact]
    public void Test_GetElementType_1()
    {
        Sample sample = new Sample();
        Assert.Equal(typeof(Sample), Reflections.GetElementType(sample.GetType()));
    }
    /// <summary>
    /// 验证数组的元素类型查询返回数组元素类型。
    /// </summary>
    [Fact]
    public void Test_GetElementType_2()
    {
        var list = new[] { new Sample() };
        var type = list.GetType();
        Assert.Equal(typeof(Sample), Reflections.GetElementType(type));
    }
    /// <summary>
    /// 验证泛型列表的元素类型查询返回集合元素类型。
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
    /// 验证普通、嵌套及带类型前缀的属性路径读取。
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
        Should.Throw<InvalidOperationException>(() => Reflections.GetPropertyValueByPath(value, value.GetType(), "Children.NotExists"));
        Should.Throw<InvalidOperationException>(() => Reflections.GetPropertyValueByPath(value, value.GetType(), "NotExists"));
    }
    /// <summary>
    /// 验证普通及嵌套属性路径写入后可读取更新值。
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
    /// <summary>
    /// 提供属性路径读写测试的根对象。
    /// </summary>
    class GetPropertyValueByPathTestClass
    {
        /// <summary>
        /// 获取或设置属性路径中的测试名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置属性路径中的测试计数。
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 获取或设置属性路径中的测试时间。
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// 获取或设置属性路径中的子对象。
        /// </summary>
        public GetPropertyValueByPathTestChildrenClass Children { get; set; }
    }
    /// <summary>
    /// 提供属性路径读写测试的子对象。
    /// </summary>
    class GetPropertyValueByPathTestChildrenClass
    {
        /// <summary>
        /// 获取或设置属性路径中的测试名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置属性路径中的测试计数。
        /// </summary>
        public int Count { get; set; }
    }
    #endregion
    #region GetPublicConstantsRecursively
    /// <summary>
    /// 验证公共常量枚举返回类型自身的常量值。
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
    /// 验证公共常量枚举包含继承的常量值。
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
    /// 验证公共常量枚举递归发现嵌套权限类型中的常量值。
    /// </summary>
    [Fact]
    public void Test_GetPublicConstantsRecursively_NestedTypes()
    {
        var constants = Reflections.GetPublicConstantsRecursively(typeof(IdentityPermissions));
        constants.ShouldNotBeEmpty();
        constants.Except(IdentityPermissions.GetAll()).Count().ShouldBe(0);
    }

    [Fact]
    public void Test_GetPublicConstantsRecursively_NullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Reflections.GetPublicConstantsRecursively(null));
    }

    [Fact]
    public void Test_GetPublicConstantsRecursively_NullStringConstant_IsSkipped()
    {
        var constants = Reflections.GetPublicConstantsRecursively(typeof(NullConstants));

        constants.ShouldBe(new[] { "Present" });
    }
    #endregion
    #region GetBaseClasses
    /// <summary>
    /// 验证基类枚举排除对象根类型并保留继承顺序。
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
    /// 验证基类枚举在指定停止类型之前截断。
    /// </summary>
    [Fact]
    public void Test_GetBaseClasses_With_StoppingType()
    {
        var baseClasses = Reflections.GetBaseClasses(typeof(MyClass), typeof(MyBaseClass1));
        baseClasses.Length.ShouldBe(1);
        baseClasses[0].ShouldBe(typeof(MyBaseClass2));
    }
    /// <summary>
    /// 用于验证基类型枚举停止位置的抽象基类。
    /// </summary>
    public abstract class MyBaseClass1
    {
    }
    /// <summary>
    /// 提供位于继承链中间的基类样例。
    /// </summary>
    public class MyBaseClass2 : MyBaseClass1
    {
    }
    /// <summary>
    /// 提供用于枚举基类链的具体类型。
    /// </summary>
    public class MyClass : MyBaseClass2
    {
    }
    #endregion
}
/// <summary>
/// 提供含公共角色常量的基类样例。
/// </summary>
public class BaseRole
{
    /// <summary>
    /// 用于验证基类常量发现的默认角色名称。
    /// </summary>
    public const string BaseRoleName = "DefaultBaseRoleName";
}
/// <summary>
/// 提供用于验证 null 字符串常量处理的测试样例。
/// </summary>
public static class NullConstants
{
    public const string Missing = null;
    public const string Present = "Present";
}
/// <summary>
/// 提供继承角色常量并声明自身常量的测试类型。
/// </summary>
public class Roles : BaseRole
{
    /// <summary>
    /// 用于验证派生类常量发现的默认角色名称。
    /// </summary>
    public const string RoleName = "DefaultRoleName";
}
/// <summary>
/// 提供用于验证递归常量枚举的身份权限分组。
/// </summary>
public static class IdentityPermissions
{
    /// <summary>
    /// 身份权限测试分组的名称前缀。
    /// </summary>
    public const string GroupName = "AbpIdentity";
    /// <summary>
    /// 提供角色权限测试常量分组。
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// 角色权限测试分组的默认键。
        /// </summary>
        public const string Default = GroupName + ".Roles";
        /// <summary>
        /// 用于验证嵌套常量枚举的角色创建权限键。
        /// </summary>
        public const string Create = Default + ".Create";
        /// <summary>
        /// 用于验证嵌套常量枚举的角色更新权限键。
        /// </summary>
        public const string Update = Default + ".Update";
        /// <summary>
        /// 用于验证嵌套常量枚举的角色删除权限键。
        /// </summary>
        public const string Delete = Default + ".Delete";
        /// <summary>
        /// 用于验证嵌套常量枚举的角色权限管理键。
        /// </summary>
        public const string ManagePermissions = Default + ".ManagePermissions";
    }
    /// <summary>
    /// 提供用户权限测试常量分组。
    /// </summary>
    public static class Users
    {
        /// <summary>
        /// 用户权限测试分组的默认键。
        /// </summary>
        public const string Default = GroupName + ".Users";
        /// <summary>
        /// 用于验证嵌套常量枚举的用户创建权限键。
        /// </summary>
        public const string Create = Default + ".Create";
        /// <summary>
        /// 用于验证嵌套常量枚举的用户更新权限键。
        /// </summary>
        public const string Update = Default + ".Update";
        /// <summary>
        /// 用于验证嵌套常量枚举的用户删除权限键。
        /// </summary>
        public const string Delete = Default + ".Delete";
        /// <summary>
        /// 用于验证嵌套常量枚举的用户权限管理键。
        /// </summary>
        public const string ManagePermissions = Default + ".ManagePermissions";
    }
    /// <summary>
    /// 提供用户查询权限测试常量分组。
    /// </summary>
    public static class UserLookup
    {
        /// <summary>
        /// 用户查询权限测试分组的默认键。
        /// </summary>
        public const string Default = GroupName + ".UserLookup";
    }
    /// <summary>
    /// 列出身份权限分组及其嵌套权限常量。
    /// </summary>
    /// <returns>按测试约定顺序排列的全部权限键。</returns>
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

