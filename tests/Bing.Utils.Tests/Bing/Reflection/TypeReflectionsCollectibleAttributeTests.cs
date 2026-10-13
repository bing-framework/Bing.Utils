using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Shouldly;

namespace Bing.Reflection;

/// <summary>特性读取不能通过第三方缓存保留可回收元数据。</summary>
public class TypeReflectionsCollectibleAttributeTests
{
    [Theory]
    [InlineData(ReflectionOptions.Default)]
    [InlineData(ReflectionOptions.Inherit)]
    public void IsAttributeDefined_InheritedThrowingAttribute_DoesNotConstructAttribute(ReflectionOptions options)
    {
        var target = options == ReflectionOptions.Inherit ? typeof(DerivedTagged) : typeof(Tagged);
        TypeReflections.IsAttributeDefined<ThrowingAttribute>(target, options).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(target, typeof(ThrowingAttribute), options).ShouldBeTrue();
    }

    [Fact]
    public void AttributeQueries_CollectibleMembersAndParameters_AllowAssemblyCollection()
    {
        var assembly = QueryCollectibleMembers();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        assembly.IsAlive.ShouldBeFalse();
    }

    [Fact]
    public void AttributeQueries_InheritedMembersReflectedFromCollectibleType_AllowAssemblyCollection()
    {
        var assembly = QueryCollectibleInheritedMembers();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        assembly.IsAlive.ShouldBeFalse();
    }

