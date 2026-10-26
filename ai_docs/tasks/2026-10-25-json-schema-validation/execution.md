# JSON Schema 校验执行记录

更新日期：2026-10-26。设计：[design.md](design.md)。计划：[plan.md](plan.md)。用户已授权实现并继续修复确认问题。

当前状态：F1—F6 均为 CLOSED，OPEN_ACTIONABLE = 0。本轮修复、验证和独立复核全部完成。

## 范围与变更影响

- 独立可选包 Bing.Utils.JsonSchema，net8.0，精确依赖 JsonSchema.Net [9.4.1]；不引用 Bing.Utils 核心项目。
- 已完成 string/JsonElement 校验、四种方言、format 选项、内部/跨文档/动态/递归引用、只读分层诊断、配置快照、实例同步及禁止自动网络/文件加载。
- 本轮 ChangedFiles：SchemaCompiler、SchemaDocumentStore、SchemaResourceAdapter、SchemaReferenceAdapter、SchemaErrorLocations 及三组回归测试。
- ChangedProjects：Bing.Utils.JsonSchema / Bing.Utils.JsonSchema.Tests。ChangedRuntimePaths：资源准备、引用求值和诊断位置。RiskLevel：MEDIUM。
- ChangedPublicContracts / ChangedTFMs / ChangedProviders / ChangedBuildPackaging / ChangedBenchmarkHarness：无。ChangedDocsOnly：否。
- AffectedDependents：模块测试与独立本地 NuGet 消费者。测试遵循 test-project-boundary，全部位于模块专属测试项目。
- 主代理负责生产实现和最终验证；luna_worker 负责独立测试及只读复核。基线 9258eab；工作区其他任务修改不属于本轮范围。

## Finding 状态

| Finding | 状态 | 修复与验收 |
|---|---|---|
| F1 外部动态引用回退缺失 | CLOSED | 无外层覆盖时使用初始目标；本轮回归通过 |
| F2 检索 URI 别名误拒绝 | CLOSED | 按规范资源 URI 查找动态锚点；本轮回归通过 |
| F3 布尔 Schema 位置丢失 | CLOSED | 保存实际目标位置；本轮回归通过 |
| F4 方言非空片段误接受 | CLOSED | 拒绝非空片段，兼容空片段；本轮回归通过 |
| F5 引用同级规则未遵循各资源方言 | CLOSED | 按实际方言处理同级规则；私有文档准备在引用解析前完成 |
| F6 内嵌资源路径未正确重置 | CLOSED | 按文档结构计算资源内路径，进入不同资源时归零 |

F5 通过私有求值入口消除引擎 9.4.1 的全局方言开关影响；旧方言只忽略同级求值，保留结构子节点供引用寻址。显式方言、默认方言、跨资源方言及宿主全局内存资源均有回归。

独立复核补充确认：在原生引用解析之后复制全局资源仍可能把共享 A→B 引用重绑定到实例内的 B。最终采用 SchemaDocumentStore，通过公开 BuildContext.From、BuildNode、IBaseDocument 接口，先登记显式文档及内嵌资源，再创建全局资源私有副本，最后统一解析引用。副本保留实际方言、规范 URI、检索别名；带相对路径的 $id 先规范化；旧方言 #anchor 形式保留原始文本。全局节点、引用目标、handlers、路径及别名保持不变的回归通过。

F6 使用公开高级 PathFromResourceRoot 属性修正资源内路径，并为 false 节点保存诊断位置；不修改引擎共享布尔单例。文档指针支持标准结构、未知关键字和容器自身，保留原始输入及公开 API。兼容适配针对固定 9.4.1，升级时须重跑本模块回归。

## 最终验证

| 验证 | 结果 |
|---|---|
| Release 构建 | PASS；0 警告、0 错误 |
| F5/F6 定向回归 | PASS；43/43 |
| 模块完整测试 | PASS；449/449，0 失败、0 跳过 |
| 固定官方 Draft 2020-12 样例 | 上述测试中 330 项通过 |
| 封装及回归测试 | 上述测试中 119 项通过 |
| NuGet / 符号包 | PASS；packages-fix2-complete 中生成 1.5.0 nupkg/snupkg |
| 独立 NuGet 消费者 | PASS；14 项，exit_code=0；独立缓存确认还原自本轮新包 |
| UTF-8 / 文本格式 | PASS；28 个源码、测试和任务文档文件 |
| 限定 git diff --check | PASS |
| 独立复核 | PASS；F5/F6 均 CLOSED，旧方言锚点增量复核通过 |

