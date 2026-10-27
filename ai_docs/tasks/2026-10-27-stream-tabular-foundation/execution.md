# 统一流式处理与表格映射交付记录

日期：2026-10-27。已完成用户批准的两个交付单元及后续“补全整块的内容”要求。本地构建与回归验收闭环。

## 当前状态

| 维度 | 结果 |
|---|---|
| IMPLEMENTATION_STATUS | PASS：1A～2D 已实施，审查指出的缺列定位和覆盖不足已补齐 |
| BUILD_STATUS | PASS：最终完整 Release 构建成功，0 错误、26 警告 |
| TEST_STATUS | PASS：修复后核心与 HTTP 项目完整复验通过，其他未改变范围复用本轮全量证据 |
| PERFORMANCE_EVIDENCE_STATUS | 未声明性能提升；既有 MapperHelper 性能测试按原 5000ms 阈值通过 |
| RESOURCE_EVIDENCE_STATUS | PASS：取消、早停、所有权、部分输出及读写/释放双重失败测试通过 |
| EXTERNAL_GATE_STATUS | NOT_APPLICABLE：本任务不包含外部服务部署或发布验收 |
| RELEASE_STATUS | 本地门禁通过；没有执行包发布 |
| GOAL_STATUS | COMPLETED：计划内与补全项无待修问题 |

## 已交付能力

- `OperationStage`、`ProgressUnit`、不可变 `OperationProgress`，以及 CSV、JSON Pointer、JSON 语法、通用表格和工作表位置类型。
- StreamTransfer、CsvStream、TabularDataReader 接入统一进度，保留旧签名和进度 API；取消保持原异常，资源清理遵循 leaveOpen。普通流不虚报 Commit，未知总量为 null。
- `ITabularRow`、`DataIssue`、`RowResult<T>`，CSV 行和数据库当前行快照适配。
- `TabularMap<T>` 支持中文/英文别名、ordinal、大小写、必需列/值、未知列策略、映射冲突预检、类型/自定义转换、文化和对象校验；配置冻结并缓存列结构绑定。
- 一行聚合多个问题，失败不返回半成品对象；缺少按 ordinal 配置的列时保留期望序号，并保持各格式坐标语义。
- 补充标量转换、异常清理和 CSV 多行字段定位测试；修复全量暴露的正则样例和测试时序问题。

API 示例和生命周期约定见 [使用指南](../../../docs/reference/tabular-mapping-guide.md)。详细修复及独立复核结论见 [补全记录](completion-fix.md)。

## 最终验证证据

| 范围 | 框架/程序集组合 | 通过 | 失败 | 跳过 | 证据 |
|---|---:|---:|---:|---:|---|
| 核心项目完整复验 | 5 | 37785 | 0 | 36 | [completion-core-full.log](completion-core-full.log) |
| HTTP 项目完整复验 | 5 | 9644 | 0 | 0 | [completion-http-full.log](completion-http-full.log) |
| 本轮全量中其余未改变范围 | 90 | 19353 | 0 | 54 | [completion-solution-test.log](completion-solution-test.log) |
| 当前验证覆盖汇总 | 100 | 66782 | 0 | 90 | [completion-test-summary.json](completion-test-summary.json) |

计数包含同一测试在不同目标框架上的执行。90 个既有跳过由核心项目 36 项历史 BinaryFormatter 兼容测试和网络集成项目 54 项组成；后者在 net8.0/net7.0/net6.0 的三个运行中整组跳过，不计为通过。

原始全量命令返回 exit 1，记录为 **66771 通过、11 失败、90 跳过**，没有改写为成功。该运行后只调整核心和 HTTP 两个测试项目；生产源码及其余项目未变，因此依据 GOAL_RULES 复用未受影响范围，并以两个项目的完整新结果替换其旧结果。汇总已检查全部 100 个程序集/框架键一致，未遗漏整组跳过的运行。

最终完整构建：`dotnet build .\Bing.Utils.sln -c Release --no-restore`，exit 0；见 [completion-final-build.log](completion-final-build.log)。测试通过后仅更新报告，不再修改源码。

本轮主要命令：

```powershell
dotnet build .\Bing.Utils.sln -c Release --no-restore
dotnet test .\Bing.Utils.sln -c Release --no-build -m:1
dotnet test .\tests\Bing.Utils.Tests\Bing.Utils.Tests.csproj -c Release --no-build -m:1
dotnet test .\tests\Bing.Utils.Http.Tests\Bing.Utils.Http.Tests.csproj -c Release --no-build -m:1
```

## 影响与边界

- 基础提交：`0aaee350a78a0eb233b3f15d3e8c931a71fca864`。验证对象是当前工作区，保留任务开始前的用户变更。
- 核心目标框架仍为 net8.0/net7.0/net6.0/netstandard2.0；测试目标框架仍为 net8.0/net7.0/net6.0/net5.0/netcoreapp3.1。
- 没有新增第三方运行时依赖、替换 MapperHelper、修改版本、Git 暂存/提交或推送。
- 新功能测试归主包 Bing.Utils.Tests；HTTP 时序测试留在 Bing.Utils.Http.Tests。没有测试迁移或依赖变更。
- Excel 读取、嵌套映射、Source Generator、读取器错误策略、进度节流和一站式导入仍按原计划排除。

## 分类与停止条件

| 分类 | 当前结果 |
|---|---|
| Completed | 两个交付单元、审查补全、文档、定向测试、完整构建和全范围回归证据 |
| Open Actionable | 无 |
| Blocked Approval / Blocked External | 无本任务范围内阻塞 |
| Not Applicable | 无性能提升声明，不执行额外 Benchmark 或发布 |
| Accepted Limitations | 同步回调须合作取消；不能回滚调用方 setter 外部副作用；最终进度只覆盖当前阶段 |
| Verified Boundaries | 格式坐标、缺列序号、取消与释放失败保真、预算和真实进度；原性能阈值保持并通过 |
| Deferred | 仅原计划明确排除的能力 |

No-Progress Check：CHANGED，真实修复及执行证据已闭环。Next Action：STOP。
