# 工具能力扩展执行记录

## 当前结果

- Implementation：**26/26 已实现并完成本地验收**。
- OPEN_ACTIONABLE：**0**。独立审查及本地回归发现的可执行问题已关闭。
- Local validation：PASS；性能数据已交付，耗时不是所有场景都改善。
- Release：BLOCKED，既有 ImageSharp 高危依赖及外部环境验证尚未完成。
- Goal status：STOPPED_BLOCKED；Next Action：STOP，不因外部门禁失败继续修改无关代码。
- 工作区保留全部此前未提交修复；未提交、推送、发布，未修改生产包版本、目标框架或升级生产依赖。

计划见 [冻结计划](capability-expansion-plan.md)，调用示例见 [能力指南](../reference/capability-expansion-guide.md)，性能原始结果与解释见 [性能实测](capability-expansion-performance.md)，FTP 行为见 [配置矩阵](../reference/ftp-capabilities.md)。

## 逐项实现和验证

以下 PASS 表示代码与本地合同测试通过，不代替真实服务器或其他操作系统的验证。

| ID | 实现 | 验证证据 | 状态 |
|---|---|---|---|
| A01 | SaveFile 重载递归修复 | 独立进程验证所有保存重载，避免旧实现栈溢出终止测试宿主 | PASS |
| A02 | 同步字节写入只打开一个句柄并截断 | 新建、覆盖、追加、空内容 | PASS |
| A03 | JSON 文件覆盖截断 | 长 JSON 改短后仍完整解析 | PASS |
| A04 | FTPS 默认严格证书校验和显式策略 | 正常/错误证书策略、自定义拒绝及策略异常 | PASS |
| A05 | PowerShell 双管道持续消费 | 大量标准输出/错误输出不因未读取阻塞 | PASS |
| A06 | DataTable/DataSet v1 双向 JSON | 白名单类型、精度、DBNull、结构/关系/约束、旧 schema 读取、预算和失败 | PASS |
| B01 | 同目录临时文件原子提交 | 限额/校验/取消失败保留旧文件并清理临时文件 | PASS |
| B02 | StreamTransfer 有界传输与摘要 | 短读、不可定位流、非零起点、超限、SHA-256、取消、资源归属 | PASS |
| B03 | 严格文件读取独立入口 | 文件不存在、限额、取消和正常读取 | PASS |
| B04 | ZIP 条目/深度/单文件/总量预算 | 预检、实际读取限额、取消和旧目标保护 | PASS |
| B05 | Regexs/ZipHelper 安全便捷入口 | 超时/预算传递及正常调用 | PASS |
| B06 | LargeMemoryStream 有效页段视图 | 跨页和 Length 边界，保持 Position | PASS |
| B07 | 流写文件移除完整中间数组 | 短读、非定位流、原起始和 EOF 完成位置、取消、保持源打开 | PASS |
| C01 | HTTP 响应直接写分片临时文件 | 读取期间临时文件已存在、限额、提交、资源版本一致性 | PASS |
| C02 | 分片调度固定工作任务 | 并发上限、致命回调停止领取、重试/取消收尾；性能分配对照 | PASS |
| C03 | 最终下载逻辑总量预算 | 已知/未知长度、普通正文隔离、恢复字节计入、拒绝后检查点可继续使用 | PASS |
| C04 | 异步序列并发遍历/转换 | 有界窗口、顺序、空序列、枚举失败、取消、提前退出和收尾 | PASS |
| C05 | JSON 数组流式写入及响应适配 | 增量、刷新、取消、选项和流生命周期 | PASS |
| C06 | NDJSON 读写及响应适配 | UTF-8/BOM、CRLF/LF、记录预算、错误行号、提前退出 | PASS |
| C07 | 内部默认 JSON 选项缓存 | 原序列化回归、同路径性能对照 | PASS |
| D01 | 结构化异步命令执行器 | 参数、非零退出码、截断仍消费、超时/取消、后代持管道收尾 | PASS |
| D02 | 原生异步 FTP 和显式配置 | 假后端验证取消/串行化/传流/配置映射，旧接口保持 | PASS；真实服务待验证 |
| E01 | IClock、显式时区换算 | 固定时钟、夏令时无效/歧义时间、UTC 先后顺序 | PASS |
| E02 | 工作日历 | 周休息、假期、补班优先、正负偏移、半开计数 | PASS |
| E03 | 自定义字符集安全 ID | 字符集合、拒绝采样、随机块边界、精确长度 | PASS |
| E04 | HTTP Activity/Meter/回调适配 | 真实请求链路、有限标签、监听器异常隔离、旧框架回调 | PASS |

## 验证结果与证据复用

原始证据目录：`artifacts/capability-expansion-2026-10-23`。

