﻿using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Bing.Utils.Json;

/// <summary>
/// Json操作辅助类
/// </summary>
public static class JsonHelper
{
    #region JsonDateTimeFormat(Json时间格式化)

    /// <summary>
    /// Json时间格式化
    /// </summary>
    /// <param name="json">json</param>
    /// <returns></returns>
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
    /// 将Json字符串转换为对象
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="json">Json字符串</param>
    /// <returns></returns>
    public static T ToObject<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default(T);
        }

        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// 将Json字符串转换为对象
    /// </summary>
    /// <param name="json">Json字符串</param>
    /// <param name="type">实体类型</param>
    /// <returns></returns>
    public static object ToObject(string json, Type type)
    {
        return JsonConvert.DeserializeObject(json, type);
    }

    /// <summary>
    /// 将Json字符串转换为对象
    /// </summary>
    /// <param name="json">Json字符串</param>
    /// <returns></returns>
    public static object ToObject(string json)
    {
        return JsonConvert.DeserializeObject(json);
    }

    #endregion

    #region ToJson(将对象转换为Json字符串)

    /// <summary>
    /// 将对象转换为Json字符串
    /// </summary>
    /// <param name="target">目标对象</param>
    /// <param name="isConvertToSingleQuotes">是否将双引号转换成单引号</param>
    /// <param name="camelCase">是否驼峰式命名</param>
    /// <param name="indented">是否缩进</param>
    /// <returns></returns>
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
    /// 将对象序列化到Json文件
    /// </summary>
    /// <param name="fileName">文件名，绝对路径</param>
    /// <param name="obj">对象</param>
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
    /// 从Json文件反序列成对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="fileName">文件名，绝对路径</param>
    /// <returns></returns>
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
    /// 将Form表单转换成Json字符串
    /// </summary>
    /// <param name="formStr">Form表单字符串</param>
    /// <returns></returns>
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
    /// 将Json字符串转换为Linq对象
    /// </summary>
    /// <param name="json">Json字符串</param>
    /// <returns></returns>
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
    /// 判断字符串是否为Json格式。为效率考虑，仅做了开始和结束字符的验证
    /// </summary>
    /// <param name="json">json字符串</param>
    public static bool IsJson(string json)
    {
        json = json.Trim();
        return json.StartsWith("{") && json.EndsWith("}") || json.StartsWith("[") && json.EndsWith("]");
    }

    #endregion
}