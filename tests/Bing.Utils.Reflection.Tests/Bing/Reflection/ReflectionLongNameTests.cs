using System.Reflection;
using System.Reflection.Emit;
using Bing.Dynamic;
using Shouldly;

namespace Bing.Reflection;

/// <summary>合法的长元数据名称仍可查询，超长请求不被缓存持有。</summary>
public class ReflectionLongNameTests
{
    [Fact]
    public void FindMethod_LongValidName_ReturnsSameMetadataWithoutRestrictingLookup()
    {
        var name = new string('M', 16384);
        var builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"LongMethod_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run).DefineDynamicModule("Main").DefineType("Sample", TypeAttributes.Public);
        var method = builder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        method.GetILGenerator().Emit(OpCodes.Ret);
        var type = builder.CreateTypeInfo()!.AsType();
        var metadata = type.GetMethod(name)!;
        for (var index = 0; index < 3; index++)
        {
            TypeVisit.FindMethod(type, name, Type.EmptyTypes).ShouldBe(metadata);
            TypeVisit.GetMethodBySignature(type, metadata).ShouldBe(metadata);
        }
    }

    [Fact]
    public void GetMethodBySignature_StructuralKeyOver8192_BypassesStructuralCacheAndFindsTarget()
    {
        var name = new string('S', 9000);
        var sourceBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"LongSignatureSource_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("Main")
            .DefineType("Source", TypeAttributes.Public);
        var targetBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"LongSignatureTarget_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("Main")
            .DefineType("Target", TypeAttributes.Public);
        DefineVoidMethod(sourceBuilder, name);
        DefineVoidMethod(targetBuilder, name);
        var sourceType = sourceBuilder.CreateTypeInfo()!.AsType();
        var targetType = targetBuilder.CreateTypeInfo()!.AsType();
        var sourceMethod = sourceType.GetMethod(name)!;
        var expected = targetType.GetMethod(name)!;

        var wasEnabled = ReflectionCacheMetrics.Enabled;
        ReflectionCacheMetrics.Enabled = true;
        try
        {
            var before = ReflectionCacheMetrics.StructuralBypassCount;
            TypeVisit.GetMethodBySignature(targetType, sourceMethod).ShouldBe(expected);
            (ReflectionCacheMetrics.StructuralBypassCount - before).ShouldBe(1);
        }
        finally
        {
            ReflectionCacheMetrics.Enabled = wasEnabled;
        }
    }

    private static void DefineVoidMethod(TypeBuilder builder, string name)
    {
        var method = builder.DefineMethod(name, MethodAttributes.Public, typeof(void), Type.EmptyTypes);
        method.GetILGenerator().Emit(OpCodes.Ret);
    }

    [Fact]
    public void DynamicBase_LongValidPropertyName_RemainsReadableAndWritable()
    {
        var name = new string('P', 2048);
        var builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"LongProperty_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Main").DefineType("Sample", TypeAttributes.Public, typeof(DynamicBase));
        var field = builder.DefineField("value", typeof(int), FieldAttributes.Private);
        var property = builder.DefineProperty(name, PropertyAttributes.None, typeof(int), null);
        var getter = builder.DefineMethod($"get_{name}", MethodAttributes.Public | MethodAttributes.SpecialName, typeof(int), Type.EmptyTypes);
        var getterIl = getter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldarg_0);
        getterIl.Emit(OpCodes.Ldfld, field);
        getterIl.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);
        var setter = builder.DefineMethod($"set_{name}", MethodAttributes.Public | MethodAttributes.SpecialName, typeof(void), new[] { typeof(int) });
        var setterIl = setter.GetILGenerator();
        setterIl.Emit(OpCodes.Ldarg_0);
        setterIl.Emit(OpCodes.Ldarg_1);
        setterIl.Emit(OpCodes.Stfld, field);
        setterIl.Emit(OpCodes.Ret);
        property.SetSetMethod(setter);
        var type = builder.CreateTypeInfo()!.AsType();
        var sample = (DynamicBase)Activator.CreateInstance(type)!;
        sample.SetPropertyValue(name, 17);
        sample.GetPropertyValue(name).ShouldBe(17);
        Should.Throw<MissingMemberException>(() => sample.GetPropertyValue(new string('X', 2048)));
    }
}
