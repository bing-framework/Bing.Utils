using System.Linq.Expressions;
using Bing;
using Shouldly;
using Xunit;

namespace Bing.Expressions;
/// <summary>
/// 覆盖谓词构造器的嵌套路径及常见组合查询。
/// </summary>
public class LambdaBuilderScenarioTests
{
    /// <summary>
    /// 验证对象值重载与字符串路径构造等价谓词。
    /// </summary>
    [Fact]
    public void Append_TypedNestedPropertyWithObjectValue_MatchesStringPath()
    {
        var typedBuilder = new PredicateExpressionBuilder<SearchRecord>();
        typedBuilder.Append(record => record.Address.City, Operator.Equal, "Paris");
        var typedPredicate = typedBuilder.ToLambda()!.Compile();

        var stringBuilder = new PredicateExpressionBuilder<SearchRecord>();
        stringBuilder.Append("Address.City", Operator.Equal, "Paris");
        var stringPredicate = stringBuilder.ToLambda()!.Compile();

        var matching = new SearchRecord { Address = new Address { City = "Paris" } };
        var nonMatching = new SearchRecord { Address = new Address { City = "London" } };
        typedPredicate(matching).ShouldBe(stringPredicate(matching));
        typedPredicate(nonMatching).ShouldBe(stringPredicate(nonMatching));
        typedPredicate(matching).ShouldBeTrue();
        typedPredicate(nonMatching).ShouldBeFalse();
    }

    /// <summary>
    /// 验证表达式值重载与字符串路径构造等价谓词。
    /// </summary>
    [Fact]
    public void Append_TypedNestedPropertyWithExpressionValue_MatchesStringPath()
    {
        var typedBuilder = new PredicateExpressionBuilder<SearchRecord>();
        typedBuilder.Append(record => record.Address.City, Operator.Equal, Expression.Constant("Paris"));
        var typedPredicate = typedBuilder.ToLambda()!.Compile();

        var stringBuilder = new PredicateExpressionBuilder<SearchRecord>();
        stringBuilder.Append("Address.City", Operator.Equal, Expression.Constant("Paris"));
        var stringPredicate = stringBuilder.ToLambda()!.Compile();

        var matching = new SearchRecord { Address = new Address { City = "Paris" } };
        var nonMatching = new SearchRecord { Address = new Address { City = "London" } };
        typedPredicate(matching).ShouldBe(stringPredicate(matching));
        typedPredicate(nonMatching).ShouldBe(stringPredicate(nonMatching));
        typedPredicate(matching).ShouldBeTrue();
        typedPredicate(nonMatching).ShouldBeFalse();
    }

    /// <summary>
    /// 验证组合查询按租户、可选条件和姓名或邮箱短路求值。
    /// </summary>
    [Fact]
    public void SearchQuery_CombinesTenantOptionalFilterAndNameOrEmailWithShortCircuiting()
    {
        const string tenantId = "tenant-a";
        const string searchText = "alex@example.test";
        var includeActiveOnly = true;

        Expression<Func<SearchRecord, bool>> predicate = record => record.ReadTenantId() == tenantId;
        predicate = predicate.AndIf(includeActiveOnly, record => record.ReadIsActive());
        predicate = predicate.And(
            ((Expression<Func<SearchRecord, bool>>)(record => record.ReadName() == searchText))
            .Or(record => record.ReadEmail() == searchText));
        var compiled = predicate.Compile();

        var tenantMismatch = new SearchRecord { TenantId = "tenant-b", IsActive = true };
        compiled(tenantMismatch).ShouldBeFalse();
        tenantMismatch.Events.ShouldBe(new[] { "tenant" });

        var inactive = new SearchRecord { TenantId = tenantId, IsActive = false };
        compiled(inactive).ShouldBeFalse();
        inactive.Events.ShouldBe(new[] { "tenant", "active" });

        var matchingName = new SearchRecord { TenantId = tenantId, IsActive = true, Name = searchText };
        compiled(matchingName).ShouldBeTrue();
        matchingName.Events.ShouldBe(new[] { "tenant", "active", "name" });

        var matchingEmail = new SearchRecord
        {
            TenantId = tenantId,
            IsActive = true,
            Name = "Alex",
            Email = searchText
        };
        compiled(matchingEmail).ShouldBeTrue();
        matchingEmail.Events.ShouldBe(new[] { "tenant", "active", "name", "email" });

        includeActiveOnly = false;
        predicate = ((Expression<Func<SearchRecord, bool>>)(record => record.ReadTenantId() == tenantId))
            .AndIf(includeActiveOnly, record => record.ReadIsActive())
            .And(record => record.ReadName() == searchText);
        var withoutOptionalFilter = new SearchRecord { TenantId = tenantId, IsActive = false, Name = searchText };
        predicate.Compile()(withoutOptionalFilter).ShouldBeTrue();
        withoutOptionalFilter.Events.ShouldBe(new[] { "tenant", "name" });
    }

