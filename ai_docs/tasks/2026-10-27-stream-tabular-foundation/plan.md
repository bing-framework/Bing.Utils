# 统一流式处理与表格映射基础能力

用户已批准交付单元 1、2 的实现。当前任务不涉及版本升级、提交或发布。

## 目标与执行单元

1. **1A**：新增 `Bing.Operations` 阶段、计量单位和不可变进度类型，以及 CSV、JSON Pointer、JSON 语法、表格、工作表定位类型。保持各格式坐标规则。
2. **1B**：兼容扩展 `StreamTransfer`、`CsvStream`、`TabularDataReader` 的统一进度；保持旧签名与旧观察器。提供旧进度适配器，记录真实完成量、未知总量及操作内事件序号。
3. **1C**：验证预取消、枚举取消、早停、失败和 `leaveOpen`；更新生命周期文档。普通流写入不代表外部文件提交。
4. **2A**：新增 `ITabularRow`、`DataIssue`、`RowResult<T>`，提供 CSV 和数据库当前行适配。
5. **2B**：新增显式 `TabularMap<T>` 配置与列预检，支持名称别名、序号、大小写、必需列及未知列策略；配置冻结，成员委托与列结构绑定复用。
6. **2C**：实现标量转换、自定义转换、文化、必需值和对象校验。一行聚合多个错误，失败不返回半成品对象，取消不变成数据错误。
7. **2D**：验证中文映射、缺列与空值区分、多错误、定位、歧义预检、两种数据源复用及兼容性；最终构建解决方案并执行全量回归。

## 已确定的实现细节

- 继续使用 net8.0/net7.0/net6.0/netstandard2.0，不增加第三方运行时依赖。
- `MaxRows`/`MaxBytes` 为预算，不是已知总量；本次流与行读取的 `Total` 为 null。
- `Commit` 仅供真正完成提交的上层使用；普通流读写只报告对应阶段。
- `TabularMap<T>` 仅映射显式配置成员，缺少可选列时保留构造后的默认值。
- 同一目标同时匹配多个别名列、名称/序号重叠到同一源列时，在读取值前抛出 `mapping.duplicate_mapping` 配置异常。
- 默认不区分大小写、使用不变文化、忽略未知列、执行 DataAnnotations 校验；自定义校验返回的文字由调用方控制。
- 数据库行适配仅捕获当前行，不推进或关闭读取器；CSV 记录号包含表头。

## 验收与验证

- 新增核心类型与 API 的正常、边界、异常、参数化测试位于 `Bing.Utils.Tests`，依据测试归属技能的主包规则。
- L0：`git diff --check`、UTF-8 检查、核心多目标框架编译。
- L1：StreamTransfer/CsvStream/TabularDataReader/OperationProgress/TabularMap 精准测试。
- 最终：`dotnet build Bing.Utils.sln -c Release --no-restore`，然后 `dotnet test Bing.Utils.sln -c Release --no-build`。
- 构建串行执行，避免多进程写同一 output/obj；保留当前用户已有文件检测、命令行和 MapperHelper 改动。

## 不包含

嵌套对象/集合自动映射、Source Generator、进度节流、读取器遇错策略、CSV 一站式导入、Excel 实现、数据库批量写入、HTTP/FTP/SSE 传输内核重写。

## 审查后补全（2026-10-27）

用户已授权补齐审查发现的整组内容，按下列工作项实施：

- F01：缺失 ordinal 必需列时保留已配置序号，验证表格与 CSV 坐标及列预检结果。
- F02：补充常用标量转换的成功/失败测试、数据库读取或取消与释放同时失败的组合测试，以及 CSV 异常位置联动测试。
- F03：查明 net5.0 正则超时断言失败原因，修复测试或实现中已证实的问题；验证原有超时约束，不通过跳过测试或放宽断言隐藏失败。
- F04：定向验证通过后，运行完整 Release 构建和全量回归；针对上轮并发运行的时序失败核对本轮结果。

影响分析：生产代码预计仅修改 `TabularMap.GetSchemaIssues` 的缺列诊断信息；新增/更新的测试归属核心 `Bing.Utils.Tests`。公开签名、TFM、依赖、构建打包和 Benchmark 均不变；其余生产路径仅在复现真实缺陷后做最小修复。风险为低至中等，先执行映射/转换/数据库/CSV/正则精准测试，再执行最终门禁。原始日志保留，新日志使用 `completion-` 前缀。
