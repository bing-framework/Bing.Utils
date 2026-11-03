using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Bing.Reflection;

/// <summary>验证强类型访问器的类型契约、缓存并发和可回收类型生命周期。</summary>
public class TypedPropertyAccessorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void TypedAccessors_IntegerProperty_RoundTripsWithoutObjectContract(int value)
    {
        var property = typeof(Sample).GetProperty(nameof(Sample.Number));
        Func<Sample, int> getter = property.GetValueGetter<Sample, int>();
        Action<Sample, int> setter = property.GetValueSetter<Sample, int>();
        var sample = new Sample();
        setter(sample, value);
        getter(sample).ShouldBe(value);
        getter.ShouldBeSameAs(property.GetValueGetter<Sample, int>());
        setter.ShouldBeSameAs(property.GetValueSetter<Sample, int>());
    }

    [Fact]
    public void TypedAccessors_NullableAndReferenceProperties_AcceptNull()
    {
        var sample = new Sample();
        var nullable = typeof(Sample).GetProperty(nameof(Sample.Optional));
        var setter = nullable.GetValueSetter<Sample, int?>();
        var getter = nullable.GetValueGetter<Sample, int?>();
        setter(sample, 3);
        getter(sample).ShouldBe(3);
        setter(sample, null);
        getter(sample).ShouldBeNull();
        var text = typeof(Sample).GetProperty(nameof(Sample.Text));
        text.GetValueSetter<Sample, string>()(sample, null);
        text.GetValueGetter<Sample, string>()(sample).ShouldBeNull();
    }

    [Fact]
    public void TypedAccessors_InheritedAndInterfaceProperties_Work()
    {
        var property = typeof(Sample).GetProperty(nameof(Sample.Number));
        var derived = new Derived();
        property.GetValueSetter<Derived, int>()(derived, 9);
        property.GetValueGetter<Derived, int>()(derived).ShouldBe(9);
        var contract = typeof(INumber).GetProperty(nameof(INumber.Number));
        contract.GetValueGetter<StructSample, int>()(new StructSample { Number = 5 }).ShouldBe(5);
        Should.Throw<NotSupportedException>(() => contract.GetValueSetter<StructSample, int>());
    }

    [Fact]
    public void TypedAccessors_InvalidArguments_KeepStrictValidation()
    {
        PropertyInfo missing = null;
        Should.Throw<ArgumentNullException>(() => missing.GetValueGetter<Sample, int>()).ParamName.ShouldBe("propertyInfo");
        Should.Throw<ArgumentNullException>(() => missing.GetValueSetter<Sample, int>()).ParamName.ShouldBe("propertyInfo");
        var property = typeof(Sample).GetProperty(nameof(Sample.Number));
        Should.Throw<ArgumentException>(() => property.GetValueGetter<Sample, long>());
        Should.Throw<ArgumentException>(() => property.GetValueSetter<Sample, object>());
        Should.Throw<ArgumentException>(() => property.GetValueGetter<string, int>());
        Should.Throw<ArgumentException>(() => typeof(Sample).GetProperty(nameof(Sample.Static)).GetValueGetter<Sample, int>());
        Should.Throw<ArgumentException>(() => typeof(Sample).GetProperty("Item").GetValueGetter<Sample, int>());
        Should.Throw<MemberAccessException>(() => typeof(Sample).GetProperty(nameof(Sample.PrivateSet)).GetValueSetter<Sample, int>());
        Should.Throw<MemberAccessException>(() => typeof(Sample).GetProperty(nameof(Sample.PrivateGet)).GetValueGetter<Sample, int>());
        Should.Throw<InvalidOperationException>(() => typeof(Sample).GetProperty(nameof(Sample.ReadOnly)).GetValueSetter<Sample, int>());
        Should.Throw<InvalidOperationException>(() => typeof(Sample).GetProperty(nameof(Sample.InitOnly)).GetValueSetter<Sample, int>());
    }

    [Fact]
    public void TypedAccessors_ConcurrentRequests_ReuseOneDelegate()
    {
        var property = typeof(ConcurrentSample).GetProperty(nameof(ConcurrentSample.Value));
        var getters = new Func<ConcurrentSample, int>[32];
        var setters = new Action<ConcurrentSample, int>[32];
        Parallel.For(0, 32, index =>
        {
            getters[index] = property.GetValueGetter<ConcurrentSample, int>();
            setters[index] = property.GetValueSetter<ConcurrentSample, int>();
        });
        getters.ShouldAllBe(getter => ReferenceEquals(getter, getters[0]));
        setters.ShouldAllBe(setter => ReferenceEquals(setter, setters[0]));
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void TypedAccessors_CollectibleGenericInstance_DoesNotRootAssembly()
    {
        var weak = CreateCollectibleAccessors();
        for (var attempt = 0; attempt < 20 && weak.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        weak.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateCollectibleAccessors()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TypedAccessor_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var builder = assembly.DefineDynamicModule("Main").DefineType("Derived", TypeAttributes.Public, typeof(Sample));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        var type = builder.CreateTypeInfo().AsType();
        // 使用普通基类的 PropertyInfo，验证泛型缓存不会通过该常驻元数据固定可回收的 T。
        var property = typeof(Sample).GetProperty(nameof(Sample.Number));
        var instance = Activator.CreateInstance(type);
        var factories = typeof(PropertyInfoExtensions).GetMethods().Where(method => method.IsGenericMethodDefinition
            && method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 1).ToArray();
        var getter = (Delegate)factories.Single(method => method.Name == "GetValueGetter").MakeGenericMethod(type, typeof(int)).Invoke(null, new object[] { property });
        var setter = (Delegate)factories.Single(method => method.Name == "GetValueSetter").MakeGenericMethod(type, typeof(int)).Invoke(null, new object[] { property });
        setter.DynamicInvoke(instance, 9);
        getter.DynamicInvoke(instance).ShouldBe(9);
        return new WeakReference(type.Assembly);
    }
#endif

    public interface INumber { int Number { get; set; } }
    public struct StructSample : INumber { public int Number { get; set; } }
    public class Sample : INumber
    {
        public int Number { get; set; }
        public int? Optional { get; set; }
        public string Text { get; set; }
        public int PrivateSet { get; private set; }
        public int PrivateGet { private get; set; }
        public int ReadOnly => 1;
        public int InitOnly { get; init; }
        public static int Static { get; set; }
        public int this[int index] => index;
    }
    public class Derived : Sample { }
    public class ConcurrentSample { public int Value { get; set; } }
}
