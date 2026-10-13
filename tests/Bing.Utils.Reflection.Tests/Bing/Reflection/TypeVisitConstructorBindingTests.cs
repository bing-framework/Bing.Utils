using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Shouldly;

namespace Bing.Reflection;

public class TypeVisitConstructorBindingTests
{
    [Fact]
    public void CreateInstance_AssignableArgument_UsesPublicConstructor()
    {
        var value = TypeVisit.CreateInstance(typeof(InterfaceArgumentSample), "value");
        value.ShouldBeOfType<InterfaceArgumentSample>().Value.ShouldBe("value");
    }

    [Fact]
    public void CreateInstance_NullArgument_MatchesReferenceParameter()
    {
        var value = TypeVisit.CreateInstance(typeof(InterfaceArgumentSample), new object[] { null! });
        value.ShouldBeOfType<InterfaceArgumentSample>().Value.ShouldBeNull();
    }

    [Fact]
    public void CreateInstance_NullArgument_PrefersMoreSpecificReferenceParameter()
    {
        var value = TypeVisit.CreateInstance<NullSpecificSample>(new object[] { null! });

        value.Selected.ShouldBe("string");
    }

    [Fact]
    public void CreateInstance_NullArgument_PrefersNullableValueParameterOverObject()
    {
        var value = TypeVisit.CreateInstance<NullNullableSpecificSample>(new object[] { null! });

        value.Selected.ShouldBe("nullable");
    }

    [Fact]
    public void CreateInstance_NullArgument_WithMultipleMatches_ThrowsAmbiguousMatchException()
    {
        Should.Throw<AmbiguousMatchException>(() =>
            TypeVisit.CreateInstance(typeof(AmbiguousSample), new object[] { null! }));
    }

    [Fact]
    public void CreateInstance_NullArgument_WithUnrelatedInterfaceCandidates_ThrowsAmbiguousMatchException()
    {
        Should.Throw<AmbiguousMatchException>(() =>
            TypeVisit.CreateInstance(typeof(AmbiguousAssignableSample), new object[] { null! }));
    }

    [Fact]
    public void CreateInstance_ConcurrentFirstAccess_UsesSameCompatibleConstructor()
    {
        var values = Enumerable.Range(0, 32).AsParallel()
            .Select(_ => TypeVisit.CreateInstance<InterfaceArgumentSample>("value"))
            .ToArray();
        values.ShouldAllBe(value => (string)value.Value == "value");
    }

    [Fact]
    public void CreateInstance_ExactOverload_WinsOverAssignableOverload()
    {
        var value = TypeVisit.CreateInstance<OverloadedSample>("value");
        value.Selected.ShouldBe("string");
    }

    [Fact]
    public void CreateInstance_AssignableOverloads_PrefersMoreSpecificInterface()
    {
        var value = TypeVisit.CreateInstance<AssignableOverloadSample>("value");

        value.Selected.ShouldBe("comparable");
    }

    [Fact]
    public void CreateInstance_AssignableOverloads_PrefersBaseClassOverImplementedInterface()
    {
        var value = TypeVisit.CreateInstance<BaseInterfaceOverloadSample>(new DerivedArgument());

        value.Selected.ShouldBe("base");
    }

    [Fact]
    public void CreateInstance_CrossParameterSpecificity_ThrowsAmbiguousMatchException()
    {
        Should.Throw<AmbiguousMatchException>(() =>
            TypeVisit.CreateInstance<CrossParameterAmbiguousSample>("first", "second"));
    }

    [Fact]
    public void CreateInstance_BoxedValue_MatchesNullableParameter()
    {
        var value = TypeVisit.CreateInstance<NullableArgumentSample>(7);
        value.Value.ShouldBe(7);
    }

