using System.ComponentModel;
using Bing.Extensions;
using Bing.Reflection;

namespace Bing.Helpers;

/// <summary>
/// 枚举 操作
/// </summary>
public static class Enums
{
    /// <summary>
    /// 枚举值字段
    /// </summary>
    private const string EnumValueField = "value__";

    #region Parse(获取实例)

    /// <summary>
    /// 获取实例
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="member">成员名或值，范例：Enum1枚举有成员A=0，则传入"A"或"0"获取 Enum1.A</param>
    public static TEnum Parse<TEnum>(object member)
    {
        var value = member.SafeString();
        if (string.IsNullOrWhiteSpace(value))
        {
            if (typeof(TEnum).IsGenericType)
                return default;
            throw new ArgumentNullException(nameof(member));
        }
        return (TEnum)System.Enum.Parse(Common.GetType<TEnum>(), value, true);
    }

    #endregion

    #region ParseByDescription(通过描述获取实例)

    /// <summary>
    /// 通过描述获取实例
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="desc">描述</param>
    public static TEnum ParseByDescription<TEnum>(string desc)
    {
        if (string.IsNullOrWhiteSpace(desc))
        {
            if (typeof(TEnum).IsGenericType)
                return default;
            throw new ArgumentNullException(nameof(desc));
        }
        var type = Common.GetType<TEnum>();
        var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Default);
        var fieldInfo =
            fieldInfos.FirstOrDefault(p => p.GetCustomAttribute<DescriptionAttribute>(false)?.Description == desc);
        if (fieldInfo == null)
            throw new ArgumentNullException($"在枚举（{type.FullName}）中，未发现描述为“{desc}”的枚举项。");
        return (TEnum)System.Enum.Parse(type, fieldInfo.Name);
    }

    #endregion

    #region GetName(获取成员名)

    /// <summary>
    /// 获取成员名
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="member">成员名、值、实例均可，范例：Enum1枚举有成员A=0，则传入Enum1.A或0，获取成员名"A"</param>
    public static string GetName<TEnum>(object member) => GetName(Common.GetType<TEnum>(), member);

    /// <summary>
    /// 获取成员名
    /// </summary>
    /// <param name="type">枚举类型</param>
    /// <param name="member">成员名、值、实例均可，范例：Enum1枚举有成员A=0，则传入Enum1.A或0，获取成员名"A"</param>
    public static string GetName(Type type, object member)
    {
        if (type == null)
            return string.Empty;
        if (member == null)
            return string.Empty;
        if (member is string)
            return member.ToString();
        if (type.GetTypeInfo().IsEnum == false)
            return string.Empty;
        return System.Enum.GetName(type, member);
    }

    #endregion

    #region GetValue(获取成员值)

    /// <summary>
    /// 获取成员值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="member">成员名、值、实例均可，范例:Enum1枚举有成员A=0,可传入"A"、0、Enum1.A，获取值0</param>
    /// <exception cref="ArgumentNullException">成员为空</exception>
    public static int GetValue<TEnum>(object member) => GetValue(Common.GetType<TEnum>(), member);

    /// <summary>
    /// 获取成员值
    /// </summary>
    /// <param name="type">枚举类型</param>
    /// <param name="member">成员名、值、实例均可，范例:Enum1枚举有成员A=0,可传入"A"、0、Enum1.A，获取值0</param>
    /// <exception cref="ArgumentNullException">成员为空</exception>
    public static int GetValue(Type type, object member)
    {
        var value = member.SafeString();
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(member));
        return (int)System.Enum.Parse(type, value, true);
    }

    #endregion

    #region GetValues(获取枚举所有值)

    /// <summary>
    /// 获取枚举所有值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>枚举值数组</returns>
#if NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum[] GetValues<TEnum>() where TEnum : struct, System.Enum
    {
        return System.Enum.GetValues<TEnum>();
    }
#else
    public static TEnum[] GetValues<TEnum>() where TEnum : struct, System.Enum
    {
        var type = typeof(TEnum);
        ValidateEnum(type);
        return (TEnum[])System.Enum.GetValues(type);
    }
