using System.Reflection.Emit;
using Shouldly;

namespace Bing.Reflection;

/// <summary>核心旧入口与反射子包的构造及参数失败契约保持一致。</summary>
public class ReflectionsStrictContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateInstance_BothEntrances_UseStrictAssignableAndNullBinding(bool legacy)
    {
        Func<Type, object[], object> create = legacy
            ? (type, args) => Reflections.CreateInstance<object>(type, args)
            : (type, args) => TypeVisit.CreateInstance(type, args);
        ((AssignableSample)create(typeof(AssignableSample), new object[] { "value" })).Value.ShouldBe("value");
        ((AssignableSample)create(typeof(AssignableSample), new object[] { null! })).Value.ShouldBeNull();
        ((SpecificitySample)create(typeof(SpecificitySample), new object[] { "value" })).Selected.ShouldBe("comparable");
        ((NullableSample)create(typeof(NullableSample), new object[] { 7 })).Value.ShouldBe(7);
        Should.Throw<ArgumentException>(() => create(typeof(LongSample), new object[] { 1 })).ParamName.ShouldBe("args");
        Should.Throw<ArgumentException>(() => create(typeof(IDisposable), Array.Empty<object>()));
        Should.Throw<AmbiguousMatchException>(() => create(typeof(AmbiguousSample), new object[] { null! }));
        Should.Throw<InvalidOperationException>(() => create(typeof(ThrowingSample), Array.Empty<object>()))
            .InnerException.ShouldBeOfType<ApplicationException>();
    }

    [Fact]
    public void CreateInstance_IncompatibleReturnType_FailsBeforeExecutingConstructor()
    {
        CountedSample.Calls = 0;
        Should.Throw<ArgumentException>(() => Reflections.CreateInstance<string>(typeof(CountedSample)))
            .ParamName.ShouldBe("type");
        CountedSample.Calls.ShouldBe(0);
    }

    [Fact]
    public void CreateInstance_TypeName_ResolvesAndReportsInvalidName()
    {
        Reflections.CreateInstance<object>(typeof(CountedSample).AssemblyQualifiedName!).ShouldBeOfType<CountedSample>();
        Should.Throw<ArgumentNullException>(() => Reflections.CreateInstance<object>((string)null!)).ParamName.ShouldBe("className");
        Should.Throw<ArgumentException>(() => Reflections.CreateInstance<object>(" ")).ParamName.ShouldBe("className");
        Should.Throw<ArgumentException>(() => Reflections.CreateInstance<object>("Unknown.Reflection.Type"))
            .ParamName.ShouldBe("className");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TypePredicates_NullMemberAndProperty_ReportParameterNames(int predicateIndex)
    {
        Func<MemberInfo, bool>[] memberPredicates = { Reflections.IsBool, Reflections.IsEnum, Reflections.IsDate,
            Reflections.IsInt, Reflections.IsNumber };
        Func<PropertyInfo, bool>[] propertyPredicates = { Reflections.IsBool, Reflections.IsEnum, Reflections.IsDate,
            Reflections.IsInt, Reflections.IsNumber };
        Should.Throw<ArgumentNullException>(() => memberPredicates[predicateIndex](null!)).ParamName.ShouldBe("member");
        Should.Throw<ArgumentNullException>(() => propertyPredicates[predicateIndex](null!)).ParamName.ShouldBe("property");
    }

    [Fact]
    public void TypePredicates_FieldMember_UseFieldType()
    {
        var type = typeof(FieldPredicateSample);

        Reflections.IsBool(type.GetField(nameof(FieldPredicateSample.BoolValue))!).ShouldBeTrue();
        Reflections.IsEnum(type.GetField(nameof(FieldPredicateSample.EnumValue))!).ShouldBeTrue();
        Reflections.IsEnum(type.GetField(nameof(FieldPredicateSample.NullableEnumValue))!).ShouldBeTrue();
        Reflections.IsDate(type.GetField(nameof(FieldPredicateSample.DateValue))!).ShouldBeTrue();
        Reflections.IsInt(type.GetField(nameof(FieldPredicateSample.IntValue))!).ShouldBeTrue();
        Reflections.IsNumber(type.GetField(nameof(FieldPredicateSample.DoubleValue))!).ShouldBeTrue();
    }

    [Fact]
    public void TypePredicates_TypeInfoUseRuntimeTypeIdentity()
    {
        var module = System.Reflection.Emit.AssemblyBuilder
            .DefineDynamicAssembly(
                new System.Reflection.AssemblyName($"ReflectionTypePredicateIdentity_{Guid.NewGuid():N}"),
                System.Reflection.Emit.AssemblyBuilderAccess.Run)
            .DefineDynamicModule("Main");

        var fakeBoolean = module.DefineType("System.Boolean").CreateTypeInfo()!;
        var fakeDateTime = module.DefineType("System.DateTime").CreateTypeInfo()!;
        var fakeInt32 = module.DefineType("System.Int32").CreateTypeInfo()!;
        var fakeDouble = module.DefineType("System.Double").CreateTypeInfo()!;

        Reflections.IsBool(fakeBoolean).ShouldBeFalse();
        Reflections.IsDate(fakeDateTime).ShouldBeFalse();
        Reflections.IsInt(fakeInt32).ShouldBeFalse();
        Reflections.IsNumber(fakeDouble).ShouldBeFalse();
    }

    [Fact]
    public void TypeQueries_NullArguments_ThrowExplicitParameterExceptions()
    {
        Action[] queries =
        {
            () => Reflections.IsCollection(null!), () => Reflections.IsGenericCollection(null!),
            () => Reflections.GetElementType(null!), () => Reflections.GetTopBaseType(null!),
            () => Reflections.GetUnderlyingType((Type)null!),
            () => Reflections.GetInterfaceTypes(null!), () => Reflections.GetDirectInterfaceTypes(null!),
            () => Reflections.IsBaseOn(null!, typeof(object))
        };
        foreach (var query in queries)
            Should.Throw<ArgumentNullException>(query).ParamName.ShouldBe("type");
        Should.Throw<ArgumentNullException>(() => Reflections.GetUnderlyingType((TypeInfo)null!))
            .ParamName.ShouldBe("typeInfo");
        Should.Throw<ArgumentNullException>(() => TypeReflections.GetEnumUnderlyingType(null!))
            .ParamName.ShouldBe("type");
        Should.Throw<ArgumentNullException>(() => Reflections.IsBaseOn(typeof(object), null!)).ParamName.ShouldBe("baseType");
        Should.Throw<ArgumentNullException>(() => Reflections.GetPublicProperties(null!)).ParamName.ShouldBe("instance");
        Should.Throw<ArgumentNullException>(() => Reflections.FindImplementTypes(null!, Array.Empty<Assembly>()))
            .ParamName.ShouldBe("findType");
        Should.Throw<ArgumentNullException>(() => Reflections.FindImplementTypes(typeof(object), (Assembly[])null!))
            .ParamName.ShouldBe("assemblies");
        Should.Throw<ArgumentException>(() => Reflections.FindImplementTypes(typeof(object), new Assembly[] { null! }))
            .ParamName.ShouldBe("assemblies");
        Should.Throw<ArgumentNullException>(() => Reflections.GetInstancesByInterface<IDisposable>(null!))
            .ParamName.ShouldBe("assembly");
        Should.Throw<ArgumentException>(() => Reflections.GetInterfaceTypes(typeof(object), new Type[] { null! }))
            .ParamName.ShouldBe("baseInterfaceTypes");
        Should.Throw<ArgumentNullException>(() => Types.DefaultValue(null!)).ParamName.ShouldBe("type");
        Reflections.GetTopBaseType(typeof(object)).ShouldBeNull();
    }

    [Fact]
    public void IsGenericAssignableFrom_ClosedGeneric_UsesClrAssignabilityAndVariance()
    {
        Reflections.IsGenericAssignableFrom(typeof(IEnumerable<object>), typeof(List<string>)).ShouldBeTrue();
        Reflections.IsGenericAssignableFrom(typeof(IEnumerable<string>), typeof(List<object>)).ShouldBeFalse();
        Reflections.IsGenericAssignableFrom(typeof(IComparer<string>), typeof(IComparer<object>)).ShouldBeTrue();
        Reflections.IsGenericAssignableFrom(typeof(IComparer<object>), typeof(IComparer<string>)).ShouldBeFalse();
        Reflections.IsGenericAssignableFrom(typeof(GenericBase<int>), typeof(GenericDerived)).ShouldBeTrue();
        Reflections.IsGenericAssignableFrom(typeof(GenericBase<string>), typeof(GenericDerived)).ShouldBeFalse();
        Reflections.IsGenericAssignableFrom(typeof(IEnumerable<>), typeof(List<int>)).ShouldBeTrue();

        Should.Throw<ArgumentException>(() => Reflections.IsGenericAssignableFrom(typeof(object), typeof(List<int>)));
    }

    [Fact]
    public void AttributeQueries_NullMember_ThrowArgumentNullException()
    {
        Action[] queries =
        {
            () => Reflections.GetAttribute<ObsoleteAttribute>((MemberInfo)null!),
            () => Reflections.GetAttribute((MemberInfo)null!, typeof(ObsoleteAttribute)),
            () => Reflections.GetAttributes<ObsoleteAttribute>((MemberInfo)null!).ToArray(),
            () => Reflections.GetAttributes((MemberInfo)null!, typeof(ObsoleteAttribute)).ToArray(),
            () => Reflections.GetRequiredAttribute<ObsoleteAttribute>((MemberInfo)null!),
            () => Reflections.GetRequiredAttribute((MemberInfo)null!, typeof(ObsoleteAttribute)),
            () => Reflections.GetRequiredAttributes<ObsoleteAttribute>((MemberInfo)null!).ToArray(),
            () => Reflections.GetRequiredAttributes((MemberInfo)null!, typeof(ObsoleteAttribute)).ToArray()
        };

        foreach (var query in queries)
            Should.Throw<ArgumentNullException>(query).ParamName.ShouldBe("member");
    }

    [Fact]
    public void AttributeQueries_InvalidAttributeType_UseExplicitArgumentException()
    {
        var field = typeof(AttributeTarget).GetField(nameof(AttributeTarget.Value))!;
        var member = (MemberInfo)field;
        var type = typeof(AttributeTarget);
        var typeInfo = type.GetTypeInfo();

        foreach (var attributeType in new[] { typeof(string), typeof(OpenAttribute<>) })
        {
            Action[] queries =
            {
                () => Reflections.GetAttribute(field, attributeType),
                () => Reflections.GetAttribute(member, attributeType),
                () => Reflections.GetAttribute(type, attributeType),
                () => Reflections.GetAttribute(typeInfo, attributeType),
                () => Reflections.GetAttributes(field, attributeType).ToArray(),
                () => Reflections.GetAttributes(member, attributeType).ToArray(),
                () => Reflections.GetAttributes(type, attributeType).ToArray(),
                () => Reflections.GetAttributes(typeInfo, attributeType).ToArray(),
                () => Reflections.GetRequiredAttribute(field, attributeType),
                () => Reflections.GetRequiredAttribute(member, attributeType),
                () => Reflections.GetRequiredAttribute(type, attributeType),
                () => Reflections.GetRequiredAttribute(typeInfo, attributeType),
                () => Reflections.GetRequiredAttributes(field, attributeType).ToArray(),
                () => Reflections.GetRequiredAttributes(member, attributeType).ToArray(),
                () => Reflections.GetRequiredAttributes(type, attributeType).ToArray(),
                () => Reflections.GetRequiredAttributes(typeInfo, attributeType).ToArray()
            };

            foreach (var query in queries)
                Should.Throw<ArgumentException>(query).ParamName.ShouldBe("attributeType");
        }

        Action[] nullQueries =
        {
            () => Reflections.GetAttribute(field, null!),
            () => Reflections.GetAttributes(field, null!).ToArray(),
            () => Reflections.GetRequiredAttribute(field, null!),
            () => Reflections.GetRequiredAttributes(field, null!).ToArray()
        };
        foreach (var query in nullQueries)
            Should.Throw<ArgumentNullException>(query).ParamName.ShouldBe("attributeType");
    }

    [Fact]
    public void FindImplementTypes_GenericDefinition_ContinuesPastUnrelatedGenericInterface()
    {
        var probe = CreateGenericInterfaceOrderProbe();
        var genericInterfaces = probe.Implementation
            .FindInterfaces((_, _) => true, null)
            .Where(type => type.IsGenericType)
            .Select(type => type.GetGenericTypeDefinition())
            .ToArray();
        genericInterfaces.ShouldBe(new[] { probe.UnrelatedDefinition, probe.TargetDefinition });

        var types = Reflections.FindImplementTypes(probe.TargetDefinition, probe.Assembly);

        types.ShouldContain(probe.Implementation);
    }

    [Fact]
    public void FindImplementTypes_PartialTypeLoadException_PreservesLoadableTypes()
    {
        var types = Reflections.FindImplementTypes<IPartialLoadContract>(new PartialLoadAssembly());
        var instances = Reflections.GetInstancesByInterface<IPartialLoadContract>(new PartialLoadAssembly());

        types.ShouldBe(new[] { typeof(PartialLoadImplementation) });
        instances.ShouldHaveSingleItem().ShouldBeOfType<PartialLoadImplementation>();
    }

    public sealed class AssignableSample
    {
        public AssignableSample(IComparable value) => Value = value;
        public IComparable Value { get; }
    }
    public sealed class SpecificitySample
    {
        public SpecificitySample(object value) => Selected = "object";
        public SpecificitySample(IComparable value) => Selected = "comparable";
        public string Selected { get; }
    }
    public sealed class NullableSample
    {
        public NullableSample(int? value) => Value = value;
        public int? Value { get; }
    }
    public sealed class LongSample { public LongSample(long value) { } }
    public sealed class AmbiguousSample
    {
        public AmbiguousSample(string value) { }
        public AmbiguousSample(IDisposable value) { }
    }
    public sealed class ThrowingSample { public ThrowingSample() => throw new ApplicationException("cause"); }
    public sealed class CountedSample
    {
        public static int Calls;
        public CountedSample() => Calls++;
    }

    private class GenericBase<T>
    {
    }

    private sealed class GenericDerived : GenericBase<int>
    {
    }

    private sealed class AttributeTarget
    {
        public int Value;
    }

    private sealed class OpenAttribute<T> : Attribute
    {
    }

    private interface IPartialLoadContract
    {
    }

    private sealed class PartialLoadImplementation : IPartialLoadContract
    {
        public PartialLoadImplementation()
        {
        }
    }

    private sealed class FieldPredicateSample
    {
        public bool BoolValue = true;
        public DayOfWeek EnumValue = DayOfWeek.Monday;
        public DayOfWeek? NullableEnumValue = DayOfWeek.Tuesday;
        public DateTime DateValue = DateTime.UnixEpoch;
        public int IntValue = 1;
        public double DoubleValue = 1d;
    }

    private sealed class PartialLoadAssembly : Assembly
    {
        public override Type[] GetTypes() => throw new ReflectionTypeLoadException(
            new[] { typeof(PartialLoadImplementation), null },
            new Exception[] { new TypeLoadException("测试用类型加载失败。") });
    }

    private static (Assembly Assembly, Type UnrelatedDefinition, Type TargetDefinition, Type Implementation)
        CreateGenericInterfaceOrderProbe()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"GenericInterfaceOrderProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");

        var unrelatedBuilder = moduleBuilder.DefineType(
            "Unrelated`1",
            TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        unrelatedBuilder.DefineGenericParameters("T");
        var unrelatedDefinition = unrelatedBuilder.CreateTypeInfo()!.AsType();

        var targetBuilder = moduleBuilder.DefineType(
            "Target`1",
            TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        targetBuilder.DefineGenericParameters("T");
        var targetDefinition = targetBuilder.CreateTypeInfo()!.AsType();

        var implementationBuilder = moduleBuilder.DefineType("Implementation", TypeAttributes.Public | TypeAttributes.Class);
        implementationBuilder.AddInterfaceImplementation(unrelatedDefinition.MakeGenericType(typeof(int)));
        implementationBuilder.AddInterfaceImplementation(targetDefinition.MakeGenericType(typeof(string)));
        var implementation = implementationBuilder.CreateTypeInfo()!.AsType();

        return (assemblyBuilder, unrelatedDefinition, targetDefinition, implementation);
    }
}
