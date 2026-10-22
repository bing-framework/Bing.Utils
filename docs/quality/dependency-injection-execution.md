# DependencyInjection 执行记录

更新时间：2026-10-15。依据用户批准的 DependencyInjection 增强方案，按 DI-01～DI-04 实施；开始时保留工作区全部 19 个已有变更，不暂存、提交或推送。

## 范围与进度

| 条目 | 实现及验收标准 | 当前状态 |
| --- | --- | --- |
| DI-01 | 显式接口批量注册、三策略、原生去重及失败原子性 | CLOSED |
| DI-02 | 有限程序集确定性扫描、标准开放泛型校验 | CLOSED |
| DI-03 | 有限闭合服务结构推导、约束及歧义检查 | CLOSED |
| DI-04 | 现有模块项目测试、四生产 TFM、net8 测试、本地包及中文文档 | CLOSED |

## 影响分析

- ChangedProjects：DependencyInjection、DependencyInjection.Tests。ChangedFiles：四个生产源文件、两个项目文件、三个测试源文件及 DI 文档/索引/总账。
- ChangedPublicContracts：六个扩展入口和策略枚举。ChangedRuntimePaths：仅应用启动注册，不提供解析时推导。ChangedProviders：基于 Microsoft 原生描述符，实例化及释放交给容器。
- ChangedTFMs：无；ChangedBuildPackaging：生产仅添加 Abstractions 8.0.2，现有测试添加官方容器 8.0.1。其他模块依赖及版本无变化。
- ChangedBenchmarkHarness：无；ChangedDocsOnly：否；AffectedDependents：新 API 调用者，当前无生产模块调用。RiskLevel：MEDIUM（冲突原子性和结构推导）。
- 依据 `test-project-boundary/refs/test-project-boundary.md`，主要断言目标为 DI 扩展行为，新增测试归属现有 `Bing.Utils.DependencyInjection.Tests`，无迁移、混合测试或新测试工程。
- 主代理负责实现、集成和验证；Luna 只负责边界清晰的模块测试。执行使用 execute-plan 与 test-project-boundary 技能，用户本轮批准生产实现及两个指定依赖。

## 验证与证据

以下命令均针对批准的 DI 范围；不添加工作流、矩阵或基线。实际测试额外使用 `--results-directory TestResults/todo-di --logger 'trx;LogFileName=dependency-injection-net8.trx'` 保存证据。

```powershell
dotnet restore tests/Bing.Utils.DependencyInjection.Tests/Bing.Utils.DependencyInjection.Tests.csproj --source C:/Users/jianx/.nuget/packages
dotnet build src/Bing.Utils.DependencyInjection/Bing.Utils.DependencyInjection.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet test tests/Bing.Utils.DependencyInjection.Tests/Bing.Utils.DependencyInjection.Tests.csproj -c Release -f net8.0 --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet pack src/Bing.Utils.DependencyInjection/Bing.Utils.DependencyInjection.csproj -c Release --no-build --no-restore -o TestResults/todo-di/packages
```

L0：四生产 TFM Release 编译成功，零警告、零错误；本轮 19 个文件严格 UTF-8 解码、两份 DI 文档的三个本地链接、DocFX JSON 及 `git diff --check` 通过。L1/L2：net8 模块测试 38/38 通过，无失败、无跳过，TRX 为 `TestResults/todo-di/dependency-injection-net8.trx`。覆盖三生命周期、作用域隔离/释放、三策略、原生类型/实例/工厂/keyed 去重、无实例化、扫描顺序/过滤/失败、开放泛型解析/约束，以及复杂映射和歧义。L3：真实容器的解析/释放行为由模块测试覆盖；本地包核验四个 TFM DLL 与四组依赖，每组仅含 Abstractions 8.0.2，无容器实现或 SourceLink 传递依赖。产物为 `TestResults/todo-di/packages/Bing.Utils.DependencyInjection.1.5.0.nupkg` 及同名 `.snupkg`。L4/L5：不运行；无既有生产调用方或其他模块运行路径变更，不运行全量套件、trim/AOT、性能或资源探针。此前反射/图像/FTP 的记录保持原状态，不当作 DI 证据。

首次测试编译因缺少 `System.Collections.Generic` using 失败，补齐后上述测试通过，未改变断言规避失败。歧义夹具 `AmbiguousPair<T> : IPair<T,int>, IPair<string,T>` 正常编译，实际验证同一请求产生两个不同闭合实现时拒绝整批。

首次包核验发现公共 props 将 SourceLink 工具传递成包依赖；只在 DI csproj 将既有工具标为 `PrivateAssets=all`，重新 restore/build/pack，其他模块保持不变。包版本沿用既有 1.5.0，不发布。打包提示缺少包内 README，保留公共打包约定，使用本仓库模块文档。

## 边界与剩余事项

- ACCEPTED_LIMITATION：有限闭合服务、结构推导不含变体、不能推导的参数不猜测；同一集合不支持并发注册。注册失败原子性针对输入/扫描/冲突校验，不对自定义集合写入故障提供事务。
- NOT_APPLICABLE：空工程没有等价历史性能能力，不建立 before/after 基线。
- 非本轮范围：keyed 注册入口、装饰器、全进程扫描、依赖自动注册、任意扫描裁剪/NativeAOT 兼容，以及旧测试运行时执行。
- 无独立配置清理；DocFX 仅移除 DI 空工程时期的排除项，不执行文档站生成或发布。

使用说明及四类示例见 [模块文档](../modules/Bing.Utils.DependencyInjection.md)；其他模块全部剩余任务及外部阻塞见 [TODO 总账](todo-execution.md)。

## 本轮文件

- 新增生产：`src/Bing.Utils.DependencyInjection/Bing/DependencyInjection/` 下 `BingServiceCollectionExtensions.cs`、`ServiceRegistrationStrategy.cs`、`ServiceRegistrationBuilder.cs`、`GenericImplementationMatcher.cs`。
- 新增测试：`tests/Bing.Utils.DependencyInjection.Tests/Bing/DependencyInjection/` 下 `BingServiceCollectionExtensionsTests.cs`、`ClosedGenericImplementationTests.cs`、`DependencyInjectionFixtures.cs`。
- 更新项目：生产及模块测试各自的 `Bing.Utils.DependencyInjection*.csproj`，仅指定依赖及本模块 SourceLink 私有元数据。
- 新增本执行记录；更新 `docs/modules/Bing.Utils.DependencyInjection.md`、`docs/modules/README.md`、`docs/README.md`、`docs/00-overview.md`、`docs/00-capability-map.md`、`docs/00-documentation-audit.md`、`docs/01-module-catalog.md`、`docs/docfx.json`、`docs/quality/todo-execution.md`。

## 收口状态

本轮 DI 实现 4/4，批准的本地验收门禁 3/3（四 TFM 编译、net8 测试、本地包核验），均 PASS。Completed：DI-01～DI-04；Open Actionable：无；Blocked Approval / Blocked External：DI 本轮无；Not Applicable：历史等价基线；Accepted Limitations：见上节；Verified Boundaries / Deferred：无新增。主代理 Codex 使用宿主默认模型，子任务为 `luna_worker`，未覆盖宿主模型设置。

No-Progress Check：CHANGED；Next Action：STOP，本轮批准范围完成。其他模块的外部阻塞、待决策或推迟项保留在总账，不因 DI 通过而关闭。仅有包内 README 建议提示，不影响本轮验收；未修改通用打包规则，也未发布包。
