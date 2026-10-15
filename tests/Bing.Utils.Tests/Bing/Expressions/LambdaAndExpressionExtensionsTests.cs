using System.Linq.Expressions;
using Bing.Tests.Samples;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Expressions;

// ─────────────────────────────────────────────────────────────────────────────
//  LambdaExtensions Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="LambdaExtensions"/> 中的 Expression 扩展方法。
/// </summary>
public class LambdaExtensionsTests
{
    // 辅助：创建 Sample 参数表达式
    /// <summary>
    /// 创建样例类型的测试参数表达式。
    /// </summary>
    /// <returns>名称为 t 的样例参数表达式。</returns>
    private static ParameterExpression Param() => Expression.Parameter(typeof(Sample), "t");

    // ── Property ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证属性名生成对应的成员访问表达式。
    /// </summary>
    [Fact]
    public void Property_SimplePropertyName_ReturnsMemberExpression()
    {
        var param = Param();
        var expr = param.Property("StringValue");
        expr.ShouldBeAssignableTo<MemberExpression>();
        ((MemberExpression)expr).Member.Name.ShouldBe("StringValue");
    }

    /// <summary>
    /// 验证嵌套属性路径生成末级属性的访问表达式。
    /// </summary>
    [Fact]
    public void Property_NestedPropertyName_ReturnsMemberExpressionForLeaf()
    {
        var param = Param();
        // Sample.Test2.StringValue 是两级属性
        var expr = param.Property("Test2.StringValue");
        expr.ShouldBeAssignableTo<MemberExpression>();
        ((MemberExpression)expr).Member.Name.ShouldBe("StringValue");
    }

    /// <summary>
    /// 验证成员信息生成对应的属性访问表达式。
    /// </summary>
    [Fact]
    public void Property_MemberInfo_ReturnsMemberAccess()
    {
        var param = Param();
        var member = typeof(Sample).GetProperty("IntValue")!;
        var expr = param.Property(member);
        expr.ShouldBeAssignableTo<MemberExpression>();
        ((MemberExpression)expr).Member.Name.ShouldBe("IntValue");
    }

    // ── And (untyped) ─────────────────────────────────────────────────────────

    /// <summary>
    /// 验证两个空表达式合取后返回空值。
    /// </summary>
    [Fact]
    public void And_BothNull_ReturnsNull()
    {
        Expression left = null!;
        Expression right = null!;
        var result = left.And(right);
        result.ShouldBeNull();
    }

    /// <summary>
    /// 验证左表达式为空时合取返回右表达式。
    /// </summary>
    [Fact]
    public void And_LeftNull_ReturnsRight()
    {
        Expression left = null!;
        Expression right = Expression.Constant(true);
        left.And(right).ShouldBe(right);
    }

    /// <summary>
    /// 验证右表达式为空时合取返回左表达式。
    /// </summary>
    [Fact]
    public void And_RightNull_ReturnsLeft()
    {
        Expression left = Expression.Constant(true);
        Expression right = null!;
        left.And(right).ShouldBe(left);
    }

    /// <summary>
    /// 验证合取表达式仅接受同时满足两个条件的值。
    /// </summary>
    [Fact]
    public void And_BothNonNull_CompilesAndFilters()
    {
        var param = Param();
        var left = param.Property("IntValue").Greater(5);
        var right = param.Property("IntValue").Less(20);
        var combined = left.And(right);
        var lambda = Expression.Lambda<Func<Sample, bool>>(combined, param).Compile();

        lambda(new Sample { IntValue = 10 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 3 }).ShouldBeFalse();
        lambda(new Sample { IntValue = 25 }).ShouldBeFalse();
    }

    // ── And (typed Expression<Func<T, bool>>) ─────────────────────────────────

    /// <summary>
    /// 验证泛型合取在左侧为空时返回右表达式。
    /// </summary>
    [Fact]
    public void And_TypedExpression_LeftNull_ReturnsRight()
    {
        Expression<Func<Sample, bool>> left = null!;
        Expression<Func<Sample, bool>> right = t => t.BoolValue;
        left.And(right).ShouldBe(right);
    }

