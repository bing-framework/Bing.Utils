namespace Bing.Json.Schema;

/// <summary>表示 Schema 定义、方言或引用无效，区别于待校验数据不满足规则。</summary>
public sealed class JsonSchemaConfigurationException : Exception
{
    /// <summary>使用指定说明创建配置异常。</summary>
    /// <param name="message">配置错误说明。</param>
    public JsonSchemaConfigurationException(string message) : base(message) { }

    /// <summary>使用指定说明及原始原因创建配置异常。</summary>
    /// <param name="message">配置错误说明。</param>
    /// <param name="innerException">原始解析或引擎异常。</param>
    public JsonSchemaConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
