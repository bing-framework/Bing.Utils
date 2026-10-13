# 反射跨运行时验证（2026-10-09）

## 目录迁移后验证（2026-10-13）

环境为 Windows、SDK 8.0.425、Release/net8.0；仅迁移验证工程和路径，生产源码及探针 C#/XML 内容未改。以下为迁移后实际执行结果：

- 24 个宿主/模型项目逐一构建通过；29 条 `ProjectReference` 指向存在的项目，程序集名称、版本和 root 配置保留。
- 警告检查器自测通过；RuntimeCycle 分阶段编译与真实 AssemblyRef 循环、分支、多分支和深分支夹具运行通过。
- framework 主探针、未 root 模型插件及第三方插件 smoke 通过；path/resolver/bytes 三种加载方式覆盖缺失传递依赖、版本冲突、强名称冲突、多版本和九目录依赖图（1 轮）。本次未重复长轮次/容量压力验证。
- 主探针完成 net8.0/win-x64、self-contained、partial trim、NativeAOT 单次发布并运行成功；输出 by-ref 签名、ref 返回、泛型方差、AOT 特性回退和元数据五个预期标记。
- 同次发布日志与源码映射的报告包含 421 条诊断、405 次仓库源诊断、343 个仓库指纹、12 个警告 ID。五项严格门禁全部通过；新增 ID、未登记仓库指纹、未批准仓库诊断、未归属非仓库诊断和未解析诊断均为 0。存在已登记警告，不表示无警告构建。
- 当前 baseline/allowlist 仅将 74 条主探针路径映射到新位置，363 个 key、消息指纹、上下文及合计 429 次上限保持不变；两份历史快照内容不变。
- workflow 的 85 个多行 PowerShell `run` 块静态解析通过（解析前替换 GitHub 表达式占位），当前源码引用有效，活动代码/工作流无旧 `benchmarks/ReflectionTrim*` 路径。

发布日志、映射、报告分别位于 `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimProbe/bin/layout-migration-nativeaot-20261013.log`、`layout-migration-source-map-20261013.json`、`layout-migration-warning-report-20261013.json`；本机产物不纳入 Git。迁移前构建产物集中保存在忽略目录 `artifacts/reflection-layout-before-migration-20261013/`，可供本机溯源。当前入口及严格检查命令见 [目录与基线](reflection-validation-layout.md)。

本次未修改生产代码和 xUnit 测试，复用下节原有回归证据；未重跑完整 partial/full/NativeAOT 插件矩阵，也不声明远程 CI 已通过。

## 迁移前验证证据（2026-10-13）

以下结果是在验证目录迁移前形成的既有证据，保留用于对照；迁移后新执行范围与结果以上节为准。

- SDK 固定为 8.0.425。主包 net8.0 筛选 `FullyQualifiedName~Bing.Reflection|FullyQualifiedName~MapperHelper` 已执行两次并通过，第二次记录为 453/453。映射缓存专用 xUnit collection 隔离进程级计数，类内 32 次并发映射仍保留；程序集名称 null 用例精确断言 `ArgumentNullException`，其他非法名称断言 `ArgumentException`。
- 本次之前的相同反射子包候选已在隔离副本通过 net8.0 306/306；本次未修改反射子包生产代码，复用该证据。旧运行时表格是历史结果，不作为本次候选的新鲜证据。
- 新鲜 net8.0/win-x64 NativeAOT partial 发布和原生程序执行成功，输出 `reflection-aot-metadata-ok`、`reflection-aot-attribute-fallback-ok` 及三个签名边界标记。
- 生产裁剪契约补传后，核心反射 net8.0 378/378、主包 netstandard2.0 编译通过，六个生产侧新增指纹消除。再次新鲜 NativeAOT 发布及原生运行通过。
- NativeAOT 诊断报告为 260 条诊断、244 条仓库诊断、218 个仓库指纹。相对冻结历史基线新增的 24 个探针指纹已获用户批准，合并到带日期的新版基线和责任清单；复核已有日志，五项严格门禁全部通过，新增 ID、新增仓库指纹和未解析诊断均为 0。具体差异见 [基线复核清单](reflection-nativeaot-baseline-review.md)。未覆盖历史基线或关闭门禁。
- 反射基准项目 Release/net8.0 编译通过；冷启动、并发缓存、并发 schema、结构体稳态与六类容量探针均执行通过。结构体冷编译一次，256 次稳态请求没有额外编译；1025 组映射只准入 1024 项，其余旁路。未设置业务性能阈值。
- 本机缓存还原存在既有 `NU1603` 警告；本次未调整依赖版本。远程 CI、全量解决方案及 partial/full 裁剪部署矩阵：待确认。
- 工作流静态修复：补齐三处夹具目录的 PR 触发路径；两个直接 PowerShell 脚本调用改用 `$?` 判断成功，避免空或旧 `$LASTEXITCODE` 将成功误判为失败。通过 `pwsh -File` 启动子进程的判断仍保留 `$LASTEXITCODE`。工作流已接入批准后的新版基线，远程执行结果待确认。
- 工作流四处多插件轮数条件的 `-or` 改到上一行末尾，修复 PowerShell 多行表达式解析错误。将 GitHub 表达式替换为测试占位值后，本机 Parser 对 85 个多行 `run` 块静态解析为 0 errors；不等同于 YAML 全面校验或远程 CI 运行结果。

## 历史记录说明

以下 M 编号、ACL/离线阻塞和运行时表格均保留当时的环境与结果；不是当前全部检查的状态。当前状态以上节为准。

M145 未 root 插件 init-only 属性边界已完成 net8.0 精准验证：模型与完整 `ReflectionTrimPluginLoadProbe` 源码分别经隔离构建，均为 `0 warnings/0 errors`；`InitOnlyProperty` 可读取为 `91`，缓存/可读/raw/generic/public projection 保留该属性，writable/`Setters`/`Both` 排除该属性。临时未裁剪 net8.0 宿主的 path/resolver/bytes 三种加载方式全部输出 `metadata-preserved`。验证宿主通过临时直引当前 `output/debug/net8.0` 的 Bing 程序集及本机缓存依赖构建；正式项目 build 仍受工作区 obj ACL/NuGet 资产限制。本条不代表 partial/full trim 或 NativeAOT 新鲜发布，相关现有 CI 门禁需要在候选版本上执行。

M144 嵌套成员路径安全边界：核心回归覆盖 `Child.Secret` 两段路径，默认 `MemberPathOptions` 下 `HasMemberPath`、`TryGetMemberValueByPath`、`TrySetMemberValueByPath` 分别拒绝私有叶成员且未修改原值；`IncludeNonPublic=true` 时可发现、读取和写入。隔离 net8.0 公开 API runner 通过 `1/1`。主包测试项目的本地构建在生产项目输入缓存写入时因 `MSB3491` Access denied 失败，因此这里只记录隔离行为验证，不宣称 `Bing.Utils.Tests` runner 通过。

M143 泛型约束循环保护已完成并通过隔离 L1：核心包和反射子包的 `ReflectionCacheScope.IsCollectible(Type)` 使用按次访问集合跟踪泛型参数，避免 `T : IComparable<T>` 在读取约束参数时回到自身并无限递归；普通类型路径不创建集合。新增的 `ReflectionCacheScopeTests` 验证类型泛型参数、方法泛型参数和属性缓存扫描，net8.0 精准测试为 `2/2`；核心公开 API 消费回归经隔离 runner 为 `1/1`。包含两包的隔离 `net8.0` 与 `netstandard2.0` 构建均为 `0 errors`。常规工作区输出目录写入受 ACL 拒绝，本轮验证使用隔离副本；NativeAOT/trim 证据未因内部递归保护改动而重跑。

M142 方法实例缓存并发生命周期边界已完成并通过隔离 L1：`TypeVisit.GetMethodBySignature` 的实例 CWT 命中和创建路径统一经过 `GetCachedInstanceMethod`，解析结果引用 collectible 元数据时所有并发调用方都会删除对应项并计入旁路，避免竞态留下强值缓存。新增 `MethodSignatureCacheLifecycleTests`，以 Reflection.Emit 创建可回收接口方法，预置同一个 `Lazy<MethodInfo>` 后并发 64 次调用内部清理路径，测试源码独立编译为 0 warnings/0 errors；独立 net8 harness 输出 `method-cache-concurrent-collectible:passed`。CLR 明确拒绝非 collectible 动态程序集引用 collectible 程序集，因此验证不构造不可实现的宿主图，而是直接覆盖实际竞态清理逻辑。

M141 元数据级只读引用参数缺陷已完成并通过 L1：`TypeVisit.GetPassingKind` 现在同时识别 `ParameterInfo.IsIn` 与 `IsReadOnlyAttribute` required custom modifier，并保持 `out` 优先。新增 Reflection.Emit 方法参数为 `int&`、`IsIn=false`、required modifier 为 `System.Runtime.CompilerServices.IsReadOnlyAttribute` 的回归；`ParameterPassingKind.In` 命中、`Ref` 拒绝。隔离 `net8.0` 的 `MethodSignatureReadOnlyTests` 为 `9/9`，`ReflectionTrimProbe` 编译 `0 errors`，运行输出 `reflection-trim-byref-signature-ok`、`reflection-trim-ok`。

