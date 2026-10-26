# Bing.Utils 整体审查与分阶段修复记录

审查基线：`9258eab`。实施日期：2026-10-22。首轮采用用户确认的兼容优先计划，没有删除公开 API、调整目标框架或改变公开构造方式。随后用户授权补齐 F06、CI 环境并推进正则超时与解压限额；该阶段新增公开限制入口，最新契约及验证见 [补齐与资源限制记录](resource-limits-2026-10-22.md)。

## 实施顺序与影响分析

1. 第一批处理 F01/F02/F03/F04/F10：ZIP 路径、池化流、文件及流的数据完整性，并固定 netstandard2.0 依赖链的 System.Text.Encodings.Web 4.7.2。
2. 第二批处理 F05/F06/F07/F08/F09：大内存流、命令执行、集合、正则缓存与随机串。
3. 第三批统一发布 SDK、添加独立定时漏洞审计、修正文档，记录未解决风险与证据。

影响项目为 Bing.Utils、Collections、IdUtils；主包的项目引用消费者及 NuGet 消费者需要联动验证。公开签名和 TFM 不变，但流生命周期、外部进程和 ZIP 文件写入属于高风险行为，因此先做定向回归，再做完整模块与解决方案验证。只有 ZIP 链接检查的独立审查 finding 驱动了一次追加修复；没有因已知 ImageSharp 门禁失败开始无关升级或性能优化。

## 当前问题清单

| 编号 | 触发条件 | 实施结果 | 回归证据 |
| --- | --- | --- | --- |
| F01 | ZIP 条目为绝对路径、`../`、相似目录前缀或落入已有 junction/符号链接 | 解压前预检全部条目，规范化路径、验证目录边界、拒绝已有重解析点；netstandard 覆盖分支同样校验，支持正常嵌套及空目录 | `ZipReviewRegressionTest`；相同测试加载真实 netstandard2.0 DLL |
| F02 | 脏缓冲池、非末尾位置扩容、截断后增长、定位空洞、释放后使用 ToArray 快照 | 按逻辑长度复制，新增逻辑区域清零，数组快照独立，补充借用视图生命周期说明 | `MemoryStreamReviewRegressionTest` 的 PooledMemoryStream 用例 |
| F03 | Read 每次仅返回 1–2 字节或不支持 Length/Seek | 循环读取至 EOF；可定位流从头读取并在成功后复位，不关闭输入流 | `StreamReviewRegressionTest` 的读取及取消用例 |
| F04 | 长文件覆盖为短文件/空文件；两个流采用不同分块方式 | 覆盖时截断原文件；从各自当前位置按字节序列比较，支持不可定位流 | `StreamReviewRegressionTest` 的 ToFile / ContentsEqual 用例 |
| F05 | 尾部负偏移、清空、缩容后重新增长、超过 1024 页 | 修复定位、池归还与分配状态、引用页表复制、新增逻辑区清零；内部注入小页和池 | `MemoryStreamReviewRegressionTest` 的 LargeMemoryStream 用例，页大小 8 字节 |
| F06 | 无输出正常退出、未匹配就退出、超时、关闭重定向、复用执行器，以及退出后后代仍持有管道 | 每次执行独立状态；等待直接进程退出，尾部输出最多等待 500 毫秒；匹配立即返回；提前退出未匹配及时失败；异常路径释放本次进程 | `CommandLineExecutorRegressionTest`，含 PID 终止和继承管道验证；新增场景在修复前失败 |
| F07 | 值类型未命中、默认值/null 命中、删除接近集合末尾 | 查找结果不再依赖默认值；删除数使用剩余长度上限 | `CollsOpsTest` |
| F08 | en-US 缓存忽略大小写正则后切换 tr-TR | 文化信息加入结构化缓存键；CultureInvariant 统一，获取/存在/删除一致 | `RegexPoolTest` 文化切换用例 |
| F09 | 原随机索引排除末位 `9`；force 模式仍使用有限种子 | 所有入口使用密码学随机源及拒绝采样，覆盖完整 62 字符；保留最小长度 16 和兼容参数，不承诺唯一 | `RandomNonceStrGeneratorBoundaryContractTest` 的完整映射、拒绝区间与均匀映射用例 |
| F10 | 压缩/解压输入为不可定位流 | 仅在可定位时访问 Length，同步和异步均支持短读；保留原有起始位置及关闭约定 | `StreamReviewRegressionTest` 的压缩/解压及取消用例 |
| DEP01 | netstandard2.0 主包及传递消费者解析 Encodings.Web 4.6.0 | 直接声明最低修复版本 4.7.2，并验证本地 nupkg 和消费者 assets；仅删除审计确认消失的对应记录 | 本地包、consumer/obj/project.assets.json、dependency-evidence.json |
| CI01 | 发布使用浮动 8.0.x，但 global.json 禁止 roll forward | 发布和 DocFX 的 .NET 8 SDK 改为 8.0.425；定时任务独立 restore + audit | workflow 静态检查；远程运行未验证 |
| CI02 | 普通 CI 未安装旧运行时却执行全部 TFM 测试 | Windows 显式安装全部需要的运行时并执行全 TFM；Linux/macOS 执行 net8；测试前检查 SDK 和运行时 | Test-CiDotnetRuntimes.ps1；本机全部所需运行时检查通过，远程未验证 |
| SAFE01 | 不可信正则匹配缺少显式超时入口 | 新增显式超时重载，缓存键区分超时，旧入口保持默认值 | RegexPoolTimeoutTest，覆盖真实回溯超时与惰性匹配集合 |
| SAFE02 | 解压缺少输出字节预算 | 新增 GZip 同步/异步限额及 ZIP 累计限额；实际计数、ZIP 当前条目临时写入与清理 | CompressionLimitTest，含精确边界、短读、取消、伪造声明长度 |

