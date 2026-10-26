# 流处理首批补齐实施记录（2026-10-25）

依据[本轮计划](stream-hardening-plan.md)，在当前含未提交成果的工作区上完成实现。范围为 H01—H07；保留旧入口和默认行为，未修改依赖、目标框架、发行版本或 Git 暂存区。普通工作区执行，无 UAW taskId/stageRunId。

## 实现与契约

| 编号 | 本地交付 | 验收依据 |
|---|---|---|
| H01 | 修复 BatchAsync 到期批次饥饿；到期后先交付当前批次，再处理已经读取的下一项 | 同步源阻塞后完成的回归；旧产物首次返回 3 项，当前返回 1 项 |
| H02 | CSV 缓冲快路径使用 ValueTask，移除逐字节 Task 分配 | 固定工作区基线、同环境独立 .NET 8 对照 |
| H03 | CsvOptions.MaxInputBytes / MaxRows，可选总输入与数据行数预算 | 含 BOM、表头、换行的输入边界，行数边界，短读及取消回归 |
| H04 | HttpSseOptions.IdleTimeout，可选正文读取空闲超时 | 心跳续期、重连、Last-Event-ID、消费者耗时排除、原令牌取消和响应释放 |
| H05 | FtpDirectoryTransferLimits、可选能力接口和目录扩展入口 | 文件数、根深度 0、大小未知/超限、跳过/续传和取消 |
| H06 | 修正 FTP、表格状态和指南索引；Windows CI 增加 netstandard 产物身份校验 | PowerShell 静态检查、身份校验；外部 CI 待运行 |
| H07 | 受影响完整回归、四框架消费者、实际 netstandard 和最终构建 | 下表记录；独立审查已完成，未发现确认的可执行问题 |

新增能力与使用方式见 [CSV 指南](../reference/csv-stream-guide.md)、[流处理指南](../reference/stream-platform-guide.md)、[FTP 目录指南](../reference/ftp-directory-guide.md)。

- CSV 的 MaxInputBytes 仅用于读取，从当前流位置计原始字节；含 BOM、表头及分隔符。为区分“正好达到限额”和“超额”，最多多读一个探测字节；缓冲预读可能在交付此前完整行之前发现超限。MaxRows 用于读写，不含表头，超限数据行不会交付或写出。失败不会回滚已经交付的行。
- SSE 默认不设 IdleTimeout。计时只覆盖一次底层正文 ReadAsync 等待；注释心跳也视为活动，不计消费者处理或暂停枚举的时间。通过协作取消收尾底层读取，要求自定义流响应取消；不遗留后台读取。头等待沿用 HttpClient 超时，显式取消优先传播原令牌。
- FTP 文件数含发现后被冲突策略跳过的普通文件，不跟随符号链接。深度以根目录为 0。MaxTotalFileBytes 按选中传输文件的完整声明大小预检，跳过文件不计，续传仍计完整大小；启用后未知大小会失败。这不是实际网络流量或磁盘写入量的硬上限，远程元数据失真或文件变化仍需调用方处理。失败可能留下已完成文件，不提供整个目录事务。
- BatchAsync 无法抢占同步阻塞的 MoveNextAsync；返回后立即检查单调时钟期限。始终保持至多一次在途读取和原有顺序。

## 验证与证据复用

证据根目录为 `artifacts/stream-hardening-2026-10-25/`。环境：Windows，SDK 8.0.425，正式性能宿主 .NET 8.0.31。本轮生产、回归、消费者和 CI 身份记录在 candidate.json，逐项测试计数见 test-summary.json。所有 dotnet 构建与测试串行执行，避免共享 output 目录争用。

