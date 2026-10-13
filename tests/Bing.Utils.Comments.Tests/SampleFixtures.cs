namespace Bing.Utils.Comments.Tests;

/// <summary>
/// 测试继承注释和复杂成员签名的契约。
/// </summary>
public interface ICommentContract<T>
{
    /// <summary>
    /// 接口转换操作。
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <returns>转换后的值。</returns>
    T Transform(T value);

    /// <summary>
    /// 接口属性。
    /// </summary>
    string Label { get; }

    /// <summary>
    /// 接口事件。
    /// </summary>
    event EventHandler ChangedContract;
}

/// <summary>
/// 提供多来源继承的接口补充内容。
/// </summary>
public interface IMultiSourceCommentContract
{
    /// <summary>
    /// 接口摘要，不应覆盖基类摘要。
    /// </summary>
    /// <remarks>
    /// 接口补充备注。
    /// </remarks>
    /// <exception cref="InvalidOperationException">接口异常。</exception>
    string Merge(string value);
}

/// <summary>
/// 提供多来源继承的基类内容。
/// </summary>
public class MultiSourceCommentBase
{
    /// <summary>
    /// 基类摘要。
    /// </summary>
    /// <param name="value">基类参数。</param>
    /// <returns>基类返回值。</returns>
    public virtual string Merge(string value) => value;
}

/// <summary>
/// 测试多来源 inheritdoc 合并。
/// </summary>
public class MultiSourceCommentFixture : MultiSourceCommentBase, IMultiSourceCommentContract
{
    /// <inheritdoc />
    public override string Merge(string value) => base.Merge(value);
}

/// <summary>
/// 提供基类注释来源。
/// </summary>
public class CommentBase
{
    /// <summary>
    /// 基类操作。
    /// </summary>
    /// <param name="value">基类输入。</param>
    /// <returns>基类输出。</returns>
    public virtual string Convert(string value) => value;

    /// <summary>
    /// 基类属性。
    /// </summary>
    /// <value>基类属性值。</value>
    public virtual string Label { get; set; }

    /// <summary>
    /// 基类事件。
    /// </summary>
    public virtual event EventHandler BaseChanged
    {
        add { }
        remove { }
    }
}

/// <summary>
/// 注释读取测试夹具。
/// </summary>
public class CommentFixture : CommentBase, ICommentContract<int>
{
    /// <summary>
    /// 静态字段。
    /// </summary>
    public static readonly string StaticField = "field";

    /// <summary>
    /// 实例字段。
    /// </summary>
    public int Field;

    /// <summary>
    /// 测试事件。
    /// </summary>
    public event EventHandler Changed;

    /// <summary>
    /// 测试属性。
    /// </summary>
    public string Name { get; set; }

    /// <value>
    /// 索引器值。
    /// </value>
    public int this[int index]
    {
        get => Field + index;
        set => Field = value;
    }

    /// <summary>
    /// 无参数构造函数。
    /// </summary>
    public CommentFixture()
    {
    }

