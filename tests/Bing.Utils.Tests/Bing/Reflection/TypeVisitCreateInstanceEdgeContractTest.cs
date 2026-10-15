namespace Bing.Reflection;

/// <summary>
/// 验证实例创建的构造函数匹配及边界行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.CreateInstance.EdgeContract")]
public class TypeVisitCreateInstanceEdgeContractTest
{
    /// <summary>
    /// 验证当显式传入 null 参数数组时，应走无参构造并成功创建实例。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_NullArgsArray_UsesParameterlessConstructor()
    {
        var instance = TypeVisit.CreateInstance(typeof(ParameterlessSample), (object[])null);

        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<ParameterlessSample>();
    }

    /// <summary>
    /// 验证泛型重载在 null 参数数组场景下，应成功创建强类型实例。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_NullArgsArray_ReturnsTypedInstance()
    {
        var instance = TypeVisit.CreateInstance<ParameterlessSample>((object[])null);

        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<ParameterlessSample>();
    }

    /// <summary>
    /// 验证构造函数参数为可赋值类型时，应成功创建实例。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_AssignableCtorSignature_ReturnsCreatedObject()
    {
        var instance = TypeVisit.CreateInstance(typeof(ObjectCtorSample), "payload") as ObjectCtorSample;

        instance.ShouldNotBeNull();
        instance.Payload.ShouldBe("payload");
    }

    /// <summary>
    /// 验证构造函数参数与实际参数类型不兼容时，应抛出 InvalidCastException。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_IncompatibleNumericArgument_ThrowsInvalidCastException()
    {
        Should.Throw<InvalidCastException>(() => TypeVisit.CreateInstance(typeof(LongCtorSample), 1));
    }

    /// <summary>
    /// 验证构造函数参数类型精确匹配时，应成功创建实例。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_ExactNumericArgument_ReturnsCreatedObject()
    {
        var instance = TypeVisit.CreateInstance(typeof(LongCtorSample), 1L) as LongCtorSample;

        instance.ShouldNotBeNull();
        instance.Value.ShouldBe(1L);
    }

    /// <summary>
    /// 验证参数数组包含 null 且无法匹配构造函数时，当前行为返回 null。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_NullElementInArgsArray_CurrentlyReturnsNull()
    {
        var instance = TypeVisit.CreateInstance(typeof(ObjectCtorSample), new object[] { null });

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 验证泛型+Type 重载在目标类型不兼容时，应返回默认值。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericWithTypeOverload_IncompatibleTargetType_ReturnsDefault()
    {
        var instance = TypeVisit.CreateInstance<int>(typeof(ParameterlessSample));

        instance.ShouldBe(0);
    }

    /// <summary>
    /// 验证仅存在私有无参构造时，当前行为返回 null。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_PrivateParameterlessConstructor_CurrentlyReturnsNull()
    {
        var instance = TypeVisit.CreateInstance(typeof(PrivateCtorSample));

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 验证泛型重载在没有可用构造函数时，应返回引用类型默认值 null。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_NoAvailableConstructor_ReturnsNull()
    {
        var instance = TypeVisit.CreateInstance<OnlyParameterizedSample>();

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 验证当目标类型为抽象类时，当前行为应返回 null。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_AbstractType_CurrentlyReturnsNull()
    {
        var instance = TypeVisit.CreateInstance(typeof(AbstractSample));

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 验证当目标类型为接口时，泛型重载应返回引用类型默认值 null。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_InterfaceType_ReturnsDefaultNull()
    {
        var instance = TypeVisit.CreateInstance<IInterfaceSample>();

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 验证当目标类型为值类型且无可见无参构造时，泛型重载应返回值类型默认值。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_ValueTypeWithoutPublicCtor_ReturnsDefaultValue()
    {
        var instance = TypeVisit.CreateInstance<int>();

        instance.ShouldBe(0);
    }

    /// <summary>
    /// 验证非泛型重载在接口类型场景下，当前行为应返回 null。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_InterfaceType_CurrentlyReturnsNull()
    {
        var instance = TypeVisit.CreateInstance(typeof(IInterfaceSample));

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 验证非泛型重载在值类型无参创建场景下，当前行为返回 null（未走默认值补偿）。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_ValueTypeWithoutCtor_CurrentlyReturnsNull()
    {
        var instance = TypeVisit.CreateInstance(typeof(int));

        instance.ShouldBeNull();
    }

    /// <summary>
    /// 验证泛型+Type 重载在值类型无参创建场景下，应返回值类型默认值。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericWithTypeOverload_ValueTypeWithoutCtor_ReturnsDefaultValue()
    {
        var instance = TypeVisit.CreateInstance<int>(typeof(int));

        instance.ShouldBe(0);
    }

    /// <summary>
    /// 验证构造参数数组应保持只读契约，不应被 CreateInstance 修改。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_ArgsArray_DoesNotMutateInput()
    {
        object[] args = { "payload" };

        var instance = TypeVisit.CreateInstance(typeof(ObjectCtorSample), args);

        instance.ShouldNotBeNull();
        args[0].ShouldBe("payload");
    }

    /// <summary>
    /// 提供隐式公开无参构造函数的实例创建样例。
    /// </summary>
    private sealed class ParameterlessSample;

    /// <summary>
    /// 提供对象参数构造函数以验证可赋值类型匹配。
    /// </summary>
    private sealed class ObjectCtorSample
    {
        /// <summary>
        /// 初始化一个 <see cref="ObjectCtorSample"/> 类型的实例。
        /// </summary>
        /// <param name="payload">保存到只读属性的测试数据。</param>
        public ObjectCtorSample(object payload) => Payload = payload;

        /// <summary>
        /// 获取构造函数传入的测试数据。
        /// </summary>
        public object Payload { get; }
    }

    /// <summary>
    /// 提供长整数参数构造函数以验证数值类型精确匹配。
    /// </summary>
    private sealed class LongCtorSample
    {
        /// <summary>
        /// 初始化一个 <see cref="LongCtorSample"/> 类型的实例。
        /// </summary>
        /// <param name="value">保存到只读属性的长整数测试值。</param>
        public LongCtorSample(long value) => Value = value;

        /// <summary>
        /// 获取构造函数传入的长整数测试值。
        /// </summary>
        public long Value { get; }
    }

    /// <summary>
    /// 提供仅含私有构造函数的实例创建样例。
    /// </summary>
    private sealed class PrivateCtorSample
    {
        /// <summary>
        /// 初始化一个 <see cref="PrivateCtorSample"/> 类型的实例。
        /// </summary>
        private PrivateCtorSample()
        {
        }
    }

    /// <summary>
    /// 提供仅含字符串参数构造函数的实例创建样例。
    /// </summary>
    private sealed class OnlyParameterizedSample
    {
        /// <summary>
        /// 初始化一个 <see cref="OnlyParameterizedSample"/> 类型的实例。
        /// </summary>
        /// <param name="value">保存到只读属性的测试文本。</param>
        public OnlyParameterizedSample(string value) => Value = value;

        /// <summary>
        /// 获取构造函数传入的测试文本。
        /// </summary>
        public string Value { get; }
    }

    /// <summary>
    /// 用于验证抽象类型不能直接创建实例的样例。
    /// </summary>
    private abstract class AbstractSample
    {
    }

    /// <summary>
    /// 用于验证接口不能直接创建实例的契约。
    /// </summary>
    private interface IInterfaceSample
    {
    }
}
