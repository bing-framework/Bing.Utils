namespace Bing.Tests.Samples;

/// <summary>
/// 测试服务接口
/// </summary>
public interface ITestSample : ITestSample2
{
}

/// <summary>
/// 测试服务接口
/// </summary>
public interface ITestSample2 : ITestSample3
{
}

/// <summary>
/// 测试服务接口
/// </summary>
public interface ITestSample3
{
}

/// <summary>
/// 测试服务接口
/// </summary>
public interface ITestSample4
{
}

/// <summary>
/// 测试服务接口
/// </summary>
public interface ITestSample5 : ITestSample4
{
}

/// <summary>
/// 测试服务
/// </summary>
public class TestSample : ITestSample, ITestSample5
{
}

/// <summary>
/// 测试查找接口
/// </summary>
public interface IA : IB
{
    string Value { get; set; }
}

/// <summary>
/// 测试查找接口
/// </summary>
public interface IB
{
}

/// <summary>
/// 测试查找接口
/// </summary>
public interface IC
{
}

/// <summary>
/// 测试查找接口
/// </summary>
public interface IG<T>
{
}

/// <summary>
/// 测试类型
/// </summary>
public class A : IA, IB
{
    public string Value { get; set; }
}

/// <summary>
/// 测试类型
/// </summary>
public class B : IB, IC
{
}

/// <summary>
/// 测试类型
/// </summary>
public abstract class C : IB, IC
{
}

/// <summary>
/// 测试类型
/// </summary>
public class D<T> : IC
{
}

/// <summary>
/// 测试类型
/// </summary>
public class E : IG<E>
{
}

/// <summary>
/// 测试类型
/// </summary>
public class F<T> : IG<T>
{
}