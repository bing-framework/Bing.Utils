using System.Reflection;
using Bing.Dynamic;
using Shouldly;

namespace Bing.Reflection;

/// <summary>反射入口统一参数异常和写入权限的回归测试。</summary>
public class ReflectionFailureContractTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TypePredicates_NullMetadata_ReportParameterName(int predicate)
    {
        MemberInfo member = null!;
        ParameterInfo parameter = null!;
        Action[] memberCalls = { () => member.IsNumeric(), () => member.IsTupleType(), () => member.IsStructType() };
        Action[] parameterCalls = { () => parameter.IsNumeric(), () => parameter.IsTupleType(), () => parameter.IsStructType() };

        Should.Throw<ArgumentNullException>(memberCalls[predicate]).ParamName.ShouldBe("member");
        Should.Throw<ArgumentNullException>(parameterCalls[predicate]).ParamName.ShouldBe("parameter");
    }

    [Fact]
    public void PropertyPredicates_NullMetadata_ReportParameterName()
    {
        PropertyInfo property = null!;
        Should.Throw<ArgumentNullException>(() => property.IsVirtual()).ParamName.ShouldBe("property");
        Should.Throw<ArgumentNullException>(() => property.IsAbstract()).ParamName.ShouldBe("property");
    }

    [Fact]
    public void DynamicBase_InitOnlyProperty_RejectsWriteWithoutChangingValue()
    {
        var sample = new InitSample { Value = 7 };
        Should.Throw<InvalidOperationException>(() => sample.SetPropertyValue(nameof(InitSample.Value), 42));
        sample.GetPropertyValue(nameof(InitSample.Value)).ShouldBe(7);
        sample.SetPropertyValue(nameof(InitSample.Mutable), 42);
        sample.Mutable.ShouldBe(42);
    }

    private sealed class InitSample : DynamicBase
    {
        public int Value { get; init; }
        public int Mutable { get; set; }
    }
}