    [Fact]
    public void ReflectionMetadataCaches_CollectibleGenericArgument_DoesNotPinAssembly()
    {
        var assembly = QueryCollectibleGenericArgumentCaches();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        assembly.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueryCollectibleGenericArgumentCaches()
    {
        var builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"CacheProbe_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var elementType = builder.DefineDynamicModule("Main")
            .DefineType("Element", TypeAttributes.Public)
            .CreateTypeInfo()!
            .AsType();
        var closedType = typeof(List<>).MakeGenericType(elementType);

        TypeReflections.TypeCacheManager.GetTypeProperties(closedType);
        TypeReflections.TypeCacheManager.GetTypeFields(closedType);
        TypeReflections.TypeCacheManager.GetTypeMethods(closedType);
        TypeReflections.TypeCacheManager.GetTypeProperties(closedType,
            BindingFlags.Public | BindingFlags.Instance);
        TypeReflections.GetPublicInstanceProperties(closedType).ToArray();
        TypeVisit.GetProperties(closedType, PropertyAccessOptions.Getters).ToArray();
        Reflections.HasMemberPath(closedType, "Count").ShouldBeTrue();

        var count = closedType.GetProperty("Count")!;
        count.GetValueGetter();
        return new WeakReference(elementType.Assembly);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AttributeQueries_UnsupportedEvent_HaveSameFailureContract(bool collectible)
    {
        var type = collectible ? CreateCollectibleDerivedType() : typeof(AttributeBase);
        var member = type.GetEvent(nameof(AttributeBase.Changed))!;
        Should.Throw<InvalidOperationException>(() => TypeReflections.IsAttributeDefined<MarkerAttribute>(member));
        Should.Throw<InvalidOperationException>(() => TypeReflections.GetAttribute<MarkerAttribute>(member));
        Should.Throw<InvalidOperationException>(() => TypeReflections.GetAttributes<MarkerAttribute>(member));
        Should.Throw<InvalidOperationException>(() => TypeReflections.GetAttributes(member));
        foreach (var options in new[] { ReflectionOptions.Default, ReflectionOptions.Inherit })
        {
            Should.Throw<InvalidOperationException>(() => TypeReflections.IsAttributeDefined<MarkerAttribute>(member, options));
            Should.Throw<InvalidOperationException>(() => TypeReflections.IsAttributeDefined(member, typeof(MarkerAttribute), options));
            Should.Throw<InvalidOperationException>(() => TypeReflections.GetAttribute<MarkerAttribute>(member, options));
            Should.Throw<InvalidOperationException>(() => TypeReflections.GetAttribute(member, typeof(MarkerAttribute), options));
            Should.Throw<InvalidOperationException>(() => TypeReflections.GetAttributes<MarkerAttribute>(member, options));
            Should.Throw<InvalidOperationException>(() => TypeReflections.GetAttributes(member, typeof(MarkerAttribute), options));
            Should.Throw<InvalidOperationException>(() => TypeReflections.GetAttributes(member, options));
        }
    }

    [Fact]
    public void ReflectionsAttributeQueries_UnsupportedEvent_UsePublicMemberInfoFallback()
    {
        var member = typeof(AttributeBase).GetEvent(nameof(AttributeBase.Changed))!;

        Reflections.GetAttribute<MarkerAttribute>(member).ShouldBeNull();
        Reflections.GetAttribute(member, typeof(MarkerAttribute)).ShouldBeNull();
        Reflections.GetAttributes<MarkerAttribute>(member).ShouldBeEmpty();
        Reflections.GetAttributes(member, typeof(MarkerAttribute)).ShouldBeEmpty();
        Should.Throw<ArgumentException>(() => Reflections.GetRequiredAttribute<MarkerAttribute>(member));
        Should.Throw<ArgumentException>(() => Reflections.GetRequiredAttribute(member, typeof(MarkerAttribute)));
        Should.Throw<ArgumentException>(() => Reflections.GetRequiredAttributes<MarkerAttribute>(member).ToArray());
        Should.Throw<ArgumentException>(() => Reflections.GetRequiredAttributes(member, typeof(MarkerAttribute)).ToArray());
    }

    [Fact]
    public void ReflectionsRequiredAttributeCollections_RejectEmptyResultsForAllMetadataOverloads()
    {
        var field = typeof(AttributeBase).GetField(nameof(AttributeBase.Unmarked))!;
        var type = typeof(AttributeBase);
        var typeInfo = type.GetTypeInfo();
        var attributeType = typeof(MarkerAttribute);

        Action[] genericQueries =
        {
            () => Reflections.GetRequiredAttributes<MarkerAttribute>(field).ToArray(),
            () => Reflections.GetRequiredAttributes<MarkerAttribute>((MemberInfo)field).ToArray(),
            () => Reflections.GetRequiredAttributes<MarkerAttribute>(type).ToArray(),
            () => Reflections.GetRequiredAttributes<MarkerAttribute>(typeInfo).ToArray()
        };
        Action[] nonGenericQueries =
        {
            () => Reflections.GetRequiredAttributes(field, attributeType).ToArray(),
            () => Reflections.GetRequiredAttributes((MemberInfo)field, attributeType).ToArray(),
            () => Reflections.GetRequiredAttributes(type, attributeType).ToArray(),
            () => Reflections.GetRequiredAttributes(typeInfo, attributeType).ToArray()
        };

        foreach (var query in genericQueries.Concat(nonGenericQueries))
            Should.Throw<ArgumentException>(query);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AttributeQueries_InvalidAttributeType_HaveSameFailureContract(bool collectible)
    {
        var type = collectible ? CreateCollectibleDerivedType() : typeof(AttributeBase);
        var member = type.GetMethod(nameof(AttributeBase.Accept))!;
        var parameter = member.GetParameters()[0];
        foreach (var attributeType in new[] { typeof(string), typeof(OpenAttribute<>) })
        {
            Action[] queries =
            {
                () => TypeReflections.IsAttributeDefined(member, attributeType),
                () => TypeReflections.GetAttribute(member, attributeType),
                () => TypeReflections.GetAttributes(member, attributeType),
                () => TypeReflections.IsAttributeDefined(parameter, attributeType),
                () => TypeReflections.GetAttribute(parameter, attributeType),
                () => TypeReflections.GetAttributes(parameter, attributeType)
            };
            foreach (var query in queries)
                Should.Throw<ArgumentException>(query).ParamName.ShouldBe("attributeType");
            foreach (var options in new[] { ReflectionOptions.Default, ReflectionOptions.Inherit })
            {
                Action[] optionQueries =
                {
                    () => TypeReflections.IsAttributeDefined(member, attributeType, options),
                    () => TypeReflections.GetAttribute(member, attributeType, options),
                    () => TypeReflections.GetAttributes(member, attributeType, options),
                    () => TypeReflections.IsAttributeDefined(parameter, attributeType, options),
                    () => TypeReflections.GetAttribute(parameter, attributeType, options),
                    () => TypeReflections.GetAttributes(parameter, attributeType, options)
                };
                foreach (var query in optionQueries)
                    Should.Throw<ArgumentException>(query).ParamName.ShouldBe("attributeType");
            }
        }
    }

    [Fact]
    public void AttributeQueries_InvalidOptions_AreRejectedBeforeAttributeConstruction()
    {
        var member = (MemberInfo)typeof(Tagged);
        var parameter = typeof(AttributeBase).GetMethod(nameof(AttributeBase.Accept))!.GetParameters()[0];
        var invalidReflectionOptions = (ReflectionOptions)42;
        var invalidAmbiguousOptions = (ReflectionAmbiguousOptions)42;

        Action[] invalidReflectionQueries =
        {
            () => TypeReflections.IsAttributeDefined<ThrowingAttribute>(member, invalidReflectionOptions),
            () => TypeReflections.IsAttributeDefined<ThrowingAttribute>(parameter, invalidReflectionOptions),
            () => TypeReflections.IsAttributeDefined(member, typeof(ThrowingAttribute), invalidReflectionOptions),
            () => TypeReflections.IsAttributeDefined(parameter, typeof(ThrowingAttribute), invalidReflectionOptions),
            () => TypeReflections.GetAttribute<ThrowingAttribute>(member, invalidReflectionOptions),
            () => TypeReflections.GetAttribute<ThrowingAttribute>(parameter, invalidReflectionOptions),
            () => TypeReflections.GetAttribute(member, typeof(ThrowingAttribute), invalidReflectionOptions),
            () => TypeReflections.GetAttribute(parameter, typeof(ThrowingAttribute), invalidReflectionOptions),
            () => TypeReflections.GetAttributes<ThrowingAttribute>(member, invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributes<ThrowingAttribute>(parameter, invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributes(member, typeof(ThrowingAttribute), invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributes(parameter, typeof(ThrowingAttribute), invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributes(member, invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributes(parameter, invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributeRequired<ThrowingAttribute>(member, invalidReflectionOptions),
            () => TypeReflections.GetAttributeRequired<ThrowingAttribute>(parameter, invalidReflectionOptions),
            () => TypeReflections.GetAttributeRequired(member, typeof(ThrowingAttribute), invalidReflectionOptions),
            () => TypeReflections.GetAttributeRequired(parameter, typeof(ThrowingAttribute), invalidReflectionOptions),
            () => TypeReflections.GetAttributesRequired<ThrowingAttribute>(member, invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributesRequired<ThrowingAttribute>(parameter, invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributesRequired(member, typeof(ThrowingAttribute), invalidReflectionOptions).ToArray(),
            () => TypeReflections.GetAttributesRequired(parameter, typeof(ThrowingAttribute), invalidReflectionOptions).ToArray()
        };
        foreach (var query in invalidReflectionQueries)
            Should.Throw<ArgumentOutOfRangeException>(query);

        Action[] invalidAmbiguousQueries =
        {
            () => TypeReflections.GetAttribute<ThrowingAttribute>(member, ReflectionOptions.Default, invalidAmbiguousOptions),
            () => TypeReflections.GetAttribute<ThrowingAttribute>(parameter, ReflectionOptions.Default, invalidAmbiguousOptions),
            () => TypeReflections.GetAttribute(member, typeof(ThrowingAttribute), ReflectionOptions.Default, invalidAmbiguousOptions),
            () => TypeReflections.GetAttribute(parameter, typeof(ThrowingAttribute), ReflectionOptions.Default, invalidAmbiguousOptions),
            () => TypeReflections.GetAttributeRequired<ThrowingAttribute>(member, ReflectionOptions.Default, invalidAmbiguousOptions),
            () => TypeReflections.GetAttributeRequired<ThrowingAttribute>(parameter, ReflectionOptions.Default, invalidAmbiguousOptions),
            () => TypeReflections.GetAttributeRequired(member, typeof(ThrowingAttribute), ReflectionOptions.Default, invalidAmbiguousOptions),
            () => TypeReflections.GetAttributeRequired(parameter, typeof(ThrowingAttribute), ReflectionOptions.Default, invalidAmbiguousOptions)
        };
        foreach (var query in invalidAmbiguousQueries)
            Should.Throw<ArgumentOutOfRangeException>(query);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueryCollectibleInheritedMembers()
    {
        var type = CreateCollectibleDerivedType();
        MemberInfo[] members = { type.GetField(nameof(AttributeBase.Value))!,
            type.GetProperty(nameof(AttributeBase.Count))!, type.GetMethod(nameof(AttributeBase.Accept))! };
        foreach (var member in members)
        {
            member.DeclaringType.ShouldBe(typeof(AttributeBase));
            member.ReflectedType.ShouldBe(type);
            TypeReflections.IsAttributeDefined<MarkerAttribute>(member).ShouldBeTrue();
            TypeReflections.GetAttribute<MarkerAttribute>(member).ShouldNotBeNull();
            TypeReflections.GetAttributes<MarkerAttribute>(member).Count().ShouldBe(1);
            TypeReflections.GetAttributes(member).OfType<MarkerAttribute>().Count().ShouldBe(1);
            Reflections.GetAttribute<MarkerAttribute>(member).ShouldNotBeNull();
            Reflections.GetAttributes<MarkerAttribute>(member).Count().ShouldBe(1);
            Reflections.GetRequiredAttribute<MarkerAttribute>(member).ShouldNotBeNull();
            Reflections.GetRequiredAttributes<MarkerAttribute>(member).Count().ShouldBe(1);
        }
        var parameter = type.GetMethod(nameof(AttributeBase.Accept))!.GetParameters()[0];
        TypeReflections.IsAttributeDefined<MarkerAttribute>(parameter).ShouldBeTrue();
        TypeReflections.GetAttribute<MarkerAttribute>(parameter).ShouldNotBeNull();
        TypeReflections.GetAttributes<MarkerAttribute>(parameter).Count().ShouldBe(1);
        TypeReflections.GetAttributes(parameter).OfType<MarkerAttribute>().Count().ShouldBe(1);
        return new WeakReference(type.Assembly);
    }

    private static Type CreateCollectibleDerivedType()
    {
        var builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"InheritedProbe_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        return builder.DefineDynamicModule("Main")
            .DefineType("Derived", TypeAttributes.Public, typeof(AttributeBase)).CreateTypeInfo()!.AsType();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueryCollectibleMembers()
    {
        var builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"AttributeProbe_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var typeBuilder = builder.DefineDynamicModule("Main").DefineType("Tagged", TypeAttributes.Public);
        var attribute = new CustomAttributeBuilder(typeof(ObsoleteAttribute).GetConstructor(Type.EmptyTypes)!, Array.Empty<object>());
        typeBuilder.SetCustomAttribute(attribute);
        var field = typeBuilder.DefineField("Value", typeof(int), FieldAttributes.Public);
        field.SetCustomAttribute(attribute);
        var constructor = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
        constructor.SetCustomAttribute(attribute);
        var method = typeBuilder.DefineMethod("Accept", MethodAttributes.Public | MethodAttributes.Static,
            typeof(void), new[] { typeof(int) });
        method.SetCustomAttribute(attribute);
        method.DefineParameter(1, ParameterAttributes.None, "value").SetCustomAttribute(attribute);
        method.GetILGenerator().Emit(OpCodes.Ret);
        var getter = typeBuilder.DefineMethod("get_Count", MethodAttributes.Public | MethodAttributes.SpecialName,
            typeof(int), Type.EmptyTypes);
        getter.GetILGenerator().Emit(OpCodes.Ldc_I4_0);
        getter.GetILGenerator().Emit(OpCodes.Ret);
        var property = typeBuilder.DefineProperty("Count", PropertyAttributes.None, typeof(int), null);
        property.SetGetMethod(getter);
        property.SetCustomAttribute(attribute);
        var type = typeBuilder.CreateTypeInfo()!.AsType();
        MemberInfo[] members = { type, type.GetField("Value")!, type.GetConstructor(Type.EmptyTypes)!,
            type.GetMethod("Accept")!, type.GetProperty("Count")! };
        foreach (var member in members)
        {
            TypeReflections.IsAttributeDefined<ObsoleteAttribute>(member).ShouldBeTrue();
            TypeReflections.GetAttribute(member, typeof(ObsoleteAttribute)).ShouldNotBeNull();
            TypeReflections.GetAttributes<ObsoleteAttribute>(member).Count().ShouldBe(1);
            TypeReflections.GetAttributes(member).OfType<ObsoleteAttribute>().Count().ShouldBe(1);
            Reflections.GetAttribute<ObsoleteAttribute>(member).ShouldNotBeNull();
            Reflections.GetAttributes<ObsoleteAttribute>(member).Count().ShouldBe(1);
            Reflections.GetRequiredAttribute<ObsoleteAttribute>(member).ShouldNotBeNull();
            Reflections.GetRequiredAttributes<ObsoleteAttribute>(member).Count().ShouldBe(1);
        }
        var parameter = type.GetMethod("Accept")!.GetParameters()[0];
        TypeReflections.IsAttributeDefined<ObsoleteAttribute>(parameter).ShouldBeTrue();
        TypeReflections.GetAttribute(parameter, typeof(ObsoleteAttribute)).ShouldNotBeNull();
        TypeReflections.GetAttributes<ObsoleteAttribute>(parameter).Count().ShouldBe(1);
        TypeReflections.GetAttributes(parameter).OfType<ObsoleteAttribute>().Count().ShouldBe(1);

        // 正常程序集中的封闭泛型方法也可能携带可回收类型实参。
        var closedMethod = typeof(TypeReflectionsCollectibleAttributeTests).GetMethod(nameof(GenericTarget))!.MakeGenericMethod(type);
        TypeReflections.IsAttributeDefined(closedMethod, typeof(ObsoleteAttribute)).ShouldBeFalse();
        TypeReflections.GetAttributes(closedMethod).ShouldBeEmpty();
        TypeReflections.IsAttributeDefined(closedMethod.GetParameters()[0], typeof(ObsoleteAttribute)).ShouldBeFalse();
        return new WeakReference(type.Assembly);
    }

    public static void GenericTarget<T>(T value) { }

    public class AttributeBase
    {
        [Marker] public int Value;
        public int Unmarked;
        [Marker] public int Count { get; set; }
        [Marker] public void Accept([Marker] int value) { }
        public event EventHandler Changed { add { } remove { } }
    }

    public sealed class MarkerAttribute : Attribute;
    public sealed class OpenAttribute<T> : Attribute;

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    private sealed class ThrowingAttribute : Attribute
    {
        public ThrowingAttribute() => throw new InvalidOperationException("存在性查询不应构造特性。");
    }

    [Throwing]
    private class Tagged;
    private sealed class DerivedTagged : Tagged;
}
