using System.Reflection;

namespace Bing.Reflection;

/// <summary>验证引用构造参数绑定、回写和非公开基础方法查找。</summary>
public class ConstructorAndBaseMethodRegressionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-3)]
    public void CreateInstance_RefArgument_PreservesInputAndWritesBack(int input)
    {
        object[] args = { input };
        var instance = TypeVisit.CreateInstance<RefSample>(args);
        instance.Value.ShouldBe(input);
        args[0].ShouldBe(input + 1);
    }

    [Fact]
    public void CreateInstance_InAndOutParameters_UseElementTypes()
    {
        object[] input = { 7 };
        TypeVisit.CreateInstance<InSample>(input).Value.ShouldBe(7);
        input[0].ShouldBe(7);
        object[] output = { null };
        TypeVisit.CreateInstance<OutSample>(output).ShouldNotBeNull();
        output[0].ShouldBe(9);
    }

    [Fact]
    public void CreateInstance_RefReferenceAndNullable_WritesBack()
    {
        object[] reference = { "before" };
        TypeVisit.CreateInstance<RefReference>(reference);
        reference[0].ShouldBe("after");
        object[] nullable = { null };
        TypeVisit.CreateInstance<RefNullable>(nullable);
        nullable[0].ShouldBe(3);
    }

    [Fact]
    public void CreateInstance_InvalidRefInput_FailsBeforeInvocation()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance<RefSample>(new object[] { null }));
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance<RefSample>(new object[] { "7" }));
    }

    [Fact]
    public void CreateInstance_ValueOverload_RemainsPreferredForExactValueType()
    {
        TypeVisit.CreateInstance<OverloadedSample>(7).Kind.ShouldBe("value");
        TypeVisit.GetMatchingConstructor(typeof(OverloadedSample), new[] { typeof(int).MakeByRefType() })
            .GetParameters()[0].ParameterType.IsByRef.ShouldBeTrue();
    }

    [Fact]
    public void CreateInstance_ThrowingRefConstructor_PreservesOriginalCause()
    {
        var exception = Should.Throw<InvalidOperationException>(() => TypeVisit.CreateInstance<ThrowingRef>(7));
        exception.InnerException.ShouldBeOfType<ArgumentException>().Message.ShouldBe("original");
    }

    [Fact]
    public void GetBaseMethod_ProtectedOverride_ReturnsNearestBase()
    {
        var method = typeof(Last).GetMethod("Execute", BindingFlags.Instance | BindingFlags.NonPublic);
        TypeVisit.GetBaseMethod(method).DeclaringType.ShouldBe(typeof(Middle));
        var middle = typeof(Middle).GetMethod("Execute", BindingFlags.Instance | BindingFlags.NonPublic);
        TypeVisit.GetBaseMethod(middle).DeclaringType.ShouldBe(typeof(First));
    }

    [Fact]
    public void GetBaseMethod_PrivateBaseMember_IsNotInherited()
    {
        var method = typeof(Last).GetMethod("Hidden", BindingFlags.Instance | BindingFlags.NonPublic);
        TypeVisit.GetBaseMethod(method).ShouldBeNull();
    }

    public class RefSample { public int Value; public RefSample(ref int value) { Value = value; value++; } }
    public class InSample { public int Value; public InSample(in int value) { Value = value; } }
    public class OutSample { public OutSample(out int value) { value = 9; } }
    public class RefReference { public RefReference(ref string value) { value = "after"; } }
    public class RefNullable { public RefNullable(ref int? value) { value = 3; } }
    public class ThrowingRef { public ThrowingRef(ref int value) { throw new ArgumentException("original"); } }
    public class OverloadedSample
    {
        public string Kind;
        public OverloadedSample(int value) { Kind = "value"; }
        public OverloadedSample(ref int value) { Kind = "ref"; }
    }
    public class First { protected virtual void Execute() { } private void Hidden() { } }
    public class Middle : First { protected override void Execute() { } }
    public class Last : Middle { protected override void Execute() { } protected void Hidden() { } }
}
