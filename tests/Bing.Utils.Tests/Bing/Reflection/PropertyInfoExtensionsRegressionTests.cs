using Bing.Reflection;
using Shouldly;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Bing.Reflection;

public class PropertyInfoExtensionsRegressionTests
{
    [Fact]
    public void GetValueSetterByRef_UpdatesOriginalStruct()
    {
        var property = typeof(ValueSample).GetProperty(nameof(ValueSample.Count))!;
        var setter = property.GetValueSetterByRef<ValueSample>();
        setter.ShouldBeSameAs(property.GetValueSetterByRef<ValueSample>());
        var value = new ValueSample();

        setter(ref value, 42);

        value.Count.ShouldBe(42);
    }

    [Fact]
    public void GetValueSetter_GenericValueType_ThrowsGuidingException()
    {
        var property = typeof(ValueSample).GetProperty(nameof(ValueSample.Count))!;
        Should.Throw<NotSupportedException>(() => property.GetValueSetter<ValueSample>());
    }

    [Fact]
    public void GetValueSetterByRef_InvalidAccessors_UseSameValidationAsOtherSetters()
    {
        Should.Throw<InvalidOperationException>(() =>
            typeof(ValueSample).GetProperty(nameof(ValueSample.ReadOnly))!.GetValueSetterByRef<ValueSample>());
        Should.Throw<MemberAccessException>(() =>
            typeof(ValueSample).GetProperty(nameof(ValueSample.PrivateSet))!.GetValueSetterByRef<ValueSample>());
        Should.Throw<ArgumentException>(() =>
            typeof(ValueSample).GetProperty(nameof(ValueSample.StaticValue))!.GetValueSetterByRef<ValueSample>());
    }

    [Fact]
    public void InitOnlySetter_IsNotAvailableForPostConstructionMutation()
    {
        var property = typeof(InitSample).GetProperty(nameof(InitSample.Value))!;
        var sample = new InitSample { Value = 3 };

        Should.Throw<InvalidOperationException>(() => property.GetValueSetter());
        Should.Throw<InvalidOperationException>(() => property.GetValueSetter<InitSample>());
        Reflections.TrySetMemberValueByPath(sample, nameof(InitSample.Value), 9).ShouldBeFalse();
        Reflections.TrySetMemberValueByPath(sample, nameof(InitSample.Value), 9,
            new MemberPathOptions { IncludeNonPublic = true }).ShouldBeFalse();
        sample.Value.ShouldBe(3);
    }

    [Fact]
    public void UnavailableAccessor_ThrowsAcrossRepeatedCalls()
    {
        var readOnly = typeof(AccessSample).GetProperty(nameof(AccessSample.ReadOnly))!;
        Should.Throw<InvalidOperationException>(() => readOnly.GetValueSetter<AccessSample>());
        Should.Throw<InvalidOperationException>(() => readOnly.GetValueSetter<AccessSample>());
    }

    [Fact]
    public void InvalidAccessors_ThrowBeforeEnteringDelegateCache()
    {
        var indexer = typeof(AccessSample).GetProperty("Item")!;
        var privateGetter = typeof(AccessSample).GetProperty(nameof(AccessSample.PrivateGet))!;

        Should.Throw<ArgumentException>(() => indexer.GetValueGetter());
        Should.Throw<MemberAccessException>(() => privateGetter.GetValueGetter());
    }

    [Fact]
    public void BoxedDelegates_RejectUnsupportedPropertyTypesBeforeCaching()
    {
        var refProperty = typeof(RefReturnSample).GetProperty(nameof(RefReturnSample.Value))!;
        var spanProperty = typeof(SpanReturnSample).GetProperty(nameof(SpanReturnSample.Value))!;
        var pointerProperty = CreatePointerProperty();

        Should.Throw<NotSupportedException>(() => refProperty.GetValueGetter<RefReturnSample>());
        Should.Throw<NotSupportedException>(() => refProperty.GetValueGetter());
        Should.Throw<NotSupportedException>(() => spanProperty.GetValueGetter());
        Should.Throw<NotSupportedException>(() => pointerProperty.GetValueGetter());
        Should.Throw<NotSupportedException>(() => pointerProperty.GetValueSetter());
    }

    [Fact]
    public void GetValueGetter_PropertyFromAnotherType_ThrowsArgumentException()
    {
        var property = typeof(AccessSample).GetProperty(nameof(AccessSample.ReadOnly))!;
        Should.Throw<ArgumentException>(() => property.GetValueGetter<ConcurrentGetterSample>());
    }

