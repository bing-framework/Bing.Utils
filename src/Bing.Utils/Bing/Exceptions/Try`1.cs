namespace Bing.Exceptions;

/// <summary>
/// 尝试
/// </summary>
/// <typeparam name="T">类型</typeparam>
public abstract class Try<T>
{
    /// <summary>
    /// 是否失败
    /// </summary>
    public abstract bool IsFailure { get; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public abstract bool IsSuccess { get; }

    /// <summary>
    /// 异常
    /// </summary>
    public abstract TryCreatingValueException Exception { get; }

    /// <summary>
    /// 值
    /// </summary>
    public abstract T Value { get; }

    /// <summary>
    /// 获取值
    /// </summary>
    public virtual T GetValue() => Value;

    /// <summary>
    /// 获取值
    /// </summary>
    public virtual Task<T> GetValueAsync() => IsSuccess ? Task.FromResult(Value) : FromException<T>(Exception);

    /// <summary>
    /// 安全获取值
    /// </summary>
    public virtual T GetSafeValue() => IsSuccess ? Value : default!;

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultVal">默认值</param>
    public virtual T GetSafeValue(T defaultVal) => IsSuccess ? Value : defaultVal;

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual T GetSafeValue(Func<T> defaultValFunc) => IsSuccess ? Value : defaultValFunc is null ? default! : defaultValFunc();

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual T GetSafeValue(Func<TryCreatingValueException, T> defaultValFunc) => IsSuccess ? Value : defaultValFunc is null ? default! : defaultValFunc(Exception);

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual T GetSafeValue(Func<Exception, string, T> defaultValFunc) => IsSuccess ? Value : defaultValFunc is null ? default! : defaultValFunc(Exception?.InnerException, Exception?.Cause);

    /// <summary>
    /// 安全获取值
    /// </summary>
    public virtual Task<T> GetSafeValueAsync() => Task.FromResult(GetSafeValue());

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultVal">默认值</param>
    public virtual Task<T> GetSafeValueAsync(T defaultVal) => Task.FromResult(GetSafeValue(defaultVal));

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Task<T> GetSafeValueAsync(Func<T> defaultValFunc) => Task.FromResult(GetSafeValue(defaultValFunc));

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Task<T> GetSafeValueAsync(Func<TryCreatingValueException, T> defaultValFunc) => Task.FromResult(GetSafeValue(defaultValFunc));

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Task<T> GetSafeValueAsync(Func<Exception, string, T> defaultValFunc) => Task.FromResult(GetSafeValue(defaultValFunc));

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValAsyncFunc">默认值函数</param>
    public virtual Task<T> GetSafeValueAsync(Func<Task<T>> defaultValAsyncFunc)
    {
        if (IsSuccess)
            return Task.FromResult(Value);
        return defaultValAsyncFunc is null
            ? Task.FromResult(default(T)!)
            : defaultValAsyncFunc();
    }

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValAsyncFunc">默认值函数</param>
    public virtual Task<T> GetSafeValueAsync(Func<TryCreatingValueException, Task<T>> defaultValAsyncFunc)
    {
        if (IsSuccess)
            return Task.FromResult(Value);
        return defaultValAsyncFunc is null
            ? Task.FromResult(default(T)!)
            : defaultValAsyncFunc(Exception);
    }

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <param name="defaultValAsyncFunc">默认值函数</param>
    public virtual Task<T> GetSafeValueAsync(Func<Exception, string, Task<T>> defaultValAsyncFunc)
    {
        if (IsSuccess)
            return Task.FromResult(Value);
        return defaultValAsyncFunc is null
            ? Task.FromResult(default(T)!)
            : defaultValAsyncFunc(Exception?.InnerException, Exception?.Cause);
    }

    /// <summary>
    /// 尝试获取值
    /// </summary>
    /// <param name="value">值</param>
    public virtual bool TryGetValue(out T value)
    {
        value = GetSafeValue();
        return IsSuccess;
    }

    /// <summary>
    /// 尝试获取值
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultVal">默认值</param>
    public virtual bool TryGetValue(out T value, T defaultVal)
    {
        value = GetSafeValue(defaultVal);
        return true;
    }

    /// <summary>
    /// 尝试获取值
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual bool TryGetValue(out T value, Func<T> defaultValFunc)
    {
        try
        {
            value = GetSafeValue(defaultValFunc);
            return true;
        }
        catch
        {
            value = default!;
            return false;
        }
    }

    /// <summary>
    /// 尝试获取值
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual bool TryGetValue(out T value, Func<TryCreatingValueException, T> defaultValFunc)
    {
        try
        {
            value = GetSafeValue(defaultValFunc);
            return true;
        }
        catch
        {
            value = default!;
            return false;
        }
    }

    /// <summary>
    /// 尝试获取值
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual bool TryGetValue(out T value, Func<Exception, string, T> defaultValFunc)
    {
        try
        {
            value = GetSafeValue(defaultValFunc);
            return true;
        }
        catch
        {
            value = default!;
            return false;
        }
    }

    /// <summary>
    /// 获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    public virtual Try<T> GetValueOut(out T value)
    {
        value = GetValue();
        return this;
    }

    /// <summary>
    /// 获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    public virtual Try<T> GetValueOutAsync(out Task<T> value)
    {
        value = GetValueAsync();
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    public virtual Try<T> GetSafeValueOut(out T value)
    {
        value = GetSafeValue();
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultVal">默认值</param>
    public virtual Try<T> GetSafeValueOut(out T value, T defaultVal)
    {
        value = GetSafeValue(defaultVal);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOut(out T value, Func<T> defaultValFunc)
    {
        value = GetSafeValue(defaultValFunc);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOut(out T value, Func<TryCreatingValueException, T> defaultValFunc)
    {
        value = GetSafeValue(defaultValFunc);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOut(out T value, Func<Exception, string, T> defaultValFunc)
    {
        value = GetSafeValue(defaultValFunc);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    public virtual Try<T> GetSafeValueOutAsync(out Task<T> value)
    {
        value = GetSafeValueAsync();
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultVal">默认值</param>
    public virtual Try<T> GetSafeValueOutAsync(out Task<T> value, T defaultVal)
    {
        value = GetSafeValueAsync(defaultVal);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOutAsync(out Task<T> value, Func<T> defaultValFunc)
    {
        value = GetSafeValueAsync(defaultValFunc);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOutAsync(out Task<T> value, Func<TryCreatingValueException, T> defaultValFunc)
    {
        value = GetSafeValueAsync(defaultValFunc);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOutAsync(out Task<T> value, Func<Exception, string, T> defaultValFunc)
    {
        value = GetSafeValueAsync(defaultValFunc);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOutAsync(out Task<T> value, Func<Task<T>> defaultValFunc)
    {
        value = GetSafeValueAsync(defaultValFunc);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOutAsync(out Task<T> value, Func<TryCreatingValueException, Task<T>> defaultValFunc)
    {
        value = GetSafeValueAsync(defaultValFunc);
        return this;
    }

    /// <summary>
    /// 安全获取值并输出
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defaultValFunc">默认值函数</param>
    public virtual Try<T> GetSafeValueOutAsync(out Task<T> value, Func<Exception, string, Task<T>> defaultValFunc)
    {
        value = GetSafeValueAsync(defaultValFunc);
        return this;
    }

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(Try<T> left, Try<T> right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(Try<T> left, Try<T> right) => !(left == right);

    /// <summary>
    /// 重载输出字符串
    /// </summary>
    public abstract override string ToString();

    /// <summary>
    /// 重载相等
    /// </summary>
    /// <param name="obj">对象</param>
    public abstract override bool Equals(object obj);

    /// <summary>
    /// 重载获取哈希码
    /// </summary>
    public abstract override int GetHashCode();

    /// <summary>
    /// 解构
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="exception">异常</param>
    public abstract void Deconstruct(out T value, out Exception exception);

    /// <summary>
    /// 解构
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="tryResult">尝试结果</param>
    /// <param name="exception">异常</param>
    public abstract void Deconstruct(out T value, out bool tryResult, out Exception exception);

    /// <summary>
    /// 将异常转换为指定类型异常
    /// </summary>
    /// <typeparam name="TException">异常类型</typeparam>
    public TException ExceptionAs<TException>() 
        where TException : Exception
    {
        Exception ex = Exception;
        while (ex is not null && ex is not TException)
            ex = ex.InnerException;
        return ex as TException;
    }

    /// <summary>
    /// 恢复
    /// </summary>
    /// <param name="recoverFunction">恢复函数</param>
    public abstract Try<T> Recover(Func<TryCreatingValueException, T> recoverFunction);

    /// <summary>
    /// 恢复
    /// </summary>
    /// <param name="recoverFunction">恢复函数</param>
    public abstract Try<T> Recover(Func<Exception, string, T> recoverFunction);

    /// <summary>
    /// 恢复
    /// </summary>
    /// <param name="recoverFunction">恢复函数</param>
    public abstract Try<T> RecoverWith(Func<TryCreatingValueException, Try<T>> recoverFunction);

    /// <summary>
    /// 恢复
    /// </summary>
    /// <param name="recoverFunction">恢复函数</param>
    public abstract Try<T> RecoverWith(Func<Exception, string, Try<T>> recoverFunction);

    /// <summary>
    /// 匹配
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="whenValue">条件值函数</param>
    /// <param name="whenException">条件异常</param>
    public abstract TResult Match<TResult>(Func<T, TResult> whenValue, Func<TryCreatingValueException, TResult> whenException);

    /// <summary>
    /// 匹配
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="whenValue">条件值函数</param>
    /// <param name="whenException">条件异常</param>
    public abstract TResult Match<TResult>(Func<T, TResult> whenValue, Func<Exception, string, TResult> whenException);

    /// <summary>
    /// 映射
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="map">映射函数</param>
    public Try<TResult> Map<TResult>(Func<T, TResult> map)
    {
        if (map is null)
            throw new ArgumentNullException(nameof(map));
        return Bind(value => Try.Create(() => map(value)));
    }

    /// <summary>
    /// 触发
    /// </summary>
    /// <param name="successAction">成功操作</param>
    /// <param name="failureAction">失败操作</param>
    public abstract Try<T> Tap(Action<T> successAction = null, Action<TryCreatingValueException> failureAction = null);

    /// <summary>
    /// 触发
    /// </summary>
    /// <param name="successAction">成功操作</param>
    /// <param name="failureAction">失败操作</param>
    public abstract Try<T> Tap(Action<T> successAction = null, Action<Exception, string> failureAction = null);

    /// <summary>
    /// 绑定
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="bind">绑定函数</param>
    internal abstract Try<TResult> Bind<TResult>(Func<T, Try<TResult>> bind);

    /// <summary>
    /// 将异常转换为指定类型结果
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="exception">异常</param>
    private static Task<TResult> FromException<TResult>(Exception exception)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));
        return Task.FromException<TResult>(exception);
    }
}