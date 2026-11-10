namespace Bing.Reflection;

/// <summary>
/// 验证结构体与接口属性元数据的按引用赋值契约一致。
/// </summary>
public class RefPropertySetterContractTests
{
    /// <summary>
    /// 接口与结构体属性都应复用委托，并更新原变量。
    /// </summary>
    /// <param name="interfaceMetadata">为 true 时使用接口属性元数据，否则使用结构体属性元数据。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValueSetterByRef_WithIntValue_UpdatesOriginalAndReusesDelegate(bool interfaceMetadata)
    {
        var property = GetCounterProperty<Counter>(interfaceMetadata);
        var setter = property.GetValueSetterByRef<Counter>();
        var counter = new Counter { Value = 11 };

        setter(ref counter, 9);

        counter.Value.ShouldBe(9);
        setter.ShouldBeSameAs(property.GetValueSetterByRef<Counter>());
    }

    /// <summary>
    /// 非可空属性不能将 null 静默转换为默认值。
    /// </summary>
    /// <param name="interfaceMetadata">为 true 时使用接口属性元数据，否则使用结构体属性元数据。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValueSetterByRef_WithNullForInt_RejectsWithoutMutation(bool interfaceMetadata)
    {
        var setter = GetCounterProperty<Counter>(interfaceMetadata).GetValueSetterByRef<Counter>();
        var counter = new Counter { Value = 11 };

        Should.Throw<NullReferenceException>(() => setter(ref counter, null!));

        counter.Value.ShouldBe(11);
    }

    /// <summary>
    /// object 入口仅执行属性类型转换，不进行隐式数值扩宽。
    /// </summary>
    /// <param name="interfaceMetadata">为 true 时使用接口属性元数据，否则使用结构体属性元数据。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValueSetterByRef_WithShortForInt_RejectsWithoutMutation(bool interfaceMetadata)
    {
        var setter = GetCounterProperty<Counter>(interfaceMetadata).GetValueSetterByRef<Counter>();
        var counter = new Counter { Value = 11 };

        Should.Throw<InvalidCastException>(() => setter(ref counter, (short)9));

        counter.Value.ShouldBe(11);
    }

    /// <summary>
    /// 引用类型和可空值类型属性仍支持赋值 null。
    /// </summary>
    /// <param name="interfaceMetadata">为 true 时使用接口属性元数据，否则使用结构体属性元数据。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValueSetterByRef_WithNullableAndReferenceValues_AcceptsNull(bool interfaceMetadata)
    {
        var type = interfaceMetadata ? typeof(IOptionalValues) : typeof(OptionalValues);
        var setNumber = type.GetProperty(nameof(IOptionalValues.Number))!.GetValueSetterByRef<OptionalValues>();
        var setText = type.GetProperty(nameof(IOptionalValues.Text))!.GetValueSetterByRef<OptionalValues>();
        var value = new OptionalValues();

        setNumber(ref value, 7);
        setText(ref value, "text");
        value.Number.ShouldBe(7);
        value.Text.ShouldBe("text");
        setNumber(ref value, null!);
        setText(ref value, null!);

        value.Number.ShouldBeNull();
        value.Text.ShouldBeNull();
    }

    /// <summary>
    /// 用户 setter 的原始异常与异常前已发生的字段修改都应保留。
    /// </summary>
    /// <param name="interfaceMetadata">为 true 时使用接口属性元数据，否则使用结构体属性元数据。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValueSetterByRef_WithThrowingSetter_PreservesCauseAndMutation(bool interfaceMetadata)
    {
        var setter = GetCounterProperty<ThrowingCounter>(interfaceMetadata).GetValueSetterByRef<ThrowingCounter>();
        var failure = new SetterFailureException();
        var counter = new ThrowingCounter(failure);

        var exception = Should.Throw<SetterFailureException>(() => setter(ref counter, 9));

        exception.ShouldBeSameAs(failure);
        counter.Value.ShouldBe(9);
    }

    /// <summary>
    /// 显式接口实现应通过接口属性元数据更新原结构体。
    /// </summary>
    [Fact]
    public void GetValueSetterByRef_WithExplicitInterfaceImplementation_UpdatesOriginal()
    {
        var setter = typeof(ICounter).GetProperty(nameof(ICounter.Value))!.GetValueSetterByRef<ExplicitCounter>();
        var counter = new ExplicitCounter();

        setter(ref counter, 9);

        counter.StoredValue.ShouldBe(9);
    }