命令：

```powershell
dotnet test tests/Bing.Utils.JsonSchema.Tests/Bing.Utils.JsonSchema.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~JsonSchemaValidatorDialectResourceTests|FullyQualifiedName~JsonSchemaValidatorGlobalDialectTests" --logger trx --results-directory artifacts/json-schema-validation/fix2-private-targeted --nologo
dotnet test tests/Bing.Utils.JsonSchema.Tests/Bing.Utils.JsonSchema.Tests.csproj -c Release --no-restore --logger trx --results-directory artifacts/json-schema-validation/fix2-complete --nologo
dotnet pack src/Bing.Utils.JsonSchema/Bing.Utils.JsonSchema.csproj -c Release --no-build --no-restore -o artifacts/json-schema-validation/packages-fix2-complete --nologo
dotnet run --project artifacts/json-schema-validation/consumer-fix2-complete/Consumer.csproj -c Release --nologo
```

最终证据（相对 artifacts/json-schema-validation）：

- 定向：fix2-private-targeted/jianx_LAPTOP-2V5T48RH_2026-10-26_08_31_41.trx。
- 全量：fix2-complete/jianx_LAPTOP-2V5T48RH_2026-10-26_08_40_23.trx。
- 包消费：consumer-fix2-complete/run-result.json，独立还原缓存 consumer-packages-fix2-complete。
- 包：packages-fix2-complete。

缺陷复现及中间证据保留：

- fix2-red/*08_06_20.trx：初始 31 项中 19 失败、12 通过。一个夹具把 Draft 7 的 $id 与 $ref 同级，导致 $id 按规范被忽略；已将引用移入属性节点并保持验收预期，另补 4 个目标文档方言用例。
- fix2-global-red/*08_22_03.trx：全局资源 6 项中 2 失败，确认旧方言同级规则问题。
- fix2-global-isolation-red/*08_30_22.trx：8 项中 2 失败，确认全局引用目标被重绑定。最终私有文档流程修复后全部通过。
- fix2-pointer-red/*08_33_53.trx：引用整个 $defs 容器时错误返回父节点；修复指针回退判断后通过。另 2 个未知关键字指针基线保持通过。
- anchor-probe：Draft 7 根 $id:#node 原生通过、包装创建失败，定位为规范化破坏锚点登记；保留片段形式后对照均通过，Draft 6/7 两个回归均通过。
- 中间的 436、442、444、447 项通过记录及对应包目录保留，最终交付证据以上述 449 项及 packages-fix2-complete 为准。

历史第 1 轮 F1—F4：401 项通过，见 fix1-full/jianx_LAPTOP-2V5T48RH_2026-10-25_22_14_24.trx；定向 23 项、包消费 11 项通过。官方样例固定提交 89e83a74014617ffbe79a56d83d14d054cd11417，来源与 MIT 许可证见 Fixtures/SOURCES.md、LICENSE。包依赖和解决方案集成复用前轮核查证据。

## 进度与停止条件

- Completed：J01—J06、F1—F6 修复和独立关闭；最终构建、模块回归及新包消费验证。
- Open Actionable：0。
- Blocked Approval / Blocked External：无。
- Not Applicable：历史同构 before、性能/资源门禁、无关 solution 全量回归。
- Accepted Limitations：仅 net8.0；DOM 求值不承诺硬资源隔离；不隔离宿主并发直接操作引擎全局注册表；官方依赖二进制有额外许可条款。
- Verified Boundaries：449 项模块测试、14 项最终包消费检查。
- Deferred：Stream/NDJSON、DTO Schema 生成、ASP.NET Core 接入、旧 TFM。
- Next Action：本轮完成，停止自动修复；只有新确认的 OPEN_ACTIONABLE 才重新进入修复。
- 无暂存、提交、推送、版本升级或发布；无 UAW taskId/stageRunId，不伪造 StageResult。
