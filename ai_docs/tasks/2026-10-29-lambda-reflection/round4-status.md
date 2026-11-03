# Lambda 第四轮修复与优化记录

2026-10-29。COMPLETED；R10、R11 和冷路径优化均经独立复核 CLOSED，OPEN_ACTIONABLE=0。

## 功能修复

- **R10**：标准搜索方法识别加入 `ParallelEnumerable.Contains(source, value[, comparer])`。PLINQ 集合可正常提取，比较器继续排除在查询值之外；集合和值均依赖查询对象时仍抛 `NotSupportedException`。Enumerable、Queryable 对照行为保持。
- **R11**：成员、操作符和参数解析同时穿透 `Convert` / `ConvertChecked`。checked 成员表达式正确返回 Number，GetType 保持返回原成员 int 类型的约定。实际求值仍执行 checked 转换；闭包值溢出时抛 OverflowException，随后恢复合法值可继续求值。

## 冷路径优化

以小型 `ValueEvaluator` 条目替代每个表达式的 Lazy、初始化工厂委托和闭包。首次编译通过私有条目锁串行，通过 Volatile 发布结果；命中走无锁读取。编译异常保存到条目中，递归初始化明确拒绝；实际委托执行在锁外，运行期异常与闭包值不缓存。ConditionalWeakTable 的表达式弱键及回收语义保持。

| 工作负载 | 修复前 B/op | 修复后 B/op |
| --- | ---: | ---: |
| 每次新建表达式并求值一次（包含建树） | 2408.85 | 2288.85 |
| 同一复杂表达式重复求值 | 208 | 208 |

一次性场景每次少分配 120 B，约减少 5%。缓存登记仍有成本，持续新建等价表达式无法复用条目；高频业务应在循环外构造并复用表达式。热路径分配保持不变，旧/强类型 getter 工厂仍为 0 B/op。耗时在采样间波动，本轮不承诺固定延迟提升。

冷路径使用审查阶段的同一个探针二进制，热路径使用前轮同一个 ReviewProbe 二进制；基线数据复用。运行时 .NET 8.0.31，预热 2000 次、5 组各 10000 次、实际并发 1。本轮每种工作负载各执行一次诊断和一次确认，确认后的核心程序集 MVID 为 `564267b8-c305-47e1-b7fa-89a495411ad9`。

冷路径与功能探针源码已保存到 [LambdaColdPathProbe](../../../benchmarks/LambdaColdPathProbe/README.md)。[冷路径基线](round4-cold-before.json)、[诊断](round4-cold-diagnostic.json)、[确认](round4-cold-after.json)；[热路径基线](round4-warm-before.json)、[诊断](round4-warm-diagnostic.json)、[确认](round4-warm-after.json)。

## 验证

- Lambda 定向测试 **142/142**，包括 PLINQ 两重载、比较器隔离、checked 元数据与溢出、闭包更新、运行异常恢复、编译失败、并发求值、弱键及程序集回收。
- 核心包 .NET 8 Release 完整测试 **7645/7645**，无失败或跳过。
- 两个生产包 **net8.0、net7.0、net6.0、netstandard2.0** Release 编译通过，0 错误；仓库原有警告保留。
- 反射子包生产路径未变，复用上一轮 **323/323** 完整测试证据，不重复执行。
- 缓存清单 **37 项**通过，未登记 **0 项**；持久化探针编译通过。
- 独立复核 PASS，无剩余验证阻塞。没有更改公开成员签名、生产依赖或目标框架。

[验证命令与退出码](round4-verification.json)。本轮完成 2/2 修复、1/1 优化；Open Actionable、Blocked Approval、Blocked External、Deferred 均无。无正式发布门槛任务。下一步：STOP。
