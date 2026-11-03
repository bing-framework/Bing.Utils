using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Bing.Reflection;

/// <summary>验证旧 object 访问器的缓存复用和可回收类型生命周期。</summary>
public class LegacyAccessorLifetimeTests
{
    [Fact]
    public void LegacyAccessors_OrdinaryGenericAndObjectCalls_ReuseValidatedDelegates()
    {
        var property = typeof(Base).GetProperty(nameof(Base.Value))!;
        property.GetValueGetter<Base>().ShouldBeSameAs(property.GetValueGetter<Base>());
        property.GetValueSetter<Base>().ShouldBeSameAs(property.GetValueSetter<Base>());
        property.GetValueGetter().ShouldBeSameAs(property.GetValueGetter());
        property.GetValueSetter().ShouldBeSameAs(property.GetValueSetter());

        var valueProperty = typeof(IValue).GetProperty(nameof(IValue.Value))!;
        valueProperty.GetValueSetterByRef<ValueStruct>().ShouldBeSameAs(
            valueProperty.GetValueSetterByRef<ValueStruct>());
    }

#if NET6_0_OR_GREATER
    [Theory]
    [InlineData("GetValueGetter")]
    [InlineData("GetValueSetter")]
    public void GenericObjectAccessor_CollectibleInstance_DoesNotRootAssembly(string accessorName)
    {
        var assembly = CreateCollectibleReferenceAccessor(accessorName);

        CollectUntilReleased(assembly);

        assembly.IsAlive.ShouldBeFalse();
    }

    [Fact]
    public void RefSetter_CollectibleValueType_DoesNotRootAssembly()
    {
        var assembly = CreateCollectibleRefSetter();

        CollectUntilReleased(assembly);

        assembly.IsAlive.ShouldBeFalse();
    }

    private static void CollectUntilReleased(WeakReference assembly)
    {
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateCollectibleReferenceAccessor(string accessorName)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("LegacyAccessor_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var builder = assembly.DefineDynamicModule("Main").DefineType("Derived", TypeAttributes.Public, typeof(Base));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        var type = builder.CreateTypeInfo()!.AsType();
        var property = typeof(Base).GetProperty(nameof(Base.Value))!;
        var instance = Activator.CreateInstance(type);
        var factory = typeof(PropertyInfoExtensions).GetMethods().Single(method =>
            method.Name == accessorName && method.IsGenericMethodDefinition
            && method.GetGenericArguments().Length == 1 && method.GetParameters().Length == 1);
        var accessor = (Delegate)factory.MakeGenericMethod(type).Invoke(null, new object[] { property })!;
        if (accessorName == "GetValueGetter")
            accessor.DynamicInvoke(instance).ShouldBe(3);
        else
            accessor.DynamicInvoke(instance, 7);
        return new WeakReference(type.Assembly);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateCollectibleRefSetter()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("LegacyRefSetter_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule("Main");
        var builder = module.DefineType("DynamicValue", TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(ValueType), new[] { typeof(IValue) });
        var field = builder.DefineField("_value", typeof(int), FieldAttributes.Private);
        var getter = builder.DefineMethod("get_Value",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
            | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(int), Type.EmptyTypes);
        var getIl = getter.GetILGenerator();
        getIl.Emit(OpCodes.Ldarg_0);
        getIl.Emit(OpCodes.Ldfld, field);
        getIl.Emit(OpCodes.Ret);
        var setter = builder.DefineMethod("set_Value",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
            | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(void), new[] { typeof(int) });
        var setIl = setter.GetILGenerator();
        setIl.Emit(OpCodes.Ldarg_0);
        setIl.Emit(OpCodes.Ldarg_1);
        setIl.Emit(OpCodes.Stfld, field);
        setIl.Emit(OpCodes.Ret);
        var propertyBuilder = builder.DefineProperty("Value", PropertyAttributes.None, typeof(int), Type.EmptyTypes);
        propertyBuilder.SetGetMethod(getter);
        propertyBuilder.SetSetMethod(setter);
        builder.DefineMethodOverride(getter, typeof(IValue).GetProperty(nameof(IValue.Value))!.GetMethod!);
        builder.DefineMethodOverride(setter, typeof(IValue).GetProperty(nameof(IValue.Value))!.SetMethod!);

        var type = builder.CreateTypeInfo()!.AsType();
        var property = typeof(IValue).GetProperty(nameof(IValue.Value))!;
        var factory = typeof(PropertyInfoExtensions).GetMethods().Single(method =>
            method.Name == "GetValueSetterByRef" && method.IsGenericMethodDefinition
            && method.GetGenericArguments().Length == 1 && method.GetParameters().Length == 1);
        var accessor = (Delegate)factory.MakeGenericMethod(type).Invoke(null, new object[] { property })!;
        var arguments = new[] { Activator.CreateInstance(type), (object)9 };
        accessor.DynamicInvoke(arguments);
        property.GetValue(arguments[0]).ShouldBe(9);
        return new WeakReference(type.Assembly);
    }
#endif

    public class Base
    {
        public int Value { get; set; } = 3;
    }

    public interface IValue
    {
        int Value { get; set; }
    }

    public struct ValueStruct : IValue
    {
        public int Value { get; set; }
    }
}