测试位置遵循模块归属：主包测试在 Bing.Utils.Tests，集合测试在 Bing.Utils.Collections.Tests，随机串测试在 Bing.Utils.IdUtils.Tests，没有迁移无关测试。

## 验证记录

本地环境为 Windows、SDK 8.0.425。下表保留首轮结果；后续阶段的最新结果见 [补齐与资源限制记录](resource-limits-2026-10-22.md)。

| 验证项 | 结果 |
| --- | --- |
| Release 解决方案构建（包含主包全部四个 TFM） | 通过，0 错误；仍有现存编译/平台/文档警告 |
| net8.0 解决方案测试 | 12,684 通过，0 失败；18 项网络集成用例未执行 |
| 受影响项目完整测试 | 主包 7,039；Collections 289；IdUtils 322，全部通过 |
| 最终 ZIP 定向回归 | net8.0 9 项通过，真实 netstandard2.0 9 项通过 |
| 命令执行定向回归（含既有用例） | 14 项通过 |
| 本地四个 nupkg 与 netstandard 消费者还原 | 主包、Text、IdUtils、Collections 包成功生成；消费者解析 Encodings.Web 4.7.2 |
| NuGet 漏洞门禁 | 失败：ImageSharp 三条新增 High 公告，保留发布阻断 |
| Windows/Linux/macOS GitHub Actions | NOT_VERIFIED，未提交/推送触发远程运行 |

Encodings.Web 对应的 19 条旧版本基线记录已删除，其他 12 条历史记录保留。漏洞门禁已执行并确认新增阻断仅为 ImageSharp；随后补充导出 JSON 的命令长时间无输出，停止该导出。依赖关闭证据采用已完成的门禁结果、生产项目还原清单及本地包消费者解析，未将未完成的导出视为成功。

原始日志、TRX、基线 DLL、隔离探针与本地包保存于仓库忽略目录 `artifacts/project-review-2026-10-22/`，不作为发布产物。

可复用命令：

```powershell
dotnet build Bing.Utils.sln -c Release
dotnet test Bing.Utils.sln -c Release -f net8.0 --no-build
pwsh ./build/Test-ReviewNetStandard.ps1 -NoBuild
pwsh ./build/Test-NuGetAudit.ps1
```

单独运行 netstandard ZIP 回归时省略 `-NoBuild`，脚本会先构建目标库与测试项目。测试使用独立 AssemblyLoadContext，并断言加载的 DLL 的目标框架确为 .NETStandard 2.0，不以 net8 分支替代。

旧版本对照：保存的 9258eab 主包 DLL 重跑关键新用例，核心 25 项中 23 项按预期失败，ZIP 8 项中 7 项按预期失败。对 F05/F07/F09 另取基线源码运行小型对照探针：LargeMemoryStream 只注入小页/池接缝及修正无关 nameof 编译依赖，不改变旧算法；八项探针在旧源码失败、在修复源码通过。随机串的公开随机样本只作补充观察，正式单测用确定性随机源验证完整映射与拒绝采样，不依赖随机唯一性断言。

## 未关闭风险与后续专项

- **DEP02 / DEFERRED_RELEASE_BLOCKER：ImageSharp 2.1.11 的三条新增 High 公告仍未解决，审计失败保持可见。** 本轮不扩充白名单、不升级框架。公告：GHSA-j9gm-c75j-xc9q、GHSA-jjfr-hcj7-qf5w、GHSA-j3p4-wp97-rph4；升级另按用户确认的迁移专项处理。
- **SAFE01 / SAFE02：显式正则超时和解压输出预算已实现。** 旧入口保持原默认行为，调用方必须使用新入口或重载启用限制；详细边界见补齐记录。
- **FOLLOWUP：LargeMemoryStream 默认约 1 GB 页的资源成本及原始页面视图。** GetSpan/GetMemory/ToArraySegment 是借用底层容量的高级接口，调用方必须按 Length 限定逻辑数据，不能将整页容量序列化对外发送；本轮保证新增逻辑内容清零，不额外执行 GB 级整页初始化。缩容、扩容或释放后不可继续使用失效视图，安全视图设计另行评估。
- **使用边界：ZIP 目标目录应由调用方控制。** 本轮拒绝检查时已经存在的链接，但不能用可移植的路径预检抵御其他进程在检查与写入之间替换目录；需要对抗并发目录替换的场景应使用独占的新目录或平台级句柄约束。
- **兼容边界：命令参数按调用方提供的命令行语法传递，不再统一额外转义双引号。** 超时在 .NET 6/7/8 终止进程树，在 netstandard2.0 受可用 API 限制终止直接创建的进程；成功返回的 Process 由调用方释放，输出匹配模式可返回仍运行的进程。
- Windows 本地验证不等同于 Linux/macOS 和 GitHub Actions 验证。跨平台 ZIP/命令回归已进入 CI；外部运行结果仍为 NOT_VERIFIED。
- FTP 未接入配置、旧 TFM 支持策略、其他历史依赖债务及图像/反射性能门禁维持独立专项，历史已关闭问题不重新打开。

首轮关闭的 F06 曾因继承管道场景重新打开，现已补齐。F01–F05、F07–F10 继续复用未变更范围的证据；F06、CI02、SAFE01、SAFE02 以补齐记录的最终验收为准。发布验收仍 BLOCKED（ImageSharp），远程 CI 未验证；不会仅因发布门禁失败进入无关修复。
