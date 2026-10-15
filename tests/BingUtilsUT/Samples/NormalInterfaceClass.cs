namespace BingUtilsUT.Samples;

/// <summary>
/// 提供同时实现多个测试接口的泛型类型样例。
/// </summary>
/// <typeparam name="T">泛型接口 <see cref="IModelThree{T}"/> 使用的类型。</typeparam>
public class NormalInterfaceClass<T> : IModelOne, IModelTwo, IModelThree<T>
{
}

/// <summary>
/// 定义用于验证接口实现关系的第一组测试契约。
/// </summary>
public interface IModelOne { }

/// <summary>
/// 定义用于验证接口实现关系的第二组测试契约。
/// </summary>
public interface IModelTwo { }

/// <summary>
/// 定义用于验证泛型接口实现关系的测试契约。
/// </summary>
/// <typeparam name="T">接口携带的测试类型。</typeparam>
public interface IModelThree<T> { }

/// <summary>
/// 定义用于验证未实现接口场景的测试契约。
/// </summary>
public interface IModelFour { }
