using System.Diagnostics;
using Bing.Reflection;
using Bing.Text;
using Bing.Utils.Properties;

namespace Bing.Helpers;

/// <summary>
/// 参数检查 操作
/// </summary>
[DebuggerStepThrough]
public static class Check
{
    #region Required(断言)

    /// <summary>
    /// 验证指定值的断言<paramref name="assertion"/>是否为真，如果不为真，抛出指定消息<paramref name="message"/>的指定类型<typeparamref name="TException"/>异常
    /// </summary>
    /// <typeparam name="TException">异常类型</typeparam>
    /// <param name="assertion">要验证的断言</param>
    /// <param name="message">异常消息</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static void Require<TException>(bool assertion, string message) where TException : Exception
    {
        if (assertion)
            return;
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message));
        var exception = (TException)Activator.CreateInstance(typeof(TException), message);
        throw exception;
    }

    /// <summary>
    /// 验证指定值的断言表达式是否为真，不为值抛出<see cref="Exception"/>异常
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要判断的值</param>
    /// <param name="assertionFunc">要验证的断言</param>
    /// <param name="message">异常消息</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Required<T>(T value, Func<T, bool> assertionFunc, string message)
    {
        if (assertionFunc == null)
            throw new ArgumentNullException(nameof(assertionFunc));
        Require<Exception>(assertionFunc(value), message);
    }

    /// <summary>
    /// 验证指定值的断言表达式是否为真，不为真抛出<see cref="Exception"/>异常
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <typeparam name="TException">异常类型</typeparam>
    /// <param name="value">要判断的值</param>
    /// <param name="assertionFunc">要验证的断言</param>
    /// <param name="message">异常消息</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Required<T, TException>(T value, Func<T, bool> assertionFunc, string message)
        where TException : Exception
    {
        if (assertionFunc == null)
            throw new ArgumentNullException(nameof(assertionFunc));
        Require<TException>(assertionFunc(value), message);
    }

    #endregion

    #region NotNull(不可空检查)

    /// <summary>
    /// 检查参数不能为空引用，否则抛出<see cref="ArgumentNullException"/>异常
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要判断的值</param>
    /// <param name="parameterName">参数名</param>
    /// <exception cref="ArgumentNullException"></exception>
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
    /// 检查参数不能为空引用，否则抛出<see cref="ArgumentNullException"/>异常
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要判断的值</param>
    /// <param name="parameterName">参数名</param>
    /// <param name="message">错误消息</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static T NotNull<T>(T value, string parameterName, string message)
    {
        if (value == null)
            throw new ArgumentNullException(parameterName, message);
        return value;
    }

    /// <summary>
    /// 检查字符串不能为空引用，否则抛出<see cref="ArgumentException"/>异常
    /// </summary>
    /// <param name="value">要判断的值</param>
    /// <param name="parameterName">参数名</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="minLength">最小长度</param>
    /// <exception cref="ArgumentException"></exception>
    public static string NotNull(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value == null)
            throw new ArgumentException(string.Format(R.ParameterCheck_NotNull, parameterName), parameterName);
        if (value.Length > maxLength)
            throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
        if (value.Length > 0 && value.Length < minLength)
            throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
        return value;
    }

    /// <summary>
    /// 检查字符串不能为空引用或空白字符，否则抛出<see cref="ArgumentException"/>异常
    /// </summary>
    /// <param name="value">要判断的值</param>
    /// <param name="parameterName">参数名</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="minLength">最小长度</param>
    /// <exception cref="ArgumentException"></exception>
    public static string NotNullOrWhiteSpace(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value.IsNullOrWhiteSpace())
            throw new ArgumentException(string.Format(R.ParameterCheck_NotNullOrEmpty_String, parameterName), parameterName);
        if (value.Length > maxLength)
            throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
        if (value.Length > 0 && value.Length < minLength)
            throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
        return value;
    }

    /// <summary>
    /// 检查字符串不能为空引用或空字符串，否则抛出<see cref="ArgumentException"/>异常
    /// </summary>
    /// <param name="value">要判断的值</param>
    /// <param name="parameterName">参数名</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="minLength">最小长度</param>
    /// <exception cref="ArgumentException"></exception>
    public static string NotNullOrEmpty(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value.IsNullOrEmpty())
            throw new ArgumentException(string.Format(R.ParameterCheck_NotNullOrEmpty_String, parameterName), parameterName);
        if (value.Length > maxLength)
            throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
        if (value.Length > 0 && value.Length < minLength)
            throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
        return value;
    }

    /// <summary>
    /// 检查Guid值不能为Guid.Empty，否则抛出<see cref="ArgumentException"/>异常
    /// </summary>
    /// <param name="value">要判断的值</param>
    /// <param name="paramName">参数名</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static void NotEmpty(Guid value, string paramName) => Require<ArgumentException>(value != Guid.Empty, string.Format(R.ParameterCheck_NotEmpty_Guid, paramName));

    /// <summary>
    /// 验证集合参数是否不为 null 或空，若为 null 或空，则引发 <see cref="ArgumentException"/> 异常。
    /// </summary>
    /// <typeparam name="T">集合元素的类型。</typeparam>
    /// <param name="value">要验证的集合。</param>
    /// <param name="paramName">参数的名称。</param>
    /// <returns>原始集合。</returns>
    /// <exception cref="ArgumentException">如果集合为 null 或空，则引发异常。</exception>
    public static ICollection<T> NotNullOrEmpty<T>(ICollection<T> value, string paramName)
    {
        if (value == null || value.Count <= 0)
            throw new ArgumentException($"{paramName} can not be null or empty!", paramName);
        return value;
    }

    /// <summary>
    /// 检查集合不能为空引用或空集合，否则抛出<see cref="ArgumentNullException"/>异常或<see cref="ArgumentException"/>异常。
    /// </summary>
    /// <typeparam name="T">集合项的类型</typeparam>
    /// <param name="collection">要判断的值</param>
    /// <param name="paramName">参数名</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static void NotNullOrEmpty<T>(IEnumerable<T> collection, string paramName)
    {
        NotNull(collection, paramName);
        Require<ArgumentException>(collection.Any(), string.Format(R.ParameterCheck_NotNullOrEmpty_Collection, paramName));
    }

    /// <summary>
    /// 检查字典不能为空引用或空字典，否则抛出<see cref="ArgumentNullException"/>异常或<see cref="ArgumentException"/>异常。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <param name="paramName">参数名</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static void NotNullOrEmpty<T>(IDictionary<string, T> dictionary, string paramName)
    {
        NotNull(dictionary, paramName);
        Require<ArgumentException>(dictionary.Any(), string.Format(R.ParameterCheck_NotNullOrEmpty_Collection));
    }

    #endregion

    #region AssignableTo(验证类型是否可分配给指定基础类型)

    /// <summary>
    /// 验证类型是否可分配给指定基础类型，并返回原始类型。
    /// </summary>
    /// <typeparam name="TBaseType">基础类型。</typeparam>
    /// <param name="type">要验证的类型。</param>
    /// <param name="parameterName">用于抛出异常的参数名称。</param>
    /// <returns>原始类型。</returns>
    /// <exception cref="ArgumentException"></exception>
    public static Type AssignableTo<TBaseType>(Type type, string parameterName)
    {
        NotNull(type, parameterName);
        if (!type.IsAssignableTo(typeof(TBaseType)))
            throw new ArgumentException($"{parameterName} (type of {type.AssemblyQualifiedName}) should be assignable to the {Reflections.GetFullNameWithAssemblyName(typeof(TBaseType))}!");
        return type;
    }

    #endregion

    #region Length(验证字符串的长度是否符合指定的范围)

    /// <summary>
    /// 验证字符串的长度是否符合指定的范围，并返回原始字符串。
    /// </summary>
    /// <param name="value">要验证的字符串。</param>
    /// <param name="parameterName">用于抛出异常的参数名称。</param>
    /// <param name="maxLength">允许的最大长度。</param>
    /// <param name="minLength">允许的最小长度，默认为 0。</param>
    /// <returns>原始字符串。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="value"/> 为 null 或空，并且 <paramref name="minLength"/> 大于 0。
    /// 如果 <paramref name="value"/> 的长度小于 <paramref name="minLength"/>。
    /// 如果 <paramref name="value"/> 的长度大于 <paramref name="maxLength"/>。
    /// </exception>
    public static string Length(string value, string parameterName, int maxLength, int minLength = 0)
    {
        if (minLength > 0)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"{parameterName} can not be null or empty!", parameterName);
            if (value!.Length < minLength)
                throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
        }

        if (value != null && value.Length > maxLength)
            throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
        return value;
    }

    #endregion

    #region Positive(确保值为正数)

    /// <summary>
    /// 确保 <paramref name="value"/> 是正数，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>如果值为正数，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不是正数，则引发异常。</exception>
    public static short Positive(short value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"{parameterName} is equal to zero."),
            < 0 => throw new ArgumentException($"{parameterName} is less than zero."),
            _ => value
        };
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 是正数，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>如果值为正数，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不是正数，则引发异常。</exception>
    public static int Positive(int value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"{parameterName} is equal to zero."),
            < 0 => throw new ArgumentException($"{parameterName} is less than zero."),
            _ => value
        };
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 是正数，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>如果值为正数，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不是正数，则引发异常。</exception>
    public static long Positive(long value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"{parameterName} is equal to zero."),
            < 0 => throw new ArgumentException($"{parameterName} is less than zero."),
            _ => value
        };
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 是正数，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>如果值为正数，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不是正数，则引发异常。</exception>
    public static float Positive(float value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"{parameterName} is equal to zero."),
            < 0 => throw new ArgumentException($"{parameterName} is less than zero."),
            _ => value
        };
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 是正数，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>如果值为正数，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不是正数，则引发异常。</exception>
    public static double Positive(double value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"{parameterName} is equal to zero."),
            < 0 => throw new ArgumentException($"{parameterName} is less than zero."),
            _ => value
        };
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 是正数，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>如果值为正数，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不是正数，则引发异常。</exception>
    public static decimal Positive(decimal value, string parameterName)
    {
        return value switch
        {
            0 => throw new ArgumentException($"{parameterName} is equal to zero."),
            < 0 => throw new ArgumentException($"{parameterName} is less than zero."),
            _ => value
        };
    }

    #endregion

    #region Range(确保值处于指定的范围内)

    /// <summary>
    /// 确保 <paramref name="value"/> 处于指定的范围内，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <param name="minimumValue">最小允许值。</param>
    /// <param name="maximumValue">最大允许值（默认为 <see cref="short.MaxValue"/>）。</param>
    /// <returns>如果值在指定范围内，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不在指定范围内，则引发异常。</exception>
    public static short Range(short value, string parameterName, short minimumValue, short maximumValue = short.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        return value;
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 处于指定的范围内，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <param name="minimumValue">最小允许值。</param>
    /// <param name="maximumValue">最大允许值（默认为 <see cref="int.MaxValue"/>）。</param>
    /// <returns>如果值在指定范围内，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不在指定范围内，则引发异常。</exception>
    public static int Range(int value, string parameterName, int minimumValue, int maximumValue = int.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        return value;
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 处于指定的范围内，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <param name="minimumValue">最小允许值。</param>
    /// <param name="maximumValue">最大允许值（默认为 <see cref="long.MaxValue"/>）。</param>
    /// <returns>如果值在指定范围内，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不在指定范围内，则引发异常。</exception>
    public static long Range(long value, string parameterName, long minimumValue, long maximumValue = long.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        return value;
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 处于指定的范围内，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <param name="minimumValue">最小允许值。</param>
    /// <param name="maximumValue">最大允许值（默认为 <see cref="float.MaxValue"/>）。</param>
    /// <returns>如果值在指定范围内，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不在指定范围内，则引发异常。</exception>
    public static float Range(float value, string parameterName, float minimumValue, float maximumValue = float.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        return value;
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 处于指定的范围内，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <param name="minimumValue">最小允许值。</param>
    /// <param name="maximumValue">最大允许值（默认为 <see cref="double.MaxValue"/>）。</param>
    /// <returns>如果值在指定范围内，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不在指定范围内，则引发异常。</exception>
    public static double Range(double value, string parameterName, double minimumValue, double maximumValue = double.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        return value;
    }

    /// <summary>
    /// 确保 <paramref name="value"/> 处于指定的范围内，否则抛出 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <param name="minimumValue">最小允许值。</param>
    /// <param name="maximumValue">最大允许值（默认为 <see cref="decimal.MaxValue"/>）。</param>
    /// <returns>如果值在指定范围内，则返回原始值。</returns>
    /// <exception cref="ArgumentException">如果值不在指定范围内，则引发异常。</exception>
    public static decimal Range(decimal value, string parameterName, decimal minimumValue, decimal maximumValue = decimal.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        return value;
    }

    #endregion

    #region Between(范围检查)

    /// <summary>
    /// 检查参数必须小于[或可等于，参数canEqual]指定值，否则抛出<see cref="ArgumentOutOfRangeException"/>异常
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">要判断的值</param>
    /// <param name="paramName">参数名</param>
    /// <param name="target">要比较的值</param>
    /// <param name="canEqual">是否可等于</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void LessThan<T>(T value, string paramName, T target, bool canEqual = false)
        where T : IComparable<T>
    {
        var flag = canEqual ? value.CompareTo(target) <= 0 : value.CompareTo(target) < 0;
        var format = canEqual ? R.ParameterCheck_NotLessThanOrEqual : R.ParameterCheck_NotLessThan;
        Require<ArgumentOutOfRangeException>(flag, string.Format(format, paramName, target));
    }

    /// <summary>
    /// 检查参数必须大于[或可等于，参数canEqual]指定值，否则抛出<see cref="ArgumentOutOfRangeException"/>异常
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">要判断的值</param>
    /// <param name="paramName">参数名</param>
    /// <param name="target">要比较的值</param>
    /// <param name="canEqual">是否可等于</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void GreaterThan<T>(T value, string paramName, T target, bool canEqual = false)
        where T : IComparable<T>
    {
        bool flag = canEqual ? value.CompareTo(target) >= 0 : value.CompareTo(target) > 0;
        string format = canEqual ? R.ParameterCheck_NotGreaterThanOrEqual : R.ParameterCheck_NotGreaterThan;
        Require<ArgumentOutOfRangeException>(flag, string.Format(format, paramName, target));
    }

    /// <summary>
    /// 检查参数必须在指定范围之间，否则抛出<see cref="ArgumentOutOfRangeException"/>异常
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">要判断的值</param>
    /// <param name="paramName">参数名</param>
    /// <param name="start">比较范围的起始值</param>
    /// <param name="end">比较范围的结束值</param>
    /// <param name="startEqual">是否可等于起始值</param>
    /// <param name="endEqual">是否可等于结束值</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void Between<T>(T value, string paramName, T start, T end, bool startEqual = false,
        bool endEqual = false) where T : IComparable<T>
    {
        bool flag = startEqual ? value.CompareTo(start) >= 0 : value.CompareTo(start) > 0;
        string message = startEqual
            ? string.Format(R.ParameterCheck_Between, paramName, start, end)
            : string.Format(R.ParameterCheck_BetweenNotEqual, paramName, start, end, start);
        Require<ArgumentOutOfRangeException>(flag, message);

        flag = endEqual ? value.CompareTo(end) <= 0 : value.CompareTo(end) < 0;
        message = endEqual
            ? string.Format(R.ParameterCheck_Between, paramName, start, end)
            : string.Format(R.ParameterCheck_BetweenNotEqual, paramName, start, end, end);
        Require<ArgumentOutOfRangeException>(flag, message);
    }

    /// <summary>
    /// 检查参数不能为负数或零，否则抛出<see cref="ArgumentOutOfRangeException"/>异常
    /// </summary>
    /// <param name="timeSpan">时间戳</param>
    /// <param name="paramName">参数名</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void NotNegativeOrZero(TimeSpan timeSpan, string paramName) => Require<ArgumentOutOfRangeException>(timeSpan > TimeSpan.Zero, paramName);

    #endregion

    #region IO(文件检查)

    /// <summary>
    /// 检查指定路径的文件夹必须存在，否则抛出<see cref="DirectoryNotFoundException"/>异常
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <param name="paramName">参数名</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void DirectoryExists(string directory, string paramName = null)
    {
        NotNull(directory, paramName);
        Require<DirectoryNotFoundException>(Directory.Exists(directory), string.Format(R.ParameterCheck_DirectoryNotExists, directory));
    }

    /// <summary>
    /// 检查指定路径的文件必须存在，否则抛出<see cref="FileNotFoundException"/>异常。
    /// </summary>
    /// <param name="fileName">文件路径，包含文件名</param>
    /// <param name="paramName">参数名</param>
    /// <exception cref="FileNotFoundException"></exception>
    public static void FileExists(string fileName, string paramName = null)
    {
        NotNull(fileName, paramName);
        Require<FileNotFoundException>(File.Exists(fileName), string.Format(R.ParameterCheck_FileNotExists, fileName));
    }

    #endregion
}