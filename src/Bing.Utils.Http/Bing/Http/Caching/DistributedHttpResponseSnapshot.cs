using System.Collections.ObjectModel;
using System.Net;

namespace Bing.Http.Caching;

/// <summary>

/// 可在外部共享存储中保存的不可变 HTTP 响应快照。

/// </summary>
/// <remarks>正文使用字节数组表达，避免分布式存储契约依赖具体序列化器或可释放的 HTTP 对象。</remarks>
public sealed class DistributedHttpResponseSnapshot
{
    /// <summary>
    /// 缓存快照正文的内部副本。
    /// </summary>
    private readonly byte[] _body;

    /// <summary>

    /// 初始化一个<see cref="DistributedHttpResponseSnapshot"/>类型的实例。

    /// </summary>
    /// <param name="statusCode">HTTP 状态码。</param>
    /// <param name="headers">响应头。</param>
    /// <param name="contentHeaders">正文头。</param>
    /// <param name="body">响应正文；为空表示没有正文。</param>
    /// <param name="rawContent">可选的解码正文文本。</param>
    /// <param name="version">存储返回的不透明版本令牌；新快照可为空。</param>
    public DistributedHttpResponseSnapshot(HttpStatusCode statusCode,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        IReadOnlyDictionary<string, IReadOnlyList<string>> contentHeaders,
        byte[] body, string rawContent = null, string version = null)
    {
        StatusCode = statusCode;
        Headers = Copy(headers);
        ContentHeaders = Copy(contentHeaders);
        _body = body == null ? null : (byte[])body.Clone();
        RawContent = rawContent;
        Version = version;
    }

    /// <summary>

    /// 获取 HTTP 状态码。

    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>

    /// 获取响应头快照。

    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }

    /// <summary>

    /// 获取正文头快照。

    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ContentHeaders { get; }

    /// <summary>

    /// 获取响应正文的独立副本。

    /// </summary>
    public byte[] Body => _body == null ? null : (byte[])_body.Clone();

    /// <summary>

    /// 获取可选的解码正文。

    /// </summary>
    public string RawContent { get; }

    /// <summary>

    /// 获取由存储适配器分配的不透明版本令牌。

    /// </summary>
    public string Version { get; }

    /// <summary>

    /// 获取状态码是否表示成功响应。

    /// </summary>
    public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;

    /// <summary>

    /// 复制响应头并生成不可变的头集合。

    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Copy(
        IReadOnlyDictionary<string, IReadOnlyList<string>> source)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (source != null)
            foreach (var item in source)
                result[item.Key] = Array.AsReadOnly((item.Value ?? Array.Empty<string>()).ToArray());
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(result);
    }
}
