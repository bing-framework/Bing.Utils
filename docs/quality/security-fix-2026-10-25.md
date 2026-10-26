# ZIP 安全修复与 FTP 信任边界补充

日期：2026-10-25。Task：security-fix-2026-10-25；Round：1。
本次承接上一轮安全检查中两个本地可执行项；保留现有工作区成果，没有升级依赖、调整版本或公开签名。

## 变更影响

- ChangedFiles：ZipHelper.cs、Compression.cs、ZipReviewRegressionTest.cs；FTP 指南及两个目录接口的 XML 注释。
- ChangedProjects：Bing.Utils、Bing.Utils.Tests；Bing.Utils.Net 仅注释。
- ChangedPublicContracts：签名不变；旧 ZIP 入口在副作用前拒绝不安全路径，并说明 InvalidDataException。原有参数异常、格式错误传播和分框架覆盖规则保留。
- ChangedRuntimePaths：旧 ZipHelper.UnZip 接入现有 ValidateArchivePaths；共享校验实现不变，仅改为 internal 供同程序集调用。
- ChangedProviders：无。ChangedTFMs：既有 net8/net7/net6/netstandard2.0，未增删。
- ChangedBuildPackaging / ChangedBenchmarkHarness：无。
- AffectedDependents：主包 ZIP 调用方；FTP 运行时不变。
- RiskLevel：MEDIUM（文件系统安全边界）。先定向，再主包全测和实际 netstandard 验证；无需全解决方案或性能矩阵。

## 修复与验收

| Finding | 修改及验收 | 状态 |
|---|---|---|
| SEC-ZIP-01：旧 ZipHelper.UnZip 缺少链接路径保护 | 创建目标目录前对全部条目共享路径预检，拒绝越界、绝对路径和既有重解析点。覆盖根/子目录 junction、新建/覆盖外部文件、正常嵌套及各框架覆盖行为 | 独立复核通过，CLOSED |
| SEC-FTP-DOC-01：目录可信前提未说明 | 上传源根、下载目标根及相关祖先由可信调用方控制，操作期间保持稳定；明确现有检查不提供抵御恶意本地并发替换的句柄级隔离 | 主代理复核通过，CLOSED |

测试依据 test-project-boundary 技能及 refs/test-project-boundary.md，主要断言对象属于主包，扩展现有 Bing.Utils.Tests 的 ZipReviewRegressionTest。没有迁移测试、混合归属或新增依赖。

先在未修复生产代码上运行扩展后的 25 项 ZIP 回归：14 通过、11 失败，其中 4 项旧入口 junction 用例失败。修复后全部通过；失败证据绑定修改前工作区，不以旧 HEAD 代替。

netstandard2.0 旧 ZipHelper.UnZip 仍拒绝覆盖已有文件；net6/net7/net8 保持允许覆盖。安全拒绝不套用 Compression 的外层 InvalidOperationException。损坏 ZIP 仍直接传播 InvalidDataException。

## 验证证据

环境：Windows，SDK 8.0.425，.NET 8.0.31 / .NET Core 3.1.32。证据目录：artifacts/security-fix-2026-10-25/。

| 层级 | 检查 | 结果与证据 |
|---|---|---|
| L0 | 主包四个 TFM Release 构建 | 0 错误、39 警告；build-main.log |
| L0 | 修改行空白、UTF-8、FTP XML 文档块 | 通过；既有未修改行不进行格式清理 |
| L1 before | 扩展 ZIP 回归，旧生产实现 | 11 失败 / 14 通过；before-net8.trx |
| L1 | net8 ZIP/限额相关定向 | 58 通过、0 失败、0 跳过；targeted-net8.trx |
| L2 | Bing.Utils.Tests net8 完整测试 | 7327 通过、0 失败、0 跳过；main-full-net8.trx |
| L3 | Test-ReviewNetStandard.ps1 -NoBuild | 动态加载实际 netstandard2.0 DLL；30 通过；review-netstandard.log |
| L3 | netcoreapp3.1 ZIP/限额相关定向 | 58 通过、0 失败、0 跳过；targeted-netcore31.trx |
| L3 | 实际测试目录 DLL 身份 | net8/netstandard DLL 均与对应生产输出一致；assembly-identity.json |
| L4 / L5 | 全解决方案、性能矩阵 | 本次局部安全修复未触发；未执行，不声明性能提升 |

测试范围存在重叠，不把不同层级数量累加为独立测试总数。CI 已通过现有 ZipReviewRegressionTest 筛选包含新增用例，无需改动工作流；Linux/macOS 链接行为仍待外部 CI 验证。

本轮实际源码差异保存在 round.diff；修改前相关文件保存在 before/。之前的性能基线、其他模块验证按未变化范围复用。依赖版本及审计规则未变化，复用 artifacts/security-review-2026-10-25/ 的审计结果：ImageSharp 高危风险及构建/测试依赖债务仍存在，没有扩充漏洞白名单。

## 独立复核

FTP 文档由 luna_worker 完成，主代理复核了指南与普通/限额接口四个方法的说明；仅新增注释。ZIP 修复及回归矩阵由 luna_worker 独立只读复核，结论 PASS、OPEN_ACTIONABLE = 0；确认预检位置、异常/覆盖兼容、跨框架测试与 junction 清理合理。复核者未重复运行 dotnet。

## 当前状态

- Completed：SEC-ZIP-01 与 SEC-FTP-DOC-01 均 CLOSED，2/2 完成。
- Open Actionable：无，OPEN_ACTIONABLE = 0。
- Blocked Approval：无。
- Blocked External：Linux/macOS CI 未运行。
- Not Applicable：本次无性能优化目标，不建立前后性能门禁。
- Accepted Limitations：调用方保持本地目录可信且操作期间稳定；FTP 大小为声明值准入预算，listing 先返回集合，缺少目录总数预算；ZIP 全归档不回滚。
- Verified Boundaries：旧入口链接拒绝、路径预检发生在副作用前、netstandard 原有不覆盖规则。
- Deferred：ImageSharp 迁移及历史依赖债务；FTP 目录/条目预算、增量 listing、对抗恶意本地替换的句柄级隔离。
- Implementation：2/2，PASS。Local Validation：PASS。External Gate：NOT_RUN。Release：BLOCKED（既有依赖风险），未发布。
- No-Progress Check：CHANGED。Next Action：STOP。Goal Status：STOPPED_BLOCKED（本地修复与验证完成，外部跨平台 CI 单独跟踪）。未暂存、提交、推送或发布。