    /// <summary>
    /// 验证清空后追加条件不会保留先前谓词。
    /// </summary>
    [Fact]
    public void Builder_ClearThenAppend_DoesNotRetainPreviousCondition()
    {
        var builder = new PredicateExpressionBuilder<SearchRecord>();
        builder.Append(record => record.TenantId, Operator.Equal, "old-tenant");
        builder.Clear();
        builder.Append(record => record.IsActive, Operator.Equal, true);
        var predicate = builder.ToLambda()!.Compile();

        predicate(new SearchRecord { TenantId = "old-tenant", IsActive = true }).ShouldBeTrue();
        predicate(new SearchRecord { TenantId = "new-tenant", IsActive = true }).ShouldBeTrue();
        predicate(new SearchRecord { TenantId = "old-tenant", IsActive = false }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 object 选择器仍按底层成员类型构造比较。
    /// </summary>
    [Fact]
    public void Builder_ObjectSelectorRetainsUnderlyingMemberType()
    {
        Expression<Func<SearchRecord, object>> selector = record => record.Age;
        var builder = new PredicateExpressionBuilder<SearchRecord>();
        builder.Append(selector, Operator.Equal, 18);
        var predicate = builder.ToLambda()!.Compile();

        predicate(new SearchRecord { Age = 18 }).ShouldBeTrue();
        predicate(new SearchRecord { Age = 17 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证成员路径中的接口引用转换不会改变属性路径。
    /// </summary>
    [Fact]
    public void Builder_ReferenceCastInsideMemberPathPreservesThePath()
    {
        var builder = new PredicateExpressionBuilder<SearchRecord>();
        builder.Append(record => ((IAgeRecord)record).Age, Operator.Equal, 18);
        var predicate = builder.ToLambda()!.Compile();

        predicate(new SearchRecord { Age = 18 }).ShouldBeTrue();
        predicate(new SearchRecord { Age = 17 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证表达式值中的选择器参数会重绑定到谓词参数。
    /// </summary>
    [Fact]
    public void Append_ExpressionValueRebindsSelectorParameter()
    {
        Expression<Func<SearchRecord, int>> selector = record => record.Age;
        var value = Expression.Add(selector.Body, Expression.Constant(1));
        var builder = new PredicateExpressionBuilder<SearchRecord>();
        builder.Append(selector, Operator.Less, value);

        var predicate = builder.ToLambda()!.Compile();
        predicate(new SearchRecord { Age = 17 }).ShouldBeTrue();
        predicate(new SearchRecord { Age = 0 }).ShouldBeTrue();
    }

    /// <summary>
    /// 验证非实体成员路径的选择器被拒绝且不改变现有谓词。
    /// </summary>
    [Fact]
    public void Append_RejectsSelectorsThatAreNotEntityMemberPathsAndPreservesExistingPredicate()
    {
        var externalRecord = new SearchRecord { TenantId = "external-tenant" };
        (Expression<Func<SearchRecord, object>> Selector, object Value)[] invalidSelectors =
        {
            (record => record.Name.ToUpper(), "ALEX"),
            (record => !record.IsActive, true),
            (record => (long)record.Age, 18L),
            (record => externalRecord.TenantId, "external-tenant")
        };

        foreach (var (selector, value) in invalidSelectors)
        {
            var builder = new PredicateExpressionBuilder<SearchRecord>();
            builder.Append(record => record.TenantId, Operator.Equal, "tenant-a");

            Should.Throw<ArgumentException>(() => builder.Append(selector, Operator.Equal, value));
            Should.Throw<ArgumentException>(() => builder.Append(selector, Operator.Equal, Expression.Constant(value)));

            var predicate = builder.ToLambda()!.Compile();
            predicate(new SearchRecord { TenantId = "tenant-a" }).ShouldBeTrue();
            predicate(new SearchRecord { TenantId = "tenant-b" }).ShouldBeFalse();
        }
    }

    /// <summary>
    /// 验证整数成员不支持字符串运算，且失败时保留原谓词。
    /// </summary>
    /// <param name="operator">用于测试的字符串运算符。</param>
    [Theory]
    [InlineData(Operator.Contains)]
    [InlineData(Operator.Starts)]
    [InlineData(Operator.Ends)]
    public void Append_UnsupportedStringOperatorOnInteger_ThrowsAndPreservesExistingPredicate(Operator @operator)
    {
        var typedBuilder = new PredicateExpressionBuilder<SearchRecord>();
        typedBuilder.Append(record => record.IsActive, Operator.Equal, true);

        Should.Throw<ArgumentException>(() => typedBuilder.Append(record => record.Age, @operator, "1"));
        var typedPredicate = typedBuilder.ToLambda()!.Compile();
        typedPredicate(new SearchRecord { IsActive = true, Age = 1 }).ShouldBeTrue();
        typedPredicate(new SearchRecord { IsActive = false, Age = 1 }).ShouldBeFalse();

        var stringBuilder = new PredicateExpressionBuilder<SearchRecord>();
        stringBuilder.Append(record => record.IsActive, Operator.Equal, true);

        Should.Throw<ArgumentException>(() => stringBuilder.Append("Age", @operator, "1"));
        var stringPredicate = stringBuilder.ToLambda()!.Compile();
        stringPredicate(new SearchRecord { IsActive = true, Age = 1 }).ShouldBeTrue();
        stringPredicate(new SearchRecord { IsActive = false, Age = 1 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证空构造器拒绝整数成员的字符串运算且不添加条件。
    /// </summary>
    /// <param name="operator">用于测试的字符串运算符。</param>
    [Theory]
    [InlineData(Operator.Contains)]
    [InlineData(Operator.Starts)]
    [InlineData(Operator.Ends)]
    public void Append_UnsupportedStringOperatorOnIntegerWithEmptyBuilder_ThrowsWithoutAddingPredicate(
        Operator @operator)
    {
        var typedBuilder = new PredicateExpressionBuilder<SearchRecord>();
        Should.Throw<ArgumentException>(() => typedBuilder.Append(record => record.Age, @operator, "1"));
        typedBuilder.ToLambda().ShouldBeNull();

        var stringBuilder = new PredicateExpressionBuilder<SearchRecord>();
        Should.Throw<ArgumentException>(() => stringBuilder.Append("Age", @operator, "1"));
        stringBuilder.ToLambda().ShouldBeNull();
    }

    /// <summary>
    /// 验证整数成员的字符串运算在各入口均被拒绝。
    /// </summary>
    /// <param name="operator">用于测试的字符串运算符。</param>
    [Theory]
    [InlineData(Operator.Contains)]
    [InlineData(Operator.Starts)]
    [InlineData(Operator.Ends)]
    public void StringOperations_OnIntegerMember_ThrowForDirectAndLambdaFactories(Operator @operator)
    {
        var member = Expression.Parameter(typeof(SearchRecord), "record").Property("Age");
        Should.Throw<ArgumentException>(() => member.Operation(@operator, "1"));

        switch (@operator)
        {
            case Operator.Contains:
                Should.Throw<ArgumentException>(() => member.Contains("1"));
                Should.Throw<ArgumentException>(() => Lambdas.Contains<SearchRecord>("Age", "1"));
                break;
            case Operator.Starts:
                Should.Throw<ArgumentException>(() => member.StartsWith("1"));
                Should.Throw<ArgumentException>(() => Lambdas.Starts<SearchRecord>("Age", "1"));
                break;
            case Operator.Ends:
                Should.Throw<ArgumentException>(() => member.EndsWith("1"));
                Should.Throw<ArgumentException>(() => Lambdas.Ends<SearchRecord>("Age", "1"));
                break;
        }
    }

    /// <summary>
    /// 验证集合条件仅在存在匹配元素时成立。
    /// </summary>
    [Fact]
    public void SearchQuery_AnyMatchesElementsAndReturnsFalseForEmptyOrNonMatchingCollections()
    {
        Expression<Func<SearchRecord, bool>> tenant = record => record.TenantId == "tenant-a";
        Expression<Func<SearchRecord, bool>> hasRequestedTag = record =>
            record.Tags.Any(tag => tag == "priority");
        var predicate = tenant.And(hasRequestedTag).Compile();

        predicate(new SearchRecord { TenantId = "tenant-a", Tags = Array.Empty<string>() }).ShouldBeFalse();
        predicate(new SearchRecord { TenantId = "tenant-a", Tags = new[] { "ordinary" } }).ShouldBeFalse();
        predicate(new SearchRecord { TenantId = "tenant-b", Tags = new[] { "priority" } }).ShouldBeFalse();
        predicate(new SearchRecord { TenantId = "tenant-a", Tags = new[] { "ordinary", "priority" } })
            .ShouldBeTrue();
    }

    /// <summary>
    /// 声明用于接口转换成员路径测试的年龄属性。
    /// </summary>
    private interface IAgeRecord
    {
        /// <summary>
        /// 获取年龄。
        /// </summary>
        int Age { get; }
    }

    /// <summary>
    /// 提供谓词构造场景使用的记录及可观测读取方法。
    /// </summary>
    private sealed class SearchRecord : IAgeRecord
    {
        /// <summary>
        /// 获取或设置租户标识。
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 获取或设置记录的启用状态。
        /// </summary>
        public bool IsActive { get; set; }

        /// <inheritdoc />
        public int Age { get; set; }

        /// <summary>
        /// 获取或设置姓名。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置邮箱地址。
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 获取或设置地址。
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        /// 获取或设置用于集合匹配的标签。
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 获取按条件读取顺序记录的事件列表。
        /// </summary>
        public List<string> Events { get; } = new();

        /// <summary>
        /// 记录读取租户标识的事件并返回其值。
        /// </summary>
        /// <returns>当前租户标识。</returns>
        public string ReadTenantId()
        {
            Events.Add("tenant");
            return TenantId;
        }

        /// <summary>
        /// 记录读取启用状态的事件并返回其值。
        /// </summary>
        /// <returns>记录是否启用。</returns>
        public bool ReadIsActive()
        {
            Events.Add("active");
            return IsActive;
        }

        /// <summary>
        /// 记录读取姓名的事件并返回其值。
        /// </summary>
        /// <returns>当前姓名。</returns>
        public string ReadName()
        {
            Events.Add("name");
            return Name;
        }

        /// <summary>
        /// 记录读取邮箱的事件并返回其值。
        /// </summary>
        /// <returns>当前邮箱地址。</returns>
        public string ReadEmail()
        {
            Events.Add("email");
            return Email;
        }
    }

    /// <summary>
    /// 提供成员路径访问的地址样例。
    /// </summary>
    private sealed class Address
    {
        /// <summary>
        /// 获取或设置城市名称。
        /// </summary>
        public string City { get; set; }
    }
}