    [Fact]
    public void GetValueGetter_ConcurrentFirstAccess_ReusesCompiledDelegate()
    {
        var property = typeof(ConcurrentGetterSample).GetProperty(nameof(ConcurrentGetterSample.Value))!;
        var getters = new Func<ConcurrentGetterSample, object>[32];

        Parallel.For(0, getters.Length, index => getters[index] = property.GetValueGetter<ConcurrentGetterSample>());

        getters.ShouldAllBe(getter => ReferenceEquals(getter, getters[0]));
    }

    [Fact]
    public void GetValueSetter_ConcurrentFirstAccess_ReusesCompiledDelegate()
    {
        var property = typeof(ConcurrentSetterSample).GetProperty(nameof(ConcurrentSetterSample.Value))!;
        var setters = new Action<ConcurrentSetterSample, object>[32];

        Parallel.For(0, setters.Length, index => setters[index] = property.GetValueSetter<ConcurrentSetterSample>());

        setters.ShouldAllBe(setter => ReferenceEquals(setter, setters[0]));
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void CollectibleType_PropertyAndPathCaches_DoNotKeepAssemblyAlive()
    {
        var assembly = UseCollectibleType();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        assembly.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference UseCollectibleType()
    {
        var type = TypeVisit.CreateDynamicType(new Dictionary<string, Type>
        {
            [$"Value_{Guid.NewGuid():N}"] = typeof(int)
        });
        var property = type.GetProperties().Single();
        var instance = Activator.CreateInstance(type)!;
        property.GetValueSetter()(instance, 12);
        property.GetValueGetter()(instance).ShouldBe(12);
        Reflections.TryGetMemberValueByPath(instance, property.Name, out _).ShouldBeTrue();
        Reflections.TrySetMemberValueByPath(instance, property.Name, 13).ShouldBeTrue();
        TypeReflections.TypeCacheManager.GetTypeFields(type);
        TypeReflections.TypeCacheManager.GetTypeFields(type, BindingFlags.Public | BindingFlags.Instance);
        TypeReflections.TypeCacheManager.GetTypeProperties(type, BindingFlags.Public | BindingFlags.Instance);
        TypeReflections.TypeCacheManager.GetTypeMethods(type, BindingFlags.Public | BindingFlags.Instance);
        TypeVisit.GetProperties(type, PropertyAccessOptions.Getters).Count().ShouldBe(1);
        TypeVisit.GetProperties(type, PropertyAccessOptions.Setters).Count().ShouldBe(1);
        TypeVisit.GetProperties(type, PropertyAccessOptions.Both).Count().ShouldBe(1);
        return new WeakReference(type.Assembly);
    }
#endif

    private struct ValueSample
    {
        public int Count { get; set; }
        public int ReadOnly => Count;
        public int PrivateSet { get; private set; }
        public static int StaticValue { get; set; }
    }
    private sealed class AccessSample
    {
        public int ReadOnly { get; }
        public int this[int index] => index;
        public int PrivateGet { private get; set; }
    }
    private sealed class InitSample { public int Value { get; init; } }
    private sealed class ConcurrentGetterSample { public int Value { get; } }
    private sealed class ConcurrentSetterSample { public int Value { set { } } }

    private sealed class RefReturnSample
    {
        private int _value;
        public ref int Value => ref _value;
    }

    private sealed class SpanReturnSample
    {
        public static Span<int> Value => Span<int>.Empty;
    }

    private static PropertyInfo CreatePointerProperty()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"PointerPropertyProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var typeBuilder = assembly.DefineDynamicModule("Main").DefineType(
            "PointerHolder", TypeAttributes.Public | TypeAttributes.Sealed);
        var field = typeBuilder.DefineField("_value", typeof(int), FieldAttributes.Private | FieldAttributes.Static);
        var pointerType = typeof(int).MakePointerType();
        var getter = typeBuilder.DefineMethod("get_Value",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            pointerType, Type.EmptyTypes);
        var getterIl = getter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldsflda, field);
        getterIl.Emit(OpCodes.Conv_U);
        getterIl.Emit(OpCodes.Ret);
        var setter = typeBuilder.DefineMethod("set_Value",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(void), new[] { pointerType });
        setter.GetILGenerator().Emit(OpCodes.Ret);
        var property = typeBuilder.DefineProperty("Value", PropertyAttributes.None, pointerType, Type.EmptyTypes);
        property.SetGetMethod(getter);
        property.SetSetMethod(setter);
        return typeBuilder.CreateTypeInfo()!.AsType().GetProperty("Value")!;
    }
}
