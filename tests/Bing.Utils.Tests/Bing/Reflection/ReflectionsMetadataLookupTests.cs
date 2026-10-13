using System.Reflection;
using Shouldly;

namespace Bing.Reflection;

public class ReflectionsMetadataLookupTests
{
    [Fact]
    public void GetMetadata_WithMissingOrInvalidName_ThrowsClearException()
    {
        Should.Throw<ArgumentException>(() => Reflections.GetField<LookupSample>(" "));
        Should.Throw<MissingFieldException>(() => Reflections.GetField<LookupSample>("Missing"));
        Should.Throw<MissingMethodException>(() => Reflections.GetMethod<LookupSample>("Missing"));
        Should.Throw<MissingMemberException>(() => Reflections.GetProperty<LookupSample>("Missing"));
        Should.Throw<ArgumentNullException>(() => Reflections.GetProperty(null!, "Value"));
    }

    [Fact]
    public void GetMethod_WithAmbiguousOverloads_ThrowsAmbiguousMatchException()
    {
        Should.Throw<AmbiguousMatchException>(() => Reflections.GetMethod<LookupSample>("Execute"));
    }

    [Fact]
    public void GetPropertyOrField_WithOverloadedIndexers_ThrowsAmbiguousMatchException()
    {
        Should.Throw<AmbiguousMatchException>(() => Reflections.GetPropertyOrField<OverloadedIndexerSample>("Item"));
    }

    [Fact]
    public void FlaggedLookup_IgnoreCaseAndAmbiguity_MatchReflectionSemantics()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        Reflections.GetProperty<LookupSample>("value", flags).Name.ShouldBe("Value");
        Should.Throw<AmbiguousMatchException>(() => Reflections.GetMethod<LookupSample>("execute", flags));
    }

    [Fact]
    public void GetReflector_NullParameter_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeReflections.IsAttributeDefined<ObsoleteAttribute>((ParameterInfo)null!));
    }

    [Fact]
    public void GetField_RequiresExplicitNonPublicBindingFlags()
    {
        Should.Throw<MissingFieldException>(() => Reflections.GetField<LookupSample>("_hidden"));
        Reflections.GetField<LookupSample>("_hidden", BindingFlags.NonPublic | BindingFlags.Instance)
            .Name.ShouldBe("_hidden");
    }

    [Fact]
    public void GetField_HiddenBaseField_SelectsDerivedDeclaration()
    {
        Reflections.GetField<DerivedFieldSample>("Value").DeclaringType.ShouldBe(typeof(DerivedFieldSample));
        Reflections.GetField<DerivedFieldSample>("Value", BindingFlags.Public | BindingFlags.Instance)
            .DeclaringType.ShouldBe(typeof(DerivedFieldSample));
        Reflections.GetField<DerivedFieldSample>("value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            .DeclaringType.ShouldBe(typeof(DerivedFieldSample));
    }

    [Fact]
    public void FieldValueAccessors_WithBaseGenericType_UseTheSameDeclaration()
    {
        var instance = new DerivedFieldSample();

        Reflections.SetFieldValue<BaseFieldSample>(instance, "Value", 11);

        Reflections.GetFieldValue<BaseFieldSample>(instance, "Value").ShouldBe(11);
        instance.Value.ShouldBe(0);
        ((BaseFieldSample)instance).Value.ShouldBe(11);
    }

    [Fact]
    public void MemberPath_HiddenBaseField_SelectsDerivedDeclaration()
    {
        var instance = new DerivedFieldSample();

        Reflections.TrySetMemberValueByPath(instance, "Value", 7).ShouldBeTrue();
        Reflections.TryGetMemberValueByPath(instance, "Value", out var value).ShouldBeTrue();
        value.ShouldBe(7);
        instance.Value.ShouldBe(7);
        ((BaseFieldSample)instance).Value.ShouldBe(0);
        Reflections.TryGetMemberValueByPath(instance, "value", out var ignoredCaseValue,
            new MemberPathOptions { IgnoreCase = true }).ShouldBeTrue();
        ignoredCaseValue.ShouldBe(7);
    }

    [Fact]
    public void GetField_IgnoreCaseMatchesMultipleSameLevelFields_ThrowsAmbiguousMatchException()
    {
        Should.Throw<AmbiguousMatchException>(() => Reflections.GetField<CaseFieldSample>("value",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void MemberPath_IgnoreCaseMatchesMultipleSameLevelFields_ThrowsAmbiguousMatchException()
    {
        Should.Throw<AmbiguousMatchException>(() => Reflections.TryGetMemberValueByPath(
            new CaseFieldSample(), "value", out _, new MemberPathOptions { IgnoreCase = true }));
    }

    [Fact]
    public void FlaggedMetadataResults_AreIndependentSnapshots()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var instance = new LookupSample();
        var properties = Reflections.GetProperties(typeof(LookupSample), flags);
        var methods = Reflections.GetMethods<LookupSample>(flags);
        var fields = Reflections.GetFields(instance, BindingFlags.NonPublic | BindingFlags.Instance);

        properties[0] = null!;
        methods[0] = null!;
        fields[0] = null!;

        Reflections.GetProperties(typeof(LookupSample), flags)[0].ShouldNotBeNull();
        Reflections.GetMethods<LookupSample>(flags)[0].ShouldNotBeNull();
        Reflections.GetFields(instance, BindingFlags.NonPublic | BindingFlags.Instance)[0].ShouldNotBeNull();
    }

    [Fact]
    public void GetPublicProperties_ExcludesUnreadableAndIndexedProperties()
    {
        var items = Reflections.GetPublicProperties(new PropertyProjectionSample());

        items.Count.ShouldBe(1);
        items[0].Text.ShouldBe("Value");
        items[0].Value.ShouldBe(7);
    }

    private sealed class LookupSample
    {
        private int _hidden;
        public int Value { get; set; }
        public void Execute() { }
        public void Execute(int value) { }
    }

    private sealed class OverloadedIndexerSample
    {
        public int this[int index] => index;
        public int this[string index] => index.Length;
    }

    private class BaseFieldSample { public int Value; }
    private sealed class DerivedFieldSample : BaseFieldSample { public new int Value; }
    private sealed class CaseFieldSample
    {
        public int Value;
        public int value;
    }

    private sealed class PropertyProjectionSample
    {
        public int Value { get; set; } = 7;

        public int SetterOnly
        {
            set { }
        }

        public int this[int index] => index;

        public static int StaticValue => 11;
    }
}
