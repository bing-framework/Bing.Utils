using System.Linq.Expressions;
using System.Reflection;

namespace Bing.Expressions;

/// <summary>
/// 验证表达式方法调用的重载选择与参数契约。
/// </summary>
public class LambdaCallContractTests
{
    /// <summary>
    /// 方法调用契约测试共用的目标实例表达式。
    /// </summary>
    private static readonly Expression Instance = Expression.Constant(new CallTarget());

    /// <summary>
    /// 验证对象参数按精确或可赋值类型选择方法重载。
    /// </summary>
    [Fact]
    public void ObjectArguments_SelectExactAndAssignableOverloads()
    {
        Evaluate(Instance.Call(nameof(CallTarget.Choose), new object[] { "x" })).ShouldBe("string:x");
        Evaluate(Instance.Call(nameof(CallTarget.AcceptBase), new object[] { new Derived() })).ShouldBe("base");
        Evaluate(Instance.Call(nameof(CallTarget.AcceptObject), new object[] { 42 })).ShouldBe("42");
        Evaluate(Instance.Call(nameof(CallTarget.AcceptInterface), new object[] { new Derived() })).ShouldBe("interface");
        Evaluate(Instance.Call(nameof(CallTarget.NoArguments), Array.Empty<object>())).ShouldBe("none");
        Evaluate(Instance.Call(nameof(CallTarget.NumberOrNullable), new object[] { 9 })).ShouldBe("int");
    }

    /// <summary>
    /// 验证 null 参数选择最具体重载或报告歧义。
    /// </summary>
    [Fact]
    public void ObjectArguments_NullChoosesMostSpecificOrReportsAmbiguity()
    {
        Evaluate(Instance.Call(nameof(CallTarget.Choose), new object[] { null })).ShouldBe("string:null");
        Should.Throw<AmbiguousMatchException>(() => Instance.Call(nameof(CallTarget.Ambiguous), new object[] { null }));
        Instance.Call(nameof(CallTarget.Number), new object[] { null }).ShouldBeNull();
        Instance.Call("Missing", new object[] { "x" }).ShouldBeNull();
        Instance.Call(nameof(CallTarget.Number), new object[] { "x" }).ShouldBeNull();
        Instance.Call(nameof(CallTarget.WideNumber), new object[] { 1 }).ShouldBeNull();
        Should.Throw<AmbiguousMatchException>(() => Instance.Call(nameof(CallTarget.Crossed), new object[] { "x", "y" }));
    }

    /// <summary>
    /// 验证显式参数类型支持 null、可空和可赋值参数。
    /// </summary>
    [Fact]
    public void ExplicitParameterTypes_PreserveNullNullableAndAssignableValues()
    {
        Evaluate(Instance.Call(nameof(CallTarget.Choose), new[] { typeof(string) }, new object[] { null })).ShouldBe("string:null");
        Evaluate(Instance.Call(nameof(CallTarget.NullableNumber), new[] { typeof(int?) }, new object[] { null })).ShouldBe("null");
        Evaluate(Instance.Call(nameof(CallTarget.NullableNumber), new[] { typeof(int?) }, new object[] { 9 })).ShouldBe("9");
        Evaluate(Instance.Call(nameof(CallTarget.AcceptInterface), new[] { typeof(IMarker) }, new object[] { new Derived() })).ShouldBe("interface");
        Evaluate(Instance.Call(nameof(CallTarget.AcceptBase), new[] { typeof(Derived) }, new object[] { new Derived() })).ShouldBe("base");
        Evaluate(Instance.Call(nameof(CallTarget.AcceptObject), new[] { typeof(object) }, new object[] { 42 })).ShouldBe("42");
    }