| 层级 | 检查 | 结果 |
|---|---|---|
| L0 | 解决方案 Release 构建 | 0 错误、1544 警告；solution-build.log |
| L0 | Git 空白检查 / PowerShell 脚本解析 | 通过；既有换行转换提示保留 |
| L1 | 主包 Batch/CSV 定向 | 16 项通过；tests/core.trx |
| L1 | SSE 定向 | 15 项通过；tests/sse.trx |
| L1 | FTP 目录定向 | 12 项通过；tests/ftp.trx |
| L1 | 最终 CSV 取消用例 | net8.0 / netcoreapp3.1 各 1 项通过；tests/csv-cancel-*.trx |
| L2 | 主包 net8 完整 | 7311 项通过；tests/Bing.Utils.Tests-full.trx |
| L2 | HTTP net8 完整 | 1930 项通过；tests/Bing.Utils.Http.Tests-full.trx |
| L2 | Net net8 完整 | 81 项通过；tests/Bing.Utils.Net.Tests-full.trx |
| L3 | API 消费者 | netstandard2.0/net6.0/net7.0/net8.0 全部编译通过，0 错误、39 警告；consumer.log |
| L3 | .NET Core 3.1 执行真实 netstandard 产物 | 主包 50、HTTP 19、HTTP 集成 1、Net 11、IdUtils 8，共 89 项通过；netstandard/ |
| L3 | CI 身份校验模式 | 5 个项目产物 SHA-256 与 netstandard 输出一致；ci-identity/identity.json |
| L4 | 最终解决方案构建 | 通过；本轮没有重新执行解决方案全量测试 |
| L5 | CSV 同环境前后对照 | 见下表；csv-before.json / csv-after.json |

三个受影响项目完整测试共 **9322 项通过，0 失败、0 跳过**。定向用例与完整项目存在重叠，不把各层数字累加为独立用例总数。

完成全测后只加强 CSV 取消测试触发时序：每次读取 1 字节，在数据行末换行读取时取消，确保旧实现会错误交付该行；生产实现未改变，随后在两个框架精准复验。独立中文 XML 注释更新导致一次旧测试目录产物与重建输出身份不一致；已刷新对应产物并完成最终身份校验，未把旧身份记录当作最终证据。全测和性能证据按未变化的行为范围复用，不因注释和文档重跑。

测试按主包、HTTP、Net 归属放置。新增选项、可选接口及命名/null/default 调用已纳入消费者编译。实际 FTP 协议服务与外部 Windows/Linux/macOS CI 未运行；本地 FTP 目录行为通过 fake backend 验证。

## 性能实测

基线在修改前冻结当前工作区 CSV 源码及实际 net8 DLL，保存在 baseline/manifest.json 对应文件中，没有用旧 HEAD 代替工作区。候选源码、DLL 与探针身份保存在 performance-candidate.json。探针为 benchmarks/CsvAllocationProbe；固定 x 字符输入及换行，记录数据标识 20261025，不使用随机输入。

前后分别在独立 .NET 8.0.31 进程运行；每个负载预热一次、正式测量一次，使用 GC.GetTotalAllocatedBytes(true) 统计全进程线程分配。输入字节数组预先生成，测量含读取、解析及结果物化，不含输入生成。正式确认期间没有并发运行本任务构建或测试。单次耗时用于本机对照，不建立百分比门禁。

| 输入场景 | 优化前分配 B | 优化后分配 B | 优化前 ms | 优化后 ms |
|---|---:|---:|---:|---:|
| 1 KiB 单字段（含换行 1025 B） | 168128 | 20240 | 3.5405 | 1.7640 |
| 1 MiB 单字段（含换行 1048577 B） | 156248272 | 5253184 | 143.9480 | 17.6342 |
| 1024 行，每行 1024 字符 | 157189336 | 6046936 | 150.6456 | 41.6951 |

1 MiB 场景分配约 **149 MiB → 5 MiB**。改动消除重复逐字节任务分配，仍保留每行字段解码和结果对象成本。新增 CSV/FTP 预算、SSE 空闲超时没有历史等价入口，BEFORE_STATUS = NOT_APPLICABLE；不伪造性能对照。其余下载、图像、反射路径未变，本轮不重测。

## 审查与当前状态

luna_worker 对主代理的 Batch 到期优先与取消收尾、SSE 超时/重连/释放及令牌优先、FTP 累计预算与传输前检查、CI 身份校验进行独立只读审查，未发现确认的 OPEN_ACTIONABLE。主代理已复核委派的 CSV 限额计算、ValueTask 快路径、流关闭及取消传播；取消用例强化后两个框架均通过。审查没有扩展到本轮未变的历史模块。

