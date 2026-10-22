using System.ComponentModel;
using System.Linq.Expressions;
namespace Bing.Reflection;
/// <summary>
/// 验证类型名称、字段筛选及描述与特性元数据读取。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.TypesFieldsMetadata")]
public class TypeVisitTypesFieldsAndMetadataTest
{
    /// <summary>
    /// 验证完整类型名称查询拒绝空类型。
    /// </summary>
    [Fact]
    public void GetFullName_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetFullName((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证泛型类型名称包含类型名且完全限定名称包含参数类型。
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
    /// 验证完全限定名称扩展入口拒绝空类型。
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_Extension_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeMetaVisitExtensions.GetFullyQualifiedName((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证字段枚举拒绝空类型。
    /// </summary>
    [Fact]
    public void GetFields_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetFields((Type)null).ToList());
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证字段枚举包含公开实例和静态字段并排除私有字段。
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
    /// 验证字段查找拒绝方法调用表达式。
    /// </summary>
    [Fact]
    public void GetField_WithInvalidExpression_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => TypeVisit.GetField<FieldSample, string>(x => x.ToString()));
    }
    /// <summary>
    /// 验证字段查找拒绝空选择器集合。
    /// </summary>
    [Fact]
    public void GetFields_WithSelectorCollectionNull_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            TypeVisit.GetFields<FieldSample>((IEnumerable<Expression<Func<FieldSample, object>>>)null).ToList());
        ex.ParamName.ShouldBe("fieldSelectors");
    }
    /// <summary>
    /// 验证字段选择器返回指定的实例及静态字段。
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
    /// 验证描述特性检测、描述读取及默认描述回退。
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
    /// 验证空实例的描述读取与默认值入口均返回空字符串。
    /// </summary>
    [Fact]
    public void DescriptionExtensions_WithNullInstance_ShouldReturnEmptyString()
    {
        MetadataSample sample = null;
        sample.GetDescription(x => x.WithDescription).ShouldBeEmpty();
        sample.GetDescriptionOr(x => x.WithDescription, "default").ShouldBeEmpty();
    }
    /// <summary>
    /// 验证泛型特性查询的存在与不存在判定互为反向。
    /// </summary>
    [Fact]
    public void AttributeExtensions_GenericOverloads_ShouldReturnExpected()
    {
        var member = typeof(MetadataSample).GetProperty(nameof(MetadataSample.WithObsolete));
        member.IsAttributeDefined<ObsoleteAttribute>().ShouldBeTrue();
        member.IsAttributeNotDefined<ObsoleteAttribute>().ShouldBeFalse();
    }
    /// <summary>
    /// 验证非泛型特性不存在判定当前与存在判定返回相同结果。
    /// </summary>
    [Fact]
    public void AttributeExtensions_NonGenericIsAttributeNotDefined_ReturnsInverseOfDefined()
    {
        var member = typeof(MetadataSample).GetProperty(nameof(MetadataSample.WithObsolete));
        var memberWithout = typeof(MetadataSample).GetProperty(nameof(MetadataSample.WithoutDescription));
        member.IsAttributeDefined(typeof(ObsoleteAttribute)).ShouldBeTrue();
        member.IsAttributeNotDefined(typeof(ObsoleteAttribute)).ShouldBeFalse();
        memberWithout.IsAttributeDefined(typeof(ObsoleteAttribute)).ShouldBeFalse();
        memberWithout.IsAttributeNotDefined(typeof(ObsoleteAttribute)).ShouldBeTrue();
    }
    /// <summary>
    /// 提供公开、静态及私有字段的筛选样例。
    /// </summary>
    private class FieldSample
    {
        /// <summary>
        /// 用于验证公开实例字段发现的测试整数。
        /// </summary>
        public int PublicField;
        /// <summary>
        /// 用于验证公开静态字段发现的测试整数。
        /// </summary>
        public static int PublicStaticField;
        /// <summary>
        /// 用于验证私有字段被默认枚举排除的测试整数。
        /// </summary>
        private int _privateField;
    }
    /// <summary>
    /// 提供描述及过时特性的成员元数据样例。
    /// </summary>
    private class MetadataSample
    {
        /// <summary>
        /// 获取或设置附带描述特性的测试文本。
        /// </summary>
        [Description("鎻忚堪")]
        public string WithDescription { get; set; }
        /// <summary>
        /// 获取或设置附带过时特性的测试文本。
        /// </summary>
        [Obsolete]
        public string WithObsolete { get; set; }
        /// <summary>
        /// 获取或设置未附带描述特性的测试文本。
        /// </summary>
        public string WithoutDescription { get; set; }
    }
}

