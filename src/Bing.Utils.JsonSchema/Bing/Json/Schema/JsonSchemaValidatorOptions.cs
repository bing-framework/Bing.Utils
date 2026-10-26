using System.Globalization;

namespace Bing.Json.Schema;

/// <summary>配置 Schema 构建、引用来源及校验行为；创建校验器时复制配置。</summary>
public sealed class JsonSchemaValidatorOptions
{
    /// <summary>获取或设置未声明 $schema 时使用的版本，默认为 Draft 2020-12。</summary>
    public JsonSchemaDialect DefaultDialect { get; set; } = JsonSchemaDialect.Draft202012;

    /// <summary>获取或设置根 Schema 的绝对基础 URI；不得包含非空片段，$id 按此 URI 解析。</summary>
    public Uri? BaseUri { get; set; }

    /// <summary>获取预注册引用文档；键为不含非空片段的绝对 URI，值为 JSON Schema 文本。</summary>
    /// <remarks>创建后修改此集合不会改变已有校验器；不会自动下载网络或文件引用。</remarks>
    public IDictionary<Uri, string> ReferencedSchemas { get; } = new Dictionary<Uri, string>();

    /// <summary>获取或设置是否对 format 执行断言，默认只遵循方言规定的注解行为。</summary>
    public bool ValidateFormats { get; set; }

    /// <summary>获取或设置规则错误的消息语言；默认使用固定文化，未提供翻译时由引擎回退。</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;
}