    /// <summary>
    /// 接口默认 setter 通过另一属性修改结构体时，也应保留写回结果。
    /// </summary>
    [Fact]
    public void GetValueSetterByRef_WithDefaultInterfaceImplementation_UpdatesOriginal()
    {
        var setter = typeof(IDefaultCounter).GetProperty(nameof(IDefaultCounter.Value))!
            .GetValueSetterByRef<DefaultCounter>();
        var counter = new DefaultCounter();

        setter(ref counter, 9);

        counter.StoredValue.ShouldBe(9);
    }

    /// <summary>
    /// 默认接口实现也应拒绝不兼容输入，并保留原值。
    /// </summary>
    /// <param name="input">应被属性赋值拒绝的输入值。</param>
    /// <param name="exceptionType">预期抛出的异常类型。</param>
    [Theory]
    [InlineData(null, typeof(NullReferenceException))]
    [InlineData((short)9, typeof(InvalidCastException))]
    public void GetValueSetterByRef_WithDefaultInterfaceInvalidInput_RejectsWithoutMutation(
        object input, Type exceptionType)
    {
        var setter = typeof(IDefaultCounter).GetProperty(nameof(IDefaultCounter.Value))!
            .GetValueSetterByRef<DefaultCounter>();
        var counter = new DefaultCounter { StoredValue = 11 };

        var exception = Record.Exception(() => setter(ref counter, input));

        exception.ShouldNotBeNull();
        exception.GetType().ShouldBe(exceptionType);
        counter.StoredValue.ShouldBe(11);
    }

    /// <summary>
    /// 默认接口 setter 失败时保留原始异常及已执行的字段修改。
    /// </summary>
    [Fact]
    public void GetValueSetterByRef_WithThrowingDefaultInterfaceSetter_PreservesCauseAndMutation()
    {
        var setter = typeof(IThrowingDefaultCounter).GetProperty(nameof(IThrowingDefaultCounter.Value))!
            .GetValueSetterByRef<ThrowingDefaultCounter>();
        var failure = new SetterFailureException();
        var counter = new ThrowingDefaultCounter { Failure = failure };

        Should.Throw<SetterFailureException>(() => setter(ref counter, 9)).ShouldBeSameAs(failure);

        counter.StoredValue.ShouldBe(9);
    }

    /// <summary>
    /// 继承接口对抽象 setter 提供默认实现时，也应正确回写。
    /// </summary>
    [Fact]
    public void GetValueSetterByRef_WithInheritedDefaultImplementation_UpdatesOriginal()
    {
        var setter = typeof(IAbstractCounter).GetProperty(nameof(IAbstractCounter.Value))!
            .GetValueSetterByRef<InheritedDefaultCounter>();
        var counter = new InheritedDefaultCounter();

        setter(ref counter, 9);

        counter.StoredValue.ShouldBe(9);
    }

    /// <summary>
    /// 通过逆变接口属性取得的 setter 应保持兼容，并写回显式接口实现。
    /// </summary>
    [Fact]
    public void GetValueSetterByRef_WithContravariantInterface_UpdatesOriginal()
    {
        var setter = typeof(IValueSink<string>).GetProperty(nameof(IValueSink<string>.Value))!
            .GetValueSetterByRef<VariantCounter>();
        var counter = new VariantCounter();

        setter(ref counter, "text");

        counter.StoredValue.ShouldBe("text");
    }

    /// <summary>
    /// 获取结构体或接口中用于测试的计数属性。
    /// </summary>
    /// <typeparam name="T">待查询属性的结构体类型。</typeparam>
    /// <param name="interfaceMetadata">是否从接口类型读取属性元数据。</param>
    /// <returns>计数属性的反射信息。</returns>
    private static PropertyInfo GetCounterProperty<T>(bool interfaceMetadata) =>
        (interfaceMetadata ? typeof(ICounter) : typeof(T)).GetProperty(nameof(ICounter.Value))!;

    /// <summary>
    /// 结构体的公共取值契约。
    /// </summary>
    public interface ICounter
    {
        /// <summary>
        /// 获取或设置计数值。
        /// </summary>
        int Value { get; set; }
    }

    /// <summary>
    /// 普通接口属性的隐式实现。
    /// </summary>
    public struct Counter : ICounter
    {
        /// <inheritdoc />
        public int Value { get; set; }
    }

    /// <summary>
    /// 包含引用类型和可空值类型属性的契约。
    /// </summary>
    public interface IOptionalValues
    {
        /// <summary>
        /// 获取或设置可空整数值。
        /// </summary>
        int? Number { get; set; }

        /// <summary>
        /// 获取或设置文本值。
        /// </summary>
        string Text { get; set; }
    }

    /// <summary>
    /// 可空赋值测试结构体。
    /// </summary>
    public struct OptionalValues : IOptionalValues
    {
        /// <inheritdoc />
        public int? Number { get; set; }

        /// <inheritdoc />
        public string Text { get; set; }
    }

