using System.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 测试类：补充 TypeMetaVisitExtensions 的成员与参数元数据契约。
/// </summary>
[Trait("Bing.Reflection", "TypeMetaVisitExtensions.Additional")]
public class TypeMetaVisitExtensionsAdditionalContractTest
{
    /// <summary>
    /// 测试用例：IsNumeric 对成员元数据应按成员真实类型返回结果。
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
    /// 测试用例：IsNumeric 对参数元数据应按参数类型返回结果。
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
    /// 测试用例：IsTupleType 对成员与参数中的元组类型应返回 true。
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
    /// 测试用例：IsStructType 对结构体与引用类型应返回不同结果。
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
    /// 测试用例：IsOverridden 对重写方法与基类方法应返回不同结果。
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
    /// 测试用例：IsNumeric 在 null 成员元数据场景下当前实现抛 InvalidOperationException。
    /// </summary>
    [Fact]
    public void IsNumeric_NullMemberInfo_CurrentlyThrowsInvalidOperationException()
    {
        MemberInfo member = null;

        Should.Throw<InvalidOperationException>(() => member.IsNumeric());
    }

    /// <summary>
    /// 测试用例：IsNumeric 在 null 参数元数据场景下，当前实现抛 NullReferenceException。
    /// </summary>
    [Fact]
    public void IsNumeric_NullParameterInfo_CurrentlyThrowsNullReferenceException()
    {
        ParameterInfo parameter = null;

        Should.Throw<NullReferenceException>(() => parameter.IsNumeric());
    }

    /// <summary>
    /// 测试用例：IsTupleType 在 null 参数元数据场景下，当前实现抛 NullReferenceException。
    /// </summary>
    [Fact]
    public void IsTupleType_NullParameterInfo_CurrentlyThrowsNullReferenceException()
    {
        ParameterInfo parameter = null;

        Should.Throw<NullReferenceException>(() => parameter.IsTupleType());
    }

    /// <summary>
    /// 测试用例：IsStructType 在 null 参数元数据场景下，当前实现抛 NullReferenceException。
    /// </summary>
    [Fact]
    public void IsStructType_NullParameterInfo_CurrentlyThrowsNullReferenceException()
    {
        ParameterInfo parameter = null;

        Should.Throw<NullReferenceException>(() => parameter.IsStructType());
    }

    /// <summary>
    /// 测试用例：IsNumeric 在不支持的成员类型（EventInfo）场景下，当前实现抛 InvalidOperationException。
    /// </summary>
    [Fact]
    public void IsNumeric_UnsupportedMemberInfoEvent_CurrentlyThrowsInvalidOperationException()
    {
        var eventMember = typeof(MetaSample).GetEvent(nameof(MetaSample.Changed));

        Should.Throw<InvalidOperationException>(() => eventMember.IsNumeric());
    }

    private readonly struct SampleStruct
    {
        public int Value { get; init; }
    }

    private class MetaSample
    {
        public int NumericValue { get; set; }

        public string TextValue { get; set; }

        public (int Id, string Name) TupleValue { get; set; }

        public SampleStruct StructValue { get; set; }

        public event EventHandler Changed;

        public void AcceptValues(int number, string text)
        {
            _ = number;
            _ = text;
        }

        public void AcceptTuple((int Id, string Name) tuple)
        {
            _ = tuple;
        }

        public void AcceptStruct(SampleStruct value)
        {
            _ = value;
        }
    }

    private class BaseMetaSample
    {
        public virtual void Execute()
        {
        }
    }

    private class DerivedMetaSample : BaseMetaSample
    {
        public override void Execute()
        {
        }
    }
}
