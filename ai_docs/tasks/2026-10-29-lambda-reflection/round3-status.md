# Lambda 与反射第三轮修复记录

2026-10-29。状态：COMPLETED。R7–R9 与本轮三项优化均已由独立 reviewer 确认为 CLOSED，OPEN_ACTIONABLE=0。

## 修复内容

| 编号 | 修改与验收结果 |
| --- | --- |
| R9 | 旧泛型 getter、setter、ref setter 在访问静态委托缓存前检查实例类型 T 的可回收性。使用常驻基类属性搭配可回收派生类型时旁路缓存。原探针中旧 getter/setter 的程序集从 GC 后仍存活变为可释放；普通类型仍复用委托。 |
| R7 | 搜索调用按业务操作数解析成员，支持数组、内联集合、常量字符串、AsQueryable/Where 链及条件接收者。某个操作数无法解析时继续尝试另一个。实例泛型集合保留原有元素成员优先级，字符串保留接收者优先级。 |
| R8 | Enumerable/Queryable.Contains 只在集合与元素操作数间提取查询值，不再返回比较器。两个业务操作数均依赖查询对象时抛 NotSupportedException；独立集合和值的原有约定保持。 |

本轮只修改核心包中的上述解析与访问器代码；测试归入核心测试项目，使用说明与缓存登记同步更新。未改变公开成员签名、依赖或目标框架。首次及第二轮修复记录与原始证据保留。

## 优化结果

| 场景 | 本轮修复前 B/op | 修复后 B/op |
| --- | ---: | ---: |
| GetOperator 字符串操作符解析 | 40 | 0 |
| 重复获取旧 object getter | 120 | 0 |
| 同一复杂表达式重复求值 | 约 1352 | 208 |

- 操作符通过序号比较匹配，移除每次 ToLower 的字符串分配。
- 泛型及非泛型旧访问器的缓存命中复用已经校验成功的委托，保留首次参数、可见性、结构体写入和可回收元数据检查。
- 复杂表达式以 Expression 对象为 ConditionalWeakTable 弱键缓存解释委托。每次仍执行表达式并读取当前闭包值，运行时异常不作为执行结果缓存；并发调用各自执行。表达式、闭包及可回收类型的回收测试均通过。

复杂表达式的数据来自原有 ReflectionRegressionProbe 的同一探针二进制，运行时 .NET 8.0.31，预热 2000 次、5 组各 10000 次，实际并发 1。本轮运行一次诊断、一次确认，基线复用上一轮最终候选。GetOperator 使用上次审查的同一功能探针二进制，预热 2000 次、测量 10000 次。原始输出保留程序集 MVID。

本次耗时有明显波动，未修改的动态访问路径也变慢，因此不据此承诺稳定延迟提升。解释器仍有每次调用的分配；弱键缓存只对同一表达式对象的重复使用生效，持续新建表达式仍需首次编译和缓存登记。

## 验证

- 最终定向测试：185/185，通过。
- .NET 8 Release 核心全量：7639/7639；反射子包全量：323/323；合计 7962 项，无失败或跳过。
- 最后两处调整仅影响 Lambda 成员选择，核心全量在最终源码上重跑；反射子包全量复用其前的同范围证据，其生产路径未再改变。
- 两个生产包 net8.0、net7.0、net6.0、netstandard2.0 Release 编译通过，0 错误。
- 缓存清单：37 项，未登记 0 项；相关文件 UTF-8、空白与 git diff --check 通过。
- 回归覆盖查询成员选择、比较器排除、闭包实时值、异常恢复、副作用次数、并发求值、缓存委托复用、表达式/闭包及动态程序集回收。
- 独立复核 PASS，无未关闭问题。

[验证命令与退出码](round3-verification.json)；[功能修复前](round3-functional-before.json) / [修复后](round3-functional-after.json)；[回收修复前](round3-gc-before.json) / [修复后](round3-gc-after.json)；[性能基线](round3-performance-before.json) / [诊断](round3-performance-diagnostic.json) / [最终确认](round3-performance-after.json)。

本轮已完成：3/3 问题、3/3 优化。Open Actionable、Blocked Approval、Blocked External、Deferred：无。Release 发布验证不属于本次任务；未新增旧运行时执行或完整 trimming/NativeAOT 支持承诺。下一步：STOP。
