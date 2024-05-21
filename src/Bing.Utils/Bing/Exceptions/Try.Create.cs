namespace Bing.Exceptions;

// 尝试 - 创建
public static partial class Try
{
    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T>(Func<T> createFunction, string cause = null)
    {
        try
        {
            return LiftValue(createFunction());
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{TResult}"/> 实例。
    /// </summary>
    /// <typeparam name="T">输入参数类型</typeparam>
    /// <typeparam name="TResult">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{TResult}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{TResult}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{TResult}"/> 实例包含该异常。
    /// </returns>
    public static Try<TResult> Create<T, TResult>(Func<T, TResult> createFunction, T arg, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg));
        }
        catch (Exception ex)
        {
            return LiftException<TResult>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T>(Func<T1, T2, T> createFunction,
        T1 arg1, T2 arg2, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T>(Func<T1, T2, T3, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T>(Func<T1, T2, T3, T4, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T>(Func<T1, T2, T3, T4, T5, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T>(Func<T1, T2, T3, T4, T5, T6, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T>(Func<T1, T2, T3, T4, T5, T6, T7, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T13">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg13">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12,
                arg13));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T13">输入参数类型</typeparam>
    /// <typeparam name="T14">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg13">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg14">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12,
                arg13, arg14));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T13">输入参数类型</typeparam>
    /// <typeparam name="T14">输入参数类型</typeparam>
    /// <typeparam name="T15">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg13">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg14">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg15">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12,
                arg13, arg14, arg15));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }

    /// <summary>
    /// 创建一个新的 <see cref="Try{T}"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T13">输入参数类型</typeparam>
    /// <typeparam name="T14">输入参数类型</typeparam>
    /// <typeparam name="T15">输入参数类型</typeparam>
    /// <typeparam name="T16">输入参数类型</typeparam>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFunction">创建值的函数</param>
    /// <param name="arg1">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg13">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg14">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg15">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="arg16">传递给 <paramref name="createFunction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="Try{T}"/> 实例。
    /// 如果 <paramref name="createFunction"/> 成功执行，返回的 <see cref="Try{T}"/> 实例的值为 <paramref name="createFunction"/> 的返回值。
    /// 如果 <paramref name="createFunction"/> 抛出异常，返回的 <see cref="Try{T}"/> 实例包含该异常。
    /// </returns>
    public static Try<T> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T> createFunction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, string cause = null)
    {
        try
        {
            return LiftValue(createFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12,
                arg13, arg14, arg15, arg16));
        }
        catch (Exception ex)
        {
            return LiftException<T>(ex, cause);
        }
    }
}