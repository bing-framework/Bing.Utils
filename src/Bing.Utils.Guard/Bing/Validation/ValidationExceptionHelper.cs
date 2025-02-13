using System.Reflection;
using AspectCore.Extensions.Reflection;
using Bing.Exceptions;
using Bing.Reflection;

namespace Bing.Validation;

/// <summary>
/// 验证异常帮助类
/// </summary>
internal static class ValidationExceptionHelper
{
    /// <summary>
    /// 如果断言为 false，则创建并引发指定类型的异常。
    /// </summary>
    /// <typeparam name="TException">要引发的异常类型。</typeparam>
    /// <param name="assertion">断言条件。</param>
    /// <param name="exceptionParams">异常构造函数的参数。</param>
    public static void WrapAndAndRaise<TException>(bool assertion, params object[] exceptionParams)
        where TException : Exception
    {
        if (assertion)
            return;

        var exception = CreateException(typeof(TException), exceptionParams);
        var wrappedException = exception as Exception;
        ExceptionHelper.PrepareForRethrow(wrappedException);
    }

    /// <summary>
    /// 创建异常
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="args">参数</param>
    private static object CreateException(Type type, params object[] args)
    {
        return args is null || args.Length == 0
            ? type.GetConstructors().FirstOrDefault(WithoutParamPredicate)?.GetReflector().Invoke()
            : type.GetConstructor(Types.Of(args))?.GetReflector().Invoke(args);

        bool WithoutParamPredicate(MethodBase ci) => !ci.GetParameters().Any();
    }
}