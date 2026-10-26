# F06 补齐与显式资源限制

本阶段接续整体审查，范围为用户新授权的 F06、CI 运行环境、正则超时和解压输出限额。保留旧接口及其默认行为，新增公开入口；不修改目标框架、依赖版本或漏洞白名单。基线为 `9258eab` 上的上一阶段未提交修复。

## 实施计划与影响分析

1. F06：回归父进程退出但后代持有输出管道的场景，将进程退出与输出 EOF 分离，有限等待尾部输出，明确资源所有权。
2. CI02：明确 Windows 全 TFM 与 Linux/macOS net8 验证矩阵；安装需要的旧运行时并在测试前检查，SDK 保持 8.0.425。
3. SAFE01：RegexPool 新增显式超时重载，缓存键包含超时，获取、存在、移除使用同一规则，覆盖缓存与非缓存操作。
4. SAFE02：新增 GZip 同步/异步输出限额及 ZIP 累计输出限额，实际计数、取消和失败清理都有回归。

ChangedFiles：CommandLineExecutor.cs、RegexPool.cs / RegexPool.Timeout.cs、Compression.Limits.cs、相关主包测试、dotnet.yml、Test-CiDotnetRuntimes.ps1 与质量文档。
ChangedProjects：Bing.Utils、Bing.Utils.Tests。
ChangedPublicContracts：仅新增显式超时重载及 WithLimit 方法；已有签名不变。
ChangedRuntimePaths：外部进程事件、正则缓存和匹配、GZip/ZIP 解压。
ChangedProviders：无；ChangedTFMs：无；ChangedBuildPackaging：仅 CI 测试运行环境；ChangedBenchmarkHarness：无；ChangedDocsOnly：否。
AffectedDependents：主包引用消费者及解决方案。RiskLevel：HIGH（进程生命周期与文件写入）。

测试遵循 test-project-boundary：主要断言目标均属于主包，放在 Bing.Utils.Tests；没有测试迁移。执行顺序为 L1 定向回归，再在最终检查点构建解决方案并运行主包及其他项目测试；不重跑未变更的依赖审计或性能基准。

## API 用法与契约

```csharp
using System.Text.RegularExpressions;
using Bing.Text.RegularExpressions;
using Bing.Helpers;

bool matched = RegexPool.IsMatch(input, pattern, RegexOptions.None,
    TimeSpan.FromMilliseconds(100));

byte[] bytes = Compression.DecompressWithLimit(compressedBytes, 10 * 1024 * 1024);
byte[] asyncBytes = await Compression.DecompressWithLimitAsync(compressedStream,
    10 * 1024 * 1024, cancellationToken);
Compression.UnZipWithLimit(zipPath, destination, 100L * 1024 * 1024,
    overwriteFiles: false);
```

- **F06**：未设置输出条件时等待直接启动的进程退出。退出后最多接收 500 毫秒尾部输出，停止事件读取；未匹配时抛出 InvalidOperationException，不因后代持有管道而误报超时。成功返回的 Process（包括重定向读取资源）由调用方 Dispose；匹配模式可以返回仍运行的进程。已退出父进程留下的后台服务由调用方管理，不尝试追踪已失效的父子关系。直接进程仍运行而发生超时时，net6+ 终止进程树，netstandard2.0 终止直接进程。
- **正则**：GetOrCreate、IsMatch、Match、Matches、Replace、Split、GetValue、GetValues 增加显式 matchTimeout 重载，Contains / Remove 同步增加。超时向上传递 RegexMatchTimeoutException；Matches 的惰性枚举也会触发。超时不包含构造时间，也不是整个匹配集合的总时长预算。旧入口保留运行时默认超时，通常无限；显式超时与旧入口分别缓存。
- **GZip**：DecompressWithLimit / DecompressWithLimitAsync 支持 byte[]、Base64 字符串、Stream。非负限额包含边界，零仅允许空输出，按解压后的字节数而非字符数计数。超过限额抛出 InvalidDataException，不返回部分结果。可定位空流及开始前的参数/取消失败不关闭输入；开始解压后无论成功失败均关闭输入。支持短读和不可定位流。
- **ZIP**：UnZipWithLimit 按整个归档累计输出。写入前检查全部路径及声明长度，写入过程中按实际字节数再次计数，不依赖不可信声明长度。每个文件先写同目录临时文件，成功后才覆盖目标；超限失败不会覆盖当前条目的旧文件，并清理临时文件。此前完成的文件和已建目录不回滚。
- **限额边界**：输出字节预算不是总进程内存、CPU、压缩输入大小或 ZIP 条目数的预算。数组/文本结果的复制与解码有额外内存开销。ZIP 目标目录仍须由调用方控制，避免并发目录替换。
- **CI 矩阵**：Windows 安装 3.1.426、5.0.408、6.0.428、7.0.410 与 8.0.425 SDK 并执行全部既有 TFM。Linux/macOS 构建全部 TFM，运行 net8 测试及真实 netstandard2.0 ZIP 回归；旧 TFM 的跨平台运行不在当前矩阵内。Nightly 安装 6.0.428 和 8.0.425，执行 net6/net8。测试前检查所选 SDK 和 NETCore/App + AspNetCore/App 运行时。

