# Lambda 求值性能优化

2026-10-29。根据用户明确选择继续优化求值性能，完成普通方法调用快速路径与参数扫描分配优化。独立代码复核 PASS，Open Actionable=0。

## 实现

- `Lambdas.cs` 对接收者和参数均可直接读取的普通方法调用缓存反射求值委托，省去每次执行解释器帧的分配；只缓存执行代码，每次仍读取当前状态。
- 方法参数扫描改用索引循环，避免枚举器分配。
- 先读取接收者、再从左到右读取参数、最后检查空接收者；目标方法异常只解包反射添加的一层包装。ref/out、值类型接收者、复杂参数、自定义转换和可接收 `Type.Missing` 的签名保留解释器路径。
- 公开 API、依赖、目标框架均未变化。新增测试归属核心包 `Bing.Utils.Tests`，依据 test-project-boundary 的“主要断言目标”规则；无需测试迁移。

## 测量

同一探针二进制、.NET 8.0.31，固定 `DOTNET_TieredCompilation=0`；预热 2000 次，5 组各 10000 次，并发 1。诊断按 baseline→candidate 执行，确认按 candidate→baseline 执行，每个进程独立加载指定程序集。与第四轮的 JIT 配置不同，因此建立新的配对证据，历史基线未覆盖。

| 工作负载 | 修改前 B/次 | 修改后 B/次 | 分配下降 | 确认轮耗时（前→后） |
| --- | ---: | ---: | ---: | ---: |
| 复用表达式，提取 `Math.Abs(闭包值)` | 208 | 80 | 61.5% | 681.37→630.91 ns |
| 新建表达式并求值一次（含建树） | 2288.85 | 1184.81 | 48.2% | 24.21→9.35 μs |

两轮分配结果一致。诊断轮热路径为 1297.79→669.84 ns，冷路径为 71.61→12.14 μs；两轮耗时均下降，但幅度有波动，不承诺固定的生产提速比例。常量仍为 0 B/次，闭包字段/属性仍为 24 B/次。该结果适用于上述方法调用工作负载，其他复杂表达式继续使用原有路径。

基线 MVID：`feb4168f-56df-4fcd-b41f-005a8bed44fe`；候选 MVID：`fe852c2a-bbed-4743-ab37-0a3e06b2786d`。基线在本次编辑前冻结；候选来自本次通过完整测试的 .NET 8 测试输出。初步实现的默认 JIT 诊断样本不计入最终结论。

## 验证

- 新增 8 个用例，覆盖求值顺序、空接收者、getter/方法异常、自定义转换及复杂参数回退、ref/out 写回、可变结构体、Nullable、泛型、虚派发及 `Type.Missing`。
- .NET 6、7、8 Lambda 定向测试各 **150/150**，包含已有并发、checked、闭包更新、弱键及程序集回收用例。
- .NET 8 核心包完整测试 **7909/7909**，无失败或跳过；该数字为本次工作区实际结果，不沿用第四轮计数。
- 核心包 net8.0、net7.0、net6.0、netstandard2.0 编译通过。UTF-8 与 diff whitespace 检查通过。
- 独立复核仅针对新增调用路径及必要回归面，结果 PASS。

命令、退出码、逐轮数据与 MVID 见 [round5-verification.json](round5-verification.json)。

## 当前状态

Implementation：2/2。Changed Scope：1 个生产文件、1 个新增测试文件、模块文档及证据。Risk：MEDIUM。Validation：L0 编译/编码/格式、L1 三个运行时的定向测试、L2 核心包完整测试、定向性能探针；无新的发布门槛任务。复用既有探针及未变反射子包范围的证据。

Completed：2 项优化、8 个回归用例、兼容性与性能验证。Open Actionable、Blocked Approval、Blocked External、Deferred：无。Not Applicable：正式发布门槛。Accepted Limitations：复杂方法签名继续解释执行。Verified Boundaries：上述实际测量工作负载。No-Progress Check：CHANGED。Next Action：STOP。Goal Status：COMPLETED。
