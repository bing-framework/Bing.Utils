namespace Bing.Tests.Samples;

/// <summary>
/// 定义用于测试多级接口继承关系的顶层服务契约。
/// </summary>
public interface ITestSample : ITestSample2
{
}

/// <summary>
/// 定义用于测试多级接口继承关系的中间服务契约。
/// </summary>
public interface ITestSample2 : ITestSample3
{
}

/// <summary>
/// 定义用于测试多级接口继承关系的基础服务契约。
/// </summary>
public interface ITestSample3
{
}

/// <summary>
/// 定义用于测试另一组接口继承关系的基础服务契约。
/// </summary>
public interface ITestSample4
{
}

/// <summary>
/// 定义用于测试另一组接口继承关系的派生服务契约。
/// </summary>
public interface ITestSample5 : ITestSample4
{
}

/// <summary>
/// 提供两组接口继承关系的共同测试实现。
/// </summary>
public class TestSample : ITestSample, ITestSample5
{
}

/// <summary>
/// 定义带值属性的实现类型查找测试契约。
/// </summary>
public interface IA : IB
{
    /// <summary>
    /// 获取或设置实现类型查找测试使用的值。
    /// </summary>
    string Value { get; set; }
}

/// <summary>
/// 定义实现类型查找测试使用的基础契约。
/// </summary>
public interface IB
{
}

/// <summary>
/// 定义实现类型查找测试使用的附加契约。
/// </summary>
public interface IC
{
}

/// <summary>
/// 定义泛型实现类型查找测试契约。
/// </summary>
/// <typeparam name="T">用于区分泛型接口实现的测试类型。</typeparam>
public interface IG<T>
{
}

/// <summary>
/// 提供带值属性的接口查找测试实现。
/// </summary>
public class A : IA, IB
{
    /// <inheritdoc />
    public string Value { get; set; }
}

/// <summary>
/// 提供基础与附加接口的共同测试实现。
/// </summary>
public class B : IB, IC
{
}

/// <summary>
/// 表示同时实现基础与附加查找契约的抽象测试类型。
/// </summary>
public abstract class C : IB, IC
{
}

/// <summary>
/// 提供仅实现非泛型接口的泛型测试类型。
/// </summary>
/// <typeparam name="T">用于区分封闭泛型类型的测试类型。</typeparam>
public class D<T> : IC
{
}

/// <summary>
/// 提供以自身为类型参数的泛型接口测试实现。
/// </summary>
public class E : IG<E>
{
}

/// <summary>
/// 提供泛型接口的泛型测试实现。
/// </summary>
/// <typeparam name="T">泛型接口使用的测试类型。</typeparam>
public class F<T> : IG<T>
{
}
