using System.Net;
using System.Net.Http;

namespace Bing.Http;

/// <summary>

/// 在每次 HTTP 发送尝试前修改请求，并在发送后观察结果的异步拦截器。

/// </summary>
/// <remarks>发送前按注册顺序调用，发送后按相反顺序调用。每次重试都会重新执行发送前处理。</remarks>
public abstract class HttpRequestInterceptor
{
    /// <summary>
    /// 在当前发送尝试前异步检查或修改请求，例如刷新令牌或添加签名。
    /// </summary>
    /// <param name="context">当前发送尝试的请求上下文。</param>
    /// <param name="token">请求取消令牌，取消请求时可能已取消。</param>
    /// <remarks>此方法抛出的异常会阻止本次发送并向调用方传播，不会触发自动重试；此前已成功执行的拦截器仍会收到发送后回调。</remarks>
    public virtual Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token) => Task.CompletedTask;

    /// <summary>

    /// 在当前发送尝试结束后异步观察响应元数据或发送异常。

    /// </summary>
    /// <param name="context">包含当前尝试结果的请求上下文。</param>
    /// <param name="token">请求取消令牌；发送取消时可能已取消，观察逻辑应尊重该状态。</param>
    /// <remarks>成功进入发送前阶段的拦截器会按逆序收到此回调，发送失败或取消时也会调用。某个发送后回调抛出异常不会阻止其他回调；若发送本身已有异常，该异常优先传播，否则传播首个发送后异常。上下文不提供响应正文或正文流。</remarks>
    public virtual Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token) => Task.CompletedTask;
}

/// <summary>

/// 为同一次 HTTP 发送尝试的拦截器提供请求、响应元数据和共享状态。

/// </summary>
/// <remarks>每次尝试使用独立上下文；同一请求执行中的重试共享 <see cref="Items"/>，另一次执行会创建新的集合。</remarks>
public sealed class HttpRequestInterceptorContext
{
    /// <summary>
    /// 创建一次发送尝试的拦截上下文。
    /// </summary>
    /// <param name="request">当前尝试的请求消息；其生命周期由 HTTP 请求管线管理。</param>
    /// <param name="attempt">从 1 开始的尝试序号。</param>
    /// <param name="items">本次请求执行共享的可变状态集合，同次重试沿用。</param>
    public HttpRequestInterceptorContext(HttpRequestMessage request, int attempt, IDictionary<string, object> items)
    {
        Request = request;
        Attempt = attempt;
        Items = items;
    }

    /// <summary>

    /// 获取当前发送尝试的请求消息。

    /// </summary>
    /// <remarks>发送前回调可修改此消息。消息由请求管线拥有，并在尝试结束时释放；拦截器不应自行释放或在回调结束后继续使用。</remarks>
    /// <remarks>消息由请求管线拥有并在尝试结束时释放；拦截器不应释放它，也不应在回调结束后继续使用。</remarks>
    public HttpRequestMessage Request { get; }
    /// <summary>
    /// 获取从 1 开始的当前发送尝试序号。
    /// </summary>
    public int Attempt { get; }
    /// <summary>
    /// 获取请求执行范围内共享的数据。
    /// </summary>
    /// <remarks>同一次执行中的重试共享此集合，不同执行彼此隔离。</remarks>
    public IDictionary<string, object> Items { get; }
    /// <summary>
    /// 获取或设置响应状态码。
    /// </summary>
    /// <remarks>发送未取得响应时为 <see langword="null"/>。</remarks>
    public HttpStatusCode? StatusCode { get; set; }
    /// <summary>
    /// 获取或设置响应头快照。
    /// </summary>
    /// <remarks>发送未取得响应时为 <see langword="null"/>。</remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; set; }
    /// <summary>
    /// 获取或设置当前尝试耗时。
    /// </summary>
    /// <remarks>从发送前检查开始，到收到响应头或发送失败为止，不包含拦截器和正文处理耗时。</remarks>
    public TimeSpan Elapsed { get; set; }
    /// <summary>
    /// 获取或设置发送阶段异常。
    /// </summary>
    /// <remarks>发送后拦截器自身抛出的异常不会写入此属性。</remarks>
    public Exception Exception { get; set; }
}
