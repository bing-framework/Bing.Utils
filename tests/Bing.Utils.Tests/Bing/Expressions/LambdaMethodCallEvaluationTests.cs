using System.Linq.Expressions;
using System.Reflection;

namespace Bing.Expressions;

/// <summary>覆盖参数无关方法调用的快速路径与解释器回退语义。</summary>
public class LambdaMethodCallEvaluationTests
{
    [Fact]
    public void MethodCall_EvaluatesReceiverAndArgumentsOnceInOrderBeforeNullCheck()
    {
        var root = new EvaluationRoot { TargetValue = new CallTarget() };
        root.TargetValue.Events = root.Events;
        Expression<Func<string>> expression = () => root.Target.Combine(root.First, root.Second);

        Lambdas.GetValue(expression).ShouldBe("3:5");
        root.Events.SequenceEqual(new[] { "receiver", "first", "second", "invoke" }).ShouldBeTrue();

        root.Events.Clear();
        root.TargetValue = null;
        Should.Throw<NullReferenceException>(() => Lambdas.GetValue(expression));
        root.Events.SequenceEqual(new[] { "receiver", "first", "second" }).ShouldBeTrue();
    }

    [Fact]
    public void MethodCall_StopsAtThrowingArgumentAndPreservesExceptionInstance()
    {
        var root = new EvaluationRoot { TargetValue = new CallTarget() };
        root.TargetValue.Events = root.Events;
        var failure = new InvalidOperationException("first argument failed");
        root.FirstFailure = failure;
        Expression<Func<string>> expression = () => root.Target.Combine(root.First, root.Second);

        Should.Throw<InvalidOperationException>(() => Lambdas.GetValue(expression)).ShouldBeSameAs(failure);
        root.Events.SequenceEqual(new[] { "receiver", "first" }).ShouldBeTrue();
        root.TargetValue.Invocations.ShouldBe(0);
    }

    [Fact]
    public void MethodCall_NestedArgumentFallbackDoesNotReadEarlierGetterTwice()
    {
        var root = new EvaluationRoot { TargetValue = new CallTarget() };
        root.TargetValue.Events = root.Events;
        Expression<Func<string>> expression = () => root.Target.Combine(root.First, root.ReadNested());

        Lambdas.GetValue(expression).ShouldBe("3:7");
        root.Events.SequenceEqual(new[] { "receiver", "first", "nested", "invoke" }).ShouldBeTrue();
    }

    [Fact]
    public void MethodCall_UserDefinedConversionFallbackDoesNotReadEarlierGetterTwice()
    {
        var root = new EvaluationRoot { TargetValue = new CallTarget(), Conversion = new ConversionValue(11) };
        root.TargetValue.Events = root.Events;
        root.Conversion.Events = root.Events;
        Expression<Func<string>> expression = () => root.Target.Combine(root.First, (int)root.Conversion);

        Lambdas.GetValue(expression).ShouldBe("3:11");
        root.Events.SequenceEqual(new[] { "receiver", "first", "conversion", "invoke" }).ShouldBeTrue();
        root.Conversion.Invocations.ShouldBe(1);
    }

    [Fact]
    public void MethodCall_RefOutAndMutableStructReceiverMatchInterpreter()
    {
        var expectedRefHolder = new RefOutHolder { Value = 4 };
        var expectedRef = CreateRefOutExpression(expectedRefHolder).Compile(preferInterpretation: true)();
        var actualRefHolder = new RefOutHolder { Value = 4 };
        var actualRef = (int)Lambdas.GetValue(CreateRefOutExpression(actualRefHolder));

        actualRef.ShouldBe(expectedRef);
        actualRefHolder.Value.ShouldBe(expectedRefHolder.Value);
        actualRefHolder.Output.ShouldBe(expectedRefHolder.Output);

        var expectedStructHolder = new StructHolder { Value = new MutableReceiver { Count = 2 } };
        var expectedStruct = CreateStructExpression(expectedStructHolder).Compile(preferInterpretation: true)();
        var actualStructHolder = new StructHolder { Value = new MutableReceiver { Count = 2 } };
        var actualStruct = (int)Lambdas.GetValue(CreateStructExpression(actualStructHolder));

        actualStruct.ShouldBe(expectedStruct);
        actualStructHolder.Value.Count.ShouldBe(expectedStructHolder.Value.Count);
    }