| 检查 | 结果 | 证据 |
|---|---|---|
| 最终解决方案 Release 构建 | PASS，0 errors，426 warnings | `solution-rc-build.log` |
| net8 全解决方案测试 | 12,922 通过，18 外部服务测试跳过，0 失败 | `final-suite.log`、`final-suite/*.trx` |
| 最后调度优化后 HTTP 单元/集成 | net8：1,894 / 120 通过；Core 3.1：1,891 / 120 通过 | `Bing.Utils.Http.Tests-*-rc.trx`、`Bing.Utils.Http.Tests.Integration-*-rc.trx` |
| 主包、Net、DateTime、IdUtils Core 3.1 定向 | 68 / 27 / 16 / 18 通过，实际引用 netstandard2.0 | `core31.trx`、`net31.trx`、`date31.trx`、`id31.trx` |
| 动态加载 netstandard2.0 产物 | 13 通过；包含 ZIP、传输、表格、异步序列和 JSON 数组 | `netstandard-loader-confirm.log` |
| 四框架消费者编译 | net8/net7/net6/netstandard2.0，0 warnings / 0 errors | `consumer-final-build.log` |
| 前后性能 | 正式对照完成；同时记录分配收益和耗时回退 | `performance-before.json`、`performance-after-confirm.json` |
| NuGet 审计 | FAIL，仍为 ImageSharp 2.1.11 三条高危公告 | `nuget-audit.log` |
| 静态文本检查 | `git diff --check` 无错误；本轮文本 UTF-8 可解码 | 本地检查 |

全解决方案测试之后仅调整 HTTP 私有调度路径以移除实测重复分配，已重新构建解决方案并完整复测 HTTP 两个项目的 net8/Core 3.1；其他模块的运行时代码未变化，复用全量证据。主包 Core 3.1 定向之后仅补充 FileHelper XML 契约。最终源码、DLL、探针及有效证据的身份记录于 `candidate-manifest.json`，不把不同实现的结果混为一次全量运行。

构建仍有历史过时 API、平台兼容、XML cref 和测试分析器警告；不同增量构建的警告数量不作为新增问题数量。生产依赖与漏洞基线相对本轮冻结点未改变。测试宿主仅显式引用主包既有的 Microsoft.Bcl.AsyncInterfaces 1.0.0，以加载 netstandard 的异步接口转发程序集；探针使用既有 AspectCore 依赖。

## 审查与修复收口

主代理负责 API、格式、调度、流生命周期和集成；`luna_worker` 承担文件、命令、FTP、时间和随机 ID 等有明确所有权的子项，并只读审查主代理实现。主代理复核委派结果；最后一次调度修复另经窄范围独立审查，未发现明确新回归。

| 问题 | 处理结果 | 状态 |
|---|---|---|
| 旧表格 decimal 先经 double 解析可能丢精度 | 按 schema 保留数字原文，混合 decimal/double/date 用例通过 | CLOSED |
| ActivityStopped 监听器异常可能改变 HTTP 结果 | span 收尾和错误回调隔离，监听器抛错回归通过 | CLOSED |
| 下载总量预算拒绝后破坏原恢复检查点 | 预算通过后才接管新状态；拒绝后提高预算可继续原断点 | CLOSED |
| 同长度远端内容更新导致旧新片混合 | 自动入口绑定 URI/头指纹/编码/强 ETag 或 Last-Modified，逐片 If-Range 和响应校验 | CLOSED |
| 新流委托适配增加每片异步/异常分配 | 合并调度核心，移除逐片适配和闭包；确认采样分配低于基线 | CLOSED |

首次集成发现的构建/测试宿主问题（日历命名空间冲突、构建工程源码 glob、旧 TFM IsExternalInit、诊断用例位置及缺失 using、动态加载转发程序集）均已修正。失败日志保留，最终有效证据由 manifest 显式列出。

## 当前 TODO 分类

- Completed：26 项实现、中文合同/示例、定向与模块测试、跨框架、消费者编译、实测数据和独立审查。
- Open Actionable：无。
- Blocked Approval：无；新增能力和双向表格格式已获用户授权。
- Blocked External：Windows/Linux/macOS CI 尚未在远端运行；真实 FTP/FTPS/代理服务尚未验证。
- Not Applicable：新接口不存在历史等价性能入口；未设耗时百分比门禁。
- Accepted Limitations：默认表格 v1 和无参转换器保留历史快照/Rows-Tables 格式，不恢复行编辑历史及自增内部计数；历史 v1 转换器及默认自动识别读取仍缓冲完整 JSON。显式 v2 与 `OpenReaderAsync` 的读取/转写能力见[表格 v2 流式与编辑状态恢复指南](../reference/capability-refinement-guide.md)：逐行读取器可按需交付记录，而返回完整 `DataTable`/`DataSet` 的入口仍需在内存中保存完整结果。ZIP 不具备归档事务；流式失败允许部分输出；netstandard 命令只能保证终止直接进程；无服务端验证器无法识别同长度资源变化，Last-Modified 受服务器秒级精度限制；异常密集调度的耗时仍高于基线，内存收益不等于整体吞吐提高。
- Verified Boundaries：限额、无效结构/类型、错误证书、取消及旧文件保护按合同拒绝；不把这些预期失败记为能力缺失。
- Deferred：ImageSharp 兼容迁移与历史图像/反射门禁，未用扩大白名单改变发布结论。
- No-Progress Check：CHANGED，已完成可执行项；不再进入修复循环。

本地实现与发布验收分开：现有三条 ImageSharp 公告继续阻塞依赖门禁，外部 CI/真实服务结果保持未验证。
