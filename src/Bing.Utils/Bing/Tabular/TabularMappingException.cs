namespace Bing.Tabular;

/// <summary>
/// 表示表格映射配置在处理数据前无法通过预检。
/// </summary>
public sealed class TabularMappingException : ArgumentException
{
    /// <summary>
    /// 初始化映射配置异常。
    /// </summary>
    public TabularMappingException(string code, string message)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("问题编码不能为空。", nameof(code));
        Code = code;
    }

    /// <summary>获取稳定问题编码。</summary>
    public string Code { get; }
}
