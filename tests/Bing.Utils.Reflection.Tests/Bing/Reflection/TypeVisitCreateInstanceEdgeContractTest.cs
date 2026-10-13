namespace Bing.Reflection;

/// <summary>
/// 测试类：验证 TypeVisit.CreateInstance 在边界与类型匹配场景下的行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.CreateInstance.EdgeContract")]
public class TypeVisitCreateInstanceEdgeContractTest
{
    /// <summary>
    /// 测试用例：当显式传入 null 参数数组时，应走无参构造并成功创建实例。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_NullArgsArray_UsesParameterlessConstructor()
    {
        var instance = TypeVisit.CreateInstance(typeof(ParameterlessSample), (object[])null);

        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<ParameterlessSample>();
    }

    /// <summary>
    /// 测试用例：泛型重载在 null 参数数组场景下，应成功创建强类型实例。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_NullArgsArray_ReturnsTypedInstance()
    {
        var instance = TypeVisit.CreateInstance<ParameterlessSample>((object[])null);

        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<ParameterlessSample>();
    }

    /// <summary>
    /// 测试用例：构造函数参数为可赋值类型时，应成功创建实例。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_AssignableCtorSignature_ReturnsCreatedObject()
    {
        var instance = TypeVisit.CreateInstance(typeof(ObjectCtorSample), "payload") as ObjectCtorSample;

        instance.ShouldNotBeNull();
        instance.Payload.ShouldBe("payload");
    }

    /// <summary>
    /// 测试用例：构造函数参数与实际参数类型不兼容时，应抛出 ArgumentException。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_IncompatibleNumericArgument_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance(typeof(LongCtorSample), 1));
    }

    /// <summary>
    /// 测试用例：构造函数参数类型精确匹配时，应成功创建实例。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_ExactNumericArgument_ReturnsCreatedObject()
    {
        var instance = TypeVisit.CreateInstance(typeof(LongCtorSample), 1L) as LongCtorSample;

        instance.ShouldNotBeNull();
        instance.Value.ShouldBe(1L);
    }

    /// <summary>
    /// 测试用例：参数数组包含 null 时，严格绑定器应选择可接受 null 的构造函数。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_NullElementInArgsArray_CreatesInstance()
    {
        var instance = TypeVisit.CreateInstance(typeof(ObjectCtorSample), new object[] { null });

        instance.ShouldBeOfType<ObjectCtorSample>().Payload.ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：泛型+Type 重载在目标类型不兼容时，应抛出 ArgumentException。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericWithTypeOverload_IncompatibleTargetType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance<int>(typeof(ParameterlessSample)));
    }

    /// <summary>
    /// 测试用例：仅存在私有无参构造时，应抛出明确参数异常。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_PrivateParameterlessConstructor_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance(typeof(PrivateCtorSample)));
    }

    /// <summary>
    /// 测试用例：泛型重载在没有可用构造函数时，应抛出明确参数异常。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_NoAvailableConstructor_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance<OnlyParameterizedSample>());
    }

    /// <summary>
    /// 测试用例：当目标类型为抽象类时，应抛出明确参数异常。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_AbstractType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance(typeof(AbstractSample)));
    }

    /// <summary>
    /// 测试用例：当目标类型为接口时，泛型重载应抛出明确参数异常。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_InterfaceType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance<IInterfaceSample>());
    }

    /// <summary>
    /// 测试用例：当目标类型为值类型且没有显式公共无参构造时，泛型重载应抛出明确参数异常。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_ValueTypeWithoutPublicCtor_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance<int>());
    }

    /// <summary>
    /// 测试用例：非泛型重载在接口类型场景下，应抛出明确参数异常。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_InterfaceType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance(typeof(IInterfaceSample)));
    }

    /// <summary>
    /// 测试用例：非泛型重载在值类型没有显式公共无参构造场景下，应抛出明确参数异常。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_ValueTypeWithoutCtor_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance(typeof(int)));
    }

    /// <summary>
    /// 测试用例：泛型+Type 重载在值类型没有显式公共无参构造场景下，应抛出明确参数异常。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericWithTypeOverload_ValueTypeWithoutCtor_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance<int>(typeof(int)));
    }

    /// <summary>
    /// 测试用例：构造参数数组应保持只读契约，不应被 CreateInstance 修改。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_ArgsArray_DoesNotMutateInput()
    {
        object[] args = { "payload" };

        var instance = TypeVisit.CreateInstance(typeof(ObjectCtorSample), args);

        instance.ShouldNotBeNull();
        args[0].ShouldBe("payload");
    }

    private sealed class ParameterlessSample;

    private sealed class ObjectCtorSample
    {
        public ObjectCtorSample(object payload) => Payload = payload;

        public object Payload { get; }
    }

    private sealed class LongCtorSample
    {
        public LongCtorSample(long value) => Value = value;

        public long Value { get; }
    }

    private sealed class PrivateCtorSample
    {
        private PrivateCtorSample()
        {
        }
    }

    private sealed class OnlyParameterizedSample
    {
        public OnlyParameterizedSample(string value) => Value = value;

        public string Value { get; }
    }

    private abstract class AbstractSample
    {
    }

    private interface IInterfaceSample
    {
    }
}