## 验证与当前状态

证据目录：`artifacts/resource-limits-2026-10-22/`（仓库忽略）。

- F06 修复前：`f06-before.trx`，两项继承管道用例失败，普通尾部匹配通过。
- 新 API 在旧版本不存在，before 对照为 NOT_APPLICABLE；使用限额精确边界、实际回溯超时、伪造 ZIP 声明长度和公开接口调用验证。
- L0：Release 解决方案构建通过，0 错误，1718 条现存编译/测试分析警告。PowerShell 语法、YAML 解析、git diff --check 通过；本机运行时检查覆盖全部五个测试 TFM，所选 SDK 为 8.0.425。
- L1：`targeted-net8.trx`，261 项通过；包括已有正则缓存及命令测试和新增限额用例。
- L2 / L4：`solution-tests/` 的 23 个测试程序集共 12,710 项通过、0 失败、18 项 FTP 集成测试跳过。主包完整测试 7,065 项通过；汇总见 `test-summary.json`。本轮新增 26 个测试案例。
- L3：`targeted-netcore31.trx`，.NET Core 3.1 运行 44 项全部通过，项目引用实际解析到 netstandard2.0 主包；覆盖新接口、F06 及 ZIP 路径回归。最终公开接口也由解决方案测试消费者编译和调用。
- L5：不运行，无性能优化或容量基准任务。限额测试使用小型受控数据验证实际输出拒绝，不声称总内存或 CPU 上限。
- 独立小范围检查确认预算累计与边界算法；发现的输入流关闭时机、ZIP 覆盖异常说明已修正。所有最终代码和测试变更完成后执行了解决方案测试；收尾仅更新文档与汇总。

## 阶段验收

| 项目 | 实现状态 | 验收依据 |
| --- | --- | --- |
| F06 | CLOSED | 两项原继承管道失败案例已通过；尾部匹配、超时清理、复用和重定向回归通过 |
| CI02 | CLOSED | 安装和执行矩阵明确，运行环境检查脚本在本机通过；远程执行单列 |
| SAFE01 | CLOSED | 缓存与非缓存的七种匹配操作均实际触发超时，时间/文化键与旧入口兼容通过 |
| SAFE02 | CLOSED | GZip 精确上限、零上限、短读、取消，以及 ZIP 累计实际输出与失败清理通过 |

Task：resource-limits-2026-10-22；Implementation：4/4；本地验收：4/4（构建、net8 测试、旧运行时测试、CI 静态及运行时检查）。

- Completed：F06、CI02、SAFE01、SAFE02。
- Open Actionable：none。
- Blocked Approval：none，本轮用户已明确授权推进新接口。
- Blocked External：Windows/Linux/macOS GitHub Actions 尚未执行；18 项 FTP 集成测试缺连接配置，属于既有外部边界。
- Not Applicable：新超时/限额接口的旧版本同构 before 比较。
- Accepted Limitations：进程退出后输出接收窗口、后台后代所有权、ZIP 不回滚此前成功条目、字节限额不等于总资源预算、旧 TFM 仅在 Windows 执行。
- Verified Boundaries：精确字节预算可成功，超过预算被拒绝；正则回溯到达显式超时后抛出异常。
- Deferred：ImageSharp 迁移及其他历史依赖、默认大页资源优化、FTP 配置接入、框架支持策略、图像/反射性能专项。
- Reused Evidence：未变更的 F01–F05、F07–F10 旧版本对照和依赖链修复证据；最新全套 net8 测试覆盖其当前回归。
- No-Progress Check：CHANGED。Implementation / Test Status：PASS。External Gate：BLOCKED_EXTERNAL。Release：BLOCKED（既有 ImageSharp）。Goal Status：STOPPED_BLOCKED。Next Action：STOP，无本地可执行 finding，不因外部门禁继续重复修复。

已知 ImageSharp 发布阻碍及其他延期专项仍按整体审查记录跟踪，本阶段不改变其状态。远程 GitHub Actions 尚未触发，本地结果不替代远程平台证据。
