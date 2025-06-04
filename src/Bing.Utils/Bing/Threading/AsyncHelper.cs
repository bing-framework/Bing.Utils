using Bing.Helpers;

namespace Bing.Threading;

/// <summary>
/// 异步帮助类，提供用于检查和操作异步方法及任务类型的工具方法。
/// </summary>
public static class AsyncHelper
{
    /// <summary>
    /// 检查给定方法是否为异步方法
    /// </summary>
    /// <param name="method">要检查的方法</param>
    /// <returns>如果方法返回 <see cref="Task"/> 或 <see cref="Task{T}"/>，则返回 true；否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="method"/> 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// // 检查方法是否为异步方法
    /// MethodInfo method = typeof(MyService).GetMethod("GetDataAsync");
    /// bool isAsync = method.IsAsync();
    /// 
    /// if (isAsync)
    /// {
    ///     // 处理异步方法
    ///     Console.WriteLine("此方法是异步方法");
    /// }
    /// </code>
    /// </example>
    public static bool IsAsync(this MethodInfo method)
    {
        Check.NotNull(method, nameof(method));
        return method.ReturnType.IsTaskOrTaskOfT();
    }

    /// <summary>
    /// 检查给定的 Type 类型是否是 <see cref="Task"/> 或 <see cref="Task{T}"/>。
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>如果类型是 <see cref="Task"/> 或 <see cref="Task{T}"/>，则返回 true；否则返回 false</returns>
    /// <example>
    /// <code>
    /// // 检查各种类型
    /// bool isTask1 = typeof(Task).IsTaskOrTaskOfT();                // 返回 true
    /// bool isTask2 = typeof(Task&lt;int&gt;).IsTaskOrTaskOfT();     // 返回 true
    /// bool isTask3 = typeof(string).IsTaskOrTaskOfT();              // 返回 false
    /// bool isTask4 = typeof(ValueTask&lt;int&gt;).IsTaskOrTaskOfT(); // 返回 false
    /// </code>
    /// </example>
    public static bool IsTaskOrTaskOfT(this Type type) => type == typeof(Task) || type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);

    /// <summary>
    /// 检查给定的 Type 类型是否是 <see cref="Task{T}"/>。
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>如果类型是 <see cref="Task{T}"/>，则返回 true；否则返回 false</returns>
    /// <example>
    /// <code>
    /// // 检查各种类型
    /// bool isTaskOfT1 = typeof(Task).IsTaskOfT();                // 返回 false
    /// bool isTaskOfT2 = typeof(Task&lt;int&gt;).IsTaskOfT();     // 返回 true
    /// bool isTaskOfT3 = typeof(string).IsTaskOfT();              // 返回 false
    /// bool isTaskOfT4 = typeof(ValueTask&lt;int&gt;).IsTaskOfT(); // 返回 false
    /// </code>
    /// </example>
    public static bool IsTaskOfT(this Type type) => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);

    /// <summary>
    /// 解封装（Unwrap）可能包含 Task 的类型，获取实际的结果类型。
    /// </summary>
    /// <param name="type">要解封装的类型。</param>
    /// <returns>解封装后的实际类型。如果是 <see cref="Task"/>，返回 <see cref="Void"/> 类型；如果是 <see cref="Task{T}"/>，返回 T 类型；其他情况返回原类型</returns>
    /// <example>
    /// <code>
    /// // 解封装各种类型
    /// Type type1 = AsyncHelper.UnwrapTask(typeof(Task));          // 返回 typeof(void)
    /// Type type2 = AsyncHelper.UnwrapTask(typeof(Task&lt;int&gt;)); // 返回 typeof(int)
    /// Type type3 = AsyncHelper.UnwrapTask(typeof(string));        // 返回 typeof(string)
    /// 
    /// // 在反射场景中的应用
    /// MethodInfo method = typeof(MyService).GetMethod("GetDataAsync");
    /// Type returnType = AsyncHelper.UnwrapTask(method.ReturnType);
    /// Console.WriteLine($"方法的实际返回类型是: {returnType.Name}");
    /// </code>
    /// </example>
    public static Type UnwrapTask(Type type)
    {
        Check.NotNull(type, nameof(type));
        if (type == typeof(Task))
            return typeof(void);
        if (type.IsTaskOfT())
            return type.GenericTypeArguments[0];
        return type;
    }
}