| 状态分类 | 当前结论 |
|---|---|
| Completed | H01—H07 本地实现、文档、测试、性能对照及独立审查 |
| Open Actionable | 无，OPEN_ACTIONABLE = 0 |
| Blocked Approval | 无 |
| Blocked External | 外部跨平台 CI、真实 FTP 服务验证；既有依赖发布风险沿用原状态，本轮未重新审计 |
| Not Applicable | 新预算与超时入口的历史等价性能对照 |
| Accepted Limitations | FTP 声明大小预检不等于实际写入/网络硬上限；目录传输不回滚；SSE 协作取消 |
| Verified Boundaries | CSV 精确限额/零值/超限；FTP 深度与未知大小；SSE 超时及外部取消 |
| Deferred | 下载队列持久化；FTP 校验续传与原子下载；CSV 空值/坏行策略；SSE 请求配置/退避；数据库反向多结果集；CharGuard |

Task: stream-hardening-2026-10-25；Round: 1。Implementation: 7/7（本地 100%）。Release: 未执行发布验收；外部检查与既有依赖风险单独跟踪，不等同于实现未完成。No-Progress Check: CHANGED。Next Action: STOP。Goal Status: STOPPED_BLOCKED（本地交付完成，外部验证待运行）。

## FTP 回归覆盖补齐（2026-10-25）

后续检查发现 H05 的两个测试覆盖缺口，本次按用户“继续修复问题”的指令补齐。只修改 Net 模块测试、netstandard 脚本的测试筛选及本记录；不涉及生产行为、公开 API、依赖、TFM 或性能热路径。

| Finding | 验收与结果 | 状态 |
|---|---|---|
| H05-T01：累计大小预算缺少多文件回归 | 上传、下载各验证根目录与子目录两个 3 字节文件；6 字节预算全部完成，5 字节预算拒绝第二个文件，并断言后端调用记录及已完成文件状态。luna_worker 独立复核通过 | CLOSED |
| H05-T02：旧客户端缺少明确拒绝回归 | 仅实现基础接口、实现旧目录接口但没有新预算能力的客户端，上传/下载均抛 NotSupportedException，且没有基础/旧目录调用。主代理独立复核委派实现通过 | CLOSED |

测试主要断言目标是 Bing.Utils.Net，依 test-project-boundary 及其 refs/test-project-boundary.md 放在 Bing.Utils.Net.Tests；没有测试迁移、混合归属或新增依赖。风险 LOW；共享 fake 仅增加传输调用记录。实现前后未发现需要修改生产代码的缺陷。

| 验证 | 结果 |
|---|---|
| net8 新增用例定向 | 6 通过，0 失败 |
| net8 Net 项目完整测试 | 87 通过，0 失败、0 跳过 |
| netcoreapp3.1 FTP 目录和兼容回归 | 17 通过，0 失败；加载的 Net DLL 与 netstandard2.0 输出 SHA-256 一致 |
| 独立审查 | 累计预算与兼容拒绝用例分别完成交叉复核 |
| 本地静态检查 | PowerShell 语法、所改文件 UTF-8 和空白检查通过 |

证据：artifacts/ftp-budget-regression-2026-10-25/ 下 targeted-net8.trx、net-full.trx、netstandard.trx、netstandard-identity.json 及对应日志。验证脚本已将 FtpDirectoryLimitsCompatibilityTest 加入 Net 的筛选条件。当前 87 项完整测试包含新增 6 项，不与定向数字累加。

原 H01—H07 的生产实现、性能证据及其他模块测试按未变化范围复用；以上 87 项更新本轮 Net 测试证据。没有重跑全解决方案测试或性能探针，也未覆盖/更新旧性能基线。

Completed：2/2 覆盖缺口；Open Actionable：0；Blocked Approval：无；Blocked External：外部 CI、真实 FTP/FTPS 服务与既有发布风险保持原状态；Not Applicable：本次无生产性能变化，不需要新的性能对照；Accepted Limitations：FTP 声明大小预算与部分结果契约不变；Verified Boundaries：跨目录累计限额、精确边界和旧能力拒绝；Deferred：上表既有后置扩展能力保持后置。

Task: ftp-budget-regression-2026-10-25；Round: 1；Implementation: 2/2；Local Validation: PASS；Release: 未执行发布验收；No-Progress Check: CHANGED；Next Action: STOP；Goal Status: STOPPED_BLOCKED（本地完成，外部验证独立跟踪）。未暂存、提交、推送或发布。
