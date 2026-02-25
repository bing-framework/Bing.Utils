using System.ComponentModel;
namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `TypeVisitTypesFieldsAndMetadata` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.TypesFieldsMetadata")]
public class TypeVisitTypesFieldsAndMetadataTest
{
    /// <summary>
    /// 测试用例：验证 `GetFullName` 在 `NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetFullName_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetFullName((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `GetFullName` 在 `And_GetFullyQualifiedName` 场景下，结果为 `ShouldReturnExpected`。
    /// </summary>
    [Fact]
    public void GetFullName_And_GetFullyQualifiedName_ShouldReturnExpected()
    {
        var type = typeof(Dictionary<string, int>);
        var fullName = TypeVisit.GetFullName(type);
        var qualifiedName = TypeVisit.GetFullyQualifiedName(type);
        fullName.ShouldContain("Dictionary");
        qualifiedName.ShouldContain("Dictionary");
        qualifiedName.ShouldContain("System.String");
        qualifiedName.ShouldContain("System.Int32");
    }
    /// <summary>
    /// 测试用例：验证 `GetFullyQualifiedName` 在 `Extension_NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_Extension_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeMetaVisitExtensions.GetFullyQualifiedName((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `GetFields` 在 `NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetFields_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetFields((Type)null).ToList());
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `GetFields` 在 `ByType` 场景下，结果为 `ShouldReturnPublicFields`。
    /// </summary>
    [Fact]
    public void GetFields_ByType_ShouldReturnPublicFields()
    {
        var fields = TypeVisit.GetFields(typeof(FieldSample)).Select(x => x.Name).ToList();
        fields.ShouldContain(nameof(FieldSample.PublicField));
        fields.ShouldContain(nameof(FieldSample.PublicStaticField));
        fields.ShouldNotContain("_privateField");
    }
    /// <summary>
    /// 测试用例：验证 `GetField` 在 `WithInvalidExpression` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Fact]
    public void GetField_WithInvalidExpression_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.GetField<FieldSample, string>(x => x.ToString()));
    }
    /// <summary>
    /// 测试用例：验证 `GetFields` 在 `WithSelectorCollectionNull` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetFields_WithSelectorCollectionNull_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            TypeVisit.GetFields<FieldSample>((IEnumerable<Expression<Func<FieldSample, object>>>)null).ToList());
        ex.ParamName.ShouldBe("fieldSelectors");
    }
    /// <summary>
    /// 测试用例：验证 `GetFields` 在 `WithSelectors` 场景下，结果为 `ShouldReturnSelectedFields`。
    /// </summary>
    [Fact]
    public void GetFields_WithSelectors_ShouldReturnSelectedFields()
    {
        var fields = TypeVisit.GetFields<FieldSample>(x => x.PublicField, x => FieldSample.PublicStaticField)
            .Select(x => x.Name)
            .ToList();
        fields.Count.ShouldBe(2);
        fields.ShouldContain(nameof(FieldSample.PublicField));
        fields.ShouldContain(nameof(FieldSample.PublicStaticField));
    }
    /// <summary>
    /// 测试用例：验证 `DescriptionExtensions` 在 `ShouldReturnExpected` 场景下的行为。
    /// </summary>
    [Fact]
    public void DescriptionExtensions_ShouldReturnExpected()
    {
        var memberWithDesc = typeof(MetadataSample).GetProperty(nameof(MetadataSample.WithDescription));
        var memberWithoutDesc = typeof(MetadataSample).GetProperty(nameof(MetadataSample.WithoutDescription));
        memberWithDesc.IsDescriptionDefined().ShouldBeTrue();
        memberWithDesc.GetDescription().ShouldBe("鎻忚堪");
        memberWithoutDesc.GetDescriptionOr("default-desc").ShouldBe("default-desc");
    }
    /// <summary>
    /// 测试用例：验证 `DescriptionExtensions` 在 `WithNullInstance` 场景下，结果为 `ShouldReturnEmptyString`。
    /// </summary>
    [Fact]
    public void DescriptionExtensions_WithNullInstance_ShouldReturnEmptyString()
    {
        MetadataSample sample = null;
        sample.GetDescription(x => x.WithDescription).ShouldBeEmpty();
        sample.GetDescriptionOr(x => x.WithDescription, "default").ShouldBeEmpty();
    }
    /// <summary>
    /// 测试用例：验证 `AttributeExtensions` 在 `GenericOverloads` 场景下，结果为 `ShouldReturnExpected`。
    /// </summary>
    [Fact]
    public void AttributeExtensions_GenericOverloads_ShouldReturnExpected()
    {
        var member = typeof(MetadataSample).GetProperty(nameof(MetadataSample.WithObsolete));
        member.IsAttributeDefined<ObsoleteAttribute>().ShouldBeTrue();
        member.IsAttributeNotDefined<ObsoleteAttribute>().ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `AttributeExtensions` 在 `NonGenericIsAttributeNotDefined` 场景下，结果为 `CurrentBehaviorEqualsDefined`。
    /// </summary>
    [Fact]
    public void AttributeExtensions_NonGenericIsAttributeNotDefined_CurrentBehaviorEqualsDefined()
    {
        var member = typeof(MetadataSample).GetProperty(nameof(MetadataSample.WithObsolete));
        var memberWithout = typeof(MetadataSample).GetProperty(nameof(MetadataSample.WithoutDescription));
        member.IsAttributeDefined(typeof(ObsoleteAttribute)).ShouldBeTrue();
        member.IsAttributeNotDefined(typeof(ObsoleteAttribute)).ShouldBeTrue();
        memberWithout.IsAttributeDefined(typeof(ObsoleteAttribute)).ShouldBeFalse();
        memberWithout.IsAttributeNotDefined(typeof(ObsoleteAttribute)).ShouldBeFalse();
    }
    private class FieldSample
    {
        public int PublicField;
        public static int PublicStaticField;
        private int _privateField;
    }
    private class MetadataSample
    {
        [Description("鎻忚堪")]
        public string WithDescription { get; set; }
        [Obsolete]
        public string WithObsolete { get; set; }
        public string WithoutDescription { get; set; }
    }
}

