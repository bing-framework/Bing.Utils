using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

/// <summary>
/// 为 JSON 文本和对象提供便捷的转换扩展方法。
/// </summary>
public static class JsonExtensions
{
    #region ToObject(将Json字符串转换为对象)

    /// <summary>
    /// 将 JSON 文本反序列化为对象。
    /// </summary>
    /// <typeparam name="T">反序列化结果的目标类型。</typeparam>
    /// <param name="json">要处理的 JSON 文本。</param>
    /// <returns>JSON 为空或空白时返回 default(T)；否则返回反序列化结果，该结果可以为 null。</returns>
    public static T ToObject<T>(this string json)
    {
        return JsonHelper.ToObject<T>(json);
    }

    /// <summary>
    /// 将 JSON 文本反序列化为由内容确定类型的对象。
    /// </summary>
    /// <param name="json">要处理的 JSON 文本。</param>
    /// <returns>返回由 JSON 内容确定类型的反序列化结果；JSON 表示 null 时返回 null。</returns>
    public static object ToObject(this string json)
    {
        return JsonHelper.ToObject(json);
    }

    #endregion

    #region ToJson(将对象转换为Json字符串)

    /// <summary>
    /// 将对象序列化为 JSON 文本。
    /// </summary>
    /// <param name="target">要序列化的对象；为 <see langword="null"/> 时返回空字符串。</param>
    /// <param name="isConvertToSingleQuotes">是否将序列化结果中的所有双引号替换为单引号，包括字符串值中的引号。</param>
    /// <param name="camelCase">是否使用驼峰命名序列化对象成员。</param>
    /// <param name="indented">是否以缩进格式输出 JSON。</param>
    /// <returns>返回序列化后的 JSON 文本；目标对象为 null 时返回空字符串。</returns>
    public static string ToJson(this object target, bool isConvertToSingleQuotes = false, bool camelCase = false,
        bool indented = false)
    {
        return JsonHelper.ToJson(target, isConvertToSingleQuotes, camelCase, indented);
    }

    #endregion

    #region ToJObject(将Json字符串转换为Linq对象)

    /// <summary>
    /// 将 JSON 文本解析为 <see cref="JObject"/>。
    /// </summary>
    /// <param name="json">要处理的 JSON 文本。</param>
    /// <remarks>解析前会移除所有字面量 <c>&amp;nbsp;</c>。</remarks>
    /// <returns>返回解析得到的 JSON 对象；输入为空或空白时返回空对象。</returns>
    public static JObject ToJObject(this string json)
    {
        return JsonHelper.ToJObject(json);
    }

    #endregion

    #region IsJson(判断字符串是否为Json格式)

    /// <summary>
    /// 检查文本是否以 JSON 对象或数组的括号包围；此方法不验证完整 JSON 语法。
    /// </summary>
    /// <param name="json">要检查的 JSON 文本；不能为 <see langword="null"/>。</param>
    /// <returns>当去除首尾空白后的文本以一对对象括号或数组括号包围时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    /// <exception cref="NullReferenceException"><paramref name="json"/> 为 <see langword="null"/> 时抛出。</exception>
    public static bool IsJson(this string json)
    {
        return JsonHelper.IsJson(json);
    }

    #endregion
}
