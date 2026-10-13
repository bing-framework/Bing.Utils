using AspectCore.Extensions.Reflection;
using Shouldly;

namespace Bing.Reflection;

public class TypeReflectionsAttributeInheritanceTests
{
#if NET5_0_OR_GREATER
    [Fact]
    public void ReflectionRuntimeFeatures_ReportsCurrentRuntimeCapability()
    {
        var helperType = typeof(TypeReflections).Assembly
            .GetType("Bing.Reflection.ReflectionRuntimeFeatures")!;
        var property = helperType.GetProperty("IsDynamicCodeSupported",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        property.GetValue(null).ShouldBe(System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported);
    }
#endif

    [Fact]
    public void GetAttributes_NonGenericInheritedOverload_ReturnsAttributeSequence()
    {
        var attributes = TypeReflections.GetAttributes(typeof(DerivedSample),
            typeof(InspectionAttribute), ReflectionOptions.Inherit).ToArray();

        attributes.Length.ShouldBe(1);
        attributes[0].ShouldBeOfType<InspectionAttribute>();
    }

    [Fact]
    public void GetRuntimeTypeHandles_WhenTokensFieldReadFails_UsesPublicReflectorApi()
    {
        var reflector = typeof(BaseSample).GetReflector().CustomAttributeReflectors.Single();
        var unrelatedField = typeof(FieldReadFailureSample).GetField("Value",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var helperType = typeof(TypeReflections).Assembly.GetType("Bing.Reflection.TypeReflectorHelper");
        var fallback = helperType!.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method => method.Name == "GetRuntimeTypeHandles" && method.GetParameters().Length == 2);

        var handles = (HashSet<RuntimeTypeHandle>)fallback.Invoke(null, new object[] { unrelatedField!, reflector })!;

        handles.Contains(typeof(InspectionAttribute).TypeHandle).ShouldBeTrue();
    }

    [Fact]
    public void GetRuntimeTypeHandles_WhenTokensFieldReadFails_DoesNotInstantiateAttribute()
    {
        var reflector = typeof(ThrowingAttributeSample).GetReflector().CustomAttributeReflectors.Single();
        var helperType = typeof(TypeReflections).Assembly.GetType("Bing.Reflection.TypeReflectorHelper")!;
        var fallback = helperType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method => method.Name == "GetRuntimeTypeHandles" && method.GetParameters().Length == 2);

        var handles = (HashSet<RuntimeTypeHandle>)fallback.Invoke(null, new object[] { null!, reflector })!;

        handles.Contains(typeof(ThrowingInspectionAttribute).TypeHandle).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AttributeReflectorQueries_NullEntriesAreTreatedAsMissing(bool nullElement)
    {
        ICustomAttributeReflectorProvider provider = nullElement
            ? new NullElementReflectorProvider()
            : new NullArrayReflectorProvider();
        var type = typeof(InspectionAttribute);
        var methods = typeof(TypeReflections).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);

        var isDefined = methods.Single(method => method.Name == "IsAttributeDefinedImpl")
            .Invoke(null, new object[] { provider, type });
        var attribute = methods.Single(method => method.Name == "GetAttributeImpl")
            .Invoke(null, new object[] { provider, type });
        var filtered = (IEnumerable<Attribute>)methods
            .Single(method => method.Name == "GetAttributesImpl" && method.GetParameters().Length == 2)
            .Invoke(null, new object[] { provider, type });
        var all = (IEnumerable<Attribute>)methods
            .Single(method => method.Name == "GetAttributesImpl" && method.GetParameters().Length == 1)
            .Invoke(null, new object[] { provider });

        isDefined.ShouldBe(false);
        attribute.ShouldBeNull();
        filtered.ShouldBeEmpty();
        all.ShouldBeEmpty();
    }

    [Fact]
    public void ConstructorInfoAttributeOverloads_ReadConstructorAndParameterMetadata()
    {
        var constructor = typeof(ConstructorSample).GetConstructor(new[] { typeof(string) })!;
        var parameter = constructor.GetParameters().Single();

        TypeReflections.GetAttribute<ConstructorInspectionAttribute>(constructor)
            .ShouldNotBeNull();
        TypeReflections.GetAttribute(constructor, typeof(ConstructorInspectionAttribute))
            .ShouldNotBeNull();
        TypeReflections.GetAttributes(constructor, typeof(ConstructorInspectionAttribute))
            .Count().ShouldBe(1);
        TypeReflections.GetAttributeRequired(constructor, typeof(ConstructorInspectionAttribute))
            .ShouldNotBeNull();
        TypeReflections.GetAttributesRequired(constructor, typeof(ConstructorInspectionAttribute))
            .Count().ShouldBe(1);
        TypeReflections.IsAttributeDefined(constructor, typeof(ConstructorInspectionAttribute))
            .ShouldBeTrue();

        TypeReflections.GetAttribute(parameter, typeof(ConstructorInspectionAttribute))
            .ShouldNotBeNull();
        TypeReflections.GetAttributesRequired(parameter, typeof(ConstructorInspectionAttribute))
            .Count().ShouldBe(1);
        TypeReflections.IsAttributeDefined(parameter, typeof(ConstructorInspectionAttribute))
            .ShouldBeTrue();

        Reflections.GetAttribute((MemberInfo)constructor, typeof(ConstructorInspectionAttribute))
            .ShouldNotBeNull();
        Reflections.GetAttributes((MemberInfo)constructor, typeof(ConstructorInspectionAttribute))
            .Count().ShouldBe(1);
        Reflections.GetRequiredAttribute((MemberInfo)constructor, typeof(ConstructorInspectionAttribute))
            .ShouldNotBeNull();
        Reflections.GetRequiredAttributes((MemberInfo)constructor, typeof(ConstructorInspectionAttribute))
            .Count().ShouldBe(1);
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    private sealed class InspectionAttribute : Attribute;

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Parameter, Inherited = true)]
    private sealed class ConstructorInspectionAttribute : Attribute;

    [Inspection]
    private class BaseSample;

    private sealed class DerivedSample : BaseSample;

    private sealed class FieldReadFailureSample
    {
        private int Value;
    }

    private sealed class ThrowingInspectionAttribute : Attribute
    {
        public ThrowingInspectionAttribute() => throw new InvalidOperationException("attribute constructor executed");
    }

    [ThrowingInspection]
    private sealed class ThrowingAttributeSample;

    private sealed class NullArrayReflectorProvider : ICustomAttributeReflectorProvider
    {
        public CustomAttributeReflector[] CustomAttributeReflectors => null;
    }

    private sealed class NullElementReflectorProvider : ICustomAttributeReflectorProvider
    {
        public CustomAttributeReflector[] CustomAttributeReflectors => new CustomAttributeReflector[] { null };
    }

    private sealed class ConstructorSample
    {
        [ConstructorInspection]
        public ConstructorSample([ConstructorInspection] string value)
        {
        }
    }
}
