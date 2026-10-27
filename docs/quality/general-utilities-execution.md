# 通用工具主线 B 实施记录

日期：2026-10-27。计划：[plan.md](../../ai_docs/tasks/2026-10-27-general-utilities/plan.md)。使用方法：[通用工具指南](../reference/general-utilities-guide.md)。

## 范围与交付

| 单元 | 交付 | 状态 |
| --- | --- | --- |
| U0 | 保存计划，两个包和两个测试项目加入解决方案，固定 Cronos 0.13.0 / DiffPlex 1.7.0 | 完成 |
| U1 | 按 key 隔离、引用计数回收、取消/超时、幂等租约 | 完成 |
| U2 | 共享正在执行的任务、独立等待取消、异常与取消终态、回收重试 | 完成 |
| U3 | 五段/六段 Cron、中文错误、UTC、显式时区、有限枚举、DST | 完成 |
| U4 | DiffPlex 内部适配、显式工作栈、预算/取消、不可变结果和规模验证 | 完成 |
| U5 | 中文文档、四目标消费者、包结构与实际包消费、最终回归 | 完成 |

核心包仅新增 `Bing/Threading/KeyedAsyncLock.cs` 与 `SingleFlight.cs`，没有增加第三方依赖或修改目标框架。两个新包支持 `netstandard2.0;net8.0`，专属测试为 `net6.0;net8.0`。未改旧 AsyncLock 或迁移 HTTP 缓存，保留工作区已有变更。未发布、提交或修改版本号。

Cron 适配、Diff 独立夹具和并发代码只读审查由 `luna_worker` 执行；主代理完成并发核心、Diff 适配、集成和最终验证。

## 变更影响与验证选择

本次新增公共 API、包依赖节点和解决方案项目，因此先验证新增并发用例及两个新模块，再运行三个相关测试项目完整测试，最后执行一次全解决方案检查。新测试分别归属核心、Cron 和 Diff 专属测试项目，未迁移历史测试。

这是能力新增，未设置吞吐提升或峰值内存验收值，不建立旧版本性能 baseline。大文本测试记录当前实现的实际耗时和当前线程累计分配；这些数据不能替代峰值内存、跨平台性能或生产吞吐保证。

## 行为验收证据

- KeyedLock：同 key 24 个工作者累计进入 1200 次，断言最大同时持有者为 1；覆盖异 key 同时取得、取消等待、预取消、零超时、重复释放、比较器和最终条目归零。交错由完成源和异步让出控制，没有用固定休眠判定正确性。
- SingleFlight：受控的 24 个重叠请求只启动一次工厂，验证引用结果共享、完成不缓存、不同 key 独立、调用方取消隔离、生命周期取消、工厂同步异常/空任务/故障/取消后清理和重试。所有等待者退出后的故障被观察。另验证工厂同步段受阻时其他 key 仍能启动。
- Cron：两个目标各 14 项测试通过，覆盖五/六段、秒字段、非法表达式与 H 字段、月日/星期 AND、闰年、月末、UTC、包含起点、区间端点、次数上限、无结果及取消。DST 使用自定义 2024 时区规则，避免依赖机器本地时区。
- Diff：两个目标各 15 项测试通过。7 组小语料和固定种子 20261027 的 256 组随机语料对照原版 DiffPlex 块坐标并重建目标；覆盖空文本、全部删除、中文、emoji、混合换行、末尾空行、忽略规则及只读集合。
- Diff 边界：接受恰好 200000 行和 16000000 个 UTF-16 单元；超过限制明确拒绝；自定义上限分别约束旧/新输入。超长单行在 4096 分块附近的 Unicode 代理对和大小写语义通过测试。
- Diff 取消与预算：扫描、长单行哈希、实际对角线计算和结果汇总分别在内部检查点触发取消/时钟耗尽；高差异语料在真实搜索循环内退出，错误代码稳定，不返回部分结果。

核心完整模块回归通过：net8 为 7583 项；net7 为 7570 通过/9 跳过；net6 为 7569 通过/9 跳过；net5 为 7562 通过/9 跳过；netcoreapp3.1 为 7561 通过/9 跳过。之后新增的锁外执行回归在五个目标各通过 1 项，并纳入最终解决方案回归。没有因补充测试或文档重复运行核心完整模块回归。

## 10 万行实测

Windows，Release，SDK 8.0.425。输入为 `line-000000` 至 `line-099999`，LF 分隔；默认预算 30 秒。计时只覆盖 Compare，输入生成及断言不计入；分配值来自 `GC.GetAllocatedBytesForCurrentThread`，不是保留内存或峰值内存。每个场景均验证完整目标重建。