    /// <summary>
    /// 验证显式参数类型会拒绝不兼容参数和数量。
    /// </summary>
    [Fact]
    public void ExplicitParameterTypes_RejectIncompatibleArgumentsAndCounts()
    {
        Should.Throw<ArgumentException>(() => Instance.Call(nameof(CallTarget.Number), new[] { typeof(int) }, new object[] { null })).ParamName.ShouldBe("values");
        Should.Throw<ArgumentException>(() => Instance.Call(nameof(CallTarget.Number), new[] { typeof(int) }, new object[] { "wrong" })).ParamName.ShouldBe("values");
        Should.Throw<ArgumentException>(() => Instance.Call(nameof(CallTarget.WideNumber), new[] { typeof(int) }, new object[] { 1L })).ParamName.ShouldBe("values");
        Should.Throw<ArgumentException>(() => Instance.Call(nameof(CallTarget.Number), new[] { typeof(int) }, Array.Empty<object>())).ParamName.ShouldBe("values");
        Should.Throw<ArgumentNullException>(() => Instance.Call(nameof(CallTarget.Number), (Type[])null, new object[] { 1 })).ParamName.ShouldBe("paramTypes");
        Should.Throw<ArgumentNullException>(() => Instance.Call(null, new[] { typeof(int) }, new object[] { 1 })).ParamName.ShouldBe("methodName");
        Should.Throw<ArgumentException>(() => Instance.Call(nameof(CallTarget.Number), new Type[] { null }, new object[] { 1 })).ParamName.ShouldBe("paramTypes");
        Should.Throw<AmbiguousMatchException>(() => Instance.Call(nameof(CallTarget.Crossed), new[] { typeof(string), typeof(string) }, new object[] { "x", "y" }));
        Instance.Call("Missing", new[] { typeof(int) }, new object[] { 1 }).ShouldBeNull();
    }

    /// <summary>
    /// 验证对象参数调用不会推断泛型方法。
    /// </summary>
    [Fact]
    public void ObjectArguments_DoNotInferGenericMethods()
    {
        Instance.Call(nameof(CallTarget.Generic), new object[] { "value" }).ShouldBeNull();
        Instance.Call(nameof(CallTarget.Generic), new[] { typeof(string) }, new object[] { "value" }).ShouldBeNull();
        Instance.Call(nameof(CallTarget.GenericWithKnownParameter), new[] { typeof(string) }, new object[] { "value" }).ShouldBeNull();
        Evaluate(Instance.Call(nameof(CallTarget.Mixed), new object[] { "value" })).ShouldBe("non-generic");
        Evaluate(Instance.Call(nameof(CallTarget.Mixed), new[] { typeof(string) }, new object[] { "value" })).ShouldBe("non-generic");
    }

    /// <summary>
    /// 验证泛型重载签名相同时优先选择非泛型方法。
    /// </summary>
    [Fact]
    public void ExpressionArguments_SelectNonGenericMethodWhenGenericOverloadSharesSignature()
    {
        var call = Instance.Call(nameof(CallTarget.Mixed), new Expression[] { Expression.Constant("value") });

        Evaluate(call).ShouldBe("non-generic");
    }

    /// <summary>
    /// 验证表达式参数调用不会推断泛型方法。
    /// </summary>
    [Fact]
    public void ExpressionArguments_DoNotInferGenericMethods()
    {
        Instance.Call(nameof(CallTarget.Generic), new Expression[] { Expression.Constant("value") }).ShouldBeNull();
        Instance.Call(nameof(CallTarget.GenericWithKnownParameter),
            new Expression[] { Expression.Constant("value") }).ShouldBeNull();
    }

    /// <summary>
    /// 验证参数表达式保持为未求值的表达式节点。
    /// </summary>
    [Fact]
    public void ExpressionArguments_KeepParameterExpressionsUnevaluated()
    {
        var value = Expression.Parameter(typeof(string), "value");
        var call = Instance.Call(nameof(CallTarget.Echo), new Expression[] { value });
        var compiled = Expression.Lambda<Func<string, string>>(call, value).Compile();

        compiled("first").ShouldBe("first");
        compiled("second").ShouldBe("second");
    }

    /// <summary>
    /// 验证交叉参数类型的重载无法确定唯一匹配时报告歧义。
    /// </summary>
    [Fact]
    public void ExpressionArguments_ReportAmbiguousCrossedOverloads()
    {
        Should.Throw<AmbiguousMatchException>(() => Instance.Call(nameof(CallTarget.Crossed), new Expression[]
        {
            Expression.Constant("first"),
            Expression.Constant("second")
        }));
    }

