# 反射失败契约迁移指南

更新时间：2026-10-13

本文记录 `Bing.Utils` 与 `Bing.Utils.Reflection` 反射入口从“未命中返回
`null/default`”向“失败立即抛出明确异常”的迁移规则。它只覆盖本仓库已验证的
公开入口，不把未承诺的 NativeAOT/trimming 行为扩展为支持范围。

## 1. 变更矩阵

| 入口 | 当前失败契约 | 调用方迁移方式 |
| --- | --- | --- |
| `TypeVisit.CreateInstance*`、`Reflections.CreateInstance<T>` | `type` 为 null 抛 `ArgumentNullException`；接口、抽象类、开放泛型、指针或 byref-like 类型抛 `ArgumentException`；无兼容公共构造函数（包括没有显式公共无参构造的值类型）抛 `ArgumentException`（参数名 `args`）；多个候选抛 `AmbiguousMatchException`。 | 不再用 `null/default` 判断创建失败；先保证目标类型可实例化，并按可赋值类型传参。数值参数不会自动转换，`null` 只匹配引用类型或可空值类型。 |
| 构造函数执行 | 构造函数本身抛出的异常会作为 `InvalidOperationException.InnerException` 保留。 | 捕获 `InvalidOperationException` 后检查 `InnerException`，不要把所有创建失败都转换成“无构造函数”。 |
| `Reflections.CreateInstance<T>(string, ...)` | null 类型名抛 `ArgumentNullException`；空白或无法解析的名称抛 `ArgumentException`。 | 在配置或外部输入边界先校验类型名；不要把解析失败当作默认实例。 |
| `TypeVisit.FindMethod*`、`TypeVisit.GetMethodBySignature` | 参数为空抛 `ArgumentNullException`；名称/签名无效抛 `ArgumentException`；未找到方法抛 `MissingMethodException`；多个结构匹配抛 `AmbiguousMatchException`。 | 对“探测是否存在”使用 `try/catch (MissingMethodException)` 或 `Has...` 语义；不要用返回 null 判断未命中。 |
| `TypeVisit.GetMatchingConstructor`、`GetParameterlessConstructor` | 类型或参数列表非法抛参数异常；未找到唯一公共构造函数抛 `ArgumentException`；多重匹配抛 `AmbiguousMatchException`。 | 需要探测时使用 `HasParameterlessConstructor`；需要元数据时处理明确异常。 |
| `TypeVisit.HasParameterlessConstructor`、`GetBaseMethod` | 这两个探测入口保留兼容语义：无无参构造函数返回 `false`，没有基础方法返回 `null`。 | 不要把所有反射 API 统一改写成异常；仅对严格查询入口迁移失败处理。 |
| `TypeVisit.CreateInstance(IDictionary<string, object>)` | null 字典抛 `ArgumentNullException`；非法属性或只读/init-only 写入抛明确异常；字典中的 null 值推断为 `object`。 | 生成动态对象前校验键名和重复键；对属性缺失、不可写和类型不兼容分别处理。 |

## 2. 版本升级策略

1. 将本次变化作为行为兼容性变更发布，在版本说明中链接本文。
2. 把依赖旧返回值的分支改为捕获具体异常；禁止用空对象或 `default` 掩盖构造/方法解析失败。
3. 对可恢复的探测流程优先使用 `HasParameterlessConstructor` 或捕获
   `MissingMethodException`，不要捕获 `Exception` 后静默继续。
4. 对构造函数执行失败记录 `InvalidOperationException` 及其内部异常，保留原始故障原因。
5. 逐项运行下方消费者清单中的测试和基准；外部仓库升级时按相同入口搜索并补充清单。

## 3. 当前仓库消费者清单

扫描范围排除了 `output`、`bin` 和 `obj`，检索入口为
`TypeVisit.CreateInstance`、`Reflections.CreateInstance`、
`TypeVisit.FindMethod` 和 `TypeVisit.GetMethodBySignature`。

| 类别 | 文件 | 处理状态 |
| --- | --- | --- |
| 生产代码 | `src/**`（除入口定义外未发现调用方） | 无需迁移；继续由公共契约测试保护 |
| 旧测试 | `tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs` | 已改为断言严格异常 |
| 核心反射测试 | `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsStrictContractTests.cs` | 覆盖两套实例化入口、null/赋值/歧义/异常链 |
| 反射子包边界测试 | `tests/Bing.Utils.Reflection.Tests/Bing/Reflection/TypeVisitCreateInstanceEdgeContractTest.cs`、`TypeVisitCreateInstancesContractTest.cs` | 覆盖参数和批量入口；已迁移至被测子包 |
| 反射子包实例化测试 | `tests/Bing.Utils.Reflection.Tests/Bing/Reflection/TypeVisitCreateInstancesTests.cs`、`TypeVisitConstructorBindingTests.cs` | 覆盖构造匹配和失败异常 |
| 反射子包方法测试 | `tests/Bing.Utils.Reflection.Tests/Bing/Reflection/TypeVisitAdvancedTests.cs`、`MethodSignatureReadOnlyTests.cs`、`ReflectionLongNameTests.cs` | 覆盖未命中、歧义、ref/in/out 和长签名 |
| 性能入口 | `benchmarks/Bing.Utils.Benchmark/Benchmarks/ReflectionBenchmarks.cs`、`benchmarks/ReflectionComparisonProbe` | 只使用成功路径；失败计数由并发缓存探针覆盖 |
| 文档/迁移证据 | `docs/modules/reflection.md`、`docs/modules/Bing.Utils.Reflection.md` | 已同步当前异常契约和支持边界 |

刷新清单时使用：

```powershell
rg -n --glob '!output/**' --glob '!**/bin/**' --glob '!**/obj/**' `
  "TypeVisit\.CreateInstance|Reflections\.CreateInstance|TypeVisit\.FindMethod|TypeVisit\.GetMethodBySignature" `
  src tests benchmarks docs
```

## 4. 验证要求

- 核心反射筛选和 `Bing.Utils.Reflection.Tests` 必须通过。
- 生产项目的 net8/net7/net6/netstandard2.0 编译必须通过。
- 新增消费者不得把 `MissingMethodException`、`ArgumentException` 或
  `AmbiguousMatchException` 转换成无提示的默认值。
- 该迁移指南不代表已完成 NativeAOT/trimming 验证；相关支持仍以质量状态文档为准。