M140 结构体映射稳态探针已完成并通过 L1：`--reflection-struct-map-steady` 为每次运行创建唯一动态结构体源/目标类型，先执行一次冷调用，再执行 256 次稳态调用。隔离 `net8.0` 基准工具报告 `cold.compiles=1`、`warm.compiles=0`、`structuralPassed=true`；最终确认运行输出冷启动 `elapsedUs=14367.7`、`allocatedBytes=42288` 与稳态 `elapsedUs=542.1`、`allocatedBytes=18520`，并验证映射后的 `Value=17`。分配量包含对象包装和探针调用开销，只用于同机趋势观察，不替代业务预算。

M139 结构体映射缓存容量探针已完成并通过 L1：`MapperHelper` 暴露内部容量、当前条目数和容量旁路计数，`ReflectionBenchmarks.RunCapacityProbe` 新增唯一动态结构体对场景并将容量报告升级为 schema 3。隔离 `net8.0` 基准工具执行 `--reflection-capacity` 报告 `struct-map` 场景 `requests=1025`、`admitted=1024`、`bypasses=1`、`Compiles=1025`，总体 `structuralPassed=true`；其他五类缓存场景仍全部通过。该证据只证明结构体委托缓存有界且旁路可观测，不替代业务性能预算或完整多目标 runner。

M138 测试产物目标框架一致性防回归已完成并通过 L1：`MapperHelperContractTests` 比较测试程序集和被测 `Bing.Utils` 程序集的 `TargetFrameworkAttribute`，两者不一致时立即失败，避免自定义输出目录加载旧 netstandard 程序集导致契约误报。net8 精准测试当前为 `3/3`，测试项目重新编译为 `0 errors`；既有 `MapperHelperCacheTests` `4/4` 和 `MapperHelperTest` `48/48` 证据保持复用。

M137 MapperHelper 公开入口 AOT/裁剪契约已补齐并通过 L1：`Map`、`MapWith`、`MapWithout` 声明 `RequiresDynamicCode` 与 `RequiresUnreferencedCode`；`MapTo`、`MapList`、`MapDictionary`、`ToObject` 声明 `RequiresUnreferencedCode`。新增 `MapperHelperContractTests` 在当前 net8 核心产物上反射确认 2/2 通过；测试过程中发现旧的 netstandard 输出会被自定义测试目录复用，已改用当前 net8 产物复核，避免把 stale assembly 误报为契约缺失。默认 `obj\Debug` 目录本轮受 ACL 拒绝，独立 test runner 的既有 `MapperHelperCacheTests` 为 `4/4`、`MapperHelperTest` 为 `48/48`。

M136 反射缓存清单审计已闭环：将 `TypeReflections` 与 `TypeVisit` 中 3 个成员快照 `ConditionalWeakTable` 弱标记缓存登记到 `reflection-cache-inventory.json`；`Test-ReflectionCacheInventory.ps1` 最终输出 `entries=32, unlisted=0, passed=True`。

M135 MapperHelper 结构体映射委托缓存治理已完成并通过 L1：`_structMapCache` 由字符串 FullName 键改为 `(Type, Type, 操作, 规范化属性集合)` 身份键，缓存限制为 1024 条目，使用 `Lazy<object>` 保证并发首次访问只编译一次；collectible 源/目标类型不进入静态缓存，表达式编译失败会移除失效项。`MapperHelperCacheTests` 的冷启动并发、MapWith/MapWithout 属性大小写/顺序/重复项和 collectible 动态程序集回归为 `4/4`，既有 `MapperHelperTest` 为 `48/48`；`Bing.Utils` net8.0 与 netstandard2.0 编译 0 errors。测试项目常规输出仍受工作区资源文件拒绝覆盖影响，改用独立输出目录构建后以 `dotnet vstest` 执行。

M134 构造失败契约统一已完成并通过 L1：`StrictConstructorBinder` 删除值类型无显式公共无参构造时的 `Activator.CreateInstance` 默认值旁路；`CreateInstance<int>` 的泛型、运行时类型和泛型+Type 三个入口，以及 `CreateGenericInstance<int?>` 均改为断言 `ArgumentException`。有显式匹配构造函数的类型仍沿严格绑定路径处理；核心构造契约筛选 `33/33`、反射子包构造绑定筛选 `29/29`，核心 net8 编译 0 errors。

M133 DynamicBase 属性缓存实例生命周期边界已完成并通过 L1：`DynamicBase.GetProperty` 先快照 `_type` 再创建 `Lazy<PropertyInfo>`，避免类型级缓存闭包捕获查询实例。新增独立探针子类型，首次属性写读填充缓存后经过 20 轮完整 GC，弱引用确认实例可回收；现有动态基类属性访问行为保持不变。

M132 兼容属性路径入口显式根类型边界已完成并通过 L1：`GetPropertyValueByPath` 与 `SetPropertyValueByPath` 现在把传入的 `objectType` 传给成员路径解析，不再从派生实例运行时类型越权解析。新增回归验证基类公共属性读取/写入、隐藏属性选择、派生独有属性拒绝和基类完整类型前缀规范化；`Bing.Utils` net8 编译 0 errors，`LegacyPropertyPath_UsesExplicitObjectTypeAsRoot` 通过 1/1。完整多目标 runner 仍受工作区既有 MSBuild/离线 NuGet 环境限制。

M131 未 root 泛型属性数组 BindingFlags 边界矩阵已完成并通过隔离 L1：独立 `ReflectionTrimPluginLoadProbe` 通过 `Reflections.GetProperties<T>(BindingFlags)` 验证 public 实例结果包含 `PublicProperty` 且不包含私有 `ReflectedValue`，显式 `NonPublic` 实例结果命中私有属性且不包含 public 属性。当前源 probe 经 SDK 8.0.425 Roslyn 编译和 path/resolver/bytes 三种加载方式运行验证通过；本机新鲜 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能把四宿主运行结果记为本地通过。

M130 未 root 泛型字段 BindingFlags 边界矩阵已完成并通过隔离 L1：独立 `ReflectionTrimPluginLoadProbe` 通过反射调用真实泛型入口 `Reflections.GetField<T>(string, BindingFlags)`，验证 public 字段、显式 `NonPublic` 私有字段、`IgnoreCase`，以及未开启 `NonPublic` 时私有字段、缺失名称和空名称的明确异常。当前源 probe 经 SDK 8.0.425 Roslyn 编译，path/resolver/bytes 三种加载方式均输出 `metadata-preserved`、退出码 0；本机新鲜 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能把四宿主运行结果记为本地通过。

M129 未 root `ParameterInfo` 描述/显示名称 fallback 矩阵已完成并通过隔离 L1：独立 `ReflectionTrimPluginLoadProbe` 对同一参数验证 `TypeReflections.GetDisplayName` 的参数名回退、`GetDescriptionOr` 的描述命中、`GetDisplayNameOr` 的默认值回退和 `GetDescriptionOrDisplayName` 的描述优先语义。当前源 probe 经 SDK 8.0.425 Roslyn 编译和 path/resolver/bytes 三种加载方式运行验证通过；本机新鲜 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能把四宿主运行结果记为本地通过。

M128 未 root `ParameterInfo` 描述投影矩阵已完成并通过隔离 L1：独立 `ReflectionTrimPluginLoadProbe` 对 `Execute([Description])` 参数调用 `TypeReflections.GetDescription(ParameterInfo)`，验证便利投影返回 `DescriptionAttribute` 文本，并与参数特性、required 及运行时 `attributeType` 入口保持一致。当前源 probe 经 SDK 8.0.425 Roslyn 编译和 path/resolver/bytes 三种加载方式运行验证通过；本机新鲜 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能把四宿主运行结果记为本地通过。

M127 未 root 泛型方法 BindingFlags 边界矩阵已完成并通过隔离 L1：独立 `ReflectionTrimPluginLoadProbe` 通过反射调用真实泛型入口 `Reflections.GetMethod<T>(string, BindingFlags)`，验证 public 方法、显式 `NonPublic` 私有属性 getter、`IgnoreCase`，以及未开启 `NonPublic` 时私有方法、缺失名称和空名称的明确异常。当前源 probe 经 SDK 8.0.425 Roslyn 编译，path/resolver/bytes 三种加载方式均输出 `metadata-preserved`、退出码 0；本机新鲜 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能把四宿主运行结果记为本地通过。

M126 未 root 泛型属性 BindingFlags 边界矩阵已完成并通过隔离 L1：独立 `ReflectionTrimPluginLoadProbe` 通过反射调用真实泛型入口 `Reflections.GetProperty<T>(string, BindingFlags)`，验证 public 属性命中、显式 `NonPublic` 命中、`IgnoreCase` 命中，以及未开启 `NonPublic` 时私有属性、缺失名称和空名称的明确异常。当前源 probe 经 SDK 8.0.425 Roslyn 编译，path/resolver/bytes 三种加载方式均输出 `metadata-preserved`、退出码 0；本机新鲜 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能把四宿主运行结果记为本地通过。

