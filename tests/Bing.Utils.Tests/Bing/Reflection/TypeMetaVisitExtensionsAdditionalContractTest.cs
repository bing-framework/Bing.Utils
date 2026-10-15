using System.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 验证成员与参数元数据的类型判定及方法重写识别。
/// </summary>
[Trait("Bing.Reflection", "TypeMetaVisitExtensions.Additional")]
public class TypeMetaVisitExtensionsAdditionalContractTest
{
    /// <summary>
    /// 验证成员数值判定使用属性的实际类型。
    /// </summary>
    [Fact]
    public void IsNumeric_MemberInfo_ReturnsExpected()
    {
        var numericProperty = typeof(MetaSample).GetProperty(nameof(MetaSample.NumericValue));
        var textProperty = typeof(MetaSample).GetProperty(nameof(MetaSample.TextValue));

        numericProperty.IsNumeric().ShouldBeTrue();
        textProperty.IsNumeric().ShouldBeFalse();
    }

    /// <summary>
    /// 验证参数数值判定使用参数的实际类型。
    /// </summary>
    [Fact]
    public void IsNumeric_ParameterInfo_ReturnsExpected()
    {
        var method = typeof(MetaSample).GetMethod(nameof(MetaSample.AcceptValues));
        var numberParameter = method!.GetParameters()[0];
        var textParameter = method.GetParameters()[1];

        numberParameter.IsNumeric().ShouldBeTrue();
        textParameter.IsNumeric().ShouldBeFalse();
    }

    /// <summary>
    /// 验证成员和参数中的元组类型均被识别。
    /// </summary>
    [Fact]
    public void IsTupleType_MemberAndParameter_ReturnsTrue()
    {
        var tupleProperty = typeof(MetaSample).GetProperty(nameof(MetaSample.TupleValue));
        var method = typeof(MetaSample).GetMethod(nameof(MetaSample.AcceptTuple));
        var tupleParameter = method!.GetParameters()[0];

        tupleProperty.IsTupleType().ShouldBeTrue();
        tupleParameter.IsTupleType().ShouldBeTrue();
    }

    /// <summary>
    /// 验证结构体成员和参数被识别而字符串成员被排除。
    /// </summary>
    [Fact]
    public void IsStructType_MemberAndParameter_ReturnsExpected()
    {
        var structProperty = typeof(MetaSample).GetProperty(nameof(MetaSample.StructValue));
        var classProperty = typeof(MetaSample).GetProperty(nameof(MetaSample.TextValue));
        var method = typeof(MetaSample).GetMethod(nameof(MetaSample.AcceptStruct));
        var structParameter = method!.GetParameters()[0];

        structProperty.IsStructType().ShouldBeTrue();
        classProperty.IsStructType().ShouldBeFalse();
        structParameter.IsStructType().ShouldBeTrue();
    }

    /// <summary>
    /// 验证仅重写方法满足重写判定。
    /// </summary>
    [Fact]
    public void IsOverridden_OverrideAndBaseMethod_ReturnsExpected()
    {
        var baseMethod = typeof(BaseMetaSample).GetMethod(nameof(BaseMetaSample.Execute));
        var overrideMethod = typeof(DerivedMetaSample).GetMethod(nameof(DerivedMetaSample.Execute));

        baseMethod!.IsOverridden().ShouldBeFalse();
        overrideMethod!.IsOverridden().ShouldBeTrue();
    }

    /// <summary>
    /// 验证空成员元数据的数值判定当前抛出无效操作异常。
    /// </summary>
    [Fact]
    public void IsNumeric_NullMemberInfo_CurrentlyThrowsInvalidOperationException()
    {
        MemberInfo member = null;

        Should.Throw<InvalidOperationException>(() => member.IsNumeric());
    }

    /// <summary>
    /// 验证空参数元数据的数值判定当前抛出空引用异常。
    /// </summary>
    [Fact]
    public void IsNumeric_NullParameterInfo_CurrentlyThrowsNullReferenceException()
    {
        ParameterInfo parameter = null;

        Should.Throw<NullReferenceException>(() => parameter.IsNumeric());
    }

    /// <summary>
    /// 验证空参数元数据的元组判定当前抛出空引用异常。
    /// </summary>
    [Fact]
    public void IsTupleType_NullParameterInfo_CurrentlyThrowsNullReferenceException()
    {
        ParameterInfo parameter = null;

        Should.Throw<NullReferenceException>(() => parameter.IsTupleType());
    }

    /// <summary>
    /// 验证空参数元数据的结构体判定当前抛出空引用异常。
    /// </summary>
    [Fact]
    public void IsStructType_NullParameterInfo_CurrentlyThrowsNullReferenceException()
    {
        ParameterInfo parameter = null;

        Should.Throw<NullReferenceException>(() => parameter.IsStructType());
    }

    /// <summary>
    /// 验证数值判定当前不支持事件元数据。
    /// </summary>
    [Fact]
    public void IsNumeric_UnsupportedMemberInfoEvent_CurrentlyThrowsInvalidOperationException()
    {
        var eventMember = typeof(MetaSample).GetEvent(nameof(MetaSample.Changed));

        Should.Throw<InvalidOperationException>(() => eventMember.IsNumeric());
    }

    /// <summary>
    /// 提供结构体类型判定的测试值。
    /// </summary>
    private readonly struct SampleStruct
    {
        /// <summary>
        /// 获取或初始化结构体的测试整数。
        /// </summary>
        public int Value { get; init; }
    }

    /// <summary>
    /// 提供不同类型的成员和参数元数据样例。
    /// </summary>
    private class MetaSample
    {
        /// <summary>
        /// 获取或设置用于数值判定的测试整数。
        /// </summary>
        public int NumericValue { get; set; }

        /// <summary>
        /// 获取或设置用于引用类型对照的测试文本。
        /// </summary>
        public string TextValue { get; set; }

        /// <summary>
        /// 获取或设置用于元组类型判定的测试值。
        /// </summary>
        public (int Id, string Name) TupleValue { get; set; }

        /// <summary>
        /// 获取或设置用于结构体类型判定的测试值。
        /// </summary>
        public SampleStruct StructValue { get; set; }

        /// <summary>
        /// 提供用于验证不支持事件元数据的测试事件。
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// 提供用于比较整数与字符串参数元数据的空操作。
        /// </summary>
        /// <param name="number">形成数值参数元数据的测试整数。</param>
        /// <param name="text">形成非数值参数元数据的测试文本。</param>
        public void AcceptValues(int number, string text)
        {
            _ = number;
            _ = text;
        }

        /// <summary>
        /// 提供用于元组参数元数据判定的空操作。
        /// </summary>
        /// <param name="tuple">形成元组参数元数据的测试值。</param>
        public void AcceptTuple((int Id, string Name) tuple)
        {
            _ = tuple;
        }

        /// <summary>
        /// 提供用于结构体参数元数据判定的空操作。
        /// </summary>
        /// <param name="value">形成结构体参数元数据的测试值。</param>
        public void AcceptStruct(SampleStruct value)
        {
            _ = value;
        }
    }

    /// <summary>
    /// 用于验证方法重写元数据的基类样例。
    /// </summary>
    private class BaseMetaSample
    {
        /// <summary>
        /// 提供用于验证方法重写关系的空操作。
        /// </summary>
        public virtual void Execute()
        {
        }
    }

    /// <summary>
    /// 提供用于验证重写方法元数据的派生类型。
    /// </summary>
    private class DerivedMetaSample : BaseMetaSample
    {
        /// <inheritdoc />
        public override void Execute()
        {
        }
    }
}