    /// <summary>
    /// 用于确认原始异常身份的测试异常。
    /// </summary>
    private sealed class SetterFailureException : Exception { }

    /// <summary>
    /// 先修改字段再抛出指定异常的结构体。
    /// </summary>
    private struct ThrowingCounter : ICounter
    {
        /// <summary>
        /// 属性设置器写入的当前值。
        /// </summary>
        private int _value;
        /// <summary>
        /// 设置属性时需要传播的预设异常。
        /// </summary>
        private readonly Exception _failure;

        /// <summary>
        /// 初始化一个 <see cref="ThrowingCounter"/> 类型的实例。
        /// </summary>
        /// <param name="failure">属性设置后要抛出的异常。</param>
        public ThrowingCounter(Exception failure)
        {
            _value = 0;
            _failure = failure;
        }

        /// <inheritdoc />
        /// <remarks>
        /// 写入值后抛出构造时指定的异常。
        /// </remarks>
        public int Value { get => _value; set { _value = value; throw _failure; } }
    }

    /// <summary>
    /// 通过非公共方法实现接口属性的结构体。
    /// </summary>
    public struct ExplicitCounter : ICounter
    {
        /// <summary>
        /// 获取或设置实际保存的计数值。
        /// </summary>
        public int StoredValue { get; private set; }

        /// <inheritdoc />
        int ICounter.Value { get => StoredValue; set => StoredValue = value; }
    }

    /// <summary>
    /// 默认属性访问器转发到结构体实际实现的存储属性。
    /// </summary>
    public interface IDefaultCounter
    {
        /// <summary>
        /// 获取或设置默认访问器使用的存储值。
        /// </summary>
        int StoredValue { get; set; }

        /// <summary>
        /// 获取或设置由默认访问器转发的计数值。
        /// </summary>
        int Value { get => StoredValue; set => StoredValue = value; }
    }

    /// <summary>
    /// 依赖接口默认访问器的结构体。
    /// </summary>
    public struct DefaultCounter : IDefaultCounter
    {
        /// <inheritdoc />
        public int StoredValue { get; set; }
    }

    /// <summary>
    /// 会在修改存储属性后抛出指定异常的默认接口实现。
    /// </summary>
    public interface IThrowingDefaultCounter
    {
        /// <summary>
        /// 获取或设置默认访问器使用的存储值。
        /// </summary>
        int StoredValue { get; set; }

        /// <summary>
        /// 获取设置计数值时要抛出的异常。
        /// </summary>
        Exception Failure { get; }

        /// <summary>
        /// 获取或设置计数值。
        /// </summary>
        /// <remarks>设置后抛出 <see cref="Failure"/>。</remarks>
        int Value { get => StoredValue; set { StoredValue = value; throw Failure; } }
    }

    /// <summary>
    /// 默认接口异常回写测试结构体。
    /// </summary>
    public struct ThrowingDefaultCounter : IThrowingDefaultCounter
    {
        /// <inheritdoc />
        public int StoredValue { get; set; }

        /// <inheritdoc />
        public Exception Failure { get; set; }
    }

    /// <summary>
    /// 由派生接口提供实现的抽象属性契约。
    /// </summary>
    public interface IAbstractCounter
    {
        /// <summary>
        /// 获取或设置默认访问器使用的存储值。
        /// </summary>
        int StoredValue { get; set; }

        /// <summary>
        /// 获取或设置计数值。
        /// </summary>
        int Value { get; set; }
    }

    /// <summary>
    /// 对基接口属性提供默认实现。
    /// </summary>
    public interface IInheritedDefaultCounter : IAbstractCounter
    {
        /// <inheritdoc />
        int IAbstractCounter.Value { get => StoredValue; set => StoredValue = value; }
    }

    /// <summary>
    /// 使用继承默认接口属性的结构体。
    /// </summary>
    public struct InheritedDefaultCounter : IInheritedDefaultCounter
    {
        /// <inheritdoc />
        public int StoredValue { get; set; }
    }

    /// <summary>
    /// 验证逆变赋值的只写属性契约。
    /// </summary>
    /// <typeparam name="TValue">由属性接收的值类型。</typeparam>
    public interface IValueSink<in TValue>
    {
        /// <summary>
        /// 设置要接收的值。
        /// </summary>
        TValue Value { set; }
    }

    /// <summary>
    /// 显式实现 object 参数接口，通过逆变接口写入字符串。
    /// </summary>
    public struct VariantCounter : IValueSink<object>
    {
        /// <summary>
        /// 获取或设置接口属性接收的对象值。
        /// </summary>
        public object StoredValue { get; private set; }

        /// <inheritdoc />
        object IValueSink<object>.Value { set => StoredValue = value; }
    }
}