    /// <summary>
    /// 验证表达式参数输入校验及显式装箱调用。
    /// </summary>
    [Fact]
    public void ExpressionArguments_ValidateInputsAndPermitExplicitBoxing()
    {
        Evaluate(Instance.Call(nameof(CallTarget.NoArguments), (Expression[])null)).ShouldBe("none");
        Instance.Call("Missing", Array.Empty<Expression>()).ShouldBeNull();
        Should.Throw<ArgumentNullException>(() => Instance.Call(null, Array.Empty<Expression>()))
            .ParamName.ShouldBe("methodName");
        Should.Throw<ArgumentException>(() => Instance.Call(nameof(CallTarget.Echo), new Expression[] { null }))
            .ParamName.ShouldBe("values");

        var number = Expression.Parameter(typeof(int), "number");
        var call = Instance.Call(nameof(CallTarget.AcceptObject), Expression.Convert(number, typeof(object)));
        var compiled = Expression.Lambda<Func<int, string>>(call, number).Compile();
        compiled(42).ShouldBe("42");
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// 验证协变返回重写被选中而不报告重载歧义。
    /// </summary>
    [Fact]
    public void ObjectArguments_SelectCovariantOverrideInsteadOfReportingAmbiguity()
    {
        var instance = Expression.Constant(new CovariantFactoryDerived());

        Evaluate(instance.Call(nameof(CovariantFactoryBase.Create), Array.Empty<object>())).ShouldBe("derived");
    }
#endif

    /// <summary>
    /// 验证属性路径拒绝空名称和空路径段。
    /// </summary>
    [Fact]
    public void PropertyPath_RejectsNullEmptyAndEmptySegments()
    {
        Should.Throw<ArgumentNullException>(() => Instance.Property((string)null)).ParamName.ShouldBe("propertyName");
        foreach (var path in new[] { "", " ", ".Name", "Name.", "Name..Value" })
            Should.Throw<ArgumentException>(() => Instance.Property(path)).ParamName.ShouldBe("propertyName");
        Should.Throw<ArgumentException>(() => Instance.Property("Missing"));
    }

    /// <summary>
    /// 验证不支持的运算符保留明确的异常行为。
    /// </summary>
    [Fact]
    public void Operation_UnsupportedOperatorKeepsExplicitFailure()
    {
        Should.Throw<NotImplementedException>(() => Expression.Constant(1).Operation((Operator)int.MaxValue, (object)1));
        Should.Throw<NotImplementedException>(() => Expression.Constant(1).Operation((Operator)int.MaxValue, Expression.Constant(1)));
    }

    /// <summary>
    /// 编译并执行返回字符串的表达式。
    /// </summary>
    /// <param name="expression">待执行的表达式树。</param>
    /// <returns>表达式计算得到的字符串。</returns>
    private static string Evaluate(Expression expression) => Expression.Lambda<Func<string>>(expression).Compile()();

    /// <summary>
    /// 标记用于验证接口参数可赋值性的类型。
    /// </summary>
    public interface IMarker { }

    /// <summary>
    /// 提供可赋值给标记接口的基类样例。
    /// </summary>
    public class Base : IMarker { }

    /// <summary>
    /// 提供可用于基类参数调用的派生类型样例。
    /// </summary>
    public class Derived : Base { }

    /// <summary>
    /// 声明用于表达式调用测试的重载方法集合。
    /// </summary>
    public class CallTarget
    {
        /// <summary>
        /// 根据实参类型选择相应的重载。
        /// </summary>
        /// <param name="value">字符串实参。</param>
        /// <returns>包含实参的字符串结果。</returns>
        public string Choose(string value) => "string:" + (value ?? "null");

        /// <summary>
        /// 根据实参类型选择相应的重载。
        /// </summary>
        /// <param name="value">对象实参。</param>
        /// <returns>对象重载的固定结果。</returns>
        public string Choose(object value) => "object";

        /// <summary>
        /// 接收基类类型的实参。
        /// </summary>
        /// <param name="value">基类实例。</param>
        /// <returns>基类重载的固定结果。</returns>
        public string AcceptBase(Base value) => "base";

        /// <summary>
        /// 根据实参类型选择相应的歧义候选重载。
        /// </summary>
        /// <param name="value">字符串实参。</param>
        /// <returns>字符串候选的固定结果。</returns>
        public string Ambiguous(string value) => "string";

        /// <summary>
        /// 根据实参类型选择相应的歧义候选重载。
        /// </summary>
        /// <param name="value">基类实参。</param>
        /// <returns>基类候选的固定结果。</returns>
        public string Ambiguous(Base value) => "base";

        /// <summary>
        /// 返回整数参数的文本表示。
        /// </summary>
        /// <param name="value">整数实参。</param>
        /// <returns>整数参数的文本表示。</returns>
        public string Number(int value) => value.ToString();

        /// <summary>
        /// 返回长整数参数的文本表示。
        /// </summary>
        /// <param name="value">长整数实参。</param>
        /// <returns>长整数参数的文本表示。</returns>
        public string WideNumber(long value) => value.ToString();

        /// <summary>
        /// 根据参数是否可空选择相应的数值重载。
        /// </summary>
        /// <param name="value">整数实参。</param>
        /// <returns>整数重载的固定结果。</returns>
        public string NumberOrNullable(int value) => "int";

        /// <summary>
        /// 根据参数是否可空选择相应的数值重载。
        /// </summary>
        /// <param name="value">可空整数实参。</param>
        /// <returns>可空整数重载的固定结果。</returns>
        public string NumberOrNullable(int? value) => "nullable";

        /// <summary>
        /// 提供参数类型交叉的重载候选。
        /// </summary>
        /// <param name="first">字符串类型的第一个参数。</param>
        /// <param name="second">对象类型的第二个参数。</param>
        /// <returns>第一个候选的固定结果。</returns>
        public string Crossed(string first, object second) => "first";

        /// <summary>
        /// 提供参数类型交叉的重载候选。
        /// </summary>
        /// <param name="first">对象类型的第一个参数。</param>
        /// <param name="second">字符串类型的第二个参数。</param>
        /// <returns>第二个候选的固定结果。</returns>
        public string Crossed(object first, string second) => "second";

        /// <summary>
        /// 返回传入的字符串参数。
        /// </summary>
        /// <param name="value">待返回的字符串。</param>
        /// <returns>传入的字符串。</returns>
        public string Echo(string value) => value;

        /// <summary>
        /// 返回无参数调用的固定结果。
        /// </summary>
        /// <returns>表示无参数调用的文本。</returns>
        public string NoArguments() => "none";

        /// <summary>
        /// 返回传入的泛型参数。
        /// </summary>
        /// <typeparam name="T">参数和返回值的类型。</typeparam>
        /// <param name="value">待返回的泛型值。</param>
        /// <returns>传入的泛型值。</returns>
        public T Generic<T>(T value) => value;

        /// <summary>
        /// 声明具有已知形参的泛型方法。
        /// </summary>
        /// <typeparam name="T">未用于形参的泛型类型。</typeparam>
        /// <param name="value">字符串参数。</param>
        /// <returns>传入的字符串。</returns>
        public string GenericWithKnownParameter<T>(string value) => value;

        /// <summary>
        /// 提供同名泛型与非泛型方法的重载候选。
        /// </summary>
        /// <param name="value">字符串参数。</param>
        /// <returns>非泛型方法的固定结果。</returns>
        public string Mixed(string value) => "non-generic";

        /// <summary>
        /// 提供同名泛型与非泛型方法的重载候选。
        /// </summary>
        /// <typeparam name="T">不参与形参类型的泛型参数。</typeparam>
        /// <param name="value">字符串参数。</param>
        /// <returns>泛型方法的固定结果。</returns>
        public string Mixed<T>(string value) => "generic";

        /// <summary>
        /// 将可空整数格式化为文本。
        /// </summary>
        /// <param name="value">待格式化的可空整数。</param>
        /// <returns>整数文本；值为空时返回 "null"。</returns>
        public string NullableNumber(int? value) => value?.ToString() ?? "null";

        /// <summary>
        /// 接收标记接口类型的实参。
        /// </summary>
        /// <param name="value">实现标记接口的实例。</param>
        /// <returns>接口重载的固定结果。</returns>
        public string AcceptInterface(IMarker value) => "interface";

        /// <summary>
        /// 将对象参数转换为文本。
        /// </summary>
        /// <param name="value">待转换的对象。</param>
        /// <returns>对象的文本表示。</returns>
        public string AcceptObject(object value) => value.ToString();
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// 提供可协变重写的工厂方法基类。
    /// </summary>
    public class CovariantFactoryBase
    {
        /// <summary>
        /// 创建基类工厂结果。
        /// </summary>
        /// <returns>基类工厂结果对象。</returns>
        public virtual object Create() => "base";
    }

    /// <summary>
    /// 以更具体的返回类型重写工厂方法。
    /// </summary>
    public sealed class CovariantFactoryDerived : CovariantFactoryBase
    {
        /// <inheritdoc />
        public override string Create() => "derived";
    }
#endif
}