M125 公共可写属性筛选一致性已完成并通过隔离 L1：`TypeReflections.GetPublicWritableInstanceProperties` 复用 init-only setter 判定，`init` 属性不会再被报告为初始化后可写；与 `TypeVisit.GetProperties(..., PropertyAccessOptions.Setters)` 的结果保持一致。核心包和反射子包经 SDK 8.0.425 Roslyn 编译 0 errors，最小 net8 harness 输出 `init-only-writable-boundary:passed`。标准多目标测试 runner 本轮因工作区派生 MSBuild 节点挂起被中止，不能将该隔离结果扩大为完整 L2。

M124 属性筛选入口边界矩阵已完成并通过源级 L1：独立 `ReflectionTrimPluginLoadProbe` 对真实公开 `TypeVisit.GetProperties(Type, PropertyAccessOptions)` 复用 Getters/Setters/Both 成员筛选，并验证 null 类型抛出 `ArgumentNullException`、未知选项抛出 `InvalidOperationException`。断言随 path、`AssemblyDependencyResolver`、字节数组三种加载方式复用现有 framework/partial/full/NativeAOT 插件 probe；本机新鲜 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能把四宿主运行结果记为本地通过。

M123 未 root 泛型返回类型绑定矩阵已完成并通过源级 L1：独立 `ReflectionTrimPluginLoadProbe` 对未 root `Echo<T>(T)` 调用 `FindMethodByReturnType`，用 `int` 参数/返回类型验证泛型参数推断命中，并用 `int` 参数/`string` 返回类型验证 `MissingMethodException` 负向边界。该逻辑随 path、`AssemblyDependencyResolver`、字节数组三种加载方式复用现有 framework/partial/full/NativeAOT 插件 probe；本机新鲜 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能把四宿主运行结果记为本地通过。

M122 ref 返回值签名矩阵已完成并通过隔离 L1：`TrimMemberSample` 新增 `ref int RefReturnSignature()`，主 probe 使用 `FindMethodByReturnType` 以 `System.Int32&amp;` 精确匹配，并要求 `System.Int32` 按值查询抛出 `MissingMethodException`。反射子包全部源文件经 SDK 8.0.425 Roslyn 编译 0 errors，主 probe 编译 0 errors，使用当前源程序集运行输出 `reflection-trim-ref-return-ok`、`reflection-trim-byref-signature-ok`、`reflection-trim-generic-variance-ok` 和 `reflection-trim-ok`。`TrimmerRoots.xml` 和四类 workflow marker 已同步；本机标准 trim/AOT 发布仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断，不能将 CI marker 预先记为通过。

M121 自定义泛型方差矩阵已完成实现并通过隔离 L1：新增 `ITrimCovariant<out T>`、`ITrimContravariant<in T>` 和 `TrimVarianceSample`，同时验证 CLR `IsAssignableFrom`、`Reflections.IsGenericAssignableFrom`、`TypeReflections.IsInterfaceDefined` 的协变/逆变正反向结果；读取两个泛型参数的 `VarianceMask` 确认方差元数据，`Produce`/`Consume` 通过 linker root 保留，full/NativeAOT 环境开关验证未 root 方法的负向裁剪边界。SDK 8.0.425 Roslyn 编译主 probe 为 0 errors，使用当前 Debug 生产程序集调用私有验证入口输出 `reflection-trim-generic-variance-ok`。本机 partial/full/NativeAOT 发布均未形成新鲜结果，仍受 `ResolvePackageAssets` NuGet 资产缓存 I/O 阻断；workflow marker 只作为待 CI 执行门禁，不宣称四宿主已通过。

M120 by-ref 方法签名矩阵已完成实现并通过隔离 L1：`ReflectionTrimProbe.TrimMemberSample` 增加 `ref int`、`in int`、`out int` 三个入口，主探针使用 `MethodParameterSignature`/`ParameterPassingKind` 分别命中，并对六种错误传递类型要求 `MissingMethodException`；`TrimmerRoots.xml` 使用三个 `System.Int32&amp;` 方法签名，workflow 的 framework、partial、full 和 NativeAOT 步骤均要求 `reflection-trim-byref-signature-ok`。SDK 8.0.425 Roslyn 编译主探针为 0 errors，独立生产 API 探针输出 `reflection-byref-signature:verified`。本机 trim 发布因 `ResolvePackageAssets` NuGet 资产缓存 I/O 失败，未把四宿主 marker 记录为本地通过；完整 trim/AOT 运行证据待 CI 或修复外部构建环境后补齐。

M119 动态类型快路径身份快照在隔离 L1 中通过：`TypeFactory.SchemaFastEntry` 将属性类型从强引用 `Type` 改为 `SchemaTypeIdentity`（程序集限定名 + runtime handle），身份对象内部只保留 `WeakReference<Type>`，`SchemaFastEntry.Matches` 稳态命中优先比较仍存活的类型对象，弱引用失效后再用稳定身份严格比较，并继续按规范化 `null -> object` 类型匹配。SDK 8.0.425 Roslyn 直接编译 TypeFactory、DynamicBase、缓存支持代码为 0 errors；最新运行时探针输出 `dynamic-schema-identity-snapshot:verified`，证明快路径快照值不再是 `Type`。新增 `DynamicTypeFactoryTests` 以友元程序集名编译通过。该证据只覆盖快路径值结构和 schema 复用，不宣称动态程序集完整卸载或端到端 trim/AOT 发布；本机标准 runner 仍受 workload resolver、`obj` I/O 权限和离线还原阻断。

API-1 命名方法返回类型维度在隔离 L1 中通过：`TypeVisit.FindMethodByReturnType` 为命名方法提供显式返回类型筛选，命名缓存 key 同时包含返回类型或 `<any>` 标记；动态 IL 夹具覆盖同名同参数不同返回类型的独立命中、旧入口歧义异常和泛型返回类型绑定，参数化用例覆盖普通返回类型和 null 返回类型异常。net8.0 `MethodSignatureReadOnlyTests` 在独立输出目录通过 `8/8`，受影响反射项目编译为 0 errors；常规工作区输出仍受既有 `MSB3491` 权限错误和离线 `NU1900` 影响，因此该证据限定为隔离 L1，不替代完整 L2。

M114 方法签名 custom modifier 生命周期边界在隔离 smoke 中通过：核心包与反射子包的统一 collectible 判定覆盖方法返回参数、普通参数、属性和字段的 required/optional custom modifiers；普通 non-collectible modifier 方法保持缓存复用，collectible modifier 方法走旁路。反射子包源代码隔离编译为 0 errors，net8 smoke 输出 `custom-modifier-cache:passed`；完整项目 runner 本轮因工作区多目标 `MSB3491` 权限错误及离线 `NU1900` 未形成新鲜项目级结果。

M115 Guard 构造反射器生命周期边界在隔离 smoke 中关闭：`ValidationExceptionHelper` 保留原有无参/精确参数构造函数选择，改为直接调用 `ConstructorInfo.Invoke`，绕过 AspectCore `ReflectorCacheUtils<ConstructorInfo,...>` 强键缓存；null 过滤后的多余参数仍按旧行为截断，构造函数内部异常通过 `ExceptionHelper.PrepareForRethrow` 保持原异常链，现有 `ArgumentNullException` 参数契约和动态异常类型抛出契约保持不变。Guard 源文件隔离编译为 0 errors，net8 smoke 输出 `guard-constructor-cache:passed`，动态异常程序集在 GC/finalizer 后可回收；常规 Guard 测试 runner 本轮因工作区 `MSB3491` 权限错误及离线 `NU1900` 未形成新鲜项目级结果。

M116 NativeAOT attribute fallback 边界在隔离 host 中通过：`ReflectionTrimProbe` 的 `!RuntimeFeature.IsDynamicCodeSupported` 分支新增 `TypeInfo`、`MethodInfo` 和 `ParameterInfo` 的 `DescriptionAttribute` 单项、集合及存在性断言，并输出 `reflection-aot-attribute-fallback-ok`；NativeAOT workflow 现在捕获 probe 输出并要求该 marker 与 `reflection-aot-metadata-ok` 同时存在。最小 net8.0/win-x64 NativeAOT host 已发布、运行并输出 `reflection-aot-attribute-fallback-ok`，证明 false 分支实际执行；完整 benchmark 项目在本工作区仍被 `MSB3491`/离线 `NU1900` 阻断，另有 stale `output/release` 程序集导致的旧接口构造探针失败，因此未把隔离 host 结果扩大为全矩阵结论。

M117 仓库内第三方反射缓存边界审计已通过：新增 `Test-ReflectionExternalCacheBoundary.ps1`，以 UTF-8 读取源文件，要求 `TypeReflections` 的 `UsePublicAttributeApi`、collectible 旁路和 `DirectAttributeProvider` 守卫存在，要求 `Reflections` 保留公开 API fallback，要求 Guard 使用直接 `ConstructorInfo.Invoke`，并扫描 `src` 禁止未授权的 `GetReflector`、`ReflectorCacheUtils` 和 `ConstructorInfo.GetReflector` 引用。脚本报告以 JSON 输出并接入 workflow artifact；该证据只覆盖仓库内适配器和 Guard，不覆盖外部消费者自建缓存或第三方程序集内部缓存。

