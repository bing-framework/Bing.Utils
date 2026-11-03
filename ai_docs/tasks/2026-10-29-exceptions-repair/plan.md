# Exceptions 修复实施计划

日期：2026-10-29
授权：用户已批准完整实现方案及本计划，不自动提交、推送或发布。
设计来源：[Exceptions 修复与测试补强方案](../../../docs/superpowers/plans/2026-10-29-exceptions-repair-solution.md)。原方案中的“尚未实施”与临时构建阻断是历史基线；当前状态以本目录记录为准。

## 固定契约

- 保留已有公开 CLR 方法、可选参数及泛型约束；泛型 Unwrap 只在 ExceptionHelperExtensions 保留扩展标记，两个类的静态调用保留。
- Tap() 与 Tap(Action<T>) 仅添加到 Try<T> 基类并转发现有重载。
- 失败解构返回默认值、false 和原包装异常；LINQ/Map 短路保留包装异常、InnerException 和 Cause，不执行用户回调。
- 成功 Bind 捕获组合回调异常；空 Try 转为 InvalidOperationException。参数校验直接抛出；Tap/Match 副作用异常传播。
- SuccessAction 使用结果实例身份，既不保留委托也不按委托哈希判等。
- InvokeRange 参数工厂与操作属于同一项的保护范围；源枚举异常外抛，枚举器正常释放。
- 新增 CreateAsync(Func<Task<T>>) / CreateAsync(Func<CancellationToken, Task<T>>, CancellationToken) 与 InvokeTaskAsync 对应两个入口，保留可选 cause。等待实际操作完成并使用 ConfigureAwait(false)；取消保存为原异常的失败结果，外层任务正常完成。预取消不执行委托，忽略令牌的委托仍等到结束。
- 旧 CreateFromTask 在非默认同步上下文或调度器中于委托调用前返回失败；旧 InvokeAsync(Task<Action>) 继续执行同步 Action。
- 聚合查找为显式 bool 重载：根优先、左到右深度优先、按引用去重；旧入口保持单链行为。
- 两个格式化旧入口均有默认预算：深度 64、不同异常 256、UTF-16 输出 32768（最小 64）、缩进 8 层；聚合展开与隐藏堆栈默认关闭。快照配置，按需读取聚合子项，避免 AggregateException.Message 的全分支拼接；类型/HResult 分开，时间为记录时间。

输出预算控制本库追加和遍历，不承诺限制运行时或自定义异常属性 getter 的总分配。运行时 StackTrace 可能产生完整字符串；该限制需明确记录，不能把输出预算称为整个调用的内存硬上限。

## 影响分析与责任边界

生产范围仅 src/Bing.Utils/Bing/Exceptions。原测试及新回归位于 tests/Bing.Utils.Tests/Bing/Exceptions，按测试归属规则保持主包测试项目。Guard 的 ValidationExceptionHelper 是受影响消费者，执行其定向测试。其他模块已有工作区变更保留。

主代理负责生产实现、契约、集成验证与文档。luna_worker 独立拥有 build/ExceptionsConsumer、benchmarks/ExceptionsProbe 和 build/Test-Exceptions.ps1；独立审查后定向拥有两份测试文件补齐确认的缺口。未升级框架和依赖，未重做整个 Try 体系。

## 执行步骤

### S1 准备与基线（已完成）

- [x] 原测试项目 net8.0 的 88 项 Exceptions 测试通过；当前未重现旧缺失 XML 阻断，不改构建配置。
- [x] 冻结相关源码、测试及四个目标 DLL/XML/PDB，保存 baseline/manifest.json。
- [x] 建立消费者、独立上下文宿主与 BCL 性能探针；保留基线 JSON。

### S2 核心正确性（已完成）

- [x] 解构、Unwrap/Tap 歧义、Bind 捕获、失败信息保留及类型提取。
- [x] SuccessAction 身份与所有 16 个 InvokeRange 重载。
- [x] 回归覆盖现有四个工厂家族 0—16 参数的成功/故障和转发顺序。

