namespace Bing.Reflection;

/// <summary>验证接口成员路径、泛型属性身份和公开快照隔离。</summary>
public class ReflectionRepairRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MemberPath_CovariantSiblingInterfaces_RemainAmbiguous(bool includeNonPublic)
    {
        var options = new MemberPathOptions { IncludeNonPublic = includeNonPublic };
        Should.Throw<AmbiguousMatchException>(() => Reflections.HasMemberPath(typeof(CovariantRoot), "Child.Value", options));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MemberPath_InheritedInterfaceProperty_ReadsAndWrites(bool includeNonPublic)
    {
        var root = new Root();
        var options = new MemberPathOptions { IncludeNonPublic = includeNonPublic };
        Reflections.HasMemberPath(typeof(Root), "Child.Number", options).ShouldBeTrue();
        Reflections.TryGetMemberValueByPath(root, "Child.Number", out var value, options).ShouldBeTrue();
        value.ShouldBe(9);
        Reflections.TrySetMemberValueByPath(root, "Child.Number", 10, options).ShouldBeTrue();
        root.Child.Number.ShouldBe(10);
        Reflections.GetPropertyValueByPath(root.Child, typeof(IChild), "Number").ShouldBe(10);
    }

    [Fact]
    public void MemberPath_ConflictingInterfaceProperty_RemainsAmbiguous()
    {
        Should.Throw<AmbiguousMatchException>(() => Reflections.HasMemberPath(typeof(ConflictRoot), "Child.Value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MemberPath_HiddenInterfaceProperty_UsesDerivedDeclaration(bool includeNonPublic)
    {
        var root = new ShadowRoot();
        var options = new MemberPathOptions { IncludeNonPublic = includeNonPublic };

        Reflections.TryGetMemberValueByPath(root, "Child.Value", out var value, options).ShouldBeTrue();
        value.ShouldBe(9);
        Reflections.TrySetMemberValueByPath(root, "Child.Value", 10, options).ShouldBeTrue();
        root.Child.Value.ShouldBe(10);
        ((IShadowBase)root.Child).Value.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MemberPath_HiddenInterfacePropertyWithDifferentType_UsesDerivedDeclaration(bool includeNonPublic)
    {
        var root = new TypeShadowRoot();
        var options = new MemberPathOptions { IncludeNonPublic = includeNonPublic };

        Reflections.TryGetMemberValueByPath(root, "Child.Value", out var value, options).ShouldBeTrue();
        value.ShouldBe(9);
        Reflections.TrySetMemberValueByPath(root, "Child.Value", 10, options).ShouldBeTrue();
        root.Child.Value.ShouldBe(10);
        ((ITypeShadowBase)root.Child).Value.ShouldBe("base");
    }

    [Fact]
    public void MemberPath_UnrelatedSameSignatureInterfaceProperties_RemainAmbiguous()
    {
        Should.Throw<AmbiguousMatchException>(() => Reflections.HasMemberPath(typeof(IUnrelatedCombined), "Value"));
    }

    [Fact]
    public void MemberPath_IgnoreCaseDoesNotHideCaseDifferentInterfaceProperties()
    {
        Reflections.HasMemberPath(typeof(ICaseChild), "Value").ShouldBeTrue();
        Reflections.HasMemberPath(typeof(ICaseChild), "value").ShouldBeTrue();
        Should.Throw<AmbiguousMatchException>(() => Reflections.HasMemberPath(typeof(ICaseChild), "VALUE",
            new MemberPathOptions { IgnoreCase = true }));
    }

    [Fact]
    public void PublicProperties_OpenGenericParameters_KeepDistinctTypes()
    {
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(ICombined<,>));
        properties.Count.ShouldBe(2);
        properties.Select(property => property.PropertyType).Distinct().Count().ShouldBe(2);
        var nested = TypeReflections.GetPublicInstanceProperties(typeof(INestedCombined<,>));
        nested.Count.ShouldBe(2);
        nested.Select(property => property.PropertyType).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public void PublicProperties_IdenticalGenericArgument_Deduplicates()
    {
        var argument = typeof(ILeft<>).GetGenericArguments()[0];
        var type = typeof(ICombined<,>).MakeGenericType(argument, argument);
        TypeReflections.GetPublicInstanceProperties(type).Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PublicPropertySnapshots_MutationDoesNotAffectLaterQueries(int mode)
    {
        IReadOnlyList<PropertyInfo> Read() => mode switch
        {
            1 => TypeReflections.GetPublicReadableInstanceProperties(typeof(Child)),
            2 => TypeReflections.GetPublicWritableInstanceProperties(typeof(Child)),
            _ => TypeReflections.GetPublicInstanceProperties(typeof(Child))
        };
        var properties = (PropertyInfo[])Read();
        properties[0] = null;
        Read().ShouldAllBe(property => property != null);
        TypeReflections.GetPublicInstanceProperties(typeof(Child)).ShouldAllBe(property => property != null);
    }

    [Fact]
    public void CoreCreateInstance_RefArgument_WritesBackOriginalArray()
    {
        object[] arguments = { 7 };
        var value = Reflections.CreateInstance<RefSample>(typeof(RefSample), arguments);
        value.Value.ShouldBe(7);
        arguments[0].ShouldBe(8);
    }

    public interface IBase { int Number { get; set; } }
    public interface ICovariant<out T> { T Value { get; } }
    public interface ICovariantCombined : ICovariant<string>, ICovariant<object> { }
    public sealed class CovariantRoot { public ICovariantCombined Child { get; set; } }
    public interface ILeftChild : IBase { }
    public interface IRightChild : IBase { }
    public interface IChild : ILeftChild, IRightChild { }
    public sealed class Child : IChild { public int Number { get; set; } = 9; }
    public sealed class Root { public IChild Child { get; set; } = new Child(); }

    public interface IShadowBase { int Value { get; set; } }
    public interface IShadowChild : IShadowBase { new int Value { get; set; } }
    public sealed class ShadowChild : IShadowChild
    {
        private int _derivedValue = 9;
        private int _baseValue = 1;

        int IShadowBase.Value { get => _baseValue; set => _baseValue = value; }
        int IShadowChild.Value { get => _derivedValue; set => _derivedValue = value; }
    }
    public sealed class ShadowRoot { public IShadowChild Child { get; set; } = new ShadowChild(); }

    public interface ITypeShadowBase { string Value { get; set; } }
    public interface ITypeShadowChild : ITypeShadowBase { new int Value { get; set; } }
    public sealed class TypeShadowChild : ITypeShadowChild
    {
        private int _derivedValue = 9;
        private string _baseValue = "base";

        string ITypeShadowBase.Value { get => _baseValue; set => _baseValue = value; }
        int ITypeShadowChild.Value { get => _derivedValue; set => _derivedValue = value; }
    }
    public sealed class TypeShadowRoot { public ITypeShadowChild Child { get; set; } = new TypeShadowChild(); }

    public interface IUnrelatedLeft { int Value { get; set; } }
    public interface IUnrelatedRight { int Value { get; set; } }
    public interface IUnrelatedCombined : IUnrelatedLeft, IUnrelatedRight { }

    public interface ICaseBase { int Value { get; set; } }
    public interface ICaseChild : ICaseBase { new int value { get; set; } }

    public interface ILeft<T> { T Value { get; set; } }
    public interface IRight<U> { U Value { get; set; } }
    public interface ICombined<T, U> : ILeft<T>, IRight<U> { }
    public interface INestedCombined<T, U> : ILeft<List<T>>, IRight<List<U>> { }
    public sealed class ConflictRoot { public ICombined<int, string> Child { get; set; } }
    public sealed class RefSample
    {
        public int Value { get; }
        public RefSample(ref int value) { Value = value; value++; }
    }
}