M118 成员签名缓存旁路与泛型约束识别在隔离环境通过：核心包与反射子包的 `ReflectionCacheScope` 均递归检查泛型参数约束；核心 `TypeReflections.TypeCacheManager`、`TypeReflections.GetPublicInstanceProperties` 的属性/字段/方法缓存以及反射子包 `TypeVisit` 的运行时/筛选属性缓存，在非 collectible 宿主返回引用 collectible 声明类型、返回类型、字段类型或参数签名时移除对应强缓存项，仅返回当前独立快照。安全快照通过弱键标记避免稳态重复扫描。核心/子包生产源隔离 Roslyn 编译为 0 errors，新增 `ReflectionCacheScopeTests` 编译为 0 errors，动态泛型约束运行时探针输出 `runtime-generic-constraint:verified`。CLR 明确拒绝非 collectible 动态程序集引用 collectible 程序集，本轮未将不可构造的宿主/成员签名卸载场景伪装成通过；完整项目 runner 仍因 `obj` 写入权限、离线 `NU1900` 和 testhost 无终态未形成新鲜项目级结果。

M113 方法签名缓存生命周期边界在隔离 smoke 中通过：`TypeVisit` 的 named-signature、结构签名和实例方法缓存会对解析后的 `MethodInfo` 再检查 collectible 声明类型、返回类型和签名元数据，发现风险后立即移除强缓存项；普通方法查询仍复用同一惰性缓存。当前反射子包源代码隔离编译为 0 errors，net8 smoke 输出 `method-cache-lifecycle:passed`，并新增 collectible 方法 named-signature 回归调用；完整项目 runner 本轮因多目标 `GetTargetFrameworks` 失败及离线 `NU1900` 未形成新鲜项目级结果。

配置为 `ReflectionTodo`，测试编译关闭文档生成，使用 `--no-restore -m:1 -p:UseSharedCompilation=false`。核心反射筛选条件为 `FullyQualifiedName~Bing.Reflection`；.NET 8 核心项目执行全量测试。未用新版运行时滚动代替旧运行时验证。

| 原生运行时 | 反射模块 | 核心项目 | 验证范围 |
| --- | ---: | ---: | --- |
| .NET Core 3.1.32 | 159/159 | 444/444 | 模块全量、核心反射筛选 |
| .NET 5.0.17 | 159/159 | 444/444 | 模块全量、核心反射筛选 |
| .NET 6.0.36 | 171/171 | 439/439 | 模块全量、核心反射筛选 |
| .NET 7.0.20 | 171/171 | 439/439 | 模块全量、核心反射筛选 |
| .NET 8.0.31 | 171/171 | 7048/7048 | 模块全量、核心全量 |

模块中的 `ReflectionRuntimeVerificationTests` 校验 CLR 主版本必须等于目标 TFM，并把实际运行时名称与 CLR 目录写入测试输出。旧框架引用 netstandard 库资产，动态生成 API 的条件编译不同，测试数量因此少于现代框架。

本机旧运行时安装在忽略的工作区目录 `benchmarks/ReflectionComparisonProbe/bin/legacy-runtime`，不修改全局安装。二进制 ZIP 从微软发布元数据指定的地址取得并通过 SHA-512 校验后解压：

