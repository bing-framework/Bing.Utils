using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class BingHttpClientBuilderExtensions
{
    /// <summary>默认值属于实际客户端实例，避免容器或同名注册之间互相污染。</summary>
    private static readonly ConditionalWeakTable<HttpClient, ResponseLimitDefaults> ResponseLimits = new();

    /// <summary>为命名或类型化客户端设置 Bing 请求的默认响应正文上限。</summary>
    /// <param name="builder">客户端生成器。</param>
    /// <param name="maxBytes">非负正文上限，零仅允许空正文；重复注册时最后一次生效。</param>
    /// <returns>原生成器。</returns>
    /// <remarks>仅作用于 Bing 请求读取路径，直接 HttpClient.SendAsync 不应用此限制。请求级配置可覆盖它。</remarks>
    public static IHttpClientBuilder WithBingDefaultMaxResponseBytes(this IHttpClientBuilder builder, long maxBytes)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        builder.ConfigureHttpClient(client => ResponseLimits.GetValue(client, _ => new ResponseLimitDefaults()).MaxBytes = maxBytes);
        return builder;
    }

    /// <summary>读取客户端默认值；未登记表示无限制。</summary>
    internal static long? GetDefaultMaxResponseBytes(HttpClient client)
        => ResponseLimits.TryGetValue(client, out var value) ? value.MaxBytes : null;

    private sealed class ResponseLimitDefaults
    {
        internal long MaxBytes;
    }
}
