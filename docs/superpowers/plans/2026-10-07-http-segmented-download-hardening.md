# HTTP 并发分片下载可靠性 Implementation Plan

> 历史未完成计划：本文保留 2026-10-07 的部分执行状态，不是当前可直接继续执行的任务清单。勾选项仅表示当时已记录的局部结果；未勾选项、公开 API 和 TestServer 门禁不得视为已完成。当前源码与测试状态需要独立复核。

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将当前并发分片下载原型补齐为可中断恢复、资源身份安全、跨进程互斥且不会整片驻留内存的显式下载能力。

**Architecture:** 保留现有单流断点续传协议与默认下载行为；并发分片使用独立的 `.binghttp.segments` 暂存目录和 `.binghttp.segments.json` 状态文件；分片下载与单流续传共同持有 `<target>.binghttp.lock`。内置 HTTP 路径在响应仍存活时直接把正文写入分片文件，由调度器独占重试预算，并在每片持久化后串行写入原子检查点。

**Tech Stack:** C#、HttpClient、System.Text.Json、SHA-256、文件独占锁、xUnit、Shouldly、ASP.NET Core TestServer。生产目标：`net8.0;net7.0;net6.0;netstandard2.0`。

**Spec:** `docs/quality/http-api-surface-comparison-2026-10-06.md` 与 `docs/modules/http.md` 中已声明的单流续传安全边界；本计划只扩展并发分片能力，不改变旧入口。

## Global Constraints

- 不改变 `DownloadFileAsync`、`DownloadFileWithResultAsync` 和 `DownloadFileResumableWithResultAsync` 的行为。
- 不升级依赖、不修改版本号、不自动提交 Git。
- 所有状态和文档文件使用 UTF-8；状态写入必须临时文件落盘后原子替换。
- 不保存认证头、Cookie 或其他原始敏感请求头，只保存稳定请求身份摘要。
- 单元测试放 `Bing.Utils.Http.Tests`；真实 TestServer、跨进程锁与真实文件系统协作放 `Bing.Utils.Http.Tests.Integration`。
- 每轮先执行 Change Impact Analysis，再按 L0、L1、L2、L3 顺序扩大验证；没有 `OPEN_ACTIONABLE` 时停止。

---

### Task 1：冻结状态协议与分片规划约束

**Files:**
- Modify: `src/Bing.Utils.Http/Bing/Http/HttpDownloadSegmentState.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/HttpDownloadRangeProbe.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.SegmentedDownload.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpDownloadSegmentStateTest.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestSegmentedDownloadTest.cs`

**Interfaces:**
- Produce: internal `HttpDownloadSegmentStateStore.ValidatePlan(HttpDownloadSegmentsState state, long segmentSize)`。
- Note: `Validator` and `ContentEncoding` already exist on `HttpDownloadSegmentsState`; request fingerprint and response identity metadata are deferred to Task 4, where they are first consumed.

- [x] Added unit tests for matching layout, mismatched `segmentSize`, and non-positive sizes.
- [x] `DownloadFileSegmentedAsync` validates the supplied plan before sending the metadata probe.
- [x] Focused tests passed: 15/15; HTTP production `net8.0` Release build passed with 0 warnings and 0 errors.

---

### Task 2：复用目标级跨进程锁