| 版本 | 下载地址 | SHA-512 |
| --- | --- | --- |
| 3.1.32 | [微软运行时 ZIP](https://builds.dotnet.microsoft.com/dotnet/Runtime/3.1.32/dotnet-runtime-3.1.32-win-x64.zip) | `72a3feb6784c5c69cb464ad4a8678e719615bd1e4e9a7c3cf5c1f67a4f01e08c073424532772f1e320d05f63c01079c6a089be45f4777abe035f14ffbf06f38b` |
| 5.0.17 | [微软运行时 ZIP](https://builds.dotnet.microsoft.com/dotnet/Runtime/5.0.17/dotnet-runtime-5.0.17-win-x64.zip) | `424df2b535f73c40aea164623a8a8f549bd18f89406eb2cb84289cb4cfceb3450db7762c5582756263d79ed628a574730e708d87dc906fd2c00d29f64942c782` |

复核命令（PowerShell，先取得全局 SDK 命令路径，再为测试子进程设置私有运行时根目录）：

```powershell
$sdkDotnet = (Get-Command dotnet).Source
$runtimeRoot = Join-Path (Get-Location).Path 'benchmarks/ReflectionComparisonProbe/bin/legacy-runtime'
$env:DOTNET_ROOT = $runtimeRoot
$env:DOTNET_ROOT_X64 = $runtimeRoot
$env:VSTEST_DOTNET_ROOT = $runtimeRoot
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
foreach ($tfm in @('netcoreapp3.1', 'net5.0')) {
    & $sdkDotnet test tests/Bing.Utils.Reflection.Tests/Bing.Utils.Reflection.Tests.csproj -f $tfm --no-restore -p:Configuration=ReflectionTodo -p:GenerateDocumentationFile=false -m:1 -p:UseSharedCompilation=false --logger "trx;LogFileName=reflection-$tfm.trx" --results-directory benchmarks/ReflectionComparisonProbe/bin/legacy-evidence
    if ($LASTEXITCODE -ne 0) { throw "Module tests failed: $tfm" }
    & $sdkDotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f $tfm --no-restore -p:Configuration=ReflectionTodo -p:GenerateDocumentationFile=false -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~Bing.Reflection' --logger "trx;LogFileName=core-$tfm.trx" --results-directory benchmarks/ReflectionComparisonProbe/bin/legacy-evidence
    if ($LASTEXITCODE -ne 0) { throw "Core reflection tests failed: $tfm" }
}
```

本机本轮 TRX 保存在忽略目录 `benchmarks/ReflectionComparisonProbe/bin/legacy-evidence-m74`：
`reflection-netcoreapp3.1.trx`、`core-netcoreapp3.1.trx`、`reflection-net5.0-rerun.trx` 和
`core-net5.0.trx` 均无失败。net5.0 的第一次复核曾发现一个契约测试错误地要求
`netstandard2.0` 库资产携带 `RequiresUnreferencedCodeAttribute`；该断言已限定为库实际以
net6.0 或更高目标编译的情况，旧 TFM 仍保留实际运行时回归。CI 矩阵现在在执行旧 TFM 测试前
显式检查对应 `Microsoft.NETCore.App` runtime 已安装；本地通过不代表远程 CI 已运行。

M75/M76 强名称冲突边界在 framework、partial/full trim 和 NativeAOT host-output 本地验证通过：新增未签名但程序集名为
`System.Text.Json` 的依赖及插件模型，`ReflectionTrimThirdPartyPluginProbe` 的
`strong-name-conflict` profile 通过 path/resolver/bytes 三种方式检查依赖版本、空
PublicKeyToken、插件引用 token、可回收加载上下文和方法结果；四类宿主的三种方式均输出
`metadata-preserved`。M76 使用当前工作区 fresh 发布目录：partial trim 为
`benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/m76-publish-partial-third-party-plugin-strong-name-conflict`，
full trim 为
`benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/m76-publish-full-third-party-plugin-strong-name-conflict`，
NativeAOT 为
`benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/m76-publish-nativeaot-third-party-plugin-strong-name-conflict`。
该验证仍不覆盖多插件强名称组合、复杂 linker roots 或任意动态程序集安全。

M77 collectible 缓存生命周期回归在 net8.0 通过：核心项目 `TypeReflectionsCollectibleAttributeTests` 精确筛选为 `12/12`，其中新增 `List<collectible-type>` 构造泛型参数场景，覆盖类型成员、属性访问器、成员路径和属性过滤入口并在 20 轮 GC/finalizer 尝试后确认动态程序集可回收；反射子包 `FullyQualifiedName~Reflection` 筛选为 `173/173`，动态类型专项筛选为 `17/17`，其中卸载前实际通过 `DynamicBase.GetPropertyValue/SetPropertyValue`。核心包和反射子包 Release 构建均为 0 errors。该证据只证明当前已接入旁路的缓存入口，不覆盖第三方适配器、外部自建缓存或所有复杂 AOT/动态程序集组合。

M78 动态类型工厂 collectible 属性类型回归在 net8.0 通过：测试动态生成一个 `RunAndCollect` 公开属性类型，将其作为动态类型属性 schema 进入共享 `SchemaCache`，创建后的 `DynamicBase` 实例完成 null 读写；释放生成类型和 schema 引用后，20 轮 GC/finalizer 尝试确认属性类型程序集可回收。动态类型专项筛选由 `17/17` 提升为 `18/18`。该结果证明 `SchemaEntry` 构建完成后清理 `Lazy<Type>` 不会长期持有 schema，不替代第三方适配器、外部消费者静态缓存及复杂 AOT 组合审计。

M80 可回收元数据判定入口在 net8/netstandard2.0 编译通过：核心包和反射子包分别为 `ReflectionCacheScope` 增加 `MemberInfo`、`MethodBase`、`ParameterInfo` 及属性签名递归判断，构造绑定、特性 reflector、方法签名/命名方法和动态 schema 均改用统一入口；方法返回类型、参数类型、闭合泛型参数和属性/字段类型都会触发旁路。运行时兼容性回归发现并修复了 `ConstructorInfo.GetGenericArguments()` 的 `NotSupportedException` 边界，修复后核心反射 `464/464`、反射子包 `174/174`、动态类型专项 `18/18` 通过。该项证明仓库内已知入口的一致性，不证明第三方适配器或外部自建静态缓存可卸载。

M79 容量结构性矩阵在 net8.0 Release benchmark 通过：容量探针 schema 2 分别对命名方法、方法结构签名、成员路径、绑定标志成员快照和 `DynamicBase` 属性缓存执行 `capacity + 1` 次唯一请求；五个场景均报告容量内准入、恰好一次超限旁路和零失败，整体 `structuralPassed=true`。本次运行的总耗时约 561 ms，仅作为同机趋势证据，不构成未经批准的业务性能阈值；报告同时列出构造函数容量 512、绑定标志成员快照 128、方法/路径/动态基类属性容量 1024、动态 schema 4096 和动态属性限制 255。

M81 缓存生命周期清单校验在本机通过：`Test-ReflectionCacheInventory.ps1` 以 UTF-8 读取 `reflection-cache-inventory.json`，验证核心包、反射子包和动态基类目录中的 29 个静态反射缓存声明、弱键/容量策略及 collectible 旁路标记，并扫描未列入清单的静态缓存声明；报告为 `entries=29`、`unlisted=0`、`passed=true`。`PropertyInfoExtensions.IsInitOnlySetter` 对 collectible `PropertyInfo` 走直接计算路径，不进入共享缓存。该证据只覆盖仓库内已知缓存，第三方适配器、外部消费者缓存和长期委托/闭包仍未纳入生命周期证明。

M82 linker 根描述 preserve 模式矩阵在本机通过：`ReflectionTrimProbe.UnrootedModelsHost` 的 `--root-modes` 通过动态程序集名称检查 `fields`、显式 `<property>`、`methods` 和 `nothing` 四种根描述。self-contained partial trim、full trim 和 NativeAOT host 均输出 `reflection-plugin-trim-boundary:member-trimmed` 与 `reflection-plugin-root-modes:verified`，原有 host 无参数调用保持兼容。矩阵只断言每种模式的目标公开成员和 `nothing` 的成员裁剪；由于 linker 的 `methods` 模式可能通过属性访问器连带保留属性元数据，未把不同成员类别声明为互斥结果。该证据仍不覆盖复杂多插件 root 组合或所有反射入口。

M83 多程序集 linker 根组合矩阵在本机通过：`ReflectionTrimProbe.UnrootedModelsHost` 通过项目引用加载第二个独立程序集 `ReflectionTrimProbe.PluginModels`，并用宿主 `TrimRoots.xml` 同时声明两套 `fields`、显式 `<property>`、`methods` 和 `nothing` 根。`AssemblyMetadata(IsTrimmable=True)` 作为夹具项目元数据，确保项目引用发布时第二程序集实际进入链接器：输入 DLL 为 5632 字节，partial/full linked DLL 为 4608 字节；NativeAOT 将程序集元数据纳入原生宿主，不落地独立 DLL。partial trim、full trim 和 NativeAOT `--root-modes` 均输出 `reflection-plugin-trim-boundary:member-trimmed` 与 `reflection-plugin-root-modes:verified`，且没有新增 IL2037 链接器警告。该证据仍不覆盖任意第三方适配器、外部消费者缓存或无限复杂 root 组合。

M84 泛型/嵌套 linker 根矩阵在本机通过：第二程序集新增 `CompositeGenericRootModel` 泛型定义与 `CompositeNestedRootModel/PreservedNested` 嵌套类型，`TrimRoots.xml` 使用反引号泛型定义名和 `/` 嵌套类型描述；运行时探针分别用 `CompositeGenericRootModel\`1` 和 `CompositeNestedRootModel+PreservedNested` 查询保留成员。partial/full linked DLL 为 5120 字节，NativeAOT 将同一元数据纳入原生宿主；三种模式的 `--root-modes` 均输出 `reflection-plugin-trim-boundary:member-trimmed` 与 `reflection-plugin-root-modes:verified`，NativeAOT 额外的 `member-trimmed` 负向边界也通过。该证据仍不覆盖复杂多插件 root 组合或任意第三方适配器。

M85 参数化 linker signature 矩阵在本机通过：第二程序集新增 `CompositeSignatureRootModel`，宿主 `TrimRoots.xml` 使用 `System.Void .ctor(System.Int32)` 和 `System.Int32 PreservedMethod(System.Int32)` 两个完整签名；运行时探针按参数类型枚举构造函数和重载方法，确认无参构造和 `PreservedMethod(System.String)` 已裁剪。partial/full trim 与 NativeAOT 的 `--root-modes` 均输出 `reflection-plugin-trim-boundary:member-trimmed` 与 `reflection-plugin-root-modes:verified`，三种产物的 `member-trimmed` 负向边界也通过。初次探针直接调用 `Type.GetConstructor/GetMethod` 暴露 IL2075，已改为受控成员枚举并局部抑制动态类型分析警告；signature 修正后的正式发布没有新增 IL2075/IL2037。

M85b 构造函数签名边界在独立 full-trim smoke 中通过：`CompositeSignatureRootModel` 新增
`System.Void .ctor(System.String)`，descriptor 仍只保留
`System.Void .ctor(System.Int32)`；显式 `IsTrimmable` 的两项目隔离宿主输出
`reflection-trim-constructor-signature-root-ok`，外部读取发布 DLL 确认只剩整数构造函数。
正式 `UnrootedModelsHost` 的三宿主矩阵本轮未重新发布，仍受工作区 `obj`/assets I/O 阻断，
因此该证据不推广为 NativeAOT 或完整插件矩阵结论。

M86 三程序集 linker root 组合矩阵在本机通过：宿主新增项目引用 `ReflectionTrimProbe.TertiaryModels`，并在同一 `TrimRoots.xml` 中同时描述 `UnrootedModels`、`PluginModels` 和第三程序集的 fields、显式 property、nothing 根。partial/full trim 的第三程序集 linked DLL 为 4608 字节，三个程序集的动态检查分别输出 `reflection-plugin-root-modes:verified` 与 `reflection-plugin-tertiary-root-modes:verified`；NativeAOT 原生宿主同样通过，并保持 `reflection-plugin-trim-boundary:member-trimmed`。验证过程中发现未带 `PublishTrimmed/TrimMode` 的 `--no-restore` 资产会跳过 linker，已使用包含 trim 属性的 restore 重新发布，未将未裁剪中间产物作为证据。

M87 跨程序集嵌套 linker root 矩阵在本机通过：TertiaryModels 新增 `CrossAssemblyNestedRootModel`，其 `PreservedNested` 继承 PluginModels 的 `CompositeCrossAssemblyBase`，并持有 PluginModels 的 `CompositeCrossAssemblyPayload` 字段；`TrimRoots.xml` 对嵌套类型和两个 PluginModels 类型均使用显式字段 root，同时对未列字段和 `TrimmedNested` 使用 `nothing` root。宿主动态断言检查嵌套类型、跨程序集基类、字段类型及两侧保留/裁剪成员。partial trim 目录 `m87-publish-partial-confirmation` 与 full trim 目录 `m87-publish-full` 均输出 `reflection-plugin-root-modes:verified`、`reflection-plugin-tertiary-root-modes:verified` 和 `reflection-plugin-trim-boundary:member-trimmed`；两个链接程序集大小分别为 PluginModels 5632 字节、TertiaryModels 5120 字节。NativeAOT 目录 `m87-publish-nativeaot` 的原生宿主同样通过，exe 为 2030592 字节。首次 partial 诊断发现 `preserve="fields"` 会保留所有字段，已改为显式字段 root 后进行唯一正式确认；未将失败诊断作为最终证据。

M88 四程序集 linker root 组合矩阵在本机通过：宿主新增独立可裁剪 `ReflectionTrimProbe.QuaternaryModels`，同一 `TrimRoots.xml` 同时描述 UnrootedModels、PluginModels、TertiaryModels 和 QuaternaryModels；第四程序集覆盖 fields、显式 method、嵌套显式 field 和 nothing 根，宿主输出 `reflection-plugin-quaternary-root-modes:verified`，并保持 `reflection-plugin-root-modes:verified`、`reflection-plugin-tertiary-root-modes:verified` 与 `reflection-plugin-trim-boundary:member-trimmed`。partial/full trim 的 QuaternaryModels linked DLL 均为 4608 字节，NativeAOT 原生宿主 exe 为 2032640 字节；三种模式均使用实际 linker 产物运行，未把未裁剪 build 输出作为证据。workflow 的 NativeAOT source map 同步纳入 TertiaryModels/QuaternaryModels。该矩阵扩大了同时加载程序集数量，但不替代同名多版本 AssemblyLoadContext 冲突验证。

M89 第三方插件多冲突组合矩阵在本机通过：`ReflectionTrimThirdPartyPluginProbe` 新增 `--multi-plugin <v1> <v2> <strong-name>` 模式，在同一进程创建三个可回收 `AssemblyLoadContext`，分别加载同名 v1 依赖、同名 v2 依赖和未签名 `System.Text.Json` 强名称冲突依赖。三个上下文均通过 path、`AssemblyDependencyResolver` 和 bytes 三种方式加载，动态断言检查 v1/v2 版本隔离、插件类型不合并、冲突依赖与默认强签名程序集不合并、PublicKeyToken 以及方法返回值；每种方式均输出 `metadata-preserved`。framework 本地正式组合运行、已有 partial/full trim host-output 和已有 NativeAOT host-output 均通过；CI workflow 已为四类宿主接入对应 staging/执行步骤。M89 仍不证明任意数量插件、重复间接依赖、卸载后 GC 稳定性或复杂 linker root 组合的普遍安全性。

M90 多插件卸载生命周期矩阵在本机通过：多插件加载核心现在返回三个 `AssemblyLoadContext` 的 `WeakReference` 快照，执行 `Unload()` 后最多 20 轮 `GC.Collect`/finalizer 检查；只有三个上下文全部不可达时才保留 `metadata-preserved`，回收失败会降级为 `member-trimmed`。重新编译的 probe 为 0 警告/0 错误，framework、已有 partial/full trim host-output 和已有 NativeAOT host-output 的 path/resolver/bytes 均输出 `metadata-preserved`。该证据覆盖三上下文单轮卸载，不替代四个以上插件、多轮加载/卸载、重复间接依赖或复杂 linker root 组合验证。

M91 四插件重复依赖矩阵在本机通过：`--multi-plugin` 保留 v1、同名 v2 和强名称冲突前三个部署语义，并接受第四个及后续独立 v1 副本；本轮四个目录分别部署 v1（直接依赖 v1 + 间接依赖）、v2、强名称冲突依赖和重复 v1。加载核心逐项检查插件程序集及类型归属、直接依赖版本、间接依赖对象不合并、默认强名称程序集不合并，并在卸载后继续验证四个上下文均可回收。framework、已有 partial/full trim host-output 和已有 NativeAOT host-output 的 path/resolver/bytes 均输出 `metadata-preserved`；workflow 已为四类宿主接入第四目录 staging 和四参数调用。该证据仍不覆盖多轮循环、更多不同依赖图或复杂 linker root 组合。

M92 多轮和间接依赖变体矩阵在本机通过：新增 `ReflectionTrimProbe.NonTrimmablePluginTransitiveDependencyVariant`，保持与正常间接依赖相同的程序集名称、版本和类型身份，但使用不同描述特性、方法描述和 `GetBonus()` 返回值；插件 probe 的 `transitive-variant` profile 对直接依赖结果、插件结果、特性元数据和三种加载方式分别断言 variant 值，避免“只加载成功”掩盖错误依赖合并。`--multi-plugin-rounds` 将轮数限制为 1-20，本轮正式取 3 轮；每轮创建 v1、v2、强名称冲突和 variant-v1 四个可回收 `AssemblyLoadContext`，执行 path、`AssemblyDependencyResolver`、bytes 三种方式，随后通过弱引用和最多 20 轮 GC/finalizer 检查卸载。framework、已有 partial/full trim host-output 和已有 NativeAOT host-output 均完成 3 轮 × 3 方式，四类宿主共 36 个组合全部输出 `reflection-third-party-plugin-boundary:multi-plugin:round:{1|2|3}:{path|resolver|bytes}:metadata-preserved`；variant 的直接依赖结果为 24、插件结果为 66、间接方法结果为 13，正常 v1 语义仍保持 18/60/7。workflow 已加入 variant fixture 的构建、四类宿主 staging 和 `--multi-plugin-rounds 3` 调用。该证据只覆盖一个 variant 图和三轮本地组合，不代表任意依赖图、无限长循环或复杂 linker root 组合均安全。

M93 直接/间接依赖图分离矩阵在本机通过：新增 `ReflectionTrimProbe.NonTrimmablePluginDependencyVariant`，保持直接依赖程序集名称、版本和类型身份不变，仅改变直接依赖特性、方法描述和实现值；该 fixture 编译时引用正常 transitive 依赖，正式 staging 的第五目录使用 `dependencyVariant + transitive`，因此只替换直接依赖。第四目录继续使用 `dependency + transitiveVariant`，只替换间接依赖。probe 将第五目录识别为 `dependency-variant`，断言 direct-only 图的直接依赖结果为 24、插件结果为 66、间接方法结果为 7；第四目录断言 transitive-only 图为 24/66/13。每轮创建 v1、v2、强名称冲突、transitive-only 和 direct-only 五个可回收 `AssemblyLoadContext`，执行 path、`AssemblyDependencyResolver`、bytes，并通过弱引用和最多 20 轮 GC/finalizer 检查卸载。framework、已有 partial/full trim host-output 和已有 NativeAOT host-output 各完成 3 轮 × 3 方式，五类宿主共 180 个组合全部输出 `metadata-preserved`；workflow 已补齐四类宿主的 direct-variant 构建、独立 step 变量和五目录调用。该证据仍不覆盖 diamond/多层依赖、直接与间接同时冲突、无限长循环或复杂 linker root 组合。

M94 diamond 依赖图矩阵在本机通过：新增 `ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency` 与同名程序集身份的 `ReflectionTrimProbe.NonTrimmablePluginDiamondModels`；diamond 插件同时引用正常主依赖和 secondary 依赖，两条直接依赖再共享同一正常 transitive 依赖。第六目录 profile 校验插件执行结果 98、主依赖结果 18、secondary 结果 38、transitive 结果 7，确认 secondary 类型/描述/方法元数据、两条直接依赖均引用 transitive，以及当前可回收 `AssemblyLoadContext` 中恰好只有一个 transitive 程序集。每轮创建 v1、v2、强名称冲突、transitive-only、direct-only 和 diamond 六个上下文，执行 path、`AssemblyDependencyResolver`、bytes，随后通过弱引用和最多 20 轮 GC/finalizer 检查卸载；framework、已有 partial/full trim host-output 和已有 NativeAOT host-output 各完成 3 轮 × 3 方式，四类宿主共 216 个内部断言、36 个正式 marker，全部输出 `metadata-preserved`。workflow 已加入 secondary/diamond 构建、四类宿主 staging 和六目录调用。该证据仍不覆盖更深层级/更多分支、直接与间接同时冲突、无限长循环或复杂 linker root 组合。

M95 直接与间接依赖同时冲突矩阵在本机通过：第七目录同时部署普通插件、`ReflectionTrimProbe.NonTrimmablePluginDependencyVariant` 和 `ReflectionTrimProbe.NonTrimmablePluginTransitiveDependencyVariant`，不复制正常 transitive 版本，从而让两层同名依赖同时切换到 variant。`mixed-variant` profile 校验 direct dependency 描述/方法元数据和结果 30、transitive 描述/方法元数据和结果 13、插件结果 72，并确认两层程序集都属于当前可回收 `AssemblyLoadContext`，同名 transitive 程序集数量为 1 且 direct assembly 保留对 transitive 的引用。每轮创建 v1、v2、强名称冲突、transitive-only、direct-only、diamond 和 mixed 七个上下文，执行 path、`AssemblyDependencyResolver`、bytes，随后通过弱引用和最多 20 轮 GC/finalizer 检查卸载；framework、已有 partial/full trim host-output 和已有 NativeAOT host-output 各完成 3 轮 × 3 方式，四类宿主共 252 个内部断言、36 个正式 marker，全部输出 `metadata-preserved`。workflow 已加入四类宿主 mixed staging 和第七目录调用。该证据仍不覆盖三层以上依赖、更多分支/循环、无限长循环或复杂 linker root 组合。

M96 三层共享依赖图矩阵在本机通过：新增 `ReflectionTrimProbe.NonTrimmablePluginDeepDependency` 叶依赖和保持 `ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency` 程序集身份的深层变体；第八目录部署 diamond 插件、主/secondary 直接依赖、深层 transitive 变体和 leaf，profile 校验深层 transitive 结果 14、leaf 结果 5、主依赖结果 25、secondary 结果 45、插件结果 112，并确认深层引用边、同名 transitive 程序集恰好一个以及所有依赖归属当前可回收 `AssemblyLoadContext`。framework、已有 partial/full trim host-output 和已有 NativeAOT host-output 各完成 8 个目录 × 3 轮 × path/resolver/bytes，四类宿主共 288 个内部断言、36 个正式 marker，全部输出 `metadata-preserved`；workflow 已接入两个深层夹具、四类宿主 staging 和第八目录调用。该证据仍不覆盖更多分支/循环、更长轮次或复杂跨程序集 linker root 组合。

M97 三分支深层依赖图矩阵在本机通过：新增 `ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency` 和三分支 `ReflectionTrimProbe.NonTrimmablePluginTripleDiamondModels`；第九目录部署 triple-diamond 插件、主/secondary/tertiary 三条直接依赖、深层 transitive 变体和 leaf，profile 校验 tertiary 结果 55、主依赖结果 25、secondary 结果 45、深层 transitive 结果 14、插件结果 167，并确认三条引用边、同名 transitive 程序集恰好一个以及所有依赖归属当前可回收 `AssemblyLoadContext`。framework、已有 partial/full trim host-output 和已有 NativeAOT host-output 各完成 9 个目录 × 3 轮 × path/resolver/bytes，四类宿主共 324 个内部断言、36 个正式 marker，全部输出 `metadata-preserved`；workflow 已接入两个三分支夹具、四类宿主 staging 和第九目录调用。该证据仍不覆盖更多分支/循环、更长轮次或复杂跨程序集 linker root 组合。

M98 可配置长轮次矩阵在本机通过：workflow_dispatch 新增 `multi_plugin_rounds` 输入，默认值为 3，`probes` job 通过 `REFLECTION_MULTI_PLUGIN_ROUNDS` 传入，四类宿主执行块均以不变量校验限制为 1-20；PR 默认路径仍保持 3 轮。因工作区当时存在离线 NuGet 审计阻断和并发构建进程，本轮使用同一 SDK 8.0.425 的 Roslyn 直接编译当前 `ReflectionTrimThirdPartyPluginProbe/Program.cs` 到隔离目录，引用 net8.0 reference pack 与现有已验证 Bing 程序集，未覆盖生产文件。5 轮正式运行对 framework、partial trim、full trim、NativeAOT 四个既有 host-output 各执行 9 个部署目录 × path/resolver/bytes，四宿主均 `exit=0`，每宿主 15 个 marker，合计 60 个 `reflection-third-party-plugin-boundary:multi-plugin:round:{1..5}:{path|resolver|bytes}:metadata-preserved`，按 9 × 5 × 3 × 4 计 540 个内部断言。该证据仍不代表 10 轮以上、更多分支/循环或复杂 linker root 组合的普遍安全性。

M99 跨程序集 linker root 组合在本机通过：新增 `ReflectionTrimProbe.QuaternaryModels` 对 `ReflectionTrimProbe.TertiaryModels` 的单向项目引用，第四程序集的嵌套 `PreservedNested` 持有第三程序集 `TertiaryFieldsRootModel` 字段，`TrimRoots.xml` 显式保留该字段、另一个嵌套字段和 `System.Int32 PreservedMethod(System.Int32)` 完整签名，并以 `nothing` 根定义负向嵌套类型。宿主运行时同时检查字段类型所属程序集、保留字段/方法、未声明字段/方法和 `nothing` 类型边界。隔离树按 partial、full、NativeAOT 各自带 `PublishTrimmed/TrimMode` 还原并真实发布，三个宿主均 `exit=0`，分别输出 `reflection-plugin-root-modes:verified`、`reflection-plugin-tertiary-root-modes:verified`、`reflection-plugin-quaternary-root-modes:verified` 与 `reflection-plugin-quaternary-cross-root-modes:verified`，合计 12 个 marker；partial/full 的 QuaternaryModels 与 TertiaryModels linked DLL 均为 5120 字节，NativeAOT 按预期将模型元数据内嵌到原生宿主、不生成独立模型 DLL。该证据覆盖一个单向跨程序集 root 图，不替代循环引用、更多程序集/分支、超长 root 描述或容量边界验证。

M100 第五程序集跨分支 linker root 矩阵在本机通过：新增可裁剪 `ReflectionTrimProbe.QuinaryModels`，同时项目引用 `ReflectionTrimProbe.QuaternaryModels` 与 `ReflectionTrimProbe.TertiaryModels`，其 `CrossBranchRootModel/PreservedNested` 持有两个分支字段，分别指向第四、第三程序集的字段 root 类型；`TrimRoots.xml` 显式保留两个字段、嵌套字段和 `System.Int32 PreservedMethod(System.Int32)` 完整签名，并以 `nothing` 根定义负向嵌套类型。宿主运行时检查两个字段所属程序集/完整类型名、保留成员、未声明成员和 `nothing` 类型边界。隔离树按 partial、full、NativeAOT 各自带 `PublishTrimmed/TrimMode` 还原并真实发布，三个宿主均 `exit=0`，各输出 `reflection-plugin-root-modes:verified`、`reflection-plugin-tertiary-root-modes:verified`、`reflection-plugin-quaternary-root-modes:verified`、`reflection-plugin-quaternary-cross-root-modes:verified`、`reflection-plugin-quinary-cross-branch-root-modes:verified` 与 `reflection-plugin-quinary-root-modes:verified`，合计 18 个 marker；partial/full 的 QuaternaryModels、TertiaryModels、QuinaryModels linked DLL 均为 5120 字节，NativeAOT 按预期将模型元数据内嵌到原生宿主、不生成独立模型 DLL。workflow 同步补齐第五程序集路径、构建、产物检查、带裁剪属性的 NativeAOT 还原和 source map。该证据覆盖一个五程序集双分支汇合图，不替代真实循环依赖、更多分支/程序集、超大 root 描述或容量边界验证。

M101 运行时 resolver 循环边界在本机通过：`ReflectionTrimThirdPartyPluginProbe` 新增 `RuntimeCycleProbe` 和 `--runtime-cycle <first-directory> <second-directory>`，两个独立部署目录分别进入 collectible `AssemblyLoadContext`，并以 peer `AssemblyDependencyResolver` 构造显式 A→B→A 解析图。每个节点的 `active/visited` 集合及 32 节点上限阻止无界递归；探针先调用插件 `Execute(int)` 触发直接/间接依赖加载，再检查两组插件程序集、类型和两层依赖均归属各自 ALC，卸载后全部程序集与上下文弱引用均回收。使用同一 SDK 8.0.425 的隔离 Roslyn 编译结果执行 framework host，path/resolver/bytes 三种方式均输出 `reflection-third-party-plugin-boundary:runtime-cycle:{mode}:metadata-preserved`，进程退出码为 0。workflow 已为 framework、partial trim、full trim、NativeAOT host-output 接入同一入口；当前本地正式证据只覆盖 framework，且该夹具明确验证运行时 resolver 循环，不等同于真实互相 AssemblyRef 的程序集循环。

M102 真实互相 AssemblyRef 循环边界在本机通过：当前 .NET 8 SDK 未提供 `ilasm`/`ildasm`，因此夹具脚本 `Build-RuntimeCycleFixtures.ps1` 使用同一 SDK 的 Roslyn `csc.dll` 分阶段生成 A 占位程序集、引用 A 的 B，再覆盖生成引用 B 的同名 A，最终文件的元数据形成真实 A→B→A `AssemblyRef` 图；编译响应文件写入临时目录并在 `finally` 中清理，不污染部署输出。`CycleLoader` 在 path/resolver/bytes 三种入口分别输出 `runtime-cycle-assemblyrefs:{mode}:verified` 与 `runtime-cycle-collectible:{mode}:verified`，检查双方 `GetReferencedAssemblies()`、`Peer` 字段类型、同一 collectible ALC 和上下文/程序集弱引用回收。隔离编译的 `ReflectionTrimThirdPartyPluginProbe --assemblyref-cycle <fixture-directory>` 随后再次校验同一三种加载模式，正式输出 `reflection-third-party-plugin-boundary:assemblyref-cycle:{path|resolver|bytes}:metadata-preserved`，退出码为 0。workflow 已接入夹具构建和一次集中 framework 执行；partial/full/NativeAOT 宿主仍未把独立循环夹具强行复制到发布物中，因此不宣称这些宿主已完成该专项证据。该实现验证真实 AssemblyRef 元数据循环，不等同于 MSBuild `ProjectReference` 循环或任意插件图安全。

M103 真实 AssemblyRef 分支边界在本机通过：在保留 M102 A↔B 双向循环的前提下，fixture 新增 `RuntimeCycleFixture.C`，由 C 的 `Peer` 字段生成真实 C→B `AssemblyRef`；同一 `CycleLoader` 在 path/resolver/bytes 三种入口检查 A→B、B→A 和 C→B 三条元数据边、A/B/C 字段类型解析、三个程序集属于同一 collectible ALC，随后验证上下文和程序集弱引用均可回收，并输出 `runtime-cycle-branch:{mode}:verified`。隔离编译的 `ReflectionTrimThirdPartyPluginProbe --assemblyref-branch <fixture-directory>` 再次执行三种加载模式，正式输出 `reflection-third-party-plugin-boundary:assemblyref-branch:{path|resolver|bytes}:metadata-preserved`，退出码为 0；workflow 复用 M102 构建步骤并接入一次集中 framework 执行。该证据只覆盖一个第三程序集分支，不等同于任意多分支、超大 root 或资源容量安全。

M104 三部署物 10 轮长循环边界在本机通过：使用已有 framework 夹具 `m70-framework-third-party-plugin`（普通 v1）、`m71-framework-version-conflict`（版本冲突 v2）和 `third-party-plugin-strong-name-conflict`（强名称冲突）重建三个临时部署目录，并将 `ReflectionTrimThirdPartyPluginProbe` 的 `deps.json` 所声明运行时依赖一并复制到隔离 probe 目录。使用当前 Roslyn 编译的 probe 执行 `--multi-plugin-rounds 10`，每轮依次运行 path/resolver/bytes，共 30 个正式 marker，全部为 `reflection-third-party-plugin-boundary:multi-plugin:round:{1..10}:{path|resolver|bytes}:metadata-preserved`，退出码为 0；每次仍检查插件/依赖类型隔离、版本/强名称边界和 collectible `AssemblyLoadContext` 弱引用回收。该证据只证明三部署物组合在 10 轮下稳定，不代表九目录复杂依赖图、更高插件数量或资源容量上限已完成。

M108 程序集名称入口边界在隔离 framework smoke 中通过：`Reflections.GetAssembly(string)` 对 null、空白和格式非法名称在调用程序集加载前给出稳定的参数异常，对有效的当前程序集名称仍返回同一程序集；隔离 Roslyn 编译并运行四种无效输入和一种有效输入，输出 `isolated-assembly-name-validation:passed`。本轮未重复触发常规项目 runner，避免离线 NU1900 后的 SDK/testhost 悬挂环境噪声。

M112 构造缓存生命周期边界在隔离 smoke 中通过：`StrictConstructorBinder` 对 collectible 目标构造函数和签名元数据使用旁路委托，不把解析结果写入长期签名缓存或 fast path；普通构造查询同时完成缓存路径 smoke。当前源代码隔离编译为 0 errors，net8 smoke 输出 `collectible-constructor-cache:passed`。trim/AOT 和未 root 插件探针已补充接口/抽象类型 `HasParameterlessConstructor` 负向断言，但完整项目 runner 本轮因 `NU1900` 后 SDK/testhost 无终态而未形成新鲜测试结果。

M111 参数less 构造探测边界在隔离 smoke 中通过：`TypeVisit.HasParameterlessConstructor` 现在将接口、抽象类等不可实例化类型的 `ArgumentException` 归一化为 `false`，null 仍保留 `ArgumentNullException("type")`；当前源代码隔离编译为 0 errors，net8 smoke 输出 `parameterless-constructor-boundary:passed`。`GetParameterlessConstructor` 的显式异常契约未改变。本轮未重复完整项目 runner，复用既有 `MSB3554`/`NU1900` 环境阻断证据。

M110 netstandard2.0 动态代码能力边界在隔离环境通过：新增 `ReflectionRuntimeFeatures`，net5+ 直接读取 `RuntimeFeature.IsDynamicCodeSupported`，netstandard2.0 使用运行时公开属性探测；`TypeReflections` 与 `Reflections` 特性入口不再在 netstandard 资产中被编译条件固定到第三方 `Reflection.Emit` 路径。真正的 netstandard2.0 源集（包含两个特性入口）编译为 0 errors，由 net8 宿主加载后输出 `netstandard-type-reflections-boundary:passed`；独立 netstandard 能力库由 net8 宿主加载并输出 `netstandard-runtime-feature-boundary:passed`。现代 TFM 回归源代码隔离编译为 0 errors。M116 又用最小 net8.0/win-x64 NativeAOT host 实际执行 `IsDynamicCodeSupported=false` 分支并验证 Type/Method/ParameterInfo attribute fallback；更广泛的 NativeAOT 运行矩阵与依赖/工具链诊断仍按待确认项管理。

M109 第三方特性 reflector 集合边界在隔离 framework smoke 中通过：`TypeReflections` 的存在性、单项、筛选集合和全量集合实现统一把 provider 返回的 null 数组视为空集合，并跳过 null reflector 元素；正常 AspectCore 2.4.0 reflector 集合仍走原有私有令牌快路径。隔离核心反射编译无错误，smoke 对 null 数组和 null 元素两种 provider 均输出 `null-reflector-boundary:passed`。本轮未把异常 provider 行为推广为第三方库的完整兼容性保证，私有字段漂移和 netstandard2.0 动态代码限制仍保留为兼容性监控项。

M107 动态类型 null schema 边界在隔离 framework smoke 中通过：`TypeFactory.CreateType` 在 schema 快照排序后将 `null` 属性类型规范化为 `System.Object`，快速缓存匹配同样按 `object` 规范化，生成类型的公开属性先写入 `null`、再写入字符串均可读回；属性名/非法类型/255 属性/存活 schema 容量校验路径保持不变。新增 `DynamicTypeFactoryTests.CreateDynamicType_NullPropertyType_InfersObjectAndAllowsNull`，同一 SDK Roslyn 直接编译当前反射模块并通过 PowerShell 反射调用验证，输出 `System.Object` 与 `isolated-null-schema:passed`。常规项目 runner 在 NU1900 后持续产生低 CPU SDK/testhost 子进程，本轮已终止并将项目级 build/test 记为环境阻断。

M105 真实 AssemblyRef 多分支边界在本机通过：在保留 M102 的 A↔B 双向循环和 M103 的 C→B 分支前提下，fixture 新增 D 程序集并生成 D→A、D→C 两条真实 `AssemblyRef`；`CycleLoader` 与隔离编译的 `ReflectionTrimThirdPartyPluginProbe --assemblyref-multi-branch` 在 path/resolver/bytes 三种入口检查 A→B、B→A、C→B、D→A、D→C 五条元数据边、A/B/C/D 字段类型映射、四程序集属于同一 collectible ALC 且每个程序集身份唯一，随后验证上下文和四个程序集弱引用均可回收。三种方式均输出 `reflection-third-party-plugin-boundary:assemblyref-multi-branch:{path|resolver|bytes}:metadata-preserved`，退出码为 0；workflow 已接入同一 fixture 构建后的集中 framework 执行。该证据扩展了多分支/四程序集图，不代表更大依赖图、超大 root 描述或资源容量边界已经完成。

M106 真实 AssemblyRef 深层分支边界在本机通过：在保留 M105 的 A↔B、C→B、D→A/D→C 图前提下，fixture 新增 E 程序集并生成 E→D、E→B 两条真实 `AssemblyRef`；`CycleLoader` 与隔离编译的 `ReflectionTrimThirdPartyPluginProbe --assemblyref-deep-branch` 在 path/resolver/bytes 三种入口检查 A→B、B→A、C→B、D→A、D→C、E→D、E→B 七条元数据边、A/B/C/D/E 字段类型映射、五程序集属于同一 collectible ALC 且每个程序集身份唯一，随后验证上下文和五个程序集弱引用均可回收。三种方式均输出 `reflection-third-party-plugin-boundary:assemblyref-deep-branch:{path|resolver|bytes}:metadata-preserved`，退出码为 0；workflow 已接入同一 fixture 构建后的集中 framework 执行。该证据扩展了深层跨分支/五程序集图，不代表更大依赖图、超大 root 描述或资源容量边界已经完成。

M100a 多插件资源边界在本机通过：`ReflectionTrimThirdPartyPluginProbe` 为 `--multi-plugin` 和 `--multi-plugin-rounds` 定义 32 个可回收插件上下文的固定上限，并在路径规范化、`Directory.Exists` 和任何程序集加载前拒绝第 33 个目录；两个拒绝命令均返回退出码 `2`，输出 `reflection-third-party-plugin-boundary:multi-plugin:resource-rejected:contexts:33>32`。`Test-MultiPluginCapacity.ps1` 使用 33 个不存在目录执行隔离 Roslyn probe，输出 `reflection-third-party-plugin-capacity:verified:max-contexts=32`；同一编译 probe 使用 9 个真实 profile 目录并以合法 v1 副本补齐到 32 个上下文，在新建临时 staging 中对 framework、partial trim、full trim 和 NativeAOT 四类宿主均执行 `--multi-plugin` 单轮及 `--multi-plugin-rounds 3`，每类宿主的 path/resolver/bytes 正向 marker 全部为 `metadata-preserved`，并输出 `reflection-third-party-plugin-capacity:positive-admission:verified:contexts=32:rounds=3:markers=9`，四类宿主命令均退出码 `0`。该上限只证明验证探针的有界输入、拒绝路径不触发加载以及当前复杂夹具在上限处可准入，不推广为生产插件宿主容量或任意动态 AssemblyLoadContext 图的安全结论。

M100b 复杂 profile 长轮次边界已实现并在四类宿主通过：新增 `Test-MultiPluginLongRounds.ps1`，强制接收 9 个真实 profile 目录；framework smoke 执行 `--multi-plugin-rounds 20`，逐轮校验 path/resolver/bytes 共 60 个 `metadata-preserved` marker。复用工作区已有 m76 partial/full/NativeAOT host-output 分别执行 10 轮，每类均返回 `0` 并通过 30 个 marker 校验；四类宿主 workflow 同步接入相同脚本，默认 PR 轮数仍为 3。该边界不推广为任意更高轮次、更多依赖图或生产容量的普遍安全性。
