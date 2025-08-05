using System.Diagnostics;
using Bing.Reflection;
using Bing.Text;
using Bing.Utils.Properties;

namespace Bing.Helpers;

/// <summary>
/// 参数验证辅助类，提供常用的参数检查方法。
/// </summary>
/// <remarks>
/// 此类包含多种参数验证方法，用于确保方法参数的有效性。
/// 所有方法都会在验证失败时抛出相应的异常。
/// </remarks>
[DebuggerStepThrough]
public static class Check
{
    #region Required(断言验证)

    /// <summary>
    /// 验证指定的断言是否为真，如果不为真，抛出指定类型的异常。
    /// </summary>
    /// <typeparam name="TException">要抛出的异常类型</typeparam>
    /// <param name="assertion">要验证的断言条件</param>
    /// <param name="message">异常消息</param>
    /// <exception cref="ArgumentNullException">当消息为空时抛出</exception>
    private static void Require<TException>(bool assertion, string message) 
        where TException : Exception
    {
        if (assertion)
            return;
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message), "异常消息不能为空");

        var exception = (TException)Activator.CreateInstance(typeof(TException), message);
        if (exception == null)
            throw new InvalidOperationException($"无法创建异常类型 {typeof(TException).Name} 的实例");
        throw exception;
    }

    /// <summary>
    /// 验证指定值的断言表达式是否为真，不为真时抛出 <see cref="Exception"/> 异常。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="assertionFunc">验证断言函数</param>
    /// <param name="message">异常消息</param>
    /// <exception cref="ArgumentNullException">当断言函数为 null 时抛出</exception>
    /// <exception cref="Exception">当断言验证失败时抛出</exception>
    public static void Required<T>(T value, Func<T, bool> assertionFunc, string message)
    {
        if (assertionFunc == null)
            throw new ArgumentNullException(nameof(assertionFunc), "断言函数不能为空");
        Require<Exception>(assertionFunc(value), message);
    }

    /// <summary>
    /// 验证指定值的断言表达式是否为真，不为真时抛出指定类型的异常。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <typeparam name="TException">异常类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="assertionFunc">验证断言函数</param>
    /// <param name="message">异常消息</param>
    /// <exception cref="ArgumentNullException">当断言函数为 null 时抛出</exception>
    /// <exception cref="Exception">当断言验证失败时抛出 <typeparamref name="TException"/> 类型的异常</exception>
    public static void Required<T, TException>(T value, Func<T, bool> assertionFunc, string message)
        where TException : Exception
    {
        if (assertionFunc == null)
            throw new ArgumentNullException(nameof(assertionFunc), "断言函数不能为空");
        Require<TException>(assertionFunc(value), message);
    }

    #endregion

    #region NotNull(非空验证)

    /// <summary>
    /// 验证参数不能为 null，否则抛出 <see cref="ArgumentNullException"/> 异常。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentNullException">当值为 null 时抛出</exception>
    public static T NotNull<T>(T value, string parameterName)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, parameterName);
#else
        if (value is null)
            throw new ArgumentNullException(parameterName, string.Format(R.ParameterCheck_NotNull, parameterName));