#endif

    /// <summary>
    /// 获取枚举所有值作为整数数组
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>枚举值对应的整数数组</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int[] GetValuesAsInt<TEnum>() where TEnum : struct, System.Enum
    {
        var enumValues = GetValues<TEnum>();
        var result = new int[enumValues.Length];
        for (var i = 0; i < enumValues.Length; i++) 
            result[i] = Unsafe.As<TEnum, int>(ref enumValues[i]);
        return result;
    }

    /// <summary>
    /// 获取枚举所有值作为字符串数组（.NET 5.0+ 优化版本）
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>枚举值对应的字符串数组</returns>
    /// <remarks>
    /// 返回枚举值的字符串表示，不是名称
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] GetValuesAsString<TEnum>() where TEnum : struct, System.Enum
    {
        var enumValues = GetValues<TEnum>();
        var result = new string[enumValues.Length];
        for (var i = 0; i < enumValues.Length; i++) 
            result[i] = enumValues[i].ToString();
        return result;
    }

    /// <summary>
    /// 获取枚举值与名称的键值对
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>值为键，名称为值的字典</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dictionary<int, string> GetValueNamePairs<TEnum>() where TEnum : struct, System.Enum
    {
        var enumValues = GetValues<TEnum>();
        var enumNames = GetNames<TEnum>();
        var result = new Dictionary<int, string>(enumValues.Length);

        for (var i = 0; i < enumValues.Length; i++)
        {
            var value = Unsafe.As<TEnum, int>(ref enumValues[i]);
            result[value] = enumNames[i];
        }
        return result;
    }

    /// <summary>
    /// 获取枚举值与描述的键值对
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>值为键，描述为值的字典</returns>
    public static Dictionary<int, string> GetValueDescriptionPairs<TEnum>() where TEnum : struct, System.Enum
    {
        var enumValues = GetValues<TEnum>();
        var result = new Dictionary<int, string>(enumValues.Length);
        var type = typeof(TEnum);

        foreach (var enumValue in enumValues)
        {
            var value = Unsafe.As<TEnum, int>(ref Unsafe.AsRef(in enumValue));
            var fieldInfo = type.GetField(enumValue.ToString());
            var description = fieldInfo?.GetCustomAttribute<DescriptionAttribute>()?.Description
                              ?? enumValue.ToString();
            result[value] = description;
        }
        return result;
    }

    /// <summary>
    /// 获取枚举所有值（非泛型版本）
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>枚举值数组</returns>
    /// <exception cref="ArgumentNullException">枚举类型为空</exception>
    /// <exception cref="InvalidOperationException">类型不是枚举</exception>
    public static Array GetValues(Type enumType)
    {
        if (enumType == null)
            throw new ArgumentNullException(nameof(enumType));

        enumType = Common.GetType(enumType);
        ValidateEnum(enumType);
        return System.Enum.GetValues(enumType);
    }

    /// <summary>
    /// 获取枚举值与名称的键值对（非泛型版本）
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>值为键，名称为值的字典</returns>
    /// <exception cref="ArgumentNullException">枚举类型为空</exception>
    /// <exception cref="InvalidOperationException">类型不是枚举</exception>
    public static Dictionary<int, string> GetValueNamePairs(Type enumType)
    {
        if (enumType == null)
            throw new ArgumentNullException(nameof(enumType));

        enumType = Common.GetType(enumType);
        ValidateEnum(enumType);

        var enumValues = System.Enum.GetValues(enumType);
        var enumNames = System.Enum.GetNames(enumType);
        var result = new Dictionary<int, string>(enumValues.Length);

        for (int i = 0; i < enumValues.Length; i++)
        {
            var value = (int)enumValues.GetValue(i);
            result[value] = enumNames[i];
        }
        return result;
    }

#endregion

    #region HasValue(是否包含指定值)

    /// <summary>
    /// 检查枚举中是否包含指定值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">要检查的值</param>
    /// <returns>如果包含返回true，否则返回false</returns>
#if NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasValue<TEnum>(TEnum value) where TEnum : struct, System.Enum
    {
        return System.Enum.IsDefined<TEnum>(value);
    }
#else
    public static bool HasValue<TEnum>(TEnum value) where TEnum : struct
    {
        var type = typeof(TEnum);
        ValidateEnum(type);
        return System.Enum.IsDefined(type, value);
    }