| 场景 | net6 / ns2 产物耗时 | net8 产物耗时 | 分配范围（字节） |
| --- | ---: | ---: | ---: |
| 追加 1000 行 | 79.6 ms | 106.9 ms | 29,738,680–29,738,704 |
| 分散插入 100 处 | 60.6 ms | 109.9 ms | 29,690,000 |
| 分散删除 100 处 | 63.8 ms | 110.5 ms | 29,677,800–29,683,240 |
| 分散替换 100 处 | 87.0 ms | 103.2 ms | 29,684,976 |
| 同时插入/删除/替换各 100 处 | 74.4 ms | 80.5 ms | 29,701,408–29,701,432 |

数据来自 `artifacts/general-utilities/diff-final_*.trx`，为本次模块检查实测，未作统计基准或跨版本性能结论。

## 打包与消费

本地生成 `Bing.Utils.Cron.1.5.0.nupkg` 与 `Bing.Utils.Diff.1.5.0.nupkg`，沿用仓库版本，未发布。检查记录：`artifacts/general-utilities/package-inspection.json`。

- 两包均包含 netstandard2.0/net8.0 DLL、XML 注释、README 和项目 LICENSE。
- Cron 两个依赖组均只含 Cronos `[0.13.0]`，SourceLink 构建工具不传递给消费者。
- Diff 两个依赖组均为空，包含 `ThirdParty/DiffPlex/LICENSE.txt` 和 `SOURCE.md`，打包内容与源码文件逐字节一致。
- GitHub 固定标签归档验证了上游许可内容，标签不含独立 NOTICE；来源核对记录位于 `artifacts/general-utilities/source-verification.json`。
- 本地包消费者通过源映射强制 Bing.Utils.* 从本次本地包目录恢复，并使用隔离 NuGet 缓存；netstandard2.0/net6/net7/net8 均编译通过，net6/net8 运行验证 Cron 和 Diff 结果以及实际程序集目标框架。net6 明确加载 `.NETStandard,Version=v2.0`，net8 加载 `.NETCoreApp,Version=v8.0`。
- 已扩展现有 `build/CapabilityConsumer`，四个目标全部编译通过，0 警告、0 错误。

## 审查与修复闭环

| Finding | 分类 | 处理与证据 | 最终状态 |
| --- | --- | --- | --- |
| 故障状态的 OperationCanceledException 被误转取消，包括可取消等待包装 | 实现契约 | 根据原工厂任务终态发布，用 Unwrap 保留等待任务状态；两条路径回归通过 | FIXED |
| Diff 包未包含第三方材料 | 包契约 | 显式声明许可证和来源文件为打包项；实际 nupkg 字节核对通过 | FIXED |
| 工厂锁外执行缺少独立回归 | 测试缺口 | 增加同步阻塞首段的受控并发测试，五个运行目标通过 | FIXED |

当前 OPEN_ACTIONABLE = 0。没有因为历史跳过项、构建警告或未设定的性能门禁继续自动修复。

## 最终检查

```powershell
git diff --check
dotnet build Bing.Utils.sln -c Release
dotnet build build/CapabilityConsumer/CapabilityConsumer.csproj -c Release
dotnet test Bing.Utils.sln -c Release --no-build
```

`git diff --check` 通过；全解决方案 Release 构建通过（0 错误，447 条现有图像平台/过时 API/测试等警告，新增实现与测试文件无警告）；CapabilityConsumer 四目标编译通过（0 警告、0 错误）。

最终 `dotnet test Bing.Utils.sln -c Release --no-build` 退出码为 0：104 个项目/目标框架运行批次，累计 **66905 通过、90 跳过、0 失败**，总计 66995。跳过统计直接依据 TRX 的 `UnitTestResult outcome="NotExecuted"`，不将跳过项计为已验证通过。

最终核心项目通过数：net8 7584、net7 7571、net6 7570、net5 7563、netcoreapp3.1 7562；Cron 在 net6/net8 各 14 项，Diff 各 15 项。聚合结果位于 `artifacts/general-utilities/solution-test-summary.json`。本次全解决方案构建和测试均只执行一次，后续仅更新交付文档。

34 个新增源码、项目与文档文件通过 UTF-8、XML、Markdown 本地链接及行尾空白检查。OPEN_ACTIONABLE = 0，U0–U5 全部完成；没有待继续的修复或验证任务。

原始日志、TRX、包、消费者与结构检查存放于忽略目录 `artifacts/general-utilities/`；汇总记录随本文件保存。
