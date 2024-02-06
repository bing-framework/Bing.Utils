using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "InterfaceDefined")]
public class InterfaceDefinedTest
{
    public Type ModelType = typeof(NormalInterfaceClass<string>);
    public Type InterfaceOne = typeof(IModelOne);
    public Type InterfaceTwo = typeof(IModelTwo);
    public Type InterfaceThree = typeof(IModelThree<string>);
    public Type InterfaceFour = typeof(IModelFour);
    public Type NotInterface = typeof(int);

    /// <summary>
    /// 测试 - 基于类型的接口定义
    /// </summary>
    [Fact]
    public void Test_DirectType_InterfaceDefined()
    {
        TypeReflections.IsInterfaceDefined(ModelType, InterfaceOne).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined(ModelType, InterfaceTwo).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined(ModelType, InterfaceThree).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined(ModelType, InterfaceFour).ShouldBeFalse();
        TypeReflections.IsInterfaceDefined(ModelType, NotInterface).ShouldBeFalse();

        Types.IsInterfaceDefined(ModelType, InterfaceOne).ShouldBeTrue();
        Types.IsInterfaceDefined(ModelType, InterfaceTwo).ShouldBeTrue();
        Types.IsInterfaceDefined(ModelType, InterfaceThree).ShouldBeTrue();
        Types.IsInterfaceDefined(ModelType, InterfaceFour).ShouldBeFalse();
        Types.IsInterfaceDefined(ModelType, NotInterface).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 基于泛型的接口定义
    /// </summary>
    [Fact]
    public void Test_GenericType_InterfaceDefined()
    {
        TypeReflections.IsInterfaceDefined<IModelOne>(ModelType).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined<IModelTwo>(ModelType).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined<IModelThree<string>>(ModelType).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined<IModelFour>(ModelType).ShouldBeFalse();
        TypeReflections.IsInterfaceDefined<int>(ModelType).ShouldBeFalse();

        Types.IsInterfaceDefined<IModelOne>(ModelType).ShouldBeTrue();
        Types.IsInterfaceDefined<IModelTwo>(ModelType).ShouldBeTrue();
        Types.IsInterfaceDefined<IModelThree<string>>(ModelType).ShouldBeTrue();
        Types.IsInterfaceDefined<IModelFour>(ModelType).ShouldBeFalse();
        Types.IsInterfaceDefined<int>(ModelType).ShouldBeFalse();

        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelOne>().ShouldBeTrue();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelTwo>().ShouldBeTrue();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelThree<string>>().ShouldBeTrue();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelFour>().ShouldBeFalse();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, int>().ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 基于类型的接口定义 - 忽略泛型参数
    /// </summary>
    [Fact]
    public void Test_DirectType_InterfaceDefined_IgnoreGenericArgs()
    {
        TypeReflections.IsInterfaceDefined(ModelType, InterfaceThree).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined(ModelType, typeof(IModelThree<>)).ShouldBeFalse();
        TypeReflections.IsInterfaceDefined(ModelType, typeof(IModelThree<>), InterfaceOptions.IgnoreGenericArgs).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined(ModelType, typeof(IModelThree<int>)).ShouldBeFalse();
        TypeReflections.IsInterfaceDefined(ModelType, typeof(IModelThree<int>), InterfaceOptions.IgnoreGenericArgs).ShouldBeTrue();

        Types.IsInterfaceDefined(ModelType, InterfaceThree).ShouldBeTrue();
        Types.IsInterfaceDefined(ModelType, typeof(IModelThree<>)).ShouldBeFalse();
        Types.IsInterfaceDefined(ModelType, typeof(IModelThree<>), InterfaceOptions.IgnoreGenericArgs).ShouldBeTrue();
        Types.IsInterfaceDefined(ModelType, typeof(IModelThree<int>)).ShouldBeFalse();
        Types.IsInterfaceDefined(ModelType, typeof(IModelThree<int>), InterfaceOptions.IgnoreGenericArgs).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 基于泛型的接口定义 - 忽略泛型参数
    /// </summary>
    [Fact]
    public void Test_GenericType_InterfaceDefined_IgnoreGenericArgs()
    {
        TypeReflections.IsInterfaceDefined<IModelThree<string>>(ModelType).ShouldBeTrue();
        TypeReflections.IsInterfaceDefined<IModelThree<int>>(ModelType).ShouldBeFalse();
        TypeReflections.IsInterfaceDefined<IModelThree<int>>(ModelType, InterfaceOptions.IgnoreGenericArgs).ShouldBeTrue();

        Types.IsInterfaceDefined<IModelThree<string>>(ModelType).ShouldBeTrue();
        Types.IsInterfaceDefined<IModelThree<int>>(ModelType).ShouldBeFalse();
        Types.IsInterfaceDefined<IModelThree<int>>(ModelType, InterfaceOptions.IgnoreGenericArgs).ShouldBeTrue();

        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelThree<string>>().ShouldBeTrue();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelThree<int>>().ShouldBeFalse();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelThree<int>>(InterfaceOptions.IgnoreGenericArgs).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 基于对象的接口定义
    /// </summary>
    [Fact]
    public void Test_Object_InterfaceDefined()
    {
        var model = new NormalInterfaceClass<string>();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelOne>(model).ShouldBeTrue();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelTwo>(model).ShouldBeTrue();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelThree<string>>(model).ShouldBeTrue();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelThree<int>>(model).ShouldBeFalse();
        Types.IsInterfaceDefined<NormalInterfaceClass<string>, IModelThree<int>>(model, InterfaceOptions.IgnoreGenericArgs).ShouldBeTrue();
    }
}