#endif

    /// <summary>
    /// 检查枚举中是否包含指定整数值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">要检查的整数值</param>
    /// <returns>如果包含返回true，否则返回false</returns>
    public static bool HasValue<TEnum>(int value) where TEnum : struct
    {
        var type = typeof(TEnum);
        ValidateEnum(type);
        return System.Enum.IsDefined(type, value);
    }

    #endregion

    #region GetDescription(获取描述)

    /// <summary>
    /// 获取描述，使用<see cref="DescriptionAttribute"/>特性设置描述
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="member">成员名、值、实例均可,范例:Enum1枚举有成员A=0,可传入"A"、0、Enum1.A，获取值0</param>
    public static string GetDescription<TEnum>(object member) => Reflections.GetDescription<TEnum>(GetName<TEnum>(member));

    /// <summary>
    /// 获取描述，使用<see cref="DescriptionAttribute"/>特性设置描述
    /// </summary>
    /// <param name="type">枚举类型</param>
    /// <param name="member">成员名、值、实例均可,范例:Enum1枚举有成员A=0,可传入"A"、0、Enum1.A，获取值0</param>
    public static string GetDescription(Type type, object member) => Reflections.GetDescription(type, GetName(type, member));

    #endregion

    #region GetItems(获取描述项集合)

    /// <summary>
    /// 获取描述项集合，文本设置为Description，值为Value
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    public static List<Item> GetItems<TEnum>() => GetItems(typeof(TEnum));

    /// <summary>
    /// 获取描述项集合，文本设置为Description，值为Value
    /// </summary>
    /// <param name="type">枚举类型</param>
    public static List<Item> GetItems(Type type)
    {
        type = Common.GetType(type);
        ValidateEnum(type);
        var result = new List<Item>();
        foreach (var field in type.GetFields())
            AddItem(type, result, field);
        return result.OrderBy(t => t.SortId).ToList();
    }

    /// <summary>
    /// 验证是否枚举类型
    /// </summary>
    /// <param name="enumType">类型</param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void ValidateEnum(Type enumType)
    {
        if (enumType.IsEnum == false)
            throw new InvalidOperationException($"类型 {enumType} 不是枚举");
    }

    /// <summary>
    /// 添加描述项
    /// </summary>
    /// <param name="type">枚举类型</param>
    /// <param name="result">集合</param>
    /// <param name="field">字段</param>
    private static void AddItem(Type type, ICollection<Item> result, FieldInfo field)
    {
        if (!field.FieldType.IsEnum)
            return;
        var group = Reflections.GetAttribute<EnumGroupAttribute>(field);
        var value = GetValue(type, field.Name);
        var description = Reflections.GetDescription(field);
        result.Add(new Item(description, value, value, group?.Title));
    }

    #endregion

    #region GetDictionary(获取枚举字典)

    /// <summary>
    /// 获取枚举字典
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    public static IDictionary<int, string> GetDictionary<TEnum>() where TEnum : struct
    {
        var enumType = Common.GetType<TEnum>().GetTypeInfo();
        ValidateEnum(enumType);
        var dic = new Dictionary<int, string>();
        foreach (var field in enumType.GetFields())
            AddItem<TEnum>(dic, field);
        return dic;
    }

    /// <summary>
    /// 添加描述项
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="result">集合</param>
    /// <param name="field">字典</param>
    private static void AddItem<TEnum>(IDictionary<int, string> result, FieldInfo field) where TEnum : struct
    {
        if (!field.FieldType.GetTypeInfo().IsEnum)
            return;
        var value = GetValue<TEnum>(field.Name);
        var description = Reflections.GetDescription(field);
        result.Add(value, description);
    }

    #endregion

    #region GetMemberInfos(获取枚举成员信息)

    /// <summary>
    /// 获取枚举成员信息
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    public static IEnumerable<Tuple<int, string, string>> GetMemberInfos<TEnum>() where TEnum : struct
    {
        var type = typeof(TEnum);
        ValidateEnum(type);
        var fields = type.GetFields();
        ICollection<Tuple<int, string, string>> collection = new HashSet<Tuple<int, string, string>>();
        foreach (var field in fields.Where(x => x.Name != EnumValueField))
        {
            var value = GetValue<TEnum>(field.Name);
            var description = Reflections.GetDescription(field);
            collection.Add(new Tuple<int, string, string>(value, field.Name,
                string.IsNullOrWhiteSpace(description) ? field.Name : description));
        }

        return collection;
    }

    #endregion

    #region GetNames(获取枚举所有成员名称)

    /// <summary>
    /// 获取枚举所有成员名称
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    public static string[] GetNames<TEnum>() => GetNames(typeof(TEnum));

    /// <summary>
    /// 获取枚举所有成员名称
    /// </summary>
    /// <param name="type">枚举类型</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static string[] GetNames(Type type)
    {
        type = Common.GetType(type);
        if (type.IsEnum == false)
            throw new InvalidOperationException($"类型 {type} 不是枚举");
        var result = new List<string>();
        foreach (var field in type.GetFields())
        {
            if (!field.FieldType.IsEnum)
                continue;
            result.Add(field.Name);
        }
        return result.ToArray();
    }

    #endregion
}