    /// <summary>
    /// 带参数构造函数。
    /// </summary>
    /// <param name="name">初始名称。</param>
    public CommentFixture(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 静态构造函数。
    /// </summary>
    static CommentFixture()
    {
    }

    /// <summary>
    /// 结构化文档样例。
    /// <para>这是第二段内容。</para>
    /// <list type="bullet">
    /// <item><description>第一项。</description></item>
    /// <item><description>第二项。</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <example><code>var fixture = new CommentFixture();</code></example>
    /// </remarks>
    /// <param name="value">包含 <paramref name="value"/> 的输入。</param>
    /// <returns>包含 <see cref="Name"/> 的结果。</returns>
    /// <seealso cref="CommentBase.Convert(string)"/>
    public string Render(string value) => value;

    /// <summary>
    /// 泛型参数样例。
    /// </summary>
    /// <typeparam name="TValue">值类型。</typeparam>
    /// <param name="value">泛型值。</param>
    public void Generic<TValue>(TValue value)
    {
    }

    /// <summary>
    /// 复杂参数样例。
    /// </summary>
    /// <param name="value">泛型引用。</param>
    /// <param name="reference">引用参数。</param>
    /// <param name="output">输出参数。</param>
    /// <param name="input">只读引用参数。</param>
    /// <param name="matrix">多维数组。</param>
    public void Complex<TValue>(TValue value, ref int reference, out int output, in int input,
        int[,] matrix)
    {
        output = input + matrix.GetLength(0) + reference;
    }

    /// <summary>
    /// 指针参数样例。
    /// </summary>
    /// <param name="pointer">指针参数。</param>
    public unsafe void Pointer(int* pointer)
    {
    }

    /// <summary>
    /// 数组参数样例。
    /// </summary>
    /// <param name="vector">向量数组。</param>
    /// <param name="jagged">锯齿数组。</param>
    public void Arrays(int[] vector, int[][] jagged)
    {
    }

    /// <summary>
    /// 构造泛型参数样例。
    /// </summary>
    /// <param name="values">字符串列表。</param>
    public void ConstructedGenericParameter(System.Collections.Generic.List<string> values)
    {
    }

    /// <summary>
    /// 构造嵌套泛型参数样例。
    /// </summary>
    /// <param name="value">嵌套泛型值。</param>
    public void NestedConstructedGenericParameter(GenericCommentFixture<int>.Nested<string> value)
    {
    }

    /// <inheritdoc />
    public override string Convert(string value) => base.Convert(value);

    /// <inheritdoc />
    public override string Label
    {
        get => Name;
        set => Name = value;
    }

    /// <inheritdoc />
    public override event EventHandler BaseChanged;

    /// <inheritdoc />
    public int Transform(int value) => value;

    /// <inheritdoc />
    int ICommentContract<int>.Transform(int value) => value;

    /// <inheritdoc />
    string ICommentContract<int>.Label => Name;

    /// <inheritdoc />
    event EventHandler ICommentContract<int>.ChangedContract
    {
        add { }
        remove { }
    }

    /// <summary>
    /// 隐式转换样例。
    /// </summary>
    /// <param name="fixture">夹具实例。</param>
    public static implicit operator int(CommentFixture fixture) => fixture?.Field ?? 0;

    /// <summary>
    /// 显式转换样例。
    /// </summary>
    /// <param name="value">数值。</param>
    public static explicit operator CommentFixture(int value) => new() { Field = value };

    /// <summary>
    /// checked 显式转换样例。
    /// </summary>
    public static explicit operator checked CommentFixture(int value) => new() { Field = value };

    /// <summary>
    /// 触发测试事件。
    /// </summary>
    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 测试泛型类型。
/// </summary>
/// <typeparam name="T">外层类型参数。</typeparam>
public class GenericCommentFixture<T>
{
    /// <summary>
    /// 测试嵌套泛型类型。
    /// </summary>
    /// <typeparam name="TNested">内层类型参数。</typeparam>
    public class Nested<TNested>
    {
        /// <summary>
        /// 返回外层和内层类型参数。
        /// </summary>
        /// <param name="outer">外层值。</param>
        /// <param name="inner">内层值。</param>
        public void Join(T outer, TNested inner)
        {
        }
    }
}

/// <summary>
/// 测试显式 inheritdoc 来源。
/// </summary>
public class ExplicitInheritCommentFixture
{
    /// <inheritdoc cref="CommentBase.Convert(string)" path="/member/summary" />
    public string Convert(string value) => value;
}

/// <summary>
/// 测试 inheritdoc 的多节点 path 选择。
/// </summary>
public class ExplicitInheritPathCommentFixture
{
    /// <inheritdoc cref="CommentBase.Convert(string)" path="/member/summary | /member/returns" />
    public string Convert(string value) => value;
}

/// <summary>
/// 测试非法 inheritdoc path。
/// </summary>
public class InvalidInheritPathCommentFixture
{
    /// <inheritdoc cref="CommentBase.Convert(string)" path="[" />
    public string Convert(string value) => value;
}

/// <summary>
/// 测试 inheritdoc path 选择文本节点。
/// </summary>
public class TextInheritPathCommentFixture
{
    /// <inheritdoc cref="CommentBase.Convert(string)" path="/member/summary/text()" />
    public string Convert(string value) => value;
}

/// <summary>
/// 类型级 inheritdoc 的基类。
/// </summary>
public class TypeInheritCommentBase
{
    /// <summary>
    /// 类型级继承内容。
    /// </summary>
    public string Name => "base";
}

/// <inheritdoc />
public class TypeInheritCommentFixture : TypeInheritCommentBase
{
    /// <inheritdoc />
    public new string Name => "derived";

    /// <inheritdoc />
    public string Local => "derived";
}

/// <summary>
/// 提供内联 inheritdoc 来源。
/// </summary>
public class InlineCommentBase
{
    /// <summary>
    /// 基类内联内容。
    /// </summary>
    public virtual string Describe() => "base";
}

/// <summary>
/// 测试内联 inheritdoc。
/// </summary>
public class InlineCommentFixture : InlineCommentBase
{
    /// <summary>
    /// 前缀。<inheritdoc />后缀。
    /// </summary>
    public override string Describe() => "derived";
}

/// <summary>
/// 测试 inheritdoc 循环。
/// </summary>
public class InheritCycleFixture
{
    /// <inheritdoc cref="Second()" />
    public string First() => "first";

    /// <inheritdoc cref="First()" />
    public string Second() => "second";
}

/// <summary>
/// 用于测试枚举注释的辅助枚举
/// </summary>
public enum SampleColor
{
    /// <summary>
    /// 红色
    /// </summary>
    Red,

    /// <summary>
    /// 绿色
    /// </summary>
    [Description("绿")]
    Green,

    /// <summary>
    /// 蓝色
    /// </summary>
    Blue
}

public enum FallbackColor
{
    Red,

    [Description("绿")]
    Green
}
