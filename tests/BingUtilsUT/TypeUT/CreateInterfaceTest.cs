using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeVisit.CreateInstance")]
public class CreateInterfaceTest
{
    /// <summary>
    /// 测试 - 类型 - 创建实例 - 无参
    /// </summary>
    [Fact]
    public void Test_DirectType_CreateInstance_Without_Params()
    {
        var instance = TypeVisit.CreateInstance(typeof(NormalWithAttrClass));

        instance.ShouldNotBeNull();
        instance.GetType().ShouldBe(typeof(NormalWithAttrClass));
    }

    /// <summary>
    /// 测试 - 泛型类型 - 创建实例 - 无参
    /// </summary>
    [Fact]
    public void Test_GenericType_CreateInstance_Without_Params()
    {
        var instance = TypeVisit.CreateInstance<NormalWithAttrClass>();

        instance.ShouldNotBeNull();
        instance.GetType().ShouldBe(typeof(NormalWithAttrClass));
    }

    /// <summary>
    /// 测试 - 类型 - 创建实例 - 1个参数
    /// </summary>
    [Fact]
    public void Test_DirectType_CreateInstance_With_One_Params()
    {
        var instance = TypeVisit.CreateInstance(typeof(NormalWithAttrClass), "test");

        instance.ShouldNotBeNull();
        instance.GetType().ShouldBe(typeof(NormalWithAttrClass));
        ((NormalWithAttrClass) instance).Nice.ShouldBe("test");
    }

    /// <summary>
    /// 测试 - 泛型类型 - 创建实例 - 1个参数
    /// </summary>
    [Fact]
    public void Test_GenericType_CreateInstance_With_One_Params()
    {
        var instance = TypeVisit.CreateInstance<NormalWithAttrClass>("test");

        instance.ShouldNotBeNull();
        instance.GetType().ShouldBe(typeof(NormalWithAttrClass));
        instance.Nice.ShouldBe("test");
    }

    /// <summary>
    /// 测试 - 类型 - 创建实例 - 2个参数
    /// </summary>
    [Fact]
    public void Test_DirectType_CreateInstance_With_Two_Params()
    {
        var instance = TypeVisit.CreateInstance(typeof(NormalWithAttrClass), "test", 2);

        instance.ShouldNotBeNull();
        instance.GetType().ShouldBe(typeof(NormalWithAttrClass));
        ((NormalWithAttrClass) instance).Nice.ShouldBe("test");
        ((NormalWithAttrClass) instance).Index.ShouldBe(2);
    }

    /// <summary>
    /// 测试 - 泛型类型 - 创建实例 - 2个参数
    /// </summary>
    [Fact]
    public void Test_GenericType_CreateInstance_With_Two_Params()
    {
        var instance = TypeVisit.CreateInstance<NormalWithAttrClass>("test", 2);

        instance.ShouldNotBeNull();
        instance.GetType().ShouldBe(typeof(NormalWithAttrClass));
        instance.Nice.ShouldBe("test");
        instance.Index.ShouldBe(2);
    }

    /// <summary>
    /// 测试 - 类型 - 创建实例 - 2个参数 - 错误排序
    /// </summary>
    [Fact]
    public void Test_DirectType_CreateInstance_With_Two_Params_With_WrongSort()
    {
        var instance = TypeVisit.CreateInstance(typeof(NormalWithAttrClass), 2, "test");

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 测试 - 泛型类型 - 创建实例 - 2个参数 - 错误排序
    /// </summary>
    [Fact]
    public void Test_GenericType_CreateInstance_With_Two_Params_With_WrongSort()
    {
        var instance = TypeVisit.CreateInstance<NormalWithAttrClass>(2, "test");

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 测试 - 类型 - 创建实例 - 转换成指定泛型类型
    /// </summary>
    [Fact]
    public void Test_DirectType_CreateInstance_With_DeclareGenericType()
    {
        var instance1 = TypeVisit.CreateInstance<NormalWithAttrClass>(typeof(NormalWithAttrClass));
        var instance2 = TypeVisit.CreateInstance<NormalWithAttrClass>(typeof(NormalWithAttrClass), "test");
        var instance3 = TypeVisit.CreateInstance<NormalWithAttrClass>(typeof(NormalWithAttrClass), "test", 2);
        var instance4 = TypeVisit.CreateInstance<NormalWithAttrClass>(typeof(NormalWithAttrClass), 2, "test");

        instance1.ShouldNotBeNull();
        instance2.ShouldNotBeNull();
        instance3.ShouldNotBeNull();
        instance4.ShouldBeNull();
    }
}