using System.Web;
using Bing.Extensions;

namespace Bing.Utils.Parameters.Parsers;

/// <summary>
/// Url参数解析器
/// </summary>
public class UrlParameterParser : ParameterParserBase, IParameterParser
{
    /// <inheritdoc />
    public override void LoadData(string data)
    {
        if (data.IsEmpty())
            return;
        if (data.Contains("?"))
            data = data.Substring(data.IndexOf("?", StringComparison.Ordinal) + 1);
        var parameters = HttpUtility.ParseQueryString(data);
        foreach (var key in parameters.AllKeys)
            Add(key, parameters.Get(key));
    }
}