**Files:**
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.SegmentedDownload.cs`
- Test: `tests/Bing.Utils.Http.Tests.Integration/Http/HttpResumableDownloadIntegrationTest.cs`

**Interfaces:**
- Reused: private `HttpRequest<TResult>.AcquireResumeLock(string fullPath)` and the existing `<target>.binghttp.lock`; a target cannot run single-stream resumable and segmented downloads concurrently.

- [x] Added an independent-process test that holds `.binghttp.lock`; segmented download throws `IOException` and sends no HTTP request.
- [x] Verified the existing target and staged segment remain unchanged after lock conflict.
- [x] Acquired the shared lock after argument/plan validation and before metadata probing; held it through scheduling and final merge.
- [x] Integration test passed: 1/1; HTTP production `net8.0` Release build passed with 0 warnings and 0 errors.

---

### Task 3：实现检查点持久化与恢复清洗

**Files:**
- Modify: `src/Bing.Utils.Http/Bing/Http/HttpDownloadSegmentState.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/HttpDownloadSegmentFileStore.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/HttpDownloadSegmentScheduler.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.SegmentedDownload.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpDownloadSegmentSchedulerTest.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestSegmentedDownloadTest.cs`

**Interfaces:**
- Produce: `HttpDownloadSegmentFileStore.Reconcile(HttpDownloadSegmentsState state)`，把缺失、长度错误或摘要错误的已完成片段降级为未完成。
- Produce: scheduler checkpoint callback `Func<HttpDownloadSegmentsState, CancellationToken, Task>`。

- [x] 写中断恢复测试：第一次保留一个已完成片，第二次只请求未完成片并最终合并。
- [x] 每片先完成写入、`Flush(true)`、原子替换和 SHA-256，再在检查点临界区更新克隆状态并调用 `SaveAsync`；检查点成功后才通知进度。
- [ ] 锁内加载并清洗状态；规划或资源身份不匹配时重新开始，单片文件损坏时只重下该片，未知 schema 不复用。
- [x] 合并成功后删除状态文件和空暂存目录；锁文件保留。
- [x] 运行中断恢复、损坏片重下、未知 schema、取消保留和成功清理测试。

---

### Task 4：绑定服务端资源身份

**Files:**
- Modify: `src/Bing.Utils.Http/Bing/Http/HttpDownloadRangeProbe.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.SegmentedDownload.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpDownloadRangeProbeTest.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestSegmentedDownloadTest.cs`

**Interfaces:**
- Produce: 规范化的强 ETag 或 Last-Modified 验证器、`Content-Encoding`（无编码时为 `identity`）和关键请求头摘要。

- [ ] 写身份变化测试：强 ETag、弱 ETag 回退、Last-Modified、Content-Encoding、关键请求头摘要任一变化都不得拼接旧片。
- [ ] 初始探测选择强 ETag，否则选择 Last-Modified；范围请求发送 `Accept-Encoding: identity`、`Range`、`If-Range`。
- [ ] 每个 206 在写盘前比较验证器、编码和总长度。
- [ ] 无可靠验证器时允许一次调用内下载，但失败后删除不可安全跨调用复用的状态和片文件，保留原目标。

---

### Task 5：移除整片内存缓冲并统一重试预算

**Files:**
- Modify: `src/Bing.Utils.Http/Bing/Http/HttpDownloadSegmentScheduler.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.SegmentedDownload.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestSegmentedDownloadTest.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestSegmentedDownloadTest.cs`

**Interfaces:**
- Produce: internal scheduler delegate `Func<HttpDownloadRange, CancellationToken, Task<HttpDownloadSegmentState>>`。
- Preserve: public custom-stream overload通过 `HttpDownloadSegmentFileStore.WriteSegmentAsync` 适配。

- [ ] 写流式落盘测试：第一批响应字节应直接进入片文件，而非等待整片读完。
- [ ] 内置请求路径在 `ExecuteAsync` 回调中完成元数据校验，并把响应流直接传给文件存储，不创建 `MemoryStream`。
- [ ] 范围请求使用 `allowRetries: false`，仅调度器控制分片重试；初始探测可保留请求级重试。
- [ ] 对始终失败的分片断言总请求次数严格等于 `maxRetries + 1`，并校验 observer 次数和 attempt。

---

### Task 6：真实 TestServer 端到端覆盖

**Files:**
- Existing integration coverage: `tests/Bing.Utils.Http.Tests.Integration/Http/HttpResumableDownloadIntegrationTest.cs` 中的共享锁冲突场景。
- Planned but not created: 独立的分片下载 TestServer 端到端测试文件。

- [ ] TestServer 返回稳定 ETag、`Accept-Ranges: bytes` 和严格 206；断言实际并发大于 1、范围无重叠、最终 SHA-256 正确。
- [ ] 第一次让一个分片中途断开，第二次只重下未完成片并复用已校验片。
- [ ] 使用源站与转发 TestServer 覆盖编码、ETag 变化、错误 Content-Range、失败状态和取消；失败不得替换已有目标。
- [ ] 独立 TestServer 测试套件创建后，再以其实际类名运行定向集成测试；历史计划中的 `HttpSegmentedDownloadIntegrationTest` 类并未创建。

---

### Task 7：公共 API、文档与阶段验收

**Files:**
- Modify: `src/Bing.Utils/Bing/Http/IHttpRequestCapabilities.cs`
- Modify: `src/Bing.Utils/Bing/Http/HttpTransferCapabilityExtensions.cs`
- Modify: `docs/modules/http.md`
- Modify: `docs/modules/http-optimization-migration.md`
- Modify: `docs/quality/http-api-surface-comparison-2026-10-06.md`

**Interfaces:**
- Produce: 可选 `IHttpRequestSegmentedDownload<TResult>` 和扩展方法；不得向既有 `IHttpRequest<TResult>` 增加必需成员。

- [ ] 可靠性闭环后再暴露可选能力；公开入口内部创建/恢复状态，不要求业务调用方手工构造状态对象。
- [ ] 记录侧车命名、锁生命周期、验证器要求、无验证器限制、重试预算、失败保留与清理方式。
- [ ] L0：HTTP Release net8 构建、`git diff --check`、UTF-8 检查。
- [ ] L1：Tasks 1-6 精准测试；L2：HTTP 单元 net8 全量；L3：HTTP 集成 net8 全量。
- [ ] 只有 API 形状冻结后才运行 L4 多目标构建与 API baseline；本计划不触发 L5 重型基准。
- [ ] 按 GOAL_RULES 输出全部状态分组；`Open Actionable` 为 0 时停止。

## Deferred Independent Plans

- 二进制 API baseline、包产物 consumer smoke 与发布前门禁。
- `DataTableConverter` / `DataSetConverter` 反序列化契约：实现读取或明确声明 `CanRead=false`。
- 模块文档与真实测试项目映射同步，清理已过期的“缺少测试项目”说明。
- 宿主侧 `Microsoft.Extensions.Http.Resilience` / OpenTelemetry 示例包；核心请求对象不重复实现熔断、限流或底层 span。
- 测试目标框架支持政策：继续维护 net5/netcoreapp3.1，或转为编译兼容与定期 legacy gate，需要维护者批准。