    /// <summary>
    /// 验证泛型合取在右侧为空时返回左表达式。
    /// </summary>
    [Fact]
    public void And_TypedExpression_RightNull_ReturnsLeft()
    {
        Expression<Func<Sample, bool>> left = t => t.BoolValue;
        Expression<Func<Sample, bool>> right = null!;
        left.And(right).ShouldBe(left);
    }

    /// <summary>
    /// 验证泛型合取仅接受同时满足两个条件的值。
    /// </summary>
    [Fact]
    public void And_TypedExpression_BothNonNull_CombinesCorrectly()
    {
        Expression<Func<Sample, bool>> left = t => t.IntValue > 5;
        Expression<Func<Sample, bool>> right = t => t.IntValue < 20;
        var combined = left.And(right).Compile();

        combined(new Sample { IntValue = 10 }).ShouldBeTrue();
        combined(new Sample { IntValue = 3 }).ShouldBeFalse();
        combined(new Sample { IntValue = 25 }).ShouldBeFalse();
    }

    // ── AndIf (untyped) ───────────────────────────────────────────────────────

    /// <summary>
    /// 验证条件为真时合取两个表达式。
    /// </summary>
    [Fact]
    public void AndIf_ConditionTrue_CombinesExpressions()
    {
        var param = Param();
        var left = param.Property("IntValue").Greater(0);
        var right = param.Property("IntValue").Less(100);
        var combined = left.AndIf(true, right);
        // condition=true → should combine
        var lambda = Expression.Lambda<Func<Sample, bool>>(combined, param).Compile();
        lambda(new Sample { IntValue = 50 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 0 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证条件为假时保留左表达式。
    /// </summary>
    [Fact]
    public void AndIf_ConditionFalse_ReturnsLeftOnly()
    {
        var param = Param();
        var left = param.Property("IntValue").Greater(0);
        var right = param.Property("IntValue").Less(10);   // would restrict if applied
        var result = left.AndIf(false, right);
        // condition=false → result should be identical to left
        result.ShouldBe(left);
    }

    // ── AndIf (typed) ─────────────────────────────────────────────────────────

    /// <summary>
    /// 验证泛型条件合取在条件为真时组合两个表达式。
    /// </summary>
    [Fact]
    public void AndIf_TypedExpression_ConditionTrue_CombinesExpressions()
    {
        Expression<Func<Sample, bool>> left = t => t.IntValue > 0;
        Expression<Func<Sample, bool>> right = t => t.IntValue < 100;
        var combined = left.AndIf(true, right).Compile();

        combined(new Sample { IntValue = 50 }).ShouldBeTrue();
        combined(new Sample { IntValue = 0 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证泛型条件合取在条件为假时保留左表达式。
    /// </summary>
    [Fact]
    public void AndIf_TypedExpression_ConditionFalse_ReturnsLeft()
    {
        Expression<Func<Sample, bool>> left = t => t.IntValue > 0;
        Expression<Func<Sample, bool>> right = t => t.IntValue < 10;
        left.AndIf(false, right).ShouldBe(left);
    }

    // ── Or (untyped) ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证左表达式为空时析取返回右表达式。
    /// </summary>
    [Fact]
    public void Or_LeftNull_ReturnsRight()
    {
        Expression left = null!;
        Expression right = Expression.Constant(true);
        left.Or(right).ShouldBe(right);
    }

    /// <summary>
    /// 验证右表达式为空时析取返回左表达式。
    /// </summary>
    [Fact]
    public void Or_RightNull_ReturnsLeft()
    {
        Expression left = Expression.Constant(true);
        Expression right = null!;
        left.Or(right).ShouldBe(left);
    }

    /// <summary>
    /// 验证析取表达式接受满足任一条件的值。
    /// </summary>
    [Fact]
    public void Or_BothNonNull_CompilesOrElse()
    {
        var param = Param();
        var left = param.Property("IntValue").Less(5);
        var right = param.Property("IntValue").Greater(20);
        var combined = left.Or(right);
        var lambda = Expression.Lambda<Func<Sample, bool>>(combined, param).Compile();

        lambda(new Sample { IntValue = 3 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 25 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 10 }).ShouldBeFalse();
    }

    // ── Or (typed) ────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证泛型析取接受满足任一条件的值。
    /// </summary>
    [Fact]
    public void Or_TypedExpression_BothNonNull_CombinesCorrectly()
    {
        Expression<Func<Sample, bool>> left = t => t.IntValue < 5;
        Expression<Func<Sample, bool>> right = t => t.IntValue > 20;
        var compiled = left.Or(right).Compile();

        compiled(new Sample { IntValue = 3 }).ShouldBeTrue();
        compiled(new Sample { IntValue = 25 }).ShouldBeTrue();
        compiled(new Sample { IntValue = 10 }).ShouldBeFalse();
    }

    // ── OrIf (untyped) ────────────────────────────────────────────────────────

    /// <summary>
    /// 验证条件为真时析取两个表达式。
    /// </summary>
    [Fact]
    public void OrIf_ConditionTrue_CombinesExpressions()
    {
        var param = Param();
        var left = param.Property("IntValue").Less(5);
        var right = param.Property("IntValue").Greater(20);
        var combined = left.OrIf(true, right);
        var lambda = Expression.Lambda<Func<Sample, bool>>(combined, param).Compile();

        lambda(new Sample { IntValue = 25 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 10 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证条件为假时保留左表达式。
    /// </summary>
    [Fact]
    public void OrIf_ConditionFalse_ReturnsLeftOnly()
    {
        var param = Param();
        var left = param.Property("IntValue").Less(5);
        var right = param.Property("IntValue").Greater(20);
        left.OrIf(false, right).ShouldBe(left);
    }

    // ── OrIf (typed) ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证泛型条件析取在条件为真时组合两个表达式。
    /// </summary>
    [Fact]
    public void OrIf_TypedExpression_ConditionTrue_CombinesExpressions()
    {
        Expression<Func<Sample, bool>> left = t => t.IntValue < 5;
        Expression<Func<Sample, bool>> right = t => t.IntValue > 20;
        var compiled = left.OrIf(true, right).Compile();

        compiled(new Sample { IntValue = 25 }).ShouldBeTrue();
        compiled(new Sample { IntValue = 10 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证泛型条件析取在条件为假时保留左表达式。
    /// </summary>
    [Fact]
    public void OrIf_TypedExpression_ConditionFalse_ReturnsLeft()
    {
        Expression<Func<Sample, bool>> left = t => t.IntValue < 5;
        Expression<Func<Sample, bool>> right = t => t.IntValue > 20;
        left.OrIf(false, right).ShouldBe(left);
    }

    // ── Value<T> ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证从比较表达式中提取字符串常量。
    /// </summary>
    [Fact]
    public void Value_ExpressionWithStringConstant_ReturnsConstant()
    {
        Expression<Func<Sample, bool>> expr = t => t.StringValue == "ValueTest";
        expr.Value().ShouldBe("ValueTest");
    }

    // ── Equal ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字符串属性相等表达式匹配相同文本。
    /// </summary>
    [Fact]
    public void Equal_StringProperty_MatchesTrueCase()
    {
        var param = Param();
        var equalExpr = param.Property("StringValue").Equal("Hello");
        var lambda = Expression.Lambda<Func<Sample, bool>>(equalExpr, param).Compile();

        lambda(new Sample { StringValue = "Hello" }).ShouldBeTrue();
        lambda(new Sample { StringValue = "World" }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证两个表达式生成相等比较节点。
    /// </summary>
    [Fact]
    public void Equal_TwoExpressions_CreatesEqualNode()
    {
        var left = Expression.Constant(5);
        var right = Expression.Constant(5);
        var node = left.Equal(right);
        node.NodeType.ShouldBe(ExpressionType.Equal);
    }

    // ── NotEqual ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字符串属性不等表达式匹配不同文本。
    /// </summary>
    [Fact]
    public void NotEqual_StringProperty_WorksCorrectly()
    {
        var param = Param();
        var expr = param.Property("StringValue").NotEqual("Hello");
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { StringValue = "World" }).ShouldBeTrue();
        lambda(new Sample { StringValue = "Hello" }).ShouldBeFalse();
    }

    // ── Greater ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证整数属性大于表达式排除边界及较小值。
    /// </summary>
    [Fact]
    public void Greater_IntProperty_WorksCorrectly()
    {
        var param = Param();
        var expr = param.Property("IntValue").Greater(10);
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { IntValue = 15 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 5 }).ShouldBeFalse();
        lambda(new Sample { IntValue = 10 }).ShouldBeFalse();
    }

    // ── GreaterEqual ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证整数属性大于等于表达式包含边界。
    /// </summary>
    [Fact]
    public void GreaterEqual_IntProperty_IncludesBoundary()
    {
        var param = Param();
        var expr = param.Property("IntValue").GreaterEqual(10);
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { IntValue = 10 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 11 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 9 }).ShouldBeFalse();
    }

    // ── Less ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证整数属性小于表达式排除边界及较大值。
    /// </summary>
    [Fact]
    public void Less_IntProperty_WorksCorrectly()
    {
        var param = Param();
        var expr = param.Property("IntValue").Less(10);
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { IntValue = 5 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 10 }).ShouldBeFalse();
        lambda(new Sample { IntValue = 15 }).ShouldBeFalse();
    }

    // ── LessEqual ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证整数属性小于等于表达式包含边界。
    /// </summary>
    [Fact]
    public void LessEqual_IntProperty_IncludesBoundary()
    {
        var param = Param();
        var expr = param.Property("IntValue").LessEqual(10);
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { IntValue = 10 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 9 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 11 }).ShouldBeFalse();
    }

    // ── StartsWith ────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字符串属性的前缀匹配表达式。
    /// </summary>
    [Fact]
    public void StartsWith_MatchingPrefix_ReturnsTrue()
    {
        var param = Param();
        var expr = param.Property("StringValue").StartsWith("He");
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { StringValue = "Hello" }).ShouldBeTrue();
        lambda(new Sample { StringValue = "World" }).ShouldBeFalse();
    }

    // ── EndsWith ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字符串属性的后缀匹配表达式。
    /// </summary>
    [Fact]
    public void EndsWith_MatchingSuffix_ReturnsTrue()
    {
        var param = Param();
        var expr = param.Property("StringValue").EndsWith("lo");
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { StringValue = "Hello" }).ShouldBeTrue();
        lambda(new Sample { StringValue = "World" }).ShouldBeFalse();
    }

    // ── Contains ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字符串属性的子串匹配表达式。
    /// </summary>
    [Fact]
    public void Contains_SubstringMatch_ReturnsTrue()
    {
        var param = Param();
        var expr = param.Property("StringValue").Contains("ell");
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { StringValue = "Hello" }).ShouldBeTrue();
        lambda(new Sample { StringValue = "World" }).ShouldBeFalse();
    }

    // ── Operation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证相等操作符生成对应的比较表达式。
    /// </summary>
    [Fact]
    public void Operation_Equal_DispatchesCorrectly()
    {
        var param = Param();
        var expr = param.Property("IntValue").Operation(Operator.Equal, 42);
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { IntValue = 42 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 0 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证不等操作符生成对应的比较表达式。
    /// </summary>
    [Fact]
    public void Operation_NotEqual_DispatchesCorrectly()
    {
        var param = Param();
        var expr = param.Property("IntValue").Operation(Operator.NotEqual, 42);
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { IntValue = 1 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 42 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证大于操作符生成对应的比较表达式。
    /// </summary>
    [Fact]
    public void Operation_Greater_DispatchesCorrectly()
    {
        var param = Param();
        var expr = param.Property("IntValue").Operation(Operator.Greater, 10);
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { IntValue = 11 }).ShouldBeTrue();
        lambda(new Sample { IntValue = 10 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证前缀操作符生成对应的字符串匹配表达式。
    /// </summary>
    [Fact]
    public void Operation_Starts_DispatchesCorrectly()
    {
        var param = Param();
        var expr = param.Property("StringValue").Operation(Operator.Starts, "He");
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { StringValue = "Hello" }).ShouldBeTrue();
        lambda(new Sample { StringValue = "World" }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证包含操作符生成对应的字符串匹配表达式。
    /// </summary>
    [Fact]
    public void Operation_Contains_DispatchesCorrectly()
    {
        var param = Param();
        var expr = param.Property("StringValue").Operation(Operator.Contains, "ell");
        var lambda = Expression.Lambda<Func<Sample, bool>>(expr, param).Compile();

        lambda(new Sample { StringValue = "Hello" }).ShouldBeTrue();
        lambda(new Sample { StringValue = "World" }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证表达式值重载拒绝不支持的操作符。
    /// </summary>
    [Fact]
    public void Operation_WithExpressionValue_UnsupportedOperator_Throws()
    {
        var param = Param();
        var left = param.Property("IntValue");
        var right = Expression.Constant(10);
        Should.Throw<NotImplementedException>(
            () => left.Operation(Operator.Starts, right));
    }

    // ── Call ──────────────────────────────────────────────────────────────────

    // 辅助类：单一重载方法，避免 AmbiguousMatchException
    /// <summary>
    /// 提供方法调用表达式测试使用的问候对象。
    /// </summary>
    private class Greeter
    {
        /// <summary>
        /// 生成带指定名称的问候文本。
        /// </summary>
        /// <param name="name">问候对象的名称。</param>
        /// <returns>包含名称的问候文本。</returns>
        public string Greet(string name) => "Hello, " + name;
    }

    /// <summary>
    /// 验证按名称创建的无参数方法调用可以编译执行。
    /// </summary>
    [Fact]
    public void Call_MethodByName_CreatesCallExpression()
    {
        var param = Expression.Parameter(typeof(string), "s");
        var callExpr = param.Call("ToUpper");
        callExpr.ShouldNotBeNull();
        var lambda = Expression.Lambda<Func<string, string>>(callExpr!, param).Compile();
        lambda("hello").ShouldBe("HELLO");
    }

    /// <summary>
    /// 验证对象参数值传入生成的方法调用表达式。
    /// </summary>
    [Fact]
    public void Call_WithObjectValues_CreatesCallExpression()
    {
        // 使用只有一个重载的方法，避免 string.Contains 的多重载问题
        var param = Expression.Parameter(typeof(Greeter), "g");
        var callExpr = param.Call("Greet", "World");
        callExpr.ShouldNotBeNull();
        var lambda = Expression.Lambda<Func<Greeter, string>>(callExpr!, param).Compile();
        lambda(new Greeter()).ShouldBe("Hello, World");
    }

    // ── ToLambda ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证表达式体转换为可编译执行的 Lambda 表达式。
    /// </summary>
    [Fact]
    public void ToLambda_ValidBody_ReturnsCompiledLambda()
    {
        var param = Expression.Parameter(typeof(int), "x");
        var body = Expression.Add(param, Expression.Constant(1));
        var lambda = body.ToLambda<Func<int, int>>(param)!.Compile();
        lambda(5).ShouldBe(6);
    }

    /// <summary>
    /// 验证空表达式体转换为 Lambda 后返回空值。
    /// </summary>
    [Fact]
    public void ToLambda_NullBody_ReturnsNull()
    {
        Expression body = null!;
        body.ToLambda<Func<int, int>>().ShouldBeNull();
    }

    // ── ToPredicate ───────────────────────────────────────────────────────────

    /// <summary>
    /// 验证真常量转换为返回真的谓词。
    /// </summary>
    [Fact]
    public void ToPredicate_ConstantTrue_ReturnsAlwaysTruePredicate()
    {
        var param = Param();
        var pred = Expression.Constant(true).ToPredicate<Sample>(param).Compile();
        pred(new Sample()).ShouldBeTrue();
    }

    /// <summary>
    /// 验证假常量转换为返回假的谓词。
    /// </summary>
    [Fact]
    public void ToPredicate_ConstantFalse_ReturnsAlwaysFalsePredicate()
    {
        var param = Param();
        var pred = Expression.Constant(false).ToPredicate<Sample>(param).Compile();
        pred(new Sample()).ShouldBeFalse();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  ExpressionExtensions Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="ExpressionExtensions"/> 中的属性信息提取与创建方法。
/// </summary>
public class ExpressionExtensionsPropertyInfoTests
{
    // ── GetPropertyInfo<T, TProperty>(expression) ─────────────────────────────

    /// <summary>
    /// 验证从字符串属性表达式提取名称和属性类型。
    /// </summary>
    [Fact]
    public void GetPropertyInfo_StringProperty_ReturnsCorrectPropertyInfo()
    {
        Expression<Func<Sample, string>> expr = t => t.StringValue;
        var info = expr.GetPropertyInfo();
        info.Name.ShouldBe("StringValue");
        info.PropertyType.ShouldBe(typeof(string));
    }

    /// <summary>
    /// 验证从整数属性表达式提取名称和属性类型。
    /// </summary>
    [Fact]
    public void GetPropertyInfo_IntProperty_ReturnsCorrectPropertyInfo()
    {
        Expression<Func<Sample, int>> expr = t => t.IntValue;
        var info = expr.GetPropertyInfo();
        info.Name.ShouldBe("IntValue");
        info.PropertyType.ShouldBe(typeof(int));
    }

    /// <summary>
    /// 验证装箱转换不影响属性信息提取。
    /// </summary>
    [Fact]
    public void GetPropertyInfo_ObjectTypedExpression_HandlesConvertNode()
    {
        // int → object 会产生 Convert(UnaryExpression) 节点
        Expression<Func<Sample, object>> expr = t => t.IntValue;
        var info = expr.GetPropertyInfo();
        info.Name.ShouldBe("IntValue");
    }

    /// <summary>
    /// 验证属性信息提取拒绝空表达式。
    /// </summary>
    [Fact]
    public void GetPropertyInfo_NullExpression_ThrowsArgumentNullException()
    {
        Expression<Func<Sample, string>> expr = null!;
        Should.Throw<ArgumentNullException>(() => expr.GetPropertyInfo());
    }

    /// <summary>
    /// 验证属性信息提取拒绝方法调用表达式。
    /// </summary>
    [Fact]
    public void GetPropertyInfo_NonPropertyExpression_ThrowsArgumentException()
    {
        // 方法调用表达式，不是属性表达式
        Expression<Func<Sample, string>> expr = t => t.StringValue!.ToUpper();
        Should.Throw<ArgumentException>(() => expr.GetPropertyInfo());
    }

    // ── GetPropertyInfo<T, TProperty>(source, expression) ────────────────────

    /// <summary>
    /// 验证从对象及属性选择器提取属性信息。
    /// </summary>
    [Fact]
    public void GetPropertyInfo_WithSource_ReturnsCorrectPropertyInfo()
    {
        var sample = new Sample();
        var info = sample.GetPropertyInfo(t => t.StringValue);
        info.Name.ShouldBe("StringValue");
    }

    // ── GetPropertyInfos<T>(source, params expressions) ───────────────────────

    /// <summary>
    /// 验证批量提取按输入顺序返回多个属性信息。
    /// </summary>
    [Fact]
    public void GetPropertyInfos_MultipleProperties_ReturnsAllInfos()
    {
        var sample = new Sample();
        var infos = sample.GetPropertyInfos(
            t => t.StringValue,
            t => (object)t.IntValue).ToList();

        infos.Count.ShouldBe(2);
        infos[0].Name.ShouldBe("StringValue");
        infos[1].Name.ShouldBe("IntValue");
    }

    /// <summary>
    /// 验证批量属性信息提取拒绝空源对象。
    /// </summary>
    [Fact]
    public void GetPropertyInfos_NullSource_ThrowsArgumentNullException()
    {
        Sample sample = null!;
        Should.Throw<ArgumentNullException>(() =>
            sample.GetPropertyInfos(t => t.StringValue).ToList());
    }

    /// <summary>
    /// 验证批量属性信息提取拒绝空表达式集合。
    /// </summary>
    [Fact]
    public void GetPropertyInfos_NullExpressions_ThrowsArgumentNullException()
    {
        var sample = new Sample();
        IEnumerable<Expression<Func<Sample, object>>> exprs = null!;
        Should.Throw<ArgumentNullException>(() =>
            sample.GetPropertyInfos(exprs).ToList());
    }

    // ── CreateGetPropertyExpression ───────────────────────────────────────────

    /// <summary>
    /// 验证属性信息生成对应的成员访问表达式。
    /// </summary>
    [Fact]
    public void CreateGetPropertyExpression_ValidProperty_ReturnsMemberExpression()
    {
        var propInfo = typeof(Sample).GetProperty("StringValue")!;
        var expr = propInfo.CreateGetPropertyExpression();
        expr.ShouldNotBeNull();
        expr.Member.Name.ShouldBe("StringValue");
    }

    /// <summary>
    /// 验证属性访问表达式拒绝不匹配的参数类型。
    /// </summary>
    [Fact]
    public void CreateGetPropertyExpression_WithMismatchedParameterType_Throws()
    {
        var propInfo = typeof(Sample).GetProperty("StringValue")!;
        var wrongParam = Expression.Parameter(typeof(string), "s"); // wrong type
        Should.Throw<InvalidOperationException>(
            () => propInfo.CreateGetPropertyExpression(wrongParam));
    }

    /// <summary>
    /// 验证属性访问表达式创建拒绝空属性信息。
    /// </summary>
    [Fact]
    public void CreateGetPropertyExpression_NullPropertyInfo_Throws()
    {
        PropertyInfo propInfo = null!;
        var param = Expression.Parameter(typeof(Sample), "t");
        Should.Throw<ArgumentNullException>(
            () => propInfo.CreateGetPropertyExpression(param));
    }

    // ── CreateGetPropertyLambdaExpression<T, TProperty> ──────────────────────

    /// <summary>
    /// 验证字符串属性访问 Lambda 返回实例属性值。
    /// </summary>
    [Fact]
    public void CreateGetPropertyLambdaExpression_StringProperty_CompilesCorrectly()
    {
        var propInfo = typeof(Sample).GetProperty("StringValue")!;
        var lambda = propInfo.CreateGetPropertyLambdaExpression<Sample, string>().Compile();
        var sample = new Sample { StringValue = "TestLambda" };
        lambda(sample).ShouldBe("TestLambda");
    }

    /// <summary>
    /// 验证整数属性访问 Lambda 返回实例属性值。
    /// </summary>
    [Fact]
    public void CreateGetPropertyLambdaExpression_IntProperty_CompilesCorrectly()
    {
        var propInfo = typeof(Sample).GetProperty("IntValue")!;
        var lambda = propInfo.CreateGetPropertyLambdaExpression<Sample, int>().Compile();
        var sample = new Sample { IntValue = 99 };
        lambda(sample).ShouldBe(99);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  PredicateExpressionBuilder Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="PredicateExpressionBuilder{TEntity}"/>。
/// </summary>
public class PredicateExpressionBuilderTests
{
    // ── ToLambda 空构建器 ──────────────────────────────────────────────────────

    /// <summary>
    /// 验证空谓词构建器返回空 Lambda。
    /// </summary>
    [Fact]
    public void ToLambda_EmptyBuilder_ReturnsNull()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        // 未 Append 任何条件，_result = null，ToLambda 应返回 null
        builder.ToLambda().ShouldBeNull();
    }

    // ── GetParameter ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证谓词构建器提供实体类型的参数表达式。
    /// </summary>
    [Fact]
    public void GetParameter_ReturnsParameterExpression()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        var param = builder.GetParameter();
        param.ShouldNotBeNull();
        param.Type.ShouldBe(typeof(Sample));
    }

    // ── Append (expression + Operator + object value) ────────────────────────

    /// <summary>
    /// 验证构建器按相等条件筛选属性值。
    /// </summary>
    [Fact]
    public void Append_SingleEqualCondition_FiltersCorrectly()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        builder.Append(t => t.StringValue, Operator.Equal, "Match");
        var predicate = builder.ToLambda()!.Compile();

        predicate(new Sample { StringValue = "Match" }).ShouldBeTrue();
        predicate(new Sample { StringValue = "Other" }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证构建器按大于条件筛选属性值。
    /// </summary>
    [Fact]
    public void Append_GreaterCondition_FiltersCorrectly()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        builder.Append(t => t.IntValue, Operator.Greater, 10);
        var predicate = builder.ToLambda()!.Compile();

        predicate(new Sample { IntValue = 15 }).ShouldBeTrue();
        predicate(new Sample { IntValue = 5 }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证构建器使用合取组合连续追加的条件。
    /// </summary>
    [Fact]
    public void Append_TwoConditions_CombinesWithAnd()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        builder.Append(t => t.IntValue, Operator.Greater, 5);
        builder.Append(t => t.IntValue, Operator.Less, 20);
        var predicate = builder.ToLambda()!.Compile();

        predicate(new Sample { IntValue = 10 }).ShouldBeTrue();
        predicate(new Sample { IntValue = 3 }).ShouldBeFalse();
        predicate(new Sample { IntValue = 25 }).ShouldBeFalse();
    }

    // ── Append (string property + Operator + object value) ───────────────────

    /// <summary>
    /// 验证构建器通过属性名追加相等条件。
    /// </summary>
    [Fact]
    public void Append_StringPropertyName_FiltersCorrectly()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        builder.Append("StringValue", Operator.Equal, "Hello");
        var predicate = builder.ToLambda()!.Compile();

        predicate(new Sample { StringValue = "Hello" }).ShouldBeTrue();
        predicate(new Sample { StringValue = "World" }).ShouldBeFalse();
    }

    /// <summary>
    /// 验证构建器通过包含操作符筛选字符串。
    /// </summary>
    [Fact]
    public void Append_ContainsOperator_FiltersCorrectly()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        builder.Append("StringValue", Operator.Contains, "ell");
        var predicate = builder.ToLambda()!.Compile();

        predicate(new Sample { StringValue = "Hello" }).ShouldBeTrue();
        predicate(new Sample { StringValue = "World" }).ShouldBeFalse();
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证清空构建器后不再返回谓词。
    /// </summary>
    [Fact]
    public void Clear_AfterAppend_ResetsToNull()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        builder.Append(t => t.StringValue, Operator.Equal, "Test");
        builder.Clear();
        builder.ToLambda().ShouldBeNull();
    }

    // ── Append (expression + Operator + Expression value) ────────────────────

    /// <summary>
    /// 验证构建器接受表达式值并包含比较边界。
    /// </summary>
    [Fact]
    public void Append_WithExpressionValue_FiltersCorrectly()
    {
        var builder = new PredicateExpressionBuilder<Sample>();
        var constExpr = Expression.Constant(10);
        builder.Append(t => t.IntValue, Operator.GreaterEqual, constExpr);
        var predicate = builder.ToLambda()!.Compile();

        predicate(new Sample { IntValue = 10 }).ShouldBeTrue();
        predicate(new Sample { IntValue = 9 }).ShouldBeFalse();
    }
}
