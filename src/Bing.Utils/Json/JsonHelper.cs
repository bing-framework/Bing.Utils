using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Bing.Utils.Json;

/// <summary>
/// 提供 JSON 文本、对象和文件之间的转换方法。
/// </summary>
public static class JsonHelper
{
    #region JsonDateTimeFormat(Json时间格式化)

    /// <summary>
    /// 将 JSON 中的旧式 <c>\/Date(milliseconds)\/</c> 标记替换为日期时间文本。
    /// </summary>
    /// <param name="json">要处理的 JSON 文本。</param>
    /// <returns>输入为空或空白时返回原文本；否则返回替换了旧式日期标记的文本。</returns>
    public static string JsonDateTimeFormat(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        json = Regex.Replace(json, @"\\/Date\((\d+)\)\\/", match =>
        {
            DateTime dt = new DateTime(1970, 1, 1);
            dt = dt.AddMilliseconds(long.Parse(match.Groups[1].Value));
            return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        });
        return json;
    }

    #endregion

    #region ToObject(将Json字符串转换为对象)

    /// <summary>
    /// 将 JSON 文本反序列化为对象。
    /// </summary>
    /// <typeparam name="T">反序列化结果的目标类型。</typeparam>
    /// <param name="json">要处理的 JSON 文本。</param>
    /// <returns>JSON 为空或空白时返回 default(T)；否则返回反序列化结果，该结果可以为 null。</returns>
    public static T ToObject<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default(T);
        }

        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// 将 JSON 文本反序列化为对象。
    /// </summary>
    /// <param name="json">要处理的 JSON 文本。</param>
    /// <param name="type">反序列化结果的目标运行时类型。</param>
    /// <returns>返回与指定运行时类型匹配的反序列化结果；JSON 表示 null 时返回 null。</returns>
    public static object ToObject(string json, Type type)
    {
        return JsonConvert.DeserializeObject(json, type);
    }

    /// <summary>
    /// 将 JSON 文本反序列化为对象。
    /// </summary>
    /// <param name="json">要处理的 JSON 文本。</param>
    /// <returns>返回由 JSON 内容确定类型的反序列化结果；JSON 表示 null 时返回 null。</returns>
    public static object ToObject(string json)
    {
        return JsonConvert.DeserializeObject(json);
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
    public static string ToJson(object target, bool isConvertToSingleQuotes = false, bool camelCase = false,
        bool indented = false)
    {
        if (target == null)
        {
            return string.Empty;
        }

        var options = new JsonSerializerSettings();
        if (camelCase)
        {
            options.ContractResolver = new CamelCasePropertyNamesContractResolver();
        }

        if (indented)
        {
            options.Formatting = Formatting.Indented;
        }

        var result = JsonConvert.SerializeObject(target, options);
        if (isConvertToSingleQuotes)
        {
            result = result.Replace("\"", "'");
        }

        return result;
    }

    #endregion

    #region SerializableToFile(将对象序列化到Json文件)

    /// <summary>
    /// 将对象序列化为 JSON 并写入指定文件。
    /// </summary>
    /// <param name="fileName">目标文件路径，可以是绝对路径或相对路径。</param>
    /// <param name="obj">要序列化并写入文件的对象。</param>
    public static void SerializableToFile(string fileName, object obj)
    {
        lock (obj)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.Write(ToJson(obj, false, false, true));
                }
            }
        }
    }

    #endregion

    #region DeserializeFromFile(从Json文件反序列成对象)

    /// <summary>
    /// 从指定文件读取 JSON 并反序列化为目标类型。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="fileName">目标文件路径，可以是绝对路径或相对路径。</param>
    /// <returns>文件内容为空或空白时返回 default(T)；否则返回反序列化结果，该结果可以为 null。</returns>
    public static T DeserializeFromFile<T>(string fileName)
    {
        try
        {
            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    return ToObject<T>(sr.ReadToEnd());
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    #endregion

    #region ToJsonByForm(将Form表单转换成Json字符串)

    /// <summary>
    /// 将以 <c>&amp;</c> 分隔的表单键值文本转换为 JSON 对象文本。
    /// </summary>
    /// <param name="formStr">由 <c>&amp;</c> 分隔的键值对文本；方法不会进行 URL 解码，值中的等号会被丢弃。</param>
    /// <returns>返回由表单键值对生成的 JSON 对象文本。</returns>
    public static string ToJsonByForm(string formStr)
    {
        Dictionary<string, string> dicData = new Dictionary<string, string>();
        var data = formStr.Split('&');
        for (int i = 0; i < data.Length; i++)
        {
            var dk = data[i].Split('=');
            StringBuilder sb = new StringBuilder(dk[1]);
            for (int j = 2; j <= dk.Length - 1; j++)
            {
                sb.Append(dk[j]);
            }

            dicData.Add(dk[0], sb.ToString());
        }

        return dicData.ToJson();
    }

    #endregion

    #region ToJObject(将Json字符串转换为Linq对象)

    /// <summary>
    /// 将 JSON 文本解析为 <see cref="JObject"/>。
    /// </summary>
    /// <param name="json">要处理的 JSON 文本。</param>
    /// <remarks>解析前会移除所有字面量 <c>&amp;nbsp;</c>。</remarks>
    /// <returns>返回解析得到的 JSON 对象；输入为空或空白时返回空对象。</returns>
    public static JObject ToJObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return JObject.Parse("{}");
        }

        return JObject.Parse(json.Replace("&nbsp;", ""));
    }

    #endregion

    #region IsJson(判断字符串是否为Json格式)

    /// <summary>
    /// 检查文本是否以 JSON 对象或数组的括号包围；此方法不验证完整 JSON 语法。
    /// </summary>
    /// <param name="json">要检查的 JSON 文本；不能为 <see langword="null"/>。</param>
    /// <returns>当去除首尾空白后的文本以一对对象括号或数组括号包围时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    /// <exception cref="NullReferenceException"><paramref name="json"/> 为 <see langword="null"/> 时抛出。</exception>
    public static bool IsJson(string json)
    {
        json = json.Trim();
        return json.StartsWith("{") && json.EndsWith("}") || json.StartsWith("[") && json.EndsWith("]");
    }

    #endregion
}
