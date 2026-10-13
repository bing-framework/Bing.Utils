# HTTP API 表面核对

历史记录日期：2026-10-06

文档复核日期：2026-10-13；复核基准：`3a1ef0c` 与本次工作树源码

原始基准：2026-10-06 当时的 HEAD 与工作树源码；未记录准确提交号

范围：HTTP 请求、传输能力、断点续传、响应缓存及本轮 HttpRequest<TResult> partial 拆分

## 历史结论与当前源码复核

本轮维护性拆分只移动 HttpRequest<TResult> 的私有响应读取、重试观测、传输、上传进度、JSON 数组流和续传实现；公开类型、公开成员及 protected 扩展点没有因为 partial 拆分被删除或改名。

以下为当时记录的 HTTP 能力扩展，不是当前工作树相对 HEAD 的未提交 API 增量：

- 响应结果与观测模型：HttpResponseResult<TResult>、HttpDownloadResult、HttpDownloadProgress、HttpUploadProgress、HttpRequestCompletion、HttpRetryScheduled、HttpRetryOptions、HttpUploadConfirmationException<TResult>。
- 请求能力接口：IHttpRequestResponse<TResult>、IHttpRequestStreamingResponse<TResult>、IHttpRequestInterceptorRegistration<TResult>、IHttpRequestDownloadResult<TResult>、IHttpRequestResumableDownload<TResult>、IHttpRequestResumableDownloadValidation<TResult>、IHttpRequestObservationRegistration<TResult>、IHttpRequestRetryObservationRegistration<TResult>。
- 传输能力接口：IHttpRequestRawContentRegistration<TResult>、IHttpRequestDownloadProgressRegistration<TResult>、IHttpRequestUploadProgressRegistration<TResult>、IHttpRequestJsonArrayStreaming<TResult>、IHttpRequestStreamingJson<TResult>。
- 能力扩展：原始二进制/流正文、正文工厂、下载/上传进度、流式 JSON、JSON 数组逐项读取、响应快照、拦截器、完成/重试观测、条件请求、上传确认、下载结果和断点续传入口。
- HTTP 方法与工厂扩展：IHttpRequestFactory.CreateRequest<TResult>，以及 Request、Patch、Head、Options 等扩展入口。
- 显式响应缓存：IHttpResponseCache<TResult>、IHttpResponseCacheStatistics、HttpResponseCacheStatistics、InMemoryHttpResponseCache<TResult>、FileHttpResponseCache、FileHttpResponseCacheOptions、HttpResponseCacheOptions、HttpResponseCacheExtensions。
- 分布式缓存边界：DistributedHttpResponseSnapshot、IDistributedHttpResponseStore 和仅供测试/单进程使用的 InMemoryDistributedHttpResponseStore。
- 续传维护：HttpResumableDownloadMaintenance、HttpResumableDownloadCleanupResult，提供显式的过期侧车清理并保留锁文件；真实跨进程续传锁测试已纳入集成用例。

`IDistributedHttpResponseStore` 只定义宿主实现边界，不包含 Redis、数据库或跨节点实现；这不是续传清理 API 的限制。

缓存 API 是显式调用模型：调用方提供稳定缓存键和 TTL，默认不会自动改变请求管线，也不自动实现 Cache-Control、Vary、ETag 或 304 语义。

## 拆分与兼容性核对

- 原 HttpRequest<TResult> 仍是同一个公开类型，当前声明为 partial。
- HttpRequest.ResponseReading.cs、HttpRequest.RetryObservation.cs、HttpRequest.Transfer.cs、HttpRequest.UploadProgress.cs、HttpRequest.JsonArrayStream.cs 和 HttpRequest.ResumableDownload.cs 只承载对应实现代码。
- protected 扩展点仍位于 HttpRequest.cs，没有为拆分改变继承入口。
- 当前 IHttpRequest<TResult> 包含 DownloadToAsync、DownloadFileAsync 等必需成员；其他能力通过可选接口和扩展方法提供。不能据此断言历史实现无需修改；缺少对应可选能力时，扩展入口抛出 NotSupportedException。
- 断点续传保持显式启用，仅 GET 使用同目录 .binghttp.part、.binghttp.resume.json 和并发锁文件；请求身份、响应编码、Content-Range、206/200/416 语义及 SHA-256/自定义验证器均有对应公开入口或结果模型。

## 历史验证记录（本次未重跑）

以下数字来自原始实施记录，未绑定准确历史提交号，不能作为当前工作树的新鲜验证证据。

- 本轮 net8.0 HTTP 单元测试全量：1753/1753；断点续传清理专项：3/3。
- 多目标 HTTP 单元测试和集成测试：沿用 [HTTP 后续增强计划](../superpowers/plans/2026-10-05-http-next-enhancements.md) 的 2026-10-06 验收记录，分别为 net8/net7/net6 1720/1720、net5/netcoreapp3.1 1719/1719，以及五目标各 80/80；本轮新增跨进程用例仅在 net8.0 全量执行为 83/83。
- 真实 TestServer 断点续传专项测试：14/14；覆盖压缩协商、代理转发和连接中断后的续传。
- HTTP 生产目标 Release 构建：netstandard2.0、net6.0、net7.0、net8.0 均通过，最新验收记录为 0 警告、0 错误。
- 原始记录报告源码 git diff --check 通过；该记录不覆盖本次未跟踪文档的格式检查。

1693/1692、65/65 与上列数字属于不同历史候选；不能混用为当前工作树的测试总数。文件缓存容量 API 的历史验证亦应按当时记录解释。

## 公开 API 基线边界

该记录是源码级 API 表面核对，不替代发布工具生成的二进制 API baseline。本次未执行远程 CI 或二进制兼容性比较；若发布流程要求阻止破坏性 API 变更，应在对应发布环境生成并比较正式 baseline。

2026-10-13 源码复核确认并发分片下载已实现：`HttpRequest.SegmentedDownload.cs` 的 `DownloadFileSegmentedAsync` 显式接收调用方提供的探测与下载委托，持久化分片状态、调度并发下载，并在完成后校验和合并；默认单流续传入口不变。原记录将其列为未实现能力已经过时。后续加固的计划状态见 [分片下载加固历史计划](../superpowers/plans/2026-10-07-http-segmented-download-hardening.md)，未完成计划项不能视为已验证能力。

自动缓存接入、真实分布式缓存后端与一致性策略，以及核心库直接集成外部 Resilience/OpenTelemetry 库仍需独立设计；当前显式缓存和可选能力接口不等于已经覆盖这些集成。