    [Fact]
    public void MethodCall_PreservesNullableClosedGenericAndVirtualDispatch()
    {
        var target = new DerivedCallTarget();
        int? nullable = null;
        Expression<Func<int?>> nullableCall = () => target.EchoNullable(nullable);
        Lambdas.GetValue(nullableCall).ShouldBeNull();
        nullable = 13;
        Lambdas.GetValue(nullableCall).ShouldBe(13);

        Expression<Func<string>> genericCall = () => target.Echo("closed");
        Lambdas.GetValue(genericCall).ShouldBe("closed");

        BaseCallTarget virtualTarget = target;
        Expression<Func<string>> virtualCall = () => virtualTarget.ReadVirtual();
        Lambdas.GetValue(virtualCall).ShouldBe("derived");
    }

    [Fact]
    public void MethodCall_PreservesTargetInvocationExceptionInstance()
    {
        var failure = new TargetInvocationException("original target exception", new InvalidOperationException());
        var target = new CallTarget { Failure = failure };
        Expression<Func<int>> expression = () => target.ThrowFailure();

        Should.Throw<TargetInvocationException>(() => Lambdas.GetValue(expression)).ShouldBeSameAs(failure);
    }

    [Fact]
    public void MethodCall_TypeMissingIsPassedAsValueForOptionalObjectParameter()
    {
        object value = Type.Missing;
        var target = new CallTarget();
        Expression<Func<object>> expression = () => target.EchoOptional(value);

        Lambdas.GetValue(expression).ShouldBeSameAs(Type.Missing);
    }

    private static Expression<Func<int>> CreateRefOutExpression(RefOutHolder holder)
    {
        var instance = Expression.Constant(holder);
        var value = Expression.Field(instance, nameof(RefOutHolder.Value));
        var output = Expression.Field(instance, nameof(RefOutHolder.Output));
        var method = typeof(RefOutMethods).GetMethod(nameof(RefOutMethods.Update));
        return Expression.Lambda<Func<int>>(Expression.Call(method, value, output));
    }

    private static Expression<Func<int>> CreateStructExpression(StructHolder holder)
    {
        var instance = Expression.Constant(holder);
        var receiver = Expression.Field(instance, nameof(StructHolder.Value));
        var method = typeof(MutableReceiver).GetMethod(nameof(MutableReceiver.Increment));
        return Expression.Lambda<Func<int>>(Expression.Call(receiver, method, Expression.Constant(3)));
    }

    private sealed class EvaluationRoot
    {
        public readonly List<string> Events = new();
        public CallTarget TargetValue;
        public Exception FirstFailure;
        public ConversionValue Conversion;

        public CallTarget Target
        {
            get
            {
                Events.Add("receiver");
                return TargetValue;
            }
        }

        public int First
        {
            get
            {
                Events.Add("first");
                if (FirstFailure != null)
                    throw FirstFailure;
                return 3;
            }
        }

        public int Second
        {
            get
            {
                Events.Add("second");
                return 5;
            }
        }

        public int ReadNested()
        {
            Events.Add("nested");
            return 7;
        }
    }

    private sealed class CallTarget
    {
        public List<string> Events;
        public Exception Failure;
        public int Invocations;

        public string Combine(int first, int second)
        {
            Invocations++;
            Events?.Add("invoke");
            return $"{first}:{second}";
        }

        public int ThrowFailure()
        {
            throw Failure;
        }

        public object EchoOptional(object value = null) => value;
    }

    private sealed class ConversionValue
    {
        public ConversionValue(int value) => Value = value;

        public int Value { get; }
        public List<string> Events;
        public int Invocations;

        public static explicit operator int(ConversionValue value)
        {
            value.Invocations++;
            value.Events?.Add("conversion");
            return value.Value;
        }
    }

    private sealed class RefOutHolder
    {
        public int Value;
        public int Output;
    }

    private static class RefOutMethods
    {
        public static int Update(ref int value, out int output)
        {
            value++;
            output = value * 2;
            return output;
        }
    }

    private sealed class StructHolder
    {
        public MutableReceiver Value;
    }

    private struct MutableReceiver
    {
        public int Count;

        public int Increment(int amount)
        {
            Count += amount;
            return Count;
        }
    }

    private class BaseCallTarget
    {
        public virtual string ReadVirtual() => "base";
    }

    private sealed class DerivedCallTarget : BaseCallTarget
    {
        public int? EchoNullable(int? value) => value;

        public T Echo<T>(T value) => value;

        public override string ReadVirtual() => "derived";
    }
}
