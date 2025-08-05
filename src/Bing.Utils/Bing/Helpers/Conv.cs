using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bing.Collections;
using Bing.Extensions;

namespace Bing.Helpers;

/// <summary>
/// 类型转换 操作
/// </summary>
public static partial class Conv
{
    #region ToSByte(转换为sbyte)

    /// <summary>
    /// 转换为有符号8位整型（<see cref="sbyte"/>，范围：-128 到 127）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>转换后的 sbyte 值，如果转换失败，则返回 sbyte 类型的默认值 0</returns>
    /// <example>
    /// <code>
    /// sbyte value1 = Conv.ToSByte("123");     // 返回 123
    /// sbyte value2 = Conv.ToSByte("-128");    // 返回 -128
    /// sbyte value3 = Conv.ToSByte("abc");     // 转换失败，返回 0
    /// sbyte value4 = Conv.ToSByte(null);      // 转换失败，返回 0
    /// </code>
    /// </example>
    public static sbyte ToSByte(object input) => ToSByte(input, default);

    /// <summary>
    /// 转换为有符号8位整型（<see cref="sbyte"/>，范围：-128 到 127）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的 sbyte 值，如果转换失败，则返回指定的默认值</returns>
    /// <example>
    /// <code>
    /// sbyte value1 = Conv.ToSByte("123", 10);     // 返回 123
    /// sbyte value2 = Conv.ToSByte("-128", 10);    // 返回 -128
    /// sbyte value3 = Conv.ToSByte("abc", 10);     // 转换失败，返回 10
    /// sbyte value4 = Conv.ToSByte(null, 10);      // 转换失败，返回 10
    /// </code>
    /// </example>
    public static sbyte ToSByte(object input, sbyte defaultValue) => ToSByteOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空的有符号8位整型（sbyte，范围：-128 到 127）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空 sbyte 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法首先尝试直接将输入解析为 sbyte，如果失败，则尝试先解析为 double，
    /// 再转换为 sbyte。这可以处理一些小数形式的输入（例如"12.0"）。
    /// 如果值超出 sbyte 范围或无法转换，返回 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// sbyte? value1 = Conv.ToSByteOrNull("123");     // 返回 123
    /// sbyte? value2 = Conv.ToSByteOrNull("-128");    // 返回 -128
    /// sbyte? value3 = Conv.ToSByteOrNull("12.0");    // 返回 12（先解析为double再转换）
    /// sbyte? value4 = Conv.ToSByteOrNull("abc");     // 转换失败，返回 null
    /// sbyte? value5 = Conv.ToSByteOrNull(null);      // 转换失败，返回 null
    /// sbyte? value6 = Conv.ToSByteOrNull("200");     // 超出范围，返回 null
    /// </code>
    /// </example>
    public static sbyte? ToSByteOrNull(object input)
    {
        var success = sbyte.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDoubleOrNull(input, 0);
            if (temp == null)
                return null;
            return Convert.ToSByte(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToByte(转换为byte)

    /// <summary>
    /// 转换为无符号8位整型（<see cref="byte"/>，范围：0 到 255）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>转换后的 byte 值，如果转换失败，则返回 byte 类型的默认值 0</returns>
    /// <example>
    /// <code>
    /// byte value1 = Conv.ToByte("123");     // 返回 123
    /// byte value2 = Conv.ToByte("255");     // 返回 255
    /// byte value3 = Conv.ToByte("abc");     // 转换失败，返回 0
    /// byte value4 = Conv.ToByte(null);      // 转换失败，返回 0
    /// </code>
    /// </example>
    public static byte ToByte(object input) => ToByte(input, default);

    /// <summary>
    /// 转换为无符号8位整型（<see cref="byte"/>，范围：0 到 255）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的 byte 值，如果转换失败，则返回指定的默认值</returns>
    /// <example>
    /// <code>
    /// byte value1 = Conv.ToByte("123", 10);     // 返回 123
    /// byte value2 = Conv.ToByte("255", 10);     // 返回 255
    /// byte value3 = Conv.ToByte("abc", 10);     // 转换失败，返回 10
    /// byte value4 = Conv.ToByte(null, 10);      // 转换失败，返回 10
    /// </code>
    /// </example>
    public static byte ToByte(object input, byte defaultValue) => ToByteOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空的无符号8位整型（<see cref="byte"/>，范围：0 到 255）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空 byte 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确或负数等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法首先尝试直接将输入解析为 byte，如果失败，则尝试先解析为 double，
    /// 再转换为 byte。这可以处理一些小数形式的输入（例如"12.0"）。
    /// 如果值超出 byte 范围（小于0或大于255）或无法转换，返回 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// byte? value1 = Conv.ToByteOrNull("123");     // 返回 123
    /// byte? value2 = Conv.ToByteOrNull("255");     // 返回 255
    /// byte? value3 = Conv.ToByteOrNull("12.0");    // 返回 12（先解析为double再转换）
    /// byte? value4 = Conv.ToByteOrNull("abc");     // 转换失败，返回 null
    /// byte? value5 = Conv.ToByteOrNull(null);      // 转换失败，返回 null
    /// byte? value6 = Conv.ToByteOrNull("-1");      // 负数超出范围，返回 null
    /// byte? value7 = Conv.ToByteOrNull("300");     // 超出最大值，返回 null
    /// </code>
    /// </example>
    public static byte? ToByteOrNull(object input)
    {
        var success = byte.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDoubleOrNull(input, 0);
            if (temp == null)
                return null;
            return Convert.ToByte(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToChar(转换为char)

    /// <summary>
    /// 转换为字符（<see cref="char"/>）
    /// </summary>
    /// <param name="input">输入值，支持字符串、数值类型对象等</param>
    /// <returns>转换后的字符值，如果转换失败，则返回 char 类型的默认值 '\0'</returns>
    /// <remarks>
    /// 该方法主要用于将字符串转换为字符。对于字符串输入，只有当字符串长度为1时才能成功转换。
    /// </remarks>
    /// <example>
    /// <code>
    /// char value1 = Conv.ToChar("A");      // 返回 'A'
    /// char value2 = Conv.ToChar("123");    // 转换失败，返回 '\0'（因为字符串长度大于1）
    /// char value3 = Conv.ToChar("");       // 转换失败，返回 '\0'
    /// char value4 = Conv.ToChar(null);     // 转换失败，返回 '\0'
    /// </code>
    /// </example>
    public static char ToChar(object input) => ToChar(input, default);

    /// <summary>
    /// 转换为字符（<see cref="char"/>）
    /// </summary>
    /// <param name="input">输入值，支持字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的字符值，如果转换失败，则返回指定的默认值</returns>
    /// <remarks>
    /// 该方法主要用于将字符串转换为字符。对于字符串输入，只有当字符串长度为1时才能成功转换。
    /// </remarks>
    /// <example>
    /// <code>
    /// char value1 = Conv.ToChar("A", 'X');      // 返回 'A'
    /// char value2 = Conv.ToChar("123", 'X');    // 转换失败，返回 'X'
    /// char value3 = Conv.ToChar("", 'X');       // 转换失败，返回 'X'
    /// char value4 = Conv.ToChar(null, 'X');     // 转换失败，返回 'X'
    /// </code>
    /// </example>
    public static char ToChar(object input, char defaultValue) => ToCharOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空字符（<see cref="char"/>?）
    /// </summary>
    /// <param name="input">输入值，支持字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空字符值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、长度大于1的字符串等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 该方法使用 <see cref="char.TryParse"/> 方法尝试将输入值转换为字符。
    /// 对于字符串输入，只有当字符串长度为1时才能成功转换。
    /// </remarks>
    /// <example>
    /// <code>
    /// char? value1 = Conv.ToCharOrNull("A");      // 返回 'A'
    /// char? value2 = Conv.ToCharOrNull("123");    // 转换失败，返回 null（因为字符串长度大于1）
    /// char? value3 = Conv.ToCharOrNull("");       // 转换失败，返回 null
    /// char? value4 = Conv.ToCharOrNull(null);     // 转换失败，返回 null
    /// char? value5 = Conv.ToCharOrNull(65);       // 转换失败，返回 null（数字不会自动转为对应ASCII码的字符）
    /// </code>
    /// </example>
    public static char? ToCharOrNull(object input)
    {
        var success = char.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        return null;
    }

    #endregion

    #region ToShort(转换为short)

    /// <summary>
    /// 转换为有符号16位整型（<see cref="short"/>，范围：-32768 到 32767）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>转换后的 short 值，如果转换失败，则返回 short 类型的默认值 0</returns>
    /// <example>
    /// <code>
    /// short value1 = Conv.ToShort("12345");     // 返回 12345
    /// short value2 = Conv.ToShort("-32768");    // 返回 -32768
    /// short value3 = Conv.ToShort("abc");       // 转换失败，返回 0
    /// short value4 = Conv.ToShort(null);        // 转换失败，返回 0
    /// </code>
    /// </example>
    public static short ToShort(object input) => ToShort(input, default);

    /// <summary>
    /// 转换为有符号16位整型（<see cref="short"/>，范围：-32768 到 32767）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的 short 值，如果转换失败，则返回指定的默认值</returns>
    /// <example>
    /// <code>
    /// short value1 = Conv.ToShort("12345", 100);     // 返回 12345
    /// short value2 = Conv.ToShort("-32768", 100);    // 返回 -32768
    /// short value3 = Conv.ToShort("abc", 100);       // 转换失败，返回 100
    /// short value4 = Conv.ToShort(null, 100);        // 转换失败，返回 100
    /// </code>
    /// </example>
    public static short ToShort(object input, short defaultValue) => ToShortOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空的有符号16位整型（<see cref="short"/>?，范围：-32768 到 32767）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空 short 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法首先尝试直接将输入解析为 short，如果失败，则尝试先解析为 double，
    /// 再转换为 short。这可以处理一些小数形式的输入（例如"123.0"）。
    /// 如果值超出 short 范围或无法转换，返回 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// short? value1 = Conv.ToShortOrNull("12345");     // 返回 12345
    /// short? value2 = Conv.ToShortOrNull("-32768");    // 返回 -32768
    /// short? value3 = Conv.ToShortOrNull("123.0");     // 返回 123（先解析为double再转换）
    /// short? value4 = Conv.ToShortOrNull("abc");       // 转换失败，返回 null
    /// short? value5 = Conv.ToShortOrNull(null);        // 转换失败，返回 null
    /// short? value6 = Conv.ToShortOrNull("40000");     // 超出范围，返回 null
    /// </code>
    /// </example>
    public static short? ToShortOrNull(object input)
    {
        var success = short.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDoubleOrNull(input, 0);
            if (temp == null)
                return null;
            return Convert.ToInt16(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToInt(转换为int)

    /// <summary>
    /// 转换为有符号32位整型（<see cref="int"/>，范围：-2,147,483,648 到 2,147,483,647）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>转换后的 int 值，如果转换失败，则返回 int 类型的默认值 0</returns>
    /// <example>
    /// <code>
    /// int value1 = Conv.ToInt("12345");      // 返回 12345
    /// int value2 = Conv.ToInt("-100");       // 返回 -100
    /// int value3 = Conv.ToInt("123.45");     // 返回 123
    /// int value4 = Conv.ToInt("abc");        // 转换失败，返回 0
    /// int value5 = Conv.ToInt(null);         // 转换失败，返回 0
    /// </code>
    /// </example>
    public static int ToInt(object input) => ToInt(input, default);

    /// <summary>
    /// 转换为有符号32位整型（<see cref="int"/>，范围：-2,147,483,648 到 2,147,483,647）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的 int 值，如果转换失败，则返回指定的默认值</returns>
    /// <example>
    /// <code>
    /// int value1 = Conv.ToInt("12345", 100);      // 返回 12345
    /// int value2 = Conv.ToInt("-100", 100);       // 返回 -100
    /// int value3 = Conv.ToInt("123.45", 100);     // 返回 123
    /// int value4 = Conv.ToInt("abc", 100);        // 转换失败，返回 100
    /// int value5 = Conv.ToInt(null, 100);         // 转换失败，返回 100
    /// </code>
    /// </example>
    public static int ToInt(object input, int defaultValue) => ToIntOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空的有符号32位整型（<see cref="int"/>?，范围：-2,147,483,648 到 2,147,483,647）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空 int 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法首先尝试直接将输入解析为 int，如果失败，则尝试先解析为 double，
    /// 再转换为 int。这可以处理一些小数形式的输入（例如"123.45"将返回123）。
    /// 如果值超出 int 范围或无法转换，返回 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// int? value1 = Conv.ToIntOrNull("12345");      // 返回 12345
    /// int? value2 = Conv.ToIntOrNull("-100");       // 返回 -100
    /// int? value3 = Conv.ToIntOrNull("123.45");     // 返回 123（先解析为double再转换）
    /// int? value4 = Conv.ToIntOrNull("abc");        // 转换失败，返回 null
    /// int? value5 = Conv.ToIntOrNull(null);         // 转换失败，返回 null
    /// int? value6 = Conv.ToIntOrNull("3000000000"); // 超出范围，返回 null
    /// </code>
    /// </example>
    public static int? ToIntOrNull(object input)
    {
        var success = int.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDoubleOrNull(input, 0);
            if (temp == null)
                return null;
            return System.Convert.ToInt32(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToUInt(转换为uint)

    /// <summary>
    /// 转换为无符号32位整型（<see cref="uint"/>，范围：0 到 4,294,967,295）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>转换后的 uint 值，如果转换失败，则返回 uint 类型的默认值 0</returns>
    /// <example>
    /// <code>
    /// uint value1 = Conv.ToUInt("12345");      // 返回 12345
    /// uint value2 = Conv.ToUInt("-100");       // 转换失败（负数超出范围），返回 0
    /// uint value3 = Conv.ToUInt("123.45");     // 返回 123
    /// uint value4 = Conv.ToUInt("abc");        // 转换失败，返回 0
    /// uint value5 = Conv.ToUInt(null);         // 转换失败，返回 0
    /// </code>
    /// </example>
    public static uint ToUInt(object input) => ToUInt(input, default);

    /// <summary>
    /// 转换为无符号32位整型（<see cref="uint"/>，范围：0 到 4,294,967,295）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的 uint 值，如果转换失败，则返回指定的默认值</returns>
    /// <example>
    /// <code>
    /// uint value1 = Conv.ToUInt("12345", 100);      // 返回 12345
    /// uint value2 = Conv.ToUInt("-100", 100);       // 转换失败（负数超出范围），返回 100
    /// uint value3 = Conv.ToUInt("123.45", 100);     // 返回 123
    /// uint value4 = Conv.ToUInt("abc", 100);        // 转换失败，返回 100
    /// uint value5 = Conv.ToUInt(null, 100);         // 转换失败，返回 100
    /// </code>
    /// </example>
    public static uint ToUInt(object input, uint defaultValue) => ToUIntOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空的无符号32位整型（<see cref="uint"/>?，范围：0 到 4,294,967,295）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空 uint 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确或负数等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法首先尝试直接将输入解析为 uint，如果失败，则尝试先解析为 double，
    /// 再转换为 uint。这可以处理一些小数形式的输入（例如"123.45"将返回123）。
    /// 如果值为负数或超出 uint 范围或无法转换，返回 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// uint? value1 = Conv.ToUIntOrNull("12345");      // 返回 12345
    /// uint? value2 = Conv.ToUIntOrNull("-100");       // 负数超出范围，返回 null
    /// uint? value3 = Conv.ToUIntOrNull("123.45");     // 返回 123（先解析为double再转换）
    /// uint? value4 = Conv.ToUIntOrNull("abc");        // 转换失败，返回 null
    /// uint? value5 = Conv.ToUIntOrNull(null);         // 转换失败，返回 null
    /// uint? value6 = Conv.ToUIntOrNull("5000000000"); // 超出范围，返回 null
    /// </code>
    /// </example>
    public static uint? ToUIntOrNull(object input)
    {
        var success = uint.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDoubleOrNull(input, 0);
            if (temp == null || temp < 0)
                return null;
            return Convert.ToUInt32(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToLong(转换为long)

    /// <summary>
    /// 转换为有符号64位整型（<see cref="long"/>，范围：-9,223,372,036,854,775,808 到 9,223,372,036,854,775,807）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>转换后的 long 值，如果转换失败，则返回 long 类型的默认值 0</returns>
    /// <example>
    /// <code>
    /// long value1 = Conv.ToLong("123456789");       // 返回 123456789
    /// long value2 = Conv.ToLong("-100");            // 返回 -100
    /// long value3 = Conv.ToLong("123.45");          // 返回 123
    /// long value4 = Conv.ToLong("abc");             // 转换失败，返回 0
    /// long value5 = Conv.ToLong(null);              // 转换失败，返回 0
    /// </code>
    /// </example>
    public static long ToLong(object input) => ToLong(input, default);

    /// <summary>
    /// 转换为有符号64位整型（<see cref="long"/>，范围：-9,223,372,036,854,775,808 到 9,223,372,036,854,775,807）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的 long 值，如果转换失败，则返回指定的默认值</returns>
    /// <example>
    /// <code>
    /// long value1 = Conv.ToLong("123456789", 100);       // 返回 123456789
    /// long value2 = Conv.ToLong("-100", 100);            // 返回 -100
    /// long value3 = Conv.ToLong("123.45", 100);          // 返回 123
    /// long value4 = Conv.ToLong("abc", 100);             // 转换失败，返回 100
    /// long value5 = Conv.ToLong(null, 100);              // 转换失败，返回 100
    /// </code>
    /// </example>
    public static long ToLong(object input, long defaultValue) => ToLongOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空的有符号64位整型（<see cref="long"/>?，范围：-9,223,372,036,854,775,808 到 9,223,372,036,854,775,807）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空 long 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法首先尝试直接将输入解析为 long，如果失败，则尝试先解析为 decimal，
    /// 再转换为 long。注意这里使用的是decimal而非double，以便支持更大范围的数值。
    /// 这可以处理一些小数形式的输入（例如"123.45"将返回123）。
    /// 如果值超出 long 范围或无法转换，返回 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// long? value1 = Conv.ToLongOrNull("123456789012345");      // 返回 123456789012345
    /// long? value2 = Conv.ToLongOrNull("-100");                 // 返回 -100
    /// long? value3 = Conv.ToLongOrNull("123.45");               // 返回 123（先解析为decimal再转换）
    /// long? value4 = Conv.ToLongOrNull("abc");                  // 转换失败，返回 null
    /// long? value5 = Conv.ToLongOrNull(null);                   // 转换失败，返回 null
    /// long? value6 = Conv.ToLongOrNull("9999999999999999999");  // 超出范围，返回 null
    /// </code>
    /// </example>
    public static long? ToLongOrNull(object input)
    {
        var success = long.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDecimalOrNull(input, 0);
            if (temp == null)
                return null;
            return System.Convert.ToInt64(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToULong(转换为ulong)

    /// <summary>
    /// 转换为无符号64位整型（<see cref="ulong"/>，范围：0 到 18,446,744,073,709,551,615）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>转换后的 ulong 值，如果转换失败，则返回 ulong 类型的默认值 0</returns>
    /// <example>
    /// <code>
    /// ulong value1 = Conv.ToULong("123456789012345");     // 返回 123456789012345
    /// ulong value2 = Conv.ToULong("-100");                // 转换失败（负数超出范围），返回 0
    /// ulong value3 = Conv.ToULong("123.45");              // 返回 123
    /// ulong value4 = Conv.ToULong("abc");                 // 转换失败，返回 0
    /// ulong value5 = Conv.ToULong(null);                  // 转换失败，返回 0
    /// </code>
    /// </example>
    public static ulong ToULong(object input) => ToULong(input, default);

    /// <summary>
    /// 转换为无符号64位整型（<see cref="ulong"/>，范围：0 到 18,446,744,073,709,551,615）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的 ulong 值，如果转换失败，则返回指定的默认值</returns>
    /// <example>
    /// <code>
    /// ulong value1 = Conv.ToULong("123456789012345", 100);     // 返回 123456789012345
    /// ulong value2 = Conv.ToULong("-100", 100);                // 转换失败（负数超出范围），返回 100
    /// ulong value3 = Conv.ToULong("123.45", 100);              // 返回 123
    /// ulong value4 = Conv.ToULong("abc", 100);                 // 转换失败，返回 100
    /// ulong value5 = Conv.ToULong(null, 100);                  // 转换失败，返回 100
    /// </code>
    /// </example>
    public static ulong ToULong(object input, ulong defaultValue) => ToULongOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空的无符号64位整型（<see cref="ulong"/>?，范围：0 到 18,446,744,073,709,551,615）
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空 ulong 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确或负数等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法首先尝试直接将输入解析为 ulong，如果失败，则尝试先解析为 decimal，
    /// 再转换为 ulong。注意这里使用的是decimal而非double，以便支持更大范围的数值。
    /// 这可以处理一些小数形式的输入（例如"123.45"将返回123）。
    /// 如果值为负数或超出 ulong 范围或无法转换，返回 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// ulong? value1 = Conv.ToULongOrNull("123456789012345");      // 返回 123456789012345
    /// ulong? value2 = Conv.ToULongOrNull("-100");                 // 负数超出范围，返回 null
    /// ulong? value3 = Conv.ToULongOrNull("123.45");               // 返回 123（先解析为decimal再转换）
    /// ulong? value4 = Conv.ToULongOrNull("abc");                  // 转换失败，返回 null
    /// ulong? value5 = Conv.ToULongOrNull(null);                   // 转换失败，返回 null
    /// ulong? value6 = Conv.ToULongOrNull("99999999999999999999"); // 超出范围，返回 null
    /// </code>
    /// </example>
    public static ulong? ToULongOrNull(object input)
    {
        var success = ulong.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDecimalOrNull(input, 0);
            if (temp == null || temp < 0)
                return null;
            return Convert.ToUInt64(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToFloat(转换为float)

    /// <summary>
    /// 转换为32位浮点型（<see cref="float"/>，范围：±1.5 × 10^-45 到 ±3.4 × 10^38），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <returns>转换后的 float 值，如果转换失败，则返回 float 类型的默认值 0</returns>
    /// <remarks>
    /// 采用Banker's rounding（银行家算法），即：四舍六入五取偶。这是IEEE的规范。
    /// 若需要传统的四舍五入，可使用 <see cref="MidpointRounding.AwayFromZero"/> 参数。
    /// </remarks>
    /// <example>
    /// <code>
    /// float value1 = Conv.ToFloat("123.45");       // 返回 123.45
    /// float value2 = Conv.ToFloat("123.45", 1);    // 返回 123.5（按指定小数位舍入）
    /// float value3 = Conv.ToFloat("abc");          // 转换失败，返回 0
    /// float value4 = Conv.ToFloat(null);           // 转换失败，返回 0
    /// </code>
    /// </example>
    public static float ToFloat(object input, int? digits = null) => ToFloat(input, default, digits, MidpointRounding.AwayFromZero);

    /// <summary>
    /// 转换为32位浮点型（<see cref="float"/>，范围：±1.5 × 10^-45 到 ±3.4 × 10^38），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <param name="mode">舍入模式，默认采用 <see cref="MidpointRounding.AwayFromZero"/> (四舍五入)</param>
    /// <returns>转换后的 float 值，如果转换失败，则返回指定的默认值</returns>
    /// <remarks>
    /// 采用Banker's rounding（银行家算法），即：四舍六入五取偶。这是IEEE的规范。
    /// 若需要传统的四舍五入，可使用 <see cref="MidpointRounding.AwayFromZero"/> 参数。
    /// </remarks>
    /// <example>
    /// <code>
    /// float value1 = Conv.ToFloat("123.45", 0.0f);       // 返回 123.45
    /// float value2 = Conv.ToFloat("123.45", 0.0f, 1);    // 返回 123.5（按指定小数位舍入）
    /// float value3 = Conv.ToFloat("abc", 99.9f);         // 转换失败，返回 99.9
    /// float value4 = Conv.ToFloat(null, 99.9f);          // 转换失败，返回 99.9
    /// float value5 = Conv.ToFloat("123.5", 0.0f, 0, MidpointRounding.ToEven); // 返回 124（银行家舍入）
    /// </code>
    /// </example>
    public static float ToFloat(object input, float defaultValue, int? digits = null, MidpointRounding mode = MidpointRounding.AwayFromZero) => ToFloatOrNull(input, digits, mode) ?? defaultValue;

    /// <summary>
    /// 转换为可空的32位浮点型（<see cref="float"/>?，范围：±1.5 × 10^-45 到 ±3.4 × 10^38），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <param name="mode">舍入模式，默认采用 <see cref="MidpointRounding.AwayFromZero"/> (四舍五入)</param>
    /// <returns>
    /// 转换后的可空 float 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法先尝试将输入解析为 float，若指定了小数位数，则会按照指定的舍入模式进行舍入。
    /// 默认采用 <see cref="MidpointRounding.AwayFromZero"/>（四舍五入），
    /// 也可以指定为 <see cref="MidpointRounding.ToEven"/>（银行家舍入，IEEE标准）。
    /// </remarks>
    /// <example>
    /// <code>
    /// float? value1 = Conv.ToFloatOrNull("123.45");       // 返回 123.45
    /// float? value2 = Conv.ToFloatOrNull("123.45", 1);    // 返回 123.5（按指定小数位舍入）
    /// float? value3 = Conv.ToFloatOrNull("abc");          // 转换失败，返回 null
    /// float? value4 = Conv.ToFloatOrNull(null);           // 转换失败，返回 null
    /// float? value5 = Conv.ToFloatOrNull("1.5E40");       // 超出范围，返回 null
    /// float? value6 = Conv.ToFloatOrNull("123.5", 0, MidpointRounding.ToEven); // 返回 124（银行家舍入）
    /// </code>
    /// </example>
    public static float? ToFloatOrNull(object input, int? digits = null, MidpointRounding mode = MidpointRounding.AwayFromZero)
    {
        var success = float.TryParse(input.SafeString(), out var result);
        if (!success)
            return null;
        if (digits == null)
            return result;
        return (float)Math.Round(result, digits.Value, mode);
    }

    #endregion

    #region ToDouble(转换为double)

    /// <summary>
    /// 转换为64位浮点型（<see cref="double"/>，范围：±5.0 × 10^-324 到 ±1.7 × 10^308），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <returns>转换后的 double 值，如果转换失败，则返回 double 类型的默认值 0</returns>
    /// <remarks>
    /// 采用Banker's rounding（银行家算法），即：四舍六入五取偶。这是IEEE的规范。
    /// 若需要传统的四舍五入，可使用 <see cref="MidpointRounding.AwayFromZero"/> 参数。
    /// </remarks>
    /// <example>
    /// <code>
    /// double value1 = Conv.ToDouble("123.45678");       // 返回 123.45678
    /// double value2 = Conv.ToDouble("123.45678", 2);    // 返回 123.46（按指定小数位舍入）
    /// double value3 = Conv.ToDouble("abc");             // 转换失败，返回 0
    /// double value4 = Conv.ToDouble(null);              // 转换失败，返回 0
    /// </code>
    /// </example>
    public static double ToDouble(object input, int? digits = null) => ToDouble(input, default, digits, MidpointRounding.AwayFromZero);

    /// <summary>
    /// 转换为64位浮点型（<see cref="double"/>，范围：±5.0 × 10^-324 到 ±1.7 × 10^308），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <param name="mode">舍入模式，默认采用 <see cref="MidpointRounding.AwayFromZero"/> (四舍五入)</param>
    /// <returns>转换后的 double 值，如果转换失败，则返回指定的默认值</returns>
    /// <remarks>
    /// 采用Banker's rounding（银行家算法），即：四舍六入五取偶。这是IEEE的规范。
    /// 若需要传统的四舍五入，可使用 <see cref="MidpointRounding.AwayFromZero"/> 参数。
    /// </remarks>
    /// <example>
    /// <code>
    /// double value1 = Conv.ToDouble("123.45678", 0.0);        // 返回 123.45678
    /// double value2 = Conv.ToDouble("123.45678", 0.0, 2);     // 返回 123.46（按指定小数位舍入）
    /// double value3 = Conv.ToDouble("abc", 99.9);             // 转换失败，返回 99.9
    /// double value4 = Conv.ToDouble(null, 99.9);              // 转换失败，返回 99.9
    /// double value5 = Conv.ToDouble("123.5", 0.0, 0, MidpointRounding.ToEven); // 返回 124（银行家舍入）
    /// </code>
    /// </example>
    public static double ToDouble(object input, double defaultValue, int? digits = null, MidpointRounding mode = MidpointRounding.AwayFromZero) => ToDoubleOrNull(input, digits, mode) ?? defaultValue;

    /// <summary>
    /// 转换为可空的64位浮点型（<see cref="double"/>?，范围：±5.0 × 10^-324 到 ±1.7 × 10^308），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <param name="mode">舍入模式，默认采用 <see cref="MidpointRounding.AwayFromZero"/> (四舍五入)</param>
    /// <returns>
    /// 转换后的可空 double 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法先尝试将输入解析为 double，若指定了小数位数，则会按照指定的舍入模式进行舍入。
    /// 默认采用 <see cref="MidpointRounding.AwayFromZero"/>（四舍五入），
    /// 也可以指定为 <see cref="MidpointRounding.ToEven"/>（银行家舍入，IEEE标准）。
    /// </remarks>
    /// <example>
    /// <code>
    /// double? value1 = Conv.ToDoubleOrNull("123.45678");       // 返回 123.45678
    /// double? value2 = Conv.ToDoubleOrNull("123.45678", 2);    // 返回 123.46（按指定小数位舍入）
    /// double? value3 = Conv.ToDoubleOrNull("abc");             // 转换失败，返回 null
    /// double? value4 = Conv.ToDoubleOrNull(null);              // 转换失败，返回 null
    /// double? value5 = Conv.ToDoubleOrNull("123.5", 0, MidpointRounding.ToEven); // 返回 124（银行家舍入）
    /// </code>
    /// </example>
    public static double? ToDoubleOrNull(object input, int? digits = null, MidpointRounding mode = MidpointRounding.AwayFromZero)
    {
        var success = double.TryParse(input.SafeString(), out var result);
        if (!success)
            return null;
        return digits == null ? result : Math.Round(result, digits.Value, mode);
    }

    #endregion

    #region ToDecimal(转换为decimal)

    /// <summary>
    /// 转换为128位十进制浮点型（<see cref="decimal"/>，范围：±1.0 × 10^-28 到 ±7.9 × 10^28，28-29位精度），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <returns>转换后的 decimal 值，如果转换失败，则返回 decimal 类型的默认值 0</returns>
    /// <remarks>
    /// 采用Banker's rounding（银行家算法），即：四舍六入五取偶。这是IEEE的规范。
    /// 若需要传统的四舍五入，可使用 <see cref="MidpointRounding.AwayFromZero"/> 参数。
    /// decimal类型特别适合于财务和货币计算，提供高精度且避免浮点数舍入误差。
    /// </remarks>
    /// <example>
    /// <code>
    /// decimal value1 = Conv.ToDecimal("123456.78901");      // 返回 123456.78901
    /// decimal value2 = Conv.ToDecimal("123456.78901", 3);   // 返回 123456.789（按指定小数位舍入）
    /// decimal value3 = Conv.ToDecimal("abc");               // 转换失败，返回 0
    /// decimal value4 = Conv.ToDecimal(null);                // 转换失败，返回 0
    /// </code>
    /// </example>
    public static decimal ToDecimal(object input, int? digits = null) => ToDecimal(input, default, digits, MidpointRounding.AwayFromZero);

    /// <summary>
    /// 转换为128位十进制浮点型（<see cref="decimal"/>，范围：±1.0 × 10^-28 到 ±7.9 × 10^28，28-29位精度），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <param name="mode">舍入模式，默认采用 <see cref="MidpointRounding.AwayFromZero"/> (四舍五入)</param>
    /// <returns>转换后的 decimal 值，如果转换失败，则返回指定的默认值</returns>
    /// <remarks>
    /// 采用Banker's rounding（银行家算法），即：四舍六入五取偶。这是IEEE的规范。
    /// 若需要传统的四舍五入，可使用 <see cref="MidpointRounding.AwayFromZero"/> 参数。
    /// decimal类型特别适合于财务和货币计算，提供高精度且避免浮点数舍入误差。
    /// </remarks>
    /// <example>
    /// <code>
    /// decimal value1 = Conv.ToDecimal("123456.78901", 0.0m);        // 返回 123456.78901
    /// decimal value2 = Conv.ToDecimal("123456.78901", 0.0m, 3);     // 返回 123456.789（按指定小数位舍入）
    /// decimal value3 = Conv.ToDecimal("abc", 99.9m);                // 转换失败，返回 99.9
    /// decimal value4 = Conv.ToDecimal(null, 99.9m);                 // 转换失败，返回 99.9
    /// decimal value5 = Conv.ToDecimal("123.5", 0.0m, 0, MidpointRounding.ToEven); // 返回 124（银行家舍入）
    /// </code>
    /// </example>
    public static decimal ToDecimal(object input, decimal defaultValue, int? digits = null, MidpointRounding mode = MidpointRounding.AwayFromZero) => ToDecimalOrNull(input, digits, mode) ?? defaultValue;

    /// <summary>
    /// 转换为可空的128位十进制浮点型（<see cref="decimal"/>?，范围：±1.0 × 10^-28 到 ±7.9 × 10^28，28-29位精度），并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值，支持数字字符串、数值类型对象等</param>
    /// <param name="digits">小数位数，若指定则按该精度进行舍入</param>
    /// <param name="mode">舍入模式，默认采用 <see cref="MidpointRounding.AwayFromZero"/> (四舍五入)</param>
    /// <returns>
    /// 转换后的可空 decimal 值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、格式不正确等情况），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法先尝试将输入解析为 decimal，若指定了小数位数，则会按照指定的舍入模式进行舍入。
    /// 默认采用 <see cref="MidpointRounding.AwayFromZero"/>（四舍五入），
    /// 也可以指定为 <see cref="MidpointRounding.ToEven"/>（银行家舍入，IEEE标准）。
    /// decimal类型特别适合于财务和货币计算，提供高精度且避免浮点数舍入误差。
    /// </remarks>
    /// <example>
    /// <code>
    /// decimal? value1 = Conv.ToDecimalOrNull("123456.78901");      // 返回 123456.78901
    /// decimal? value2 = Conv.ToDecimalOrNull("123456.78901", 3);   // 返回 123456.789（按指定小数位舍入）
    /// decimal? value3 = Conv.ToDecimalOrNull("abc");               // 转换失败，返回 null
    /// decimal? value4 = Conv.ToDecimalOrNull(null);                // 转换失败，返回 null
    /// decimal? value5 = Conv.ToDecimalOrNull("123.5", 0, MidpointRounding.ToEven); // 返回 124（银行家舍入）
    /// </code>
    /// </example>
    public static decimal? ToDecimalOrNull(object input, int? digits = null, MidpointRounding mode = MidpointRounding.AwayFromZero)
    {
        var success = decimal.TryParse(input.SafeString(), out var result);
        if (!success)
            return null;
        return digits == null ? result : Math.Round(result, digits.Value, mode);
    }

    #endregion

    #region ToBool(转换为bool)

    /// <summary>
    /// 转换为布尔值（<see cref="bool"/>）
    /// </summary>
    /// <param name="input">输入值，支持字符串、数值类型对象等</param>
    /// <returns>转换后的布尔值，如果转换失败，则返回 false</returns>
    /// <remarks>
    /// 除了标准的布尔值转换（如"true"/"false"），此方法还支持以下特殊字符串的转换：
    /// - 返回 true："1"、"是"、"ok"、"yes"
    /// - 返回 false："0"、"否"、"不"、"no"、"fail"
    /// </remarks>
    /// <example>
    /// <code>
    /// bool value1 = Conv.ToBool("true");    // 返回 true
    /// bool value2 = Conv.ToBool("1");       // 返回 true
    /// bool value3 = Conv.ToBool("是");      // 返回 true
    /// bool value4 = Conv.ToBool("false");   // 返回 false
    /// bool value5 = Conv.ToBool("0");       // 返回 false 
    /// bool value6 = Conv.ToBool("abc");     // 转换失败，返回 false
    /// bool value7 = Conv.ToBool(null);      // 转换失败，返回 false
    /// </code>
    /// </example>
    public static bool ToBool(object input) => ToBool(input, false);

    /// <summary>
    /// 转换为布尔值（<see cref="bool"/>）
    /// </summary>
    /// <param name="input">输入值，支持字符串、数值类型对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值</param>
    /// <returns>转换后的布尔值，如果转换失败，则返回指定的默认值</returns>
    /// <remarks>
    /// 除了标准的布尔值转换（如"true"/"false"），此方法还支持以下特殊字符串的转换：
    /// - 返回 true："1"、"是"、"ok"、"yes"
    /// - 返回 false："0"、"否"、"不"、"no"、"fail"
    /// </remarks>
    /// <example>
    /// <code>
    /// bool value1 = Conv.ToBool("true", true);     // 返回 true
    /// bool value2 = Conv.ToBool("1", false);       // 返回 true
    /// bool value3 = Conv.ToBool("yes", false);     // 返回 true
    /// bool value4 = Conv.ToBool("false", true);    // 返回 false
    /// bool value5 = Conv.ToBool("0", true);        // 返回 false
    /// bool value6 = Conv.ToBool("abc", true);      // 转换失败，返回 true
    /// bool value7 = Conv.ToBool(null, true);       // 转换失败，返回 true
    /// </code>
    /// </example>
    public static bool ToBool(object input, bool defaultValue) => ToBoolOrNull(input) ?? defaultValue;

    /// <summary>
    /// 转换为可空布尔值（<see cref="bool"/>?）
    /// </summary>
    /// <param name="input">输入值，支持字符串、数值类型对象等</param>
    /// <returns>
    /// 转换后的可空布尔值。如果转换成功，则返回转换后的值；
    /// 如果转换失败（输入为 null、空字符串、非布尔格式的字符串等），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法支持多种类型的输入值转换：
    /// <list type="bullet">
    /// <item><description><strong>null 或 DBNull</strong>：返回 null</description></item>
    /// <item><description><strong>布尔类型</strong>：直接返回原值</description></item>
    /// <item><description><strong>数值类型</strong>：非零值转换为 true，零值转换为 false</description></item>
    /// <item><description><strong>枚举类型</strong>：非零枚举值转换为 true，零值转换为 false</description></item>
    /// <item><description><strong>字符串类型</strong>：除标准的 "true"/"false" 外，还支持以下扩展转换：</description></item>
    /// </list>
    /// 
    /// 表示 true 的特殊字符串（不区分大小写）：
    /// <list type="bullet">
    /// <item><description>"1"、"是"、"ok"、"yes"、"y"</description></item>
    /// <item><description>"on"、"enable"、"enabled"、"t"、"true"</description></item>
    /// </list>
    /// 
    /// 表示 false 的特殊字符串（不区分大小写）：
    /// <list type="bullet">
    /// <item><description>"0"、"否"、"不"、"no"、"fail"、"n"</description></item>
    /// <item><description>"off"、"disable"、"disabled"、"f"、"false"</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 基本布尔值示例
    /// bool? value1 = Conv.ToBoolOrNull(true);         // 返回 true
    /// bool? value2 = Conv.ToBoolOrNull(false);        // 返回 false
    /// 
    /// // 数值类型示例
    /// bool? value3 = Conv.ToBoolOrNull(1);            // 返回 true
    /// bool? value4 = Conv.ToBoolOrNull(0);            // 返回 false
    /// bool? value5 = Conv.ToBoolOrNull(1.5);          // 返回 true（非零值）
    /// 
    /// // 枚举示例（假设有枚举 TestEnum { Zero = 0, One = 1 }）
    /// bool? value6 = Conv.ToBoolOrNull(TestEnum.Zero); // 返回 false
    /// bool? value7 = Conv.ToBoolOrNull(TestEnum.One);  // 返回 true
    /// 
    /// // 字符串示例 - 标准布尔值表示
    /// bool? value8 = Conv.ToBoolOrNull("true");       // 返回 true
    /// bool? value9 = Conv.ToBoolOrNull("false");      // 返回 false
    /// bool? value10 = Conv.ToBoolOrNull("True");      // 返回 true（不区分大小写）
    /// 
    /// // 字符串示例 - 扩展表示方式
    /// bool? value11 = Conv.ToBoolOrNull("1");         // 返回 true
    /// bool? value12 = Conv.ToBoolOrNull("0");         // 返回 false
    /// bool? value13 = Conv.ToBoolOrNull("是");        // 返回 true
    /// bool? value14 = Conv.ToBoolOrNull("否");        // 返回 false
    /// bool? value15 = Conv.ToBoolOrNull("yes");       // 返回 true
    /// bool? value16 = Conv.ToBoolOrNull("no");        // 返回 false
    /// bool? value17 = Conv.ToBoolOrNull("on");        // 返回 true
    /// bool? value18 = Conv.ToBoolOrNull("off");       // 返回 false
    /// bool? value19 = Conv.ToBoolOrNull("enable");    // 返回 true
    /// bool? value20 = Conv.ToBoolOrNull("disable");   // 返回 false
    /// 
    /// // 无效输入示例
    /// bool? value21 = Conv.ToBoolOrNull(null);        // 返回 null
    /// bool? value22 = Conv.ToBoolOrNull("");          // 返回 null（空字符串）
    /// bool? value23 = Conv.ToBoolOrNull(" ");         // 返回 null（空白字符串）
    /// bool? value24 = Conv.ToBoolOrNull("invalid");   // 返回 null（无法识别的字符串）
    /// bool? value25 = Conv.ToBoolOrNull(DBNull.Value);// 返回 null
    /// </code>
    /// </example>
    /// <seealso cref="ToBool(object)"/>
    /// <seealso cref="ToBool(object, bool)"/>
    public static bool? ToBoolOrNull(object input)
    {
        if (input == null || input is DBNull)
            return null;
        // 直接处理 bool 类型
        if (input is bool b)
            return b;
        
        // 处理数值类型
        if (input is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
        {
            try
            {
                var d = Convert.ToDouble(input);
                return d != 0;
            }
            catch
            {
                return null;
            }
        }

        // 处理枚举类型
        if (input.GetType().IsEnum)
            return Convert.ToInt32(input) != 0;

        // 处理字符串
        var str = input.SafeString().Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(str))
            return null;

        // 扩展支持的字符串
        switch (str)
        {
            case "1":
            case "是":
            case "ok":
            case "yes":
            case "y":
            case "on":
            case "enable":
            case "enabled":
            case "t":
            case "true":
                return true;
            case "0":
            case "否":
            case "不":
            case "no":
            case "fail":
            case "n":
            case "off":
            case "disable":
            case "disabled":
            case "f":
            case "false":
                return false;
        }
        // 尝试标准 bool 解析
        if (bool.TryParse(str, out var result))
            return result;

        return null;
    }

    #endregion

    #region ToDate(转换为日期)

    /// <summary>
    /// 转换为日期（<see cref="DateTime"/>）
    /// </summary>
    /// <param name="input">输入值，支持日期字符串、Date对象等</param>
    /// <param name="defaultValue">默认值，当转换失败时返回此值，默认为 DateTime.MinValue (0001-01-01 00:00:00)</param>
    /// <returns>
    /// 转换后的日期值。如果转换成功，则返回转换后的日期；
    /// 如果转换失败，先尝试返回指定的默认值，若默认值未指定则返回 DateTime.MinValue
    /// </returns>
    /// <remarks>
    /// 此方法使用 <see cref="DateTime.TryParse(string, out DateTime)"/> 进行日期解析，所以支持多种日期格式。
    /// 当输入为 null 或转换失败时，返回指定的默认值或 DateTime.MinValue。
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime value1 = Conv.ToDate("2023-01-15");           // 返回 2023-01-15 00:00:00
    /// DateTime value2 = Conv.ToDate("2023/01/15 13:45:30");  // 返回 2023-01-15 13:45:30
    /// DateTime value3 = Conv.ToDate("abc");                  // 转换失败，返回 0001-01-01 00:00:00
    /// DateTime value4 = Conv.ToDate(null);                   // 转换失败，返回 0001-01-01 00:00:00
    /// DateTime value5 = Conv.ToDate("abc", new DateTime(2023, 1, 1)); // 转换失败，返回 2023-01-01 00:00:00
    /// </code>
    /// </example>
    public static DateTime ToDate(object input, DateTime defaultValue = default) => ToDateOrNull(input, defaultValue) ?? DateTime.MinValue;

    /// <summary>
    /// 转换为可空日期（<see cref="DateTime"/>?）
    /// </summary>
    /// <param name="input">输入值，支持日期字符串、Date对象等</param>
    /// <param name="defaultValue">默认值，当输入为 null 时返回此值，默认为 null</param>
    /// <returns>
    /// 转换后的可空日期值。如果输入不为 null 且转换成功，则返回转换后的日期；
    /// 如果输入为 null，则返回指定的默认值；
    /// 如果转换失败，则返回 null 或指定的默认值
    /// </returns>
    /// <remarks>
    /// 此方法使用 <see cref="DateTime.TryParse(string, out DateTime)"/> 进行日期解析，所以支持多种日期格式。
    /// 区别于 ToDate 方法，此方法在转换失败时可返回 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? value1 = Conv.ToDateOrNull("2023-01-15");            // 返回 2023-01-15 00:00:00
    /// DateTime? value2 = Conv.ToDateOrNull("2023/01/15 13:45:30");   // 返回 2023-01-15 13:45:30
    /// DateTime? value3 = Conv.ToDateOrNull("abc");                   // 转换失败，返回 null
    /// DateTime? value4 = Conv.ToDateOrNull(null);                    // 输入为null，返回 null
    /// DateTime? value5 = Conv.ToDateOrNull(null, new DateTime(2023, 1, 1)); // 输入为null，返回 2023-01-01 00:00:00
    /// </code>
    /// </example>
    public static DateTime? ToDateOrNull(object input, DateTime? defaultValue = null)
    {
        if (input == null)
            return defaultValue;
        return DateTime.TryParse(input.SafeString(), out var result) ? result : defaultValue;
    }

    #endregion

    #region ToGuid(转换为Guid)

    /// <summary>
    /// 转换为全局唯一标识符（<see cref="Guid"/>）
    /// </summary>
    /// <param name="input">输入值，支持Guid字符串等</param>
    /// <returns>
    /// 转换后的Guid值。如果转换成功，则返回转换后的Guid；
    /// 如果转换失败，则返回 Guid.Empty (00000000-0000-0000-0000-000000000000)
    /// </returns>
    /// <remarks>
    /// 此方法使用 <see cref="Guid.TryParse(string, out Guid)"/> 进行解析，支持多种Guid字符串格式。
    /// Guid通常用于生成唯一标识符，例如用作数据库主键或唯一ID。
    /// </remarks>
    /// <example>
    /// <code>
    /// Guid value1 = Conv.ToGuid("83B0233C-A24F-49FD-8083-1337209EBC9A"); // 成功转换为对应Guid
    /// Guid value2 = Conv.ToGuid("{83B0233C-A24F-49FD-8083-1337209EBC9A}"); // 成功转换为对应Guid
    /// Guid value3 = Conv.ToGuid("abc");  // 转换失败，返回 00000000-0000-0000-0000-000000000000
    /// Guid value4 = Conv.ToGuid(null);   // 转换失败，返回 00000000-0000-0000-0000-000000000000
    /// </code>
    /// </example>
    public static Guid ToGuid(object input) => ToGuidOrNull(input) ?? Guid.Empty;

    /// <summary>
    /// 转换为可空的全局唯一标识符（<see cref="Guid"/>?）
    /// </summary>
    /// <param name="input">输入值，支持Guid字符串等</param>
    /// <returns>
    /// 转换后的可空Guid值。如果转换成功，则返回转换后的Guid；
    /// 如果转换失败（输入为null、空字符串或格式不正确），则返回null
    /// </returns>
    /// <remarks>
    /// 此方法使用 <see cref="Guid.TryParse(string, out Guid)"/> 进行解析，支持多种Guid字符串格式。
    /// 与 ToGuid 方法不同，此方法在转换失败时返回 null 而非 Guid.Empty。
    /// </remarks>
    /// <example>
    /// <code>
    /// Guid? value1 = Conv.ToGuidOrNull("83B0233C-A24F-49FD-8083-1337209EBC9A"); // 成功转换为对应Guid
    /// Guid? value2 = Conv.ToGuidOrNull("{83B0233C-A24F-49FD-8083-1337209EBC9A}"); // 成功转换为对应Guid
    /// Guid? value3 = Conv.ToGuidOrNull("abc");  // 转换失败，返回 null
    /// Guid? value4 = Conv.ToGuidOrNull(null);   // 转换失败，返回 null
    /// </code>
    /// </example>
    public static Guid? ToGuidOrNull(object input) => Guid.TryParse(input.SafeString(), out var result) ? result : null;

    #endregion

    #region ToGuidList(转换为Guid集合)

    /// <summary>
    /// 转换为Guid集合（<see cref="List{Guid}"/>）
    /// </summary>
    /// <param name="input">输入值，以逗号分隔的Guid集合字符串</param>
    /// <returns>
    /// 转换后的Guid集合。每个有效的Guid会被添加到集合中，无效的值会被忽略。
    /// 如果输入为null或空字符串，则返回空集合。
    /// </returns>
    /// <remarks>
    /// 此方法通过 <see cref="ToList{T}"/> 泛型方法进行转换，将输入字符串按逗号分割后，
    /// 尝试将每个部分转换为Guid。转换失败的部分会被忽略或转换为默认值，具体取决于 To{T} 的实现。
    /// Guid集合常用于批量处理唯一标识符，例如批量查询、批量操作等场景。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 假设有两个有效的Guid字符串
    /// string guidString = "83B0233C-A24F-49FD-8083-1337209EBC9A,EAB523C6-2FE7-47BE-89D5-C6D440C3033A";
    /// 
    /// List&lt;Guid&gt; guidList = Conv.ToGuidList(guidString); 
    /// // 返回包含两个Guid的集合 [83B0233C-A24F-49FD-8083-1337209EBC9A, EAB523C6-2FE7-47BE-89D5-C6D440C3033A]
    /// 
    /// List&lt;Guid&gt; emptyList = Conv.ToGuidList("");  // 返回空集合 []
    /// List&lt;Guid&gt; nullList = Conv.ToGuidList(null); // 返回空集合 []
    /// 
    /// // 对于包含无效Guid的字符串，只有有效的部分会被解析
    /// List&lt;Guid&gt; mixedList = Conv.ToGuidList("83B0233C-A24F-49FD-8083-1337209EBC9A,invalid-guid"); 
    /// // 返回包含一个Guid的集合 [83B0233C-A24F-49FD-8083-1337209EBC9A]
    /// </code>
    /// </example>
    public static List<Guid> ToGuidList(string input) => ToList<Guid>(input);

    #endregion

    #region ToBytes(转换为字节数组)

    /// <summary>
    /// 将字符串转换为字节数组（<see cref="byte"/>[]）
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>
    /// 转换后的字节数组。如果输入为 null 或空字符串，则返回空数组。
    /// 默认使用 UTF-8 编码进行转换。
    /// </returns>
    /// <remarks>
    /// 此方法是 <see cref="ToBytes(string, Encoding)"/> 的简化版本，默认使用 UTF-8 编码。
    /// 字节数组常用于二进制数据处理、网络传输、加密解密等场景。
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] bytes1 = Conv.ToBytes("Hello");      // 返回 UTF-8 编码的 "Hello" 的字节数组 [72, 101, 108, 108, 111]
    /// byte[] bytes2 = Conv.ToBytes("");           // 返回空数组 []
    /// byte[] bytes3 = Conv.ToBytes(null);         // 返回空数组 []
    /// </code>
    /// </example>
    /// <seealso cref="Encoding.UTF8"/>
    /// <seealso cref="ToBytes(string, Encoding)"/>
    public static byte[] ToBytes(string input) => ToBytes(input, Encoding.UTF8);

    /// <summary>
    /// 使用指定编码将字符串转换为字节数组（<see cref="byte"/>[]）
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="encoding">字符编码，例如 <see cref="Encoding.UTF8"/>、<see cref="Encoding.Unicode"/> 等</param>
    /// <returns>
    /// 使用指定编码转换后的字节数组。如果输入为 null 或空字符串，则返回空数组。
    /// </returns>
    /// <remarks>
    /// 不同的编码方式会产生不同的字节数组结果，常见的编码有：
    /// - UTF-8：变长编码，英文占1字节，中文通常占3字节
    /// - Unicode (UTF-16)：固定长度编码，每个字符占2字节
    /// - ASCII：仅支持英文和基本符号，每个字符占1字节
    /// 
    /// 此方法在处理 null 或空白字符串时会返回空数组（长度为0的数组），而不是 null。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 使用 UTF-8 编码（默认）
    /// byte[] bytesUtf8 = Conv.ToBytes("Hello世界", Encoding.UTF8);    
    /// // 返回 [72, 101, 108, 108, 111, 228, 184, 150, 231, 149, 140]
    /// 
    /// // 使用 Unicode 编码
    /// byte[] bytesUnicode = Conv.ToBytes("Hello世界", Encoding.Unicode); 
    /// // 返回 [72, 0, 101, 0, 108, 0, 108, 0, 111, 0, 22, 78, 76, 117]
    /// 
    /// // 使用 ASCII 编码（不支持中文，中文会被替换或丢失）
    /// byte[] bytesAscii = Conv.ToBytes("Hello世界", Encoding.ASCII);   
    /// // 返回 [72, 101, 108, 108, 111, 63, 63]（问号表示无法表示的字符）
    /// 
    /// byte[] bytesEmpty = Conv.ToBytes("", Encoding.UTF8);            // 返回空数组 []
    /// byte[] bytesNull = Conv.ToBytes(null, Encoding.UTF8);           // 返回空数组 []
    /// </code>
    /// </example>
    /// <seealso cref="Encoding"/>
    /// <seealso cref="Encoding.GetBytes(string)"/>
    public static byte[] ToBytes(string input, Encoding encoding) =>
        string.IsNullOrWhiteSpace(input) ? [] : encoding.GetBytes(input);

    #endregion

    #region ToBase64(转换为base64字符串)

    /// <summary>
    /// 将字符串转换为Base64编码的字符串
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>
    /// 转换后的Base64编码字符串。如果输入为 null 或空白字符串，则返回 null。
    /// 默认使用 UTF-8 编码将输入字符串转换为字节数组，然后再进行Base64编码。
    /// </returns>
    /// <remarks>
    /// Base64编码是一种将二进制数据转换为可打印ASCII字符的编码方式，
    /// 常用于在只能传输文本的场合传输二进制数据，如电子邮件、URL参数等。
    /// 
    /// Base64编码会将3个字节的二进制数据编码为4个ASCII字符，所以编码后的数据通常比原始数据大约增加33%。
    /// </remarks>
    /// <example>
    /// <code>
    /// string base64 = Conv.ToBase64("Hello");     // 返回 "SGVsbG8="
    /// string empty = Conv.ToBase64("");           // 返回 null
    /// string nullResult = Conv.ToBase64(null);    // 返回 null
    /// 
    /// // 中文字符串的Base64编码
    /// string base64Chinese = Conv.ToBase64("你好");  // 返回 "5L2g5aW9"
    /// 
    /// // Base64编码后的字符串可以通过Convert.FromBase64String转回字节数组
    /// byte[] decodedBytes = Convert.FromBase64String("SGVsbG8="); 
    /// string originalString = Encoding.UTF8.GetString(decodedBytes);  // 返回 "Hello"
    /// </code>
    /// </example>
    /// <seealso cref="Convert.ToBase64String(byte[])"/>
    /// <seealso cref="Convert.FromBase64String(string)"/>
    public static string ToBase64(string input) => string.IsNullOrWhiteSpace(input)
        ? null
        : Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

    #endregion

    #region ToList(泛型集合转换)

    /// <summary>
    /// 将以逗号分隔的字符串转换为泛型集合（<see cref="List{T}"/>）
    /// </summary>
    /// <typeparam name="T">目标元素类型，可以是任何可转换类型，如<see cref="int"/>、<see cref="Guid"/>等</typeparam>
    /// <param name="input">以逗号分隔的字符串，每个元素会尝试转换为指定的类型T</param>
    /// <returns>
    /// 转换后的泛型集合。如果输入为 null 或空白字符串，则返回空集合。
    /// 字符串中的每个有效元素都会被转换为指定类型的对象并添加到集合中。
    /// </returns>
    /// <remarks>
    /// 此方法会将输入字符串按逗号（","）分割，然后尝试将每个部分转换为指定的类型T。
    /// 转换过程使用 <see cref="To{T}"/> 方法进行，空白元素会被自动忽略。
    /// 如果某个元素无法转换为指定类型，则可能返回该类型的默认值或被忽略，取决于 To{T} 的实现。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 转换为整数集合
    /// List&lt;int&gt; intList = Conv.ToList&lt;int&gt;("1,2,3,4,5");    
    /// // 返回 [1, 2, 3, 4, 5]
    /// 
    /// // 转换为小数集合，包含空元素会被忽略
    /// List&lt;double&gt; doubleList = Conv.ToList&lt;double&gt;("1.1, 2.2, , 3.3");  
    /// // 返回 [1.1, 2.2, 3.3]
    /// 
    /// // 转换为Guid集合
    /// string guidStr = "83B0233C-A24F-49FD-8083-1337209EBC9A,EAB523C6-2FE7-47BE-89D5-C6D440C3033A";
    /// List&lt;Guid&gt; guidList = Conv.ToList&lt;Guid&gt;(guidStr);
    /// // 返回包含两个Guid的集合
    /// 
    /// // 混合有效和无效元素
    /// List&lt;int&gt; mixedList = Conv.ToList&lt;int&gt;("1,two,3,four,5");  
    /// // 返回 [1, 0, 3, 0, 5]，其中"two"和"four"转换为默认值0
    /// 
    /// // 空或null输入
    /// List&lt;int&gt; emptyList = Conv.ToList&lt;int&gt;("");    // 返回空集合 []
    /// List&lt;int&gt; nullList = Conv.ToList&lt;int&gt;(null);   // 返回空集合 []
    /// </code>
    /// </example>
    /// <seealso cref="To{T}"/>
    /// <seealso cref="ToGuidList"/>
    public static List<T> ToList<T>(string input)
    {
        var result = new List<T>();
        if (string.IsNullOrWhiteSpace(input))
            return result;
        var array = input.Split(',');
        result.AddRange(from each in array where !string.IsNullOrWhiteSpace(each) select To<T>(each));
        return result;
    }

    #endregion

    #region To(通用泛型转换)

    /// <summary>
    /// 通用泛型转换方法，将输入值转换为指定的目标类型
    /// </summary>
    /// <typeparam name="T">目标类型，可以是任何基础类型、枚举、字符串或支持类型转换的自定义类型</typeparam>
    /// <param name="input">待转换的输入值</param>
    /// <returns>
    /// 转换后的目标类型值。如果转换成功，则返回转换后的值；
    /// 如果转换失败或输入为 null 或空字符串，则返回目标类型的默认值（引用类型为null，值类型为0等）
    /// </returns>
    /// <remarks>
    /// 此方法是一个通用的类型转换工具，支持多种类型转换场景：
    /// 1. 支持基本类型、枚举类型和字符串类型的转换
    /// 2. 支持实现了 IConvertible 接口的类型
    /// 3. 支持 JSON 元素到目标类型的转换
    /// 
    /// 转换失败时不会抛出异常，而是返回目标类型的默认值。
    /// 所有其他的类型转换方法（如 ToInt, ToDouble 等）底层都使用此方法。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 基本类型转换
    /// int intValue = Conv.To&lt;int&gt;("123");                     // 返回 123
    /// double doubleValue = Conv.To&lt;double&gt;("123.45");          // 返回 123.45
    /// bool boolValue = Conv.To&lt;bool&gt;("true");                  // 返回 true
    /// 
    /// // 枚举类型转换（假设有一个枚举类型 Color 包含 Red, Green, Blue）
    /// Color color = Conv.To&lt;Color&gt;("Red");                    // 返回 Color.Red
    /// Color numericColor = Conv.To&lt;Color&gt;("1");               // 返回 对应值为1的枚举项
    /// 
    /// // 字符串和Guid转换
    /// string str = Conv.To&lt;string&gt;(123);                       // 返回 "123"
    /// Guid guid = Conv.To&lt;Guid&gt;("83B0233C-A24F-49FD-8083-1337209EBC9A");  // 返回对应的Guid
    /// 
    /// // 转换失败的情况
    /// int failedInt = Conv.To&lt;int&gt;("abc");                     // 转换失败，返回 0
    /// double failedDouble = Conv.To&lt;double&gt;(null);             // 转换失败，返回 0.0
    /// MyClass failedClass = Conv.To&lt;MyClass&gt;("invalid");       // 转换失败，返回 null
    /// </code>
    /// </example>
    /// <seealso cref="TypeDescriptor.GetConverter(Type)"/>
    /// <seealso cref="IConvertible"/>
    public static T To<T>(object input)
    {
        if (input == null)
            return default;
        if (input is string && string.IsNullOrWhiteSpace(input.ToString()))
            return default;

        var type = Common.GetType<T>();
        var typeName = type.Name.ToUpperInvariant();
        try
        {
            if (typeName == "STRING" || typeName == "GUID")
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(input.ToString());
            if (type.IsEnum)
                return Enums.Parse<T>(input);
            if (input is IConvertible)
                return (T)System.Convert.ChangeType(input, type, CultureInfo.InvariantCulture);
            if (input is JsonElement element)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return Json.ToObject<T>(element.GetRawText(), options);
            }
            return (T)input;
        }
        catch
        {
            return default;
        }
    }

    #endregion

    #region ToDictionary(转换为字典)

    /// <summary>
    /// 将对象转换为字典（属性名-属性值）
    /// </summary>
    /// <param name="input">要转换的对象，通常是一个类的实例</param>
    /// <returns>
    /// 包含对象所有公共属性的字典，键为属性名，值为属性值。
    /// 如果输入为 null，则返回空字典。
    /// </returns>
    /// <remarks>
    /// 此方法使用默认配置调用 <see cref="ToDictionary(object, bool)"/>，使用原始属性名作为键。
    /// 常用于对象序列化、动态访问对象属性等场景。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 假设有一个类 Person
    /// var person = new Person { Name = "张三", Age = 30 };
    /// 
    /// // 转换为字典
    /// IDictionary&lt;string, object&gt; dict = Conv.ToDictionary(person);
    /// // 返回 { "Name" = "张三", "Age" = 30 }
    /// 
    /// // 可以通过字典访问属性
    /// string name = (string)dict["Name"];  // 获取 "张三"
    /// int age = (int)dict["Age"];          // 获取 30
    /// 
    /// // 转换 null 对象
    /// IDictionary&lt;string, object&gt; emptyDict = Conv.ToDictionary(null);  // 返回空字典 {}
    /// </code>
    /// </example>
    /// <seealso cref="ToDictionary(object, bool)"/>
    /// <seealso cref="PropertyDescriptor"/>
    public static IDictionary<string, object> ToDictionary(object input) => ToDictionary(input, false);

    /// <summary>
    /// 将对象转换为字典（属性名-属性值），可选择是否使用显示名称
    /// </summary>
    /// <param name="input">要转换的对象，通常是一个类的实例</param>
    /// <param name="useDisplayName">
    /// 是否使用显示名称作为字典的键：
    /// - true: 优先使用属性的 Description 或 DisplayName 特性值作为键
    /// - false: 使用原始属性名作为键
    /// </param>
    /// <returns>
    /// 包含对象所有公共属性的字典，键为属性名或显示名称，值为属性值。
    /// 如果输入为 null，则返回空字典。
    /// </returns>
    /// <remarks>
    /// 此方法有几种处理流程：
    /// 1. 如果输入对象为 null，返回空字典
    /// 2. 如果输入对象已经是键值对集合（实现了 IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt;），则直接转换为字典
    /// 3. 否则，使用反射获取对象的所有公共属性，将其添加到结果字典中
    /// 
    /// 当 useDisplayName 为 true 时，属性的键名优先级为：
    /// 1. Description 特性值（如果存在）
    /// 2. DisplayName 特性值（如果存在）
    /// 3. 原始属性名
    /// </remarks>
    /// <example>
    /// <code>
    /// // 定义一个带特性的类
    /// public class Person
    /// {
    ///     [Description("姓名")]
    ///     public string Name { get; set; }
    ///     
    ///     [DisplayName("年龄")]
    ///     public int Age { get; set; }
    ///     
    ///     public string Address { get; set; }
    /// }
    /// 
    /// var person = new Person { Name = "张三", Age = 30, Address = "北京" };
    /// 
    /// // 使用原始属性名
    /// IDictionary&lt;string, object&gt; dict1 = Conv.ToDictionary(person, false);
    /// // 返回 { "Name" = "张三", "Age" = 30, "Address" = "北京" }
    /// 
    /// // 使用显示名称
    /// IDictionary&lt;string, object&gt; dict2 = Conv.ToDictionary(person, true);
    /// // 返回 { "姓名" = "张三", "年龄" = 30, "Address" = "北京" }
    /// </code>
    /// </example>
    /// <seealso cref="PropertyDescriptor"/>
    /// <seealso cref="TypeDescriptor.GetProperties(object)"/>
    /// <seealso cref="DescriptionAttribute"/>
    /// <seealso cref="DisplayNameAttribute"/>
    public static IDictionary<string, object> ToDictionary(object input, bool useDisplayName)
    {
        var result = new Dictionary<string, object>();
        if (input == null)
            return result;
        if (input is IEnumerable<KeyValuePair<string, object>> dict)
#if NETSTANDARD2_0
            return new Dictionary<string, object>(dict.ToDictionary());
#else
            return new Dictionary<string, object>(dict);
#endif
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(input))
        {
            var value = property.GetValue(input);
            result.Add(GetPropertyDescriptorName(property, useDisplayName), value);
        }
        return result;
    }

    /// <summary>
    /// 获取属性描述符的名称
    /// </summary>
    /// <param name="property">属性描述符</param>
    /// <param name="useDisplayName">是否使用显示名称，可使用[Description] 或 [DisplayName]特性设置</param>
    /// <returns>
    /// 属性的名称。根据 useDisplayName 参数和特性的存在情况，返回适当的名称：
    /// - 如果 useDisplayName 为 false，返回原始属性名
    /// - 如果 useDisplayName 为 true 且有 Description 特性，返回 Description 值
    /// - 如果 useDisplayName 为 true 且有 DisplayName 特性（无 Description），返回 DisplayName 值
    /// - 其他情况返回原始属性名
    /// </returns>
    private static string GetPropertyDescriptorName(PropertyDescriptor property, bool useDisplayName)
    {
        if (useDisplayName == false)
            return property.Name;
        if (string.IsNullOrEmpty(property.Description) == false)
            return property.Description;
        if (string.IsNullOrEmpty(property.DisplayName) == false)
            return property.DisplayName;
        return property.Name;
    }

    #endregion

    #region ToEnum(转换为枚举)

    /// <summary>
    /// 将输入值转换为指定的枚举类型
    /// </summary>
    /// <typeparam name="T">目标枚举类型</typeparam>
    /// <param name="input">输入值，可以是枚举名称字符串、数值或枚举值</param>
    /// <returns>
    /// 转换后的枚举值。如果转换成功，则返回转换后的枚举值；
    /// 如果转换失败，则返回枚举类型的默认值（通常为0对应的枚举项）
    /// </returns>
    /// <remarks>
    /// 此方法是 <see cref="ToEnum{T}(object, T)"/> 的简化版本，使用枚举的默认值作为转换失败时的返回值。
    /// 内部调用 <see cref="ToEnumOrNull{T}"/> 方法尝试转换，并在结果为 null 时返回默认值。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 假设有一个枚举定义：
    /// public enum Color { Red = 1, Green = 2, Blue = 3 }
    /// 
    /// // 1. 使用枚举名称字符串转换
    /// Color color1 = Conv.ToEnum&lt;Color&gt;("Red");      // 返回 Color.Red
    /// Color color2 = Conv.ToEnum&lt;Color&gt;("GREEN");     // 返回 Color.Green (不区分大小写)
    /// 
    /// // 2. 使用数值转换
    /// Color color3 = Conv.ToEnum&lt;Color&gt;(2);           // 返回 Color.Green
    /// Color color4 = Conv.ToEnum&lt;Color&gt;("3");          // 返回 Color.Blue (字符串形式的数值)
    /// 
    /// // 3. 转换失败的情况
    /// Color color5 = Conv.ToEnum&lt;Color&gt;("Yellow");    // 无效的枚举名称，返回 0 (默认值)
    /// Color color6 = Conv.ToEnum&lt;Color&gt;(99);          // 无效的枚举值，返回 0 (默认值)
    /// Color color7 = Conv.ToEnum&lt;Color&gt;(null);        // 返回 0 (默认值)
    /// </code>
    /// </example>
    public static T ToEnum<T>(object input) where T : struct => ToEnum<T>(input, default);

    /// <summary>
    /// 将输入值转换为指定的枚举类型，转换失败时返回指定的默认值
    /// </summary>
    /// <typeparam name="T">目标枚举类型</typeparam>
    /// <param name="input">输入值，可以是枚举名称字符串、数值或枚举值</param>
    /// <param name="defaultValue">转换失败时返回的默认枚举值</param>
    /// <returns>
    /// 转换后的枚举值。如果转换成功，则返回转换后的枚举值；
    /// 如果转换失败，则返回指定的默认值
    /// </returns>
    /// <remarks>
    /// 内部调用 <see cref="ToEnumOrNull{T}"/> 方法尝试转换，并在结果为 null 时返回指定的默认值。
    /// 枚举转换支持名称匹配（不区分大小写）和数值匹配。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 假设有一个枚举定义：
    /// public enum Color { Red = 1, Green = 2, Blue = 3 }
    /// 
    /// // 1. 使用枚举名称字符串转换
    /// Color color1 = Conv.ToEnum&lt;Color&gt;("Red", Color.Blue);      // 返回 Color.Red
    /// 
    /// // 2. 使用数值转换
    /// Color color2 = Conv.ToEnum&lt;Color&gt;(2, Color.Blue);          // 返回 Color.Green
    /// 
    /// // 3. 转换失败的情况，返回指定的默认值
    /// Color color3 = Conv.ToEnum&lt;Color&gt;("Yellow", Color.Blue);   // 返回 Color.Blue
    /// Color color4 = Conv.ToEnum&lt;Color&gt;(99, Color.Red);          // 返回 Color.Red
    /// Color color5 = Conv.ToEnum&lt;Color&gt;(null, Color.Green);      // 返回 Color.Green
    /// </code>
    /// </example>
    public static T ToEnum<T>(object input, T defaultValue) where T : struct => ToEnumOrNull<T>(input) ?? defaultValue;

    /// <summary>
    /// 将输入值转换为可空的指定枚举类型
    /// </summary>
    /// <typeparam name="T">目标枚举类型</typeparam>
    /// <param name="input">输入值，可以是枚举名称字符串、数值或枚举值</param>
    /// <returns>
    /// 转换后的可空枚举值。如果转换成功，则返回转换后的枚举值；
    /// 如果转换失败（输入为 null、不存在的枚举名称或无效值），则返回 null
    /// </returns>
    /// <remarks>
    /// 此方法使用 <see cref="System.Enum.TryParse{T}(string, bool, out T)"/> 进行枚举值的解析，支持名称匹配（不区分大小写）和数值匹配。
    /// 相比于直接使用 Enum.TryParse 或 Enum.Parse，此方法更安全，不会在转换失败时抛出异常。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 假设有一个枚举定义：
    /// public enum Color { Red = 1, Green = 2, Blue = 3 }
    /// 
    /// // 1. 使用枚举名称字符串转换
    /// Color? color1 = Conv.ToEnumOrNull&lt;Color&gt;("Red");      // 返回 Color.Red
    /// Color? color2 = Conv.ToEnumOrNull&lt;Color&gt;("GREEN");     // 返回 Color.Green (不区分大小写)
    /// 
    /// // 2. 使用数值转换
    /// Color? color3 = Conv.ToEnumOrNull&lt;Color&gt;(2);           // 返回 Color.Green
    /// Color? color4 = Conv.ToEnumOrNull&lt;Color&gt;("3");          // 返回 Color.Blue (字符串形式的数值)
    /// 
    /// // 3. 转换失败的情况，返回 null
    /// Color? color5 = Conv.ToEnumOrNull&lt;Color&gt;("Yellow");    // 返回 null (无效的枚举名称)
    /// Color? color6 = Conv.ToEnumOrNull&lt;Color&gt;(99);          // 返回 null (无效的枚举值)
    /// Color? color7 = Conv.ToEnumOrNull&lt;Color&gt;(null);        // 返回 null
    /// 
    /// // 4. 检测转换结果
    /// if (color5.HasValue)
    /// {
    ///     // 转换成功，可以使用 color5.Value
    /// }
    /// else
    /// {
    ///     // 转换失败，处理失败情况
    /// }
    /// </code>
    /// </example>
    public static T? ToEnumOrNull<T>(object input) where T : struct
    {
        var success = System.Enum.TryParse(input.SafeString(), true, out T result);
        if (success)
            return result;
        return null;
    }

    #endregion

    #region ToStringOrDefault(转换为字符串或默认值)

    /// <summary>
    /// 将可空值类型转换为字符串，如果为 null 则返回默认值
    /// </summary>
    /// <typeparam name="T">值类型，如 <see cref="int"/>、<see cref="double"/>、<see cref="DateTime"/> 等</typeparam>
    /// <param name="input">可空值类型实例（T?）</param>
    /// <param name="defaultValue">当 input 为 null 时返回的默认字符串</param>
    /// <returns>
    /// 如果输入值不为 null，则返回其字符串表示；
    /// 如果输入值为 null，则返回指定的默认值。
    /// </returns>
    /// <remarks>
    /// 这个方法通过判断 Nullable&lt;T&gt; 的 HasValue 属性，简化了对可空值类型的字符串转换和默认值处理。
    /// 它使用 T 的 ToString() 方法进行转换，转换后的格式取决于 T 类型的默认格式化方式。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 基本使用
    /// int? nullableInt = 42;
    /// string result1 = Conv.ToStringOrDefault(nullableInt, "默认值");   // 返回 "42"
    /// 
    /// // null 值的处理
    /// int? nullValue = null;
    /// string result2 = Conv.ToStringOrDefault(nullValue, "默认值");     // 返回 "默认值"
    /// 
    /// // 不同类型的使用示例
    /// bool? flag = true;
    /// string result3 = Conv.ToStringOrDefault(flag, "未知");           // 返回 "True"
    /// 
    /// DateTime? date = DateTime.Now;
    /// string result4 = Conv.ToStringOrDefault(date, "未设置日期");      // 返回当前日期的字符串表示
    /// 
    /// DateTime? nullDate = null;
    /// string result5 = Conv.ToStringOrDefault(nullDate, "未设置日期");  // 返回 "未设置日期"
    /// </code>
    /// </example>
    public static string ToStringOrDefault<T>(T? input, string defaultValue) where T : struct =>
        input.HasValue ? input.Value.ToString() : defaultValue;

    /// <summary>
    /// 将可空值类型按指定格式转换为字符串，如果为 null 则返回默认值
    /// </summary>
    /// <typeparam name="T">值类型，必须实现 <see cref="IFormattable"/> 接口，如 <see cref="int"/>、<see cref="decimal"/>、<see cref="DateTime"/> 等</typeparam>
    /// <param name="input">可空值类型实例（T?）</param>
    /// <param name="format">格式字符串，用于控制转换结果的格式，例如 "C" 表示货币，"N2" 表示带两位小数的数值</param>
    /// <param name="defaultValue">当 input 为 null 时返回的默认字符串</param>
    /// <returns>
    /// 如果输入值不为 null，则返回按指定格式转换的字符串表示；
    /// 如果输入值为 null，则返回指定的默认值。
    /// </returns>
    /// <remarks>
    /// 这个方法要求 T 类型必须实现 IFormattable 接口，以支持格式化转换。
    /// 它使用 T 的 ToString(string, IFormatProvider) 方法进行转换，使用当前区域设置 (CultureInfo.CurrentCulture)。<br />
    /// 
    /// 常见的格式字符串：<br />
    /// - 数字类型：<br />
    ///   - "C" 或 "C2"：货币格式，如 "¥123.46"<br />
    ///   - "N" 或 "N2"：带千位分隔符的数字格式，如 "1,234.56"<br />
    ///   - "P" 或 "P2"：百分比格式，如 "12.35%"<br />
    ///   - "F2"：固定点格式，保留2位小数，如 "123.46"<br />
    /// - 日期类型：<br />
    ///   - "d"：短日期格式，如 "2023/6/15"<br />
    ///   - "D"：长日期格式，如 "2023年6月15日"<br />
    ///   - "f"：完整日期/时间格式(短时间)，如 "2023年6月15日 9:30"<br />
    ///   - "F"：完整日期/时间格式(长时间)，如 "2023年6月15日 9:30:00"<br />
    ///   - "yyyy-MM-dd"：自定义格式，如 "2023-06-15"
    /// </remarks>
    /// <example>
    /// <code>
    /// // 数字格式化示例
    /// decimal? price = 1234.567m;
    /// string result1 = Conv.ToStringOrDefault(price, "C2", "未定价");        // 返回 "¥1,234.57"（根据当前区域设置）
    /// string result2 = Conv.ToStringOrDefault(price, "N1", "未定价");        // 返回 "1,234.6"
    /// string result3 = Conv.ToStringOrDefault(price, "F4", "未定价");        // 返回 "1234.5670"
    /// 
    /// // null 值的处理
    /// decimal? nullPrice = null;
    /// string result4 = Conv.ToStringOrDefault(nullPrice, "C2", "未定价");    // 返回 "未定价"
    /// 
    /// // 日期格式化示例
    /// DateTime? orderDate = new DateTime(2023, 6, 15, 14, 30, 0);
    /// string result5 = Conv.ToStringOrDefault(orderDate, "yyyy-MM-dd", "未下单");  // 返回 "2023-06-15"
    /// string result6 = Conv.ToStringOrDefault(orderDate, "f", "未下单");           // 返回 "2023年6月15日 14:30"
    /// 
    /// // null 日期的处理
    /// DateTime? nullDate = null;
    /// string result7 = Conv.ToStringOrDefault(nullDate, "yyyy-MM-dd", "未下单");   // 返回 "未下单"
    /// </code>
    /// </example>
    public static string ToStringOrDefault<T>(T? input, string format, string defaultValue)
        where T : struct, IFormattable =>
        input.HasValue ? input.Value.ToString(format, CultureInfo.CurrentCulture) : defaultValue;

    #endregion

    #region ToRMB(转换为人民币大写金额)

    /// <summary>
    /// 将数值转换为人民币大写金额
    /// </summary>
    /// <param name="input">
    /// 输入值，可以是表示金额的字符串、decimal、double、int等数值类型。
    /// 输入值将被尝试解析为decimal类型进行处理。
    /// </param>
    /// <returns>
    /// 转换后的人民币大写金额字符串。
    /// 如果输入为null，返回default(string)，即null；
    /// 如果输入无法解析为有效的金额，则原样返回输入的字符串表示。
    /// </returns>
    /// <remarks>
    /// 此方法使用特殊的算法将数值转换为中文大写金额表示法，常用于财务凭证、合同金额等场景。<br />
    /// 转换规则包括：<br />
    /// 1. 整数部分按"个十百千万亿兆"等单位逐位转换<br />
    /// 2. 小数部分最多保留角、分两位<br />
    /// 3. 使用"零壹贰叁肆伍陆柒捌玖"代表数字0-9<br />
    /// 4. 处理特殊情况如零的读法、连续多个零的处理等<br /><br />
    /// 
    /// 超大数额时使用"万亿兆京垓秭穰"等单位，但实际应用中极少使用超过"兆"的单位。
    /// 负数将在前面加上"负"字。
    /// </remarks>
    /// <example>
    /// <code>
    /// string rmb1 = Conv.ToRMB(123.45);           // 返回 "壹佰贰拾叁元肆角伍分"
    /// string rmb2 = Conv.ToRMB("1234567.89");     // 返回 "壹佰贰拾叁万肆仟伍佰陆拾柒元捌角玖分"
    /// string rmb3 = Conv.ToRMB(0.5);              // 返回 "伍角"
    /// string rmb4 = Conv.ToRMB(0.05);             // 返回 "伍分"
    /// string rmb5 = Conv.ToRMB(1000000);          // 返回 "壹佰万元整"
    /// string rmb6 = Conv.ToRMB(-100);             // 返回 "负壹佰元整"
    /// string rmb7 = Conv.ToRMB(10001);            // 返回 "壹万零壹元整"
    /// string rmb8 = Conv.ToRMB("invalid");        // 无法解析，返回 "invalid"
    /// string rmb9 = Conv.ToRMB(null);             // 返回 null
    /// </code>
    /// </example>
    // ReSharper disable once InconsistentNaming
    public static string ToRMB(object input)
    {
        if (input == null)
            return default;
        string tempValue;
        if (input is string valueStr)
            tempValue = valueStr;
        else
            tempValue = input.ToString();
        if (!decimal.TryParse(tempValue, out var decValue))
            return tempValue;
        tempValue = decValue.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
        var temp = Regex.Replace(tempValue,
            @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))",
            "${b}${z}");
        var result = Regex.Replace(temp, ".", m => "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
        return result;
    }

    #endregion
}