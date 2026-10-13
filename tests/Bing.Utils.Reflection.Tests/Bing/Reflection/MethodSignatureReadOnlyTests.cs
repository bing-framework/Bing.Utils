using Shouldly;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Bing.Reflection;

/// <summary>只读引用参数不能与可写引用参数混淆。</summary>
public class MethodSignatureReadOnlyTests
{
    [Fact]
    public void FindMethod_ReadOnlyReference_RequiresInSignature()
    {
        TypeVisit.FindMethod(typeof(InTarget), nameof(InTarget.Accept), 0,
            new MethodParameterSignature(typeof(int), ParameterPassingKind.In)).ShouldNotBeNull();
        Should.Throw<MissingMethodException>(() => TypeVisit.FindMethod(typeof(InTarget), nameof(InTarget.Accept), 0,
            new MethodParameterSignature(typeof(int), ParameterPassingKind.Ref)));
    }

    [Fact]
    public void FindMethod_ReadOnlyRequiredModifierWithoutInFlag_RequiresInSignature()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"ReadOnlyModifierProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("Main").DefineType("Target", TypeAttributes.Public);
        var methodBuilder = builder.DefineMethod(
            "Accept",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(void),
            null,
            null,
            new[] { typeof(int).MakeByRefType() },
            new[] { new[] { typeof(IsReadOnlyAttribute) } },
            null);
        methodBuilder.GetILGenerator().Emit(OpCodes.Ret);

        var type = builder.CreateTypeInfo()!.AsType();
        var method = type.GetMethod("Accept", BindingFlags.Public | BindingFlags.Static)!;
        var parameter = method.GetParameters().Single();
        parameter.ParameterType.IsByRef.ShouldBeTrue();
        parameter.IsIn.ShouldBeFalse();
        parameter.GetRequiredCustomModifiers().ShouldContain(typeof(IsReadOnlyAttribute));

        TypeVisit.FindMethod(type, "Accept", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            0, new MethodParameterSignature(typeof(int), ParameterPassingKind.In)).ShouldBe(method);
        Should.Throw<MissingMethodException>(() => TypeVisit.FindMethod(type, "Accept",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            0, new MethodParameterSignature(typeof(int), ParameterPassingKind.Ref)));
    }

    [Fact]
    public void GetMethodBySignature_InAndRef_DoNotShareMatch()
    {
        var inMethod = typeof(InTarget).GetMethod(nameof(InTarget.Accept))!;
        var refMethod = typeof(RefTarget).GetMethod(nameof(RefTarget.Accept))!;
        TypeVisit.IsSignatureCompatible(inMethod, refMethod).ShouldBeFalse();
        Should.Throw<MissingMethodException>(() => TypeVisit.GetMethodBySignature(typeof(InTarget), refMethod));
        TypeVisit.GetMethodBySignature(typeof(InTarget), inMethod).ShouldBe(inMethod);
    }

    private sealed class InTarget { public void Accept(in int value) { } }
    private sealed class RefTarget { public void Accept(ref int value) { } }

    private sealed class ReturnTarget
    {
        public static int ReadInt() => 1;
        public static string ReadText() => string.Empty;
    }

    [Theory]
    [InlineData(nameof(ReturnTarget.ReadInt), typeof(int))]
    [InlineData(nameof(ReturnTarget.ReadText), typeof(string))]
    public void FindMethodByReturnType_Parameterized_ReturnTypeMatches(string name, Type returnType)
    {
        TypeVisit.FindMethodByReturnType(typeof(ReturnTarget), name, returnType).ReturnType.ShouldBe(returnType);
    }

    [Fact]
    public void FindMethodByReturnType_NullReturnType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeVisit.FindMethodByReturnType(typeof(ReturnTarget), nameof(ReturnTarget.ReadInt), null));
    }

    [Fact]
    public void GetMethodBySignature_ModifierOnlyOverloads_KeepDistinctCacheEntries()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ModifierProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("Main").DefineType("Target", TypeAttributes.Public);
        foreach (var modifiers in new[] { Type.EmptyTypes, new[] { typeof(IsVolatile) } })
        {
            var method = builder.DefineMethod("Accept", MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard, typeof(void), null, null, new[] { typeof(int) }, new[] { modifiers }, null);
            method.GetILGenerator().Emit(OpCodes.Ret);
        }
        var type = builder.CreateTypeInfo()!.AsType();
        var methods = type.GetMethods().Where(method => method.Name == "Accept").ToArray();
        TypeVisit.IsSignatureCompatible(methods[0], methods[1]).ShouldBeFalse();
        foreach (var method in methods)
            TypeVisit.GetMethodBySignature(type, method).ShouldBe(method);
    }

    [Fact]
    public void FindMethodByReturnType_DistinguishesSameParametersWithDifferentReturns()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ReturnProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("Main").DefineType("Target", TypeAttributes.Public);
        var intMethodBuilder = builder.DefineMethod("Read", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, typeof(int), Type.EmptyTypes);
        var intIl = intMethodBuilder.GetILGenerator();
        intIl.Emit(OpCodes.Ldc_I4, 7);
        intIl.Emit(OpCodes.Ret);
        var stringMethodBuilder = builder.DefineMethod("Read", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, typeof(string), Type.EmptyTypes);
        var stringIl = stringMethodBuilder.GetILGenerator();
        stringIl.Emit(OpCodes.Ldnull);
        stringIl.Emit(OpCodes.Ret);

        var type = builder.CreateTypeInfo()!.AsType();
        var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        Should.Throw<AmbiguousMatchException>(() => TypeVisit.FindMethod(type, "Read", flags, 0));

        var intMethod = TypeVisit.FindMethodByReturnType(type, "Read", typeof(int), flags, 0);
        var stringMethod = TypeVisit.FindMethodByReturnType(type, "Read", typeof(string), flags, 0);
        intMethod.ShouldNotBe(stringMethod);
        TypeVisit.FindMethodByReturnType(type, "Read", typeof(int), flags, 0).ShouldBe(intMethod);
        TypeVisit.FindMethodByReturnType(type, "Read", typeof(string), flags, 0).ShouldBe(stringMethod);
    }

    [Fact]
    public void FindMethodByReturnType_BindsGenericReturnTypeFromParameters()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"GenericReturnProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("Main").DefineType("Target", TypeAttributes.Public);
        var methodBuilder = builder.DefineMethod("Echo", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard);
        var genericArguments = methodBuilder.DefineGenericParameters("T");
        methodBuilder.SetReturnType(genericArguments[0]);
        methodBuilder.SetParameters(genericArguments[0]);
        methodBuilder.GetILGenerator().Emit(OpCodes.Ldarg_0);
        methodBuilder.GetILGenerator().Emit(OpCodes.Ret);

        var type = builder.CreateTypeInfo()!.AsType();
        var method = TypeVisit.FindMethodByReturnType(type, "Echo", typeof(string),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, 1,
            new MethodParameterSignature(typeof(string)));
        method.ShouldNotBeNull();
        method.ReturnType.IsGenericParameter.ShouldBeTrue();
    }
}
