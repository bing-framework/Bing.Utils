# 反射验证目录与基线

更新时间：2026-10-13

## 当前入口

`benchmarks` 包含 26 个项目：2 个性能入口、4 个验证宿主、20 个模型/依赖夹具，另有一个运行时循环夹具目录。主反射探针直接覆盖主包与反射子包，配合裁剪器、NativeAOT 和程序集加载器验证部署行为；它们不是生产包依赖，也不是 BenchmarkDotNet 性能基准。

| 入口 | 职责 |
| --- | --- |
| `benchmarks/Bing.Utils.Benchmark` | 性能基准以及反射缓存、容量探针 |
| `benchmarks/ReflectionComparisonProbe` | 基线/候选程序集的性能比较 |
| `benchmarks/ReflectionTrimProbe` | 主反射契约、linker roots、trimming/NativeAOT smoke |
| `benchmarks/ReflectionTrimPluginLoadProbe` | path/resolver/bytes 外部加载与部署 |
| `benchmarks/ReflectionTrimThirdPartyPluginProbe` | 多版本、强名称冲突、依赖图与可回收上下文 |
| `benchmarks/ReflectionTrimProbe.UnrootedModelsHost` | 类型存在但成员未 root 的裁剪边界 |

## 基线已完成的整理

工作流默认读取以下四份当前配置，均位于 `benchmarks/ReflectionTrimProbe/baselines/`；手动运行仍可通过输入覆盖仓库责任清单与非仓库责任清单路径：

| 文件 | 用途 |
| --- | --- |
| `NativeAotWarningBaseline.txt` | 历史警告 ID 集合；CI 的 ID 严格检查沿用显式开关 |
| `NativeAotRepositoryDiagnosticBaseline.json` | 仓库诊断指纹及次数上限：363 项，合计 429 次 |
| `NativeAotRepositoryDiagnosticAllowlist.json` | 同一批 363 项的责任人与接受理由 |
| `NativeAotNonRepositoryDiagnosticOwnership.json` | 16 项生成物、依赖与工具链诊断归属 |

旧 339 项、405 次的基线和责任清单集中归档到同目录的 `history/20261013-before-approval/`，保持内容不变。批准日期与原快照关系由当前文件的 `approval` 字段记录，不再用平铺的带日期文件名区分当前输入。此次只迁移配置文件和修正引用，所有诊断 entry、源码路径、消息指纹、上下文和次数上限不变。

更新流程：先修复生产侧真实契约问题，再逐项审阅剩余诊断；新的接受范围仍需明确批准。不得直接用候选报告覆盖冻结历史快照，也不得批量忽略探针的全部诊断。生成报告和符号映射继续存放在忽略的 `bin/` 中。

从仓库根目录执行。示例使用 2026-10-13 本机验证产物（未纳入 Git）；自行发布时，将前两个变量替换为同一次发布的实际日志与源码映射路径：

```powershell
$publishLog = 'benchmarks/ReflectionTrimProbe/bin/commit-fix-nativeaot-contracts-20261013.log'
$symbolMap = 'benchmarks/ReflectionTrimProbe/bin/commit-fix-source-map-contracts-20261013.json'
$reportPath = 'benchmarks/ReflectionTrimProbe/bin/organized-baseline-warning-report-20261013.json'
$baselineDir = 'benchmarks/ReflectionTrimProbe/baselines'
& ./benchmarks/ReflectionTrimProbe/Check-NativeAotWarnings.ps1 `
  -LogPath $publishLog -SymbolMapPath $symbolMap -OutputPath $reportPath `
  -DiagnosticContext 'sdk=8.0.425;tfm=net8.0;rid=win-x64;trim=partial;aot=true;mode=ci-single-publish' `
  -BaselinePath "$baselineDir/NativeAotWarningBaseline.txt" `
  -DiagnosticBaselinePath "$baselineDir/NativeAotRepositoryDiagnosticBaseline.json" `
  -DiagnosticAllowlistPath "$baselineDir/NativeAotRepositoryDiagnosticAllowlist.json" `
  -NonRepositoryOwnershipPath "$baselineDir/NativeAotNonRepositoryDiagnosticOwnership.json" `
  -FailOnNewWarning -FailOnNewRepositoryDiagnostic `
  -FailOnUnapprovedRepositoryDiagnostic -FailOnUnownedNonRepositoryDiagnostic `
  -FailOnUnparsedDiagnostic