### S3 异步与聚合（已完成）

- [x] 四个真实异步入口、故障/null/取消及预取消契约。
- [x] 旧桥接上下文保护和显式聚合查找。
- [x] 独立子进程覆盖受限同步上下文、非默认调度器，20 秒总超时并终止进程树。

### S4 格式化预算（已完成）

- [x] 快照与校验，字符/深度/节点/缩进预算，按需分支读取，引用去重。
- [x] 修正类型、HResult、记录时间字段，真实堆栈/隐藏/重抛测试。
- [x] 同输入性能观测；区分完整短链输出与默认截断的深链资源边界。

### S5 验收与文档（已完成）

- [x] 四个生产目标构建，五个测试宿主 Exceptions 精准测试。
- [x] net8 主测试项目全量与 Guard 受影响测试；全量失败在冻结基线定向复核。
- [x] net8 消费者逐个显式引用四种产物，验证复制资产一致和实际 TargetFramework。
- [x] 公开成员签名、可选参数、泛型约束对比。
- [x] 独立审查，补正明确的测试缺口与资源边界说明。
- [x] 补正后的五宿主 290/290 精准验证、审查闭环与最终报告；全量范围外日期失败单列。

## 验收矩阵与停止条件

23 类场景 T01-01—T01-23 按原方案执行，不能以总测试数或覆盖率替代行为断言。详细对应关系、日志和观测结论记录在 findings.md 与 final-report.md。

正确性、构建、性能观测和发布状态分别记录。只有 OPEN_ACTIONABLE > 0 才进入下一轮修复；最多 3 轮 Review → Fix → Review。文档/注释更改复用不受影响的行为与性能证据；原有、范围外、外部未执行事项不触发无进展循环。

最终交付：源码、回归测试、消费者/探针/脚本、中文使用与迁移指南、问题状态、原始验证证据和最终报告。

## S6 差距复核补正（已完成）

本轮依据用户“修复一下列举出来的问题”重新打开 F13、T01，并新增 P01 记录探针证据缺口。只处理已复现或明确缺失的项。

- [x] F13：不同节点预算应允许重复引用标记；在接纳额外不同节点、读取其属性前停止；输出满后不再推进枚举器。
- [x] T01-03/14：补成功三元解构及真正执行中的协作取消。
- [x] T01-16：外部消费者编译并调用旧 InvokeAsync(Task<Action>)。
- [x] T01-18/19：旧入口默认长度、宽聚合长度、真实长堆栈及 Source/方法/内部消息断言。
- [x] P01：成功 Map、有限宽度聚合及明确截断原因的性能证据。
- [x] 精准验证与独立复核后更新报告。

变更影响：生产仅 ExceptionTraversal / ExceptionFormatter 的内部节点准入与输出停止顺序，无公开 API 变化；受影响为格式化及使用共用遍历的显式聚合 Unwrap/ExceptionAs。测试跟随主包 Exceptions。消费者和探针两个 Program.cs 由 luna_worker 独立补强，其余由主代理集成。不会修改日期模块。

验证策略：先新增回归验证已复现的节点预算错误，再运行五宿主 Exceptions 精准测试、四目标生产构建及四资产消费者。Guard 依赖单链查找逻辑未改，复用原 11/11；上下文策略与异步生产代码未改，复用独立进程证据；已有公开签名与全量套件作为历史证据，明确本轮未全量重跑。格式化和探针有变化，使用冻结原 DLL 与本轮 DLL 各运行一次同版探针，保留既有结果且不重定义 baseline。

关闭证据：五宿主各 304/304；最后新增失败解构断言五宿主各 1/1；四构建/四资产消费者通过；性能与截断原因证据齐全。独立复核确认 F13、T01 可关闭，P01 由主代理独立核验 harness 及实际输出。详见 gap-fix-report.md，OPEN_ACTIONABLE=0。