#endif
        return value;
    }

    /// <summary>
    /// 验证参数不能为 null，否则抛出 <see cref="ArgumentNullException"/> 异常。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="message">自定义错误消息</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentNullException">当值为 null 时抛出</exception>
    public static T NotNull<T>(T value, string parameterName, string message)
    {
        if (value == null)
            throw new ArgumentNullException(parameterName, message);
        return value;
    }

    /// <summary>
    /// 验证字符串不能为 null，并可选择验证长度范围。
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="maxLength">最大长度，默认为 <see cref="int.MaxValue"/></param>
    /// <param name="minLength">最小长度，默认为 0</param>
    /// <returns>验证通过后的原始字符串</returns>
    /// <exception cref="ArgumentException">当字符串为 null 或长度超出范围时抛出</exception>
    public static string NotNull(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value == null)
            throw new ArgumentException(string.Format(R.ParameterCheck_NotNull, parameterName), parameterName);
        ValidateStringLength(value, parameterName, maxLength, minLength);
        return value;
    }

    /// <summary>
    /// 验证字符串不能为 null 或空白字符，并可选择验证长度范围。
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="maxLength">最大长度，默认为 <see cref="int.MaxValue"/></param>
    /// <param name="minLength">最小长度，默认为 0</param>
    /// <returns>验证通过后的原始字符串</returns>
    /// <exception cref="ArgumentException">当字符串为 null、空或空白字符，或长度超出范围时抛出</exception>
    public static string NotNullOrWhiteSpace(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value.IsNullOrWhiteSpace())
            throw new ArgumentException(string.Format(R.ParameterCheck_NotNullOrEmpty_String, parameterName), parameterName);
        ValidateStringLength(value, parameterName, maxLength, minLength);
        return value;
    }

    /// <summary>
    /// 验证字符串不能为 null 或空字符串，并可选择验证长度范围。
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="maxLength">最大长度，默认为 <see cref="int.MaxValue"/></param>
    /// <param name="minLength">最小长度，默认为 0</param>
    /// <returns>验证通过后的原始字符串</returns>
    /// <exception cref="ArgumentException">当字符串为 null、空或长度超出范围时抛出</exception>
    public static string NotNullOrEmpty(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value.IsNullOrEmpty())
            throw new ArgumentException(string.Format(R.ParameterCheck_NotNullOrEmpty_String, parameterName), parameterName);
        ValidateStringLength(value, parameterName, maxLength, minLength);
        return value;
    }

    /// <summary>
    /// 验证 Guid 值不能为 <see cref="Guid.Empty"/>。
    /// </summary>
    /// <param name="value">要验证的 Guid 值</param>
    /// <param name="parameterName">参数名称</param>
    /// <exception cref="ArgumentException">当 Guid 为 Empty 时抛出</exception>
    public static void NotEmpty(Guid value, string parameterName) =>
        Require<ArgumentException>(value != Guid.Empty, string.Format(R.ParameterCheck_NotEmpty_Guid, parameterName));

    /// <summary>
    /// 验证集合不能为 null 或空。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="value">要验证的集合</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始集合</returns>
    /// <exception cref="ArgumentException">当集合为 null 或空时抛出</exception>
    public static ICollection<T> NotNullOrEmpty<T>(ICollection<T> value, string parameterName)
    {
        if (value == null || value.Count <= 0)
            throw new ArgumentException($"参数 {parameterName} 不能为 null 或空集合", parameterName);
        return value;
    }

    /// <summary>
    /// 验证可枚举集合不能为 null 或空。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="collection">要验证的集合</param>
    /// <param name="parameterName">参数名称</param>
    /// <exception cref="ArgumentNullException">当集合为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当集合为空时抛出</exception>
    public static void NotNullOrEmpty<T>(IEnumerable<T> collection, string parameterName)
    {
        NotNull(collection, parameterName);
        Require<ArgumentException>(collection.Any(), string.Format(R.ParameterCheck_NotNullOrEmpty_Collection, parameterName));
    }

    /// <summary>
    /// 验证字典不能为 null 或空。
    /// </summary>
    /// <typeparam name="T">字典值类型</typeparam>
    /// <param name="dictionary">要验证的字典</param>
    /// <param name="parameterName">参数名称</param>
    /// <exception cref="ArgumentNullException">当字典为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当字典为空时抛出</exception>
    public static void NotNullOrEmpty<T>(IDictionary<string, T> dictionary, string parameterName)
    {
        NotNull(dictionary, parameterName);
        Require<ArgumentException>(dictionary.Any(), string.Format(R.ParameterCheck_NotNullOrEmpty_Collection));
    }

    #endregion

    #region AssignableTo(类型分配验证)

    /// <summary>
    /// 验证类型是否可分配给指定的基础类型。
    /// </summary>
    /// <typeparam name="TBaseType">基础类型</typeparam>
    /// <param name="type">要验证的类型</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始类型</returns>
    /// <exception cref="ArgumentNullException">当类型为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当类型不能分配给基础类型时抛出</exception>
    public static Type AssignableTo<TBaseType>(Type type, string parameterName)
    {
        NotNull(type, parameterName);
        if (!type.IsAssignableTo(typeof(TBaseType)))
        {
            var message = $"参数 {parameterName} (类型: {type.AssemblyQualifiedName}) 必须可分配给 {Reflections.GetFullNameWithAssemblyName(typeof(TBaseType))}";
            throw new ArgumentException(message, parameterName);
        }
        return type;
    }

    #endregion

    #region Length(验证字符串的长度是否符合指定的范围)

    /// <summary>
    /// 验证字符串长度是否在指定范围内。
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="minLength">最小长度，默认为 0</param>
    /// <returns>验证通过后的原始字符串</returns>
    /// <exception cref="ArgumentException">当字符串长度超出范围时抛出</exception>
    public static string Length(string value, string parameterName, int maxLength, int minLength = 0)
    {
        if (minLength > 0)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"参数 {parameterName} 不能为 null 或空字符串", parameterName);

            if (value!.Length < minLength)
                throw new ArgumentException($"参数 {parameterName} 的长度必须大于等于 {minLength}", parameterName);
        }

        if (value != null && value.Length > maxLength)
            throw new ArgumentException($"参数 {parameterName} 的长度必须小于等于 {maxLength}", parameterName);
        return value;
    }

    #endregion

    #region Positive(正数验证)

    /// <summary>
    /// 验证 short 值为正数。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值不是正数时抛出</exception>
    public static short Positive(short value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"参数 {parameterName} 不能等于零", parameterName),
            < 0 => throw new ArgumentException($"参数 {parameterName} 不能小于零", parameterName),
            _ => value
        };
    }

    /// <summary>
    /// 验证 int 值为正数。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值不是正数时抛出</exception>
    public static int Positive(int value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"参数 {parameterName} 不能等于零", parameterName),
            < 0 => throw new ArgumentException($"参数 {parameterName} 不能小于零", parameterName),
            _ => value
        };
    }

    /// <summary>
    /// 验证 long 值为正数。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值不是正数时抛出</exception>
    public static long Positive(long value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"参数 {parameterName} 不能等于零", parameterName),
            < 0 => throw new ArgumentException($"参数 {parameterName} 不能小于零", parameterName),
            _ => value
        };
    }

    /// <summary>
    /// 验证 float 值为正数。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值不是正数时抛出</exception>
    public static float Positive(float value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"参数 {parameterName} 不能等于零", parameterName),
            < 0 => throw new ArgumentException($"参数 {parameterName} 不能小于零", parameterName),
            _ => value
        };
    }

    /// <summary>
    /// 验证 double 值为正数。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值不是正数时抛出</exception>
    public static double Positive(double value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"参数 {parameterName} 不能等于零", parameterName),
            < 0 => throw new ArgumentException($"参数 {parameterName} 不能小于零", parameterName),
            _ => value
        };
    }

    /// <summary>
    /// 验证 decimal 值为正数。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值不是正数时抛出</exception>
    public static decimal Positive(decimal value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"参数 {parameterName} 不能等于零", parameterName),
            < 0 => throw new ArgumentException($"参数 {parameterName} 不能小于零", parameterName),
            _ => value
        };
    }

    #endregion

    #region Range(范围验证)

    /// <summary>
    /// 验证 short 值在指定范围内。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="minimumValue">最小值</param>
    /// <param name="maximumValue">最大值</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值超出范围时抛出</exception>
    public static short Range(short value, string parameterName, short minimumValue, short maximumValue = short.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"参数 {parameterName} 的值 {value} 超出范围 [{minimumValue}, {maximumValue}]", parameterName);
        return value;
    }

    /// <summary>
    /// 验证 int 值在指定范围内。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="minimumValue">最小值</param>
    /// <param name="maximumValue">最大值</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值超出范围时抛出</exception>
    public static int Range(int value, string parameterName, int minimumValue, int maximumValue = int.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"参数 {parameterName} 的值 {value} 超出范围 [{minimumValue}, {maximumValue}]", parameterName);
        return value;
    }

    /// <summary>
    /// 验证 long 值在指定范围内。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="minimumValue">最小值</param>
    /// <param name="maximumValue">最大值</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值超出范围时抛出</exception>
    public static long Range(long value, string parameterName, long minimumValue, long maximumValue = long.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"参数 {parameterName} 的值 {value} 超出范围 [{minimumValue}, {maximumValue}]", parameterName);
        return value;
    }

    /// <summary>
    /// 验证 float 值在指定范围内。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="minimumValue">最小值</param>
    /// <param name="maximumValue">最大值</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值超出范围时抛出</exception>
    public static float Range(float value, string parameterName, float minimumValue, float maximumValue = float.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"参数 {parameterName} 的值 {value} 超出范围 [{minimumValue}, {maximumValue}]", parameterName);
        return value;
    }

    /// <summary>
    /// 验证 double 值在指定范围内。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="minimumValue">最小值</param>
    /// <param name="maximumValue">最大值</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值超出范围时抛出</exception>
    public static double Range(double value, string parameterName, double minimumValue, double maximumValue = double.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"参数 {parameterName} 的值 {value} 超出范围 [{minimumValue}, {maximumValue}]", parameterName);
        return value;
    }

    /// <summary>
    /// 验证 decimal 值在指定范围内。
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="minimumValue">最小值</param>
    /// <param name="maximumValue">最大值</param>
    /// <returns>验证通过后的原始值</returns>
    /// <exception cref="ArgumentException">当值超出范围时抛出</exception>
    public static decimal Range(decimal value, string parameterName, decimal minimumValue, decimal maximumValue = decimal.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"参数 {parameterName} 的值 {value} 超出范围 [{minimumValue}, {maximumValue}]", parameterName);
        return value;
    }

    #endregion

    #region Between(区间验证)

    /// <summary>
    /// 验证值必须小于指定目标值。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="target">比较目标值</param>
    /// <param name="canEqual">是否允许等于目标值，默认为 false</param>
    /// <exception cref="ArgumentOutOfRangeException">当值不满足条件时抛出</exception>
    public static void LessThan<T>(T value, string parameterName, T target, bool canEqual = false)
        where T : IComparable<T>
    {
        var flag = canEqual ? value.CompareTo(target) <= 0 : value.CompareTo(target) < 0;
        var format = canEqual ? R.ParameterCheck_NotLessThanOrEqual : R.ParameterCheck_NotLessThan;
        Require<ArgumentOutOfRangeException>(flag, string.Format(format, parameterName, target));
    }

    /// <summary>
    /// 验证值必须大于指定目标值。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="target">比较目标值</param>
    /// <param name="canEqual">是否允许等于目标值，默认为 false</param>
    /// <exception cref="ArgumentOutOfRangeException">当值不满足条件时抛出</exception>
    public static void GreaterThan<T>(T value, string parameterName, T target, bool canEqual = false)
        where T : IComparable<T>
    {
        bool flag = canEqual ? value.CompareTo(target) >= 0 : value.CompareTo(target) > 0;
        string format = canEqual ? R.ParameterCheck_NotGreaterThanOrEqual : R.ParameterCheck_NotGreaterThan;
        Require<ArgumentOutOfRangeException>(flag, string.Format(format, parameterName, target));
    }

    /// <summary>
    /// 验证值必须在指定范围之间。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="start">范围起始值</param>
    /// <param name="end">范围结束值</param>
    /// <param name="startEqual">是否允许等于起始值，默认为 false</param>
    /// <param name="endEqual">是否允许等于结束值，默认为 false</param>
    /// <exception cref="ArgumentOutOfRangeException">当值不在指定范围内时抛出</exception>
    public static void Between<T>(T value, string parameterName, T start, T end, bool startEqual = false, bool endEqual = false)
        where T : IComparable<T>
    {
        bool flag = startEqual ? value.CompareTo(start) >= 0 : value.CompareTo(start) > 0;
        string message = startEqual
            ? string.Format(R.ParameterCheck_Between, parameterName, start, end)
            : string.Format(R.ParameterCheck_BetweenNotEqual, parameterName, start, end, start);
        Require<ArgumentOutOfRangeException>(flag, message);

        flag = endEqual ? value.CompareTo(end) <= 0 : value.CompareTo(end) < 0;
        message = endEqual
            ? string.Format(R.ParameterCheck_Between, parameterName, start, end)
            : string.Format(R.ParameterCheck_BetweenNotEqual, parameterName, start, end, end);
        Require<ArgumentOutOfRangeException>(flag, message);
    }

    /// <summary>
    /// 验证 TimeSpan 值不能为负数或零。
    /// </summary>
    /// <param name="timeSpan">要验证的时间跨度</param>
    /// <param name="parameterName">参数名称</param>
    /// <exception cref="ArgumentOutOfRangeException">当时间跨度为负数或零时抛出</exception>
    public static void NotNegativeOrZero(TimeSpan timeSpan, string parameterName)
    {
        Require<ArgumentOutOfRangeException>(
            timeSpan > TimeSpan.Zero,
            $"参数 {parameterName} 的值不能为负数或零，当前值: {timeSpan}");
    }

    #endregion

    #region IO(文件系统验证)

    /// <summary>
    /// 验证指定目录必须存在。
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <param name="parameterName">参数名称</param>
    /// <exception cref="ArgumentNullException">当目录路径为 null 时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static void DirectoryExists(string directory, string parameterName = null)
    {
        NotNull(directory, parameterName ?? nameof(directory));
        Require<DirectoryNotFoundException>(Directory.Exists(directory), string.Format(R.ParameterCheck_DirectoryNotExists, directory));
    }

    /// <summary>
    /// 验证指定文件必须存在。
    /// </summary>
    /// <param name="fileName">文件路径</param>
    /// <param name="parameterName">参数名称</param>
    /// <exception cref="ArgumentNullException">当文件路径为 null 时抛出</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static void FileExists(string fileName, string parameterName = null)
    {
        NotNull(fileName, parameterName ?? nameof(fileName));
        Require<FileNotFoundException>(File.Exists(fileName), string.Format(R.ParameterCheck_FileNotExists, fileName));
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 验证字符串长度。
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="minLength">最小长度</param>
    private static void ValidateStringLength(string value, string parameterName, int maxLength, int minLength)
    {
        if (value.Length > maxLength)
            throw new ArgumentException($"参数 {parameterName} 的长度 {value.Length} 超过最大允许长度 {maxLength}", parameterName);

        if (value.Length < minLength)
            throw new ArgumentException($"参数 {parameterName} 的长度 {value.Length} 小于最小要求长度 {minLength}", parameterName);
    }

    #endregion
}