```

日志与映射必须是同一次发布的实际产物。仓库指纹、仓库责任、非仓库责任与未解析门禁在 CI 默认开启；警告 ID 门禁保持原来的可选设置。记录的本机五项严格检查使用全部开关，不等同于远程工作流已执行。

## 为什么保留多个夹具

- `Models/PluginModels/TertiaryModels/QuaternaryModels/QuinaryModels` 验证跨程序集 linker root 组合；合并后会失去跨程序集边界。
- `UnrootedModels` 与独立宿主验证未保留成员的行为，不能把模型合入主探针而改变 root 集合。
- `NonTrimmablePlugin*` 的依赖变体、V2、diamond 与深层图故意复用程序集名或改变依赖边，分别部署到隔离目录验证解析结果。
- `StrongNameConflict*` 故意输出无对应强名称身份的 `System.Text.Json`，验证同名冲突。
- `RuntimeCycleFixtures` 通过分阶段 Roslyn 编译构造真实 AssemblyRef 循环；`CycleAStub.cs` 是必需的构建输入。

目前没有已跟踪的明确无引用源码。项目引用、CI 构建/staging 和自动发现的 benchmark 都有对应入口；多个相同 SDK 项目模板不代表重复能力。复杂依赖图与长轮次适合专项验证，是否降低执行频率取决于后续 CI 分层要求，本次没有删减已有覆盖。

## 待确认：验证源码的目录迁移

本节是具体迁移方案，尚未执行。目标是让 `benchmarks` 只保留两个性能项目，把验证宿主、夹具、工具和基线集中到独立的 `tests/Bing.Utils.Reflection.Tests.Integration/`。该目录承载控制台集成验证，不合入现有 xUnit 项目，也不新增空壳测试项目。

| 当前目录 | 建议目标（相对集成验证目录） |
| --- | --- |
| `benchmarks/ReflectionTrimPluginLoadProbe` | `hosts/ReflectionTrimPluginLoadProbe` |
| `benchmarks/ReflectionTrimProbe` | `hosts/ReflectionTrimProbe` |
| `benchmarks/ReflectionTrimProbe.Models` | `fixtures/ReflectionTrimProbe.Models` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDeepDependency` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginDeepDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDeepTransitiveDependencyVariant` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginDeepTransitiveDependencyVariant` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDependency` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDependencyV2` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginDependencyV2` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDependencyVariant` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginDependencyVariant` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDiamondModels` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginDiamondModels` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginModels` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginModels` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginTransitiveDependencyVariant` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginTransitiveDependencyVariant` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginTripleDiamondModels` | `fixtures/ReflectionTrimProbe.NonTrimmablePluginTripleDiamondModels` |
| `benchmarks/ReflectionTrimProbe.PluginModels` | `fixtures/ReflectionTrimProbe.PluginModels` |
| `benchmarks/ReflectionTrimProbe.QuaternaryModels` | `fixtures/ReflectionTrimProbe.QuaternaryModels` |
| `benchmarks/ReflectionTrimProbe.QuinaryModels` | `fixtures/ReflectionTrimProbe.QuinaryModels` |
| `benchmarks/ReflectionTrimProbe.RuntimeCycleFixtures` | `fixtures/ReflectionTrimProbe.RuntimeCycleFixtures` |
| `benchmarks/ReflectionTrimProbe.StrongNameConflictDependency` | `fixtures/ReflectionTrimProbe.StrongNameConflictDependency` |
| `benchmarks/ReflectionTrimProbe.StrongNameConflictModels` | `fixtures/ReflectionTrimProbe.StrongNameConflictModels` |
| `benchmarks/ReflectionTrimProbe.TertiaryModels` | `fixtures/ReflectionTrimProbe.TertiaryModels` |
| `benchmarks/ReflectionTrimProbe.UnrootedModels` | `fixtures/ReflectionTrimProbe.UnrootedModels` |
| `benchmarks/ReflectionTrimProbe.UnrootedModelsHost` | `hosts/ReflectionTrimProbe.UnrootedModelsHost` |
| `benchmarks/ReflectionTrimThirdPartyPluginProbe` | `hosts/ReflectionTrimThirdPartyPluginProbe` |

工具迁移清单：主探针目录中的 `Build-NativeAotDiagnosticBaseline.ps1`、`Build-NativeAotSourceMap.ps1`、`Check-NativeAotWarnings.ps1`、`Test-Check-NativeAotWarnings.ps1`，插件加载目录中的 `Stage-ExternalPlugin.ps1`，以及第三方探针目录中的 `Test-MultiPluginCapacity.ps1`、`Test-MultiPluginLongRounds.ps1` 集中到 `tools/`。循环夹具构建脚本与其 C# 输入一起保留在 `fixtures/ReflectionTrimProbe.RuntimeCycleFixtures/`。现有 `baselines/` 整体迁移，历史快照内容不改。

迁移必须作为完整的一组修改处理：

1. 保持所有项目文件名、程序集名、命名空间、版本、root 类型和运行断言；20 个模型项目仍是独立程序集。
2. 修正 hosts 的生产源码引用为 `../../../../src/`，跨 hosts/fixtures 引用按新位置重算；更新约 328 处主探针 CI 路径，以及构建、staging、报告与上传路径。
3. tools 中依赖仓库根的脚本改用 `../../..`；checker、自测与 builder 保持同目录。循环夹具脚本继续从自身目录读取源码。
4. 源码移动会改变仓库诊断 key 的 file 部分。当前基线有 74 项指向主探针 Program.cs，包含已批准的 24 项；当前基线和 allowlist 必须按一一对应迁移路径更新，不得增加接受项或改变次数上限。保留旧位置的历史诊断记录并说明映射。
5. 编译迁移的 24 个项目；运行 checker 自测、主探针、循环夹具和插件部署 smoke；执行一次针对新源码位置的 NativeAOT 发布、符号映射及严格门禁。这里只重测迁移影响范围。
6. 旧目录中的 bin/obj 等本机构建产物可恢复归档到 artifacts，不复制到新源码目录；不得删除未知用户文件。

当前验证：整理后的四份配置再次复核已有日志，260 条诊断、244 条仓库源、218 个仓库指纹，五项严格门禁均通过，新增 ID 与未解析数均为 0；报告为忽略的 `benchmarks/ReflectionTrimProbe/bin/organized-baseline-warning-report-20261013.json`。历史快照内容和当前 entries 与整理前一致，出处路径有效，85 个工作流 PowerShell 块解析通过。本次没有修改或重编译项目源码；远程 CI 和整套源码迁移后的验证均为待确认。接受范围见 [批准记录](reflection-nativeaot-baseline-review.md)。