    [Fact]
    public void CreateInstance_ConstructorThrowsInvalidOperationException_PreservesCause()
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            TypeVisit.CreateInstance(typeof(ThrowingConstructorSample)));

        exception.InnerException.ShouldBeOfType<InvalidOperationException>()
            .Message.ShouldBe("constructor failure");
    }

    [Fact]
    public void CreateInstance_ConstructorThrowsTargetInvocationException_PreservesOuterCause()
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            TypeVisit.CreateInstance(typeof(ThrowingTargetInvocationSample)));

        exception.InnerException.ShouldBeOfType<TargetInvocationException>()
            .InnerException.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public void CreateInstance_ValueTypeConstructorThrows_PreservesCause()
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            TypeVisit.CreateInstance(typeof(ThrowingValueConstructorSample)));
        exception.InnerException.ShouldBeOfType<InvalidOperationException>()
            .Message.ShouldBe("value constructor failure");
    }

    [Theory]
    [InlineData(typeof(void))]
    [InlineData(typeof(Span<int>))]
    public void CreateInstance_NonConstructibleValueType_ThrowsArgumentException(Type type)
    {
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance(type));
    }

    [Fact]
    public void GetMatchingConstructor_AssignableParameter_ReusesBinder()
    {
        TypeVisit.GetMatchingConstructor(typeof(InterfaceArgumentSample), new[] { typeof(string) })
            .ShouldNotBeNull();
        TypeVisit.GetMatchingConstructor(typeof(InterfaceArgumentSample), new[] { typeof(string) })
            .ShouldNotBeNull();
    }

    [Fact]
    public void GetMatchingConstructor_AssignableOverloads_PrefersMoreSpecificParameter()
    {
        var constructor = TypeVisit.GetMatchingConstructor(typeof(AssignableOverloadSample), new[] { typeof(string) });

        constructor.ShouldNotBeNull();
        constructor.GetParameters()[0].ParameterType.ShouldBe(typeof(IComparable));
    }

    [Fact]
    public void GetMatchingConstructor_AmbiguousAssignableParameters_ThrowsAmbiguousMatchException()
    {
        Should.Throw<AmbiguousMatchException>(() =>
            TypeVisit.GetMatchingConstructor(typeof(AmbiguousAssignableSample), new[] { typeof(string) }));
    }

    [Fact]
    public void GetMatchingConstructor_InvalidInputs_ThrowClearExceptions()
    {
        Should.Throw<ArgumentNullException>(() => TypeVisit.GetParameterlessConstructor(null!));
        Should.Throw<ArgumentNullException>(() =>
            TypeVisit.GetMatchingConstructor(typeof(InterfaceArgumentSample), null!));
        Should.Throw<ArgumentException>(() =>
            TypeVisit.GetMatchingConstructor(typeof(InterfaceArgumentSample), new Type[] { null! }));
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void FindMethod_CollectibleTarget_DoesNotKeepAssemblyAlive()
    {
        var assembly = UseCollectibleMethod();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        assembly.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference UseCollectibleMethod()
    {
        var type = TypeVisit.CreateDynamicType(new Dictionary<string, Type>
        {
            [$"Value_{Guid.NewGuid():N}"] = typeof(int)
        });
        var method = type.GetMethods().Single(item => item.Name.StartsWith("get_Value_", StringComparison.Ordinal));
        TypeVisit.FindMethod(type, method).ShouldNotBeNull();
        TypeVisit.FindMethod(type, method.Name, BindingFlags.Public | BindingFlags.Instance, 0).ShouldNotBeNull();
        TypeVisit.GetMethodBySignature(type, method).ShouldNotBeNull();
        return new WeakReference(type.Assembly);
    }
#endif

    [Fact]
    public void CreateInstance_SameAssemblyQualifiedNameButDifferentRuntimeType_DoesNotReuseConstructor()
    {
        var first = CreateRuntimeType("SameArgument");
        var second = CreateRuntimeType("SameArgument");
        var target = CreateConstructorTarget(first);
        first.AssemblyQualifiedName.ShouldBe(second.AssemblyQualifiedName);
        first.ShouldNotBe(second);

        TypeVisit.CreateInstance(target, Activator.CreateInstance(first)!).ShouldNotBeNull();
        var exception = Should.Throw<ArgumentException>(() =>
            TypeVisit.CreateInstance(target, Activator.CreateInstance(second)!));
        exception.ParamName.ShouldBe("args");
    }

    [Fact]
    public void CreateGenericInstance_NullableDefault_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TypeVisit.CreateGenericInstance<int?>(typeof(Nullable<>), new[] { typeof(int) }));
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void CreateInstance_GenericArgumentWithCollectibleElement_DoesNotPinLoadContext()
    {
        var context = UseCollectibleGenericArgument();
        for (var attempt = 0; attempt < 20 && context.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        context.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference UseCollectibleGenericArgument()
    {
        var context = new AssemblyLoadContext($"ConstructorArgument_{Guid.NewGuid():N}", isCollectible: true);
        var assembly = context.LoadFromAssemblyPath(typeof(TypeVisit).Assembly.Location);
        var elementType = assembly.GetType(typeof(TypeVisit).FullName!, throwOnError: true)!;
        var listType = typeof(List<>).MakeGenericType(elementType);
        TypeVisit.CreateInstance(typeof(ObjectArgumentSample), Activator.CreateInstance(listType)!).ShouldNotBeNull();
        var weak = new WeakReference(context);
        context.Unload();
        return weak;
    }
#endif

    private static Type CreateRuntimeType(string name)
    {
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ConstructorIdentityTest"),
            AssemblyBuilderAccess.Run).DefineDynamicModule("Main");
        var builder = module.DefineType(name, TypeAttributes.Public);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        return builder.CreateTypeInfo()!.AsType();
    }

    private static Type CreateConstructorTarget(Type parameterType)
    {
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ConstructorTargetTest"),
            AssemblyBuilderAccess.Run).DefineDynamicModule("Main");
        var builder = module.DefineType("ConstructorTarget", TypeAttributes.Public);
        var constructor = builder.DefineConstructor(MethodAttributes.Public,
            CallingConventions.Standard, new[] { parameterType });
        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);
        return builder.CreateTypeInfo()!.AsType();
    }

    private sealed class InterfaceArgumentSample
    {
        public InterfaceArgumentSample(IComparable value) => Value = value;
        public IComparable Value { get; }
    }

    private sealed class AmbiguousSample
    {
        public AmbiguousSample(string value) { }
        public AmbiguousSample(Uri value) { }
    }

    private sealed class OverloadedSample
    {
        public OverloadedSample(object value) => Selected = "object";
        public OverloadedSample(string value) => Selected = "string";
        public string Selected { get; }
    }

    private sealed class NullSpecificSample
    {
        public NullSpecificSample(object value) => Selected = "object";
        public NullSpecificSample(string value) => Selected = "string";
        public string Selected { get; }
    }

    private sealed class NullNullableSpecificSample
    {
        public NullNullableSpecificSample(object value) => Selected = "object";
        public NullNullableSpecificSample(int? value) => Selected = "nullable";
        public string Selected { get; }
    }

    private sealed class AssignableOverloadSample
    {
        public AssignableOverloadSample(object value) => Selected = "object";
        public AssignableOverloadSample(IComparable value) => Selected = "comparable";
        public string Selected { get; }
    }

    private interface IArgumentMarker
    {
    }

    private class BaseArgument : IArgumentMarker
    {
    }

    private sealed class DerivedArgument : BaseArgument
    {
    }

    private sealed class BaseInterfaceOverloadSample
    {
        public BaseInterfaceOverloadSample(IArgumentMarker value) => Selected = "interface";
        public BaseInterfaceOverloadSample(BaseArgument value) => Selected = "base";
        public string Selected { get; }
    }

    private sealed class CrossParameterAmbiguousSample
    {
        public CrossParameterAmbiguousSample(string first, object second) { }
        public CrossParameterAmbiguousSample(object first, string second) { }
    }

    private sealed class NullableArgumentSample
    {
        public NullableArgumentSample(int? value) => Value = value;
        public int? Value { get; }
    }

    private sealed class AmbiguousAssignableSample
    {
        public AmbiguousAssignableSample(IComparable value) { }
        public AmbiguousAssignableSample(IConvertible value) { }
    }

    private sealed class ObjectArgumentSample
    {
        public ObjectArgumentSample(object value) { }
    }

    private sealed class ThrowingConstructorSample
    {
        public ThrowingConstructorSample() => throw new InvalidOperationException("constructor failure");
    }

    private sealed class ThrowingTargetInvocationSample
    {
        public ThrowingTargetInvocationSample() =>
            throw new TargetInvocationException(new ArgumentException("original"));
    }

    private struct ThrowingValueConstructorSample
    {
        public ThrowingValueConstructorSample() =>
            throw new InvalidOperationException("value constructor failure");
    }
}
