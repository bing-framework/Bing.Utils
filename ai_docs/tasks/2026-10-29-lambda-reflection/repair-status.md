# Lambda 与反射修复及优化记录

2026-10-29。状态：COMPLETED；F1–F7 和 O1–O3 均已完成，最终 OPEN_ACTIONABLE=0。变更涉及 `Bing.Utils`、`Bing.Utils.Reflection`，风险级别为中等。未改变包版本、依赖、目标框架或构建配置。

## 修复内容

| 编号 | 原问题 | 修复后的行为 |
| --- | --- | --- |
| F1 | `A && (B || C)` 分组后改变含义 | 保留 AND 内的 OR 子树；组内 AND、组间 OR 重建时保持结果与短路求值顺序，不做指数级分配展开。 |
| F2 | 闭包否定、嵌套字段、静态字段、方法调用和转换取值错误 | 识别独立值侧并按实际表达式求值，常量直接返回；保留布尔查询成员、搜索/Contains 及 nullable enum 比较约定。不能提取独立查询值时抛 `NotSupportedException`。 |
| F3 | 条件计数误识别字符串里的 `AndAlso` / `OrElse` | 基于内置逻辑表达式节点统计，不解析 `ToString()`。 |
| F4 | 成员路径找不到继承接口属性 | 查询完整接口继承图，并保留同名不兼容属性的歧义检查。 |
| F5 | 开放泛型属性去重丢失不同类型的同名属性 | 签名使用真实 `Type` 身份及访问器形状，避免 null 类型名碰撞。 |
| F6 | `ref` 构造参数无法匹配且回写不完整 | 按元素类型匹配 `ref/in`，支持 `out` 占位，回写调用方的 `object[]`，保留原始构造异常；按值精确重载优先。 |
| F7 | 无法找到 protected 基方法 | 非公开输入方法纳入非私有基方法匹配，返回最近的兼容基方法。 |

## 优化内容

| 编号 | 实现 | 验证要点 |
| --- | --- | --- |
| O1 | 新增 `GetValueGetter<T,TProperty>` / `GetValueSetter<T,TProperty>` | 属性类型必须完全匹配；值类型属性在调用时不经 object 装箱。普通元数据复用委托，元数据或泛型参数可回收时旁路缓存。 |
| O2 | 内部筛选复用属性数组，公开入口保持独立快照 | 可读/可写列表减少一次中间复制，修改返回数组不会污染缓存。 |
| O3 | 可回收动态类型使用有界实例缓存 | 首个名称直接缓存，更多名称才创建字典，总容量 1024；并发首次查询、实例隔离和程序集回收均通过测试。 |

```csharp
var property = typeof(Model).GetProperty(nameof(Model.Count));
Func<Model, int> getter = property.GetValueGetter<Model, int>();
Action<Model, int> setter = property.GetValueSetter<Model, int>();
```

`TProperty` 必须等于属性类型；结构体设置继续使用 `GetValueSetterByRef<T>`。旧 object 访问器保持可用。混合逻辑分组中的元素可能是复合表达式，消费方应保持表达式树结构。查询值求取会执行参数无关的方法/属性访问，不能把它当作不执行代码的纯元数据查询。

## 验证

- L0：两个生产包的 `net8.0 / net7.0 / net6.0 / netstandard2.0` Release 编译通过，0 错误；构建仍存在仓库既有弃用与平台分析警告。
- L1：核心定向回归 141 项通过；反射构造、动态缓存、方法匹配的定向证据由完整测试进一步覆盖。
- L2：`Bing.Utils.Tests` 全量 7,613 项通过，`Bing.Utils.Reflection.Tests` 全量 321 项通过，均为 .NET 8 Release，合计 7,934 项，无失败或跳过。
- 缓存清单检查通过：34 项，未登记静态缓存 0 项。
- 独立代码复核已完成。测试过程中发现的枚举兼容、测试夹具递归及强类型缓存生命周期问题均已修正，并由最终测试覆盖。
- 完整命令和退出状态见 [verification.json](verification.json)。本轮未执行旧运行时全量测试及完整 trimming/NativeAOT 发布矩阵，不新增这些场景的支持承诺。

## 性能观察

同一个无依赖 .NET 8 探针二进制分别加载冻结的修复前、修复后程序集，每次启动新进程。运行时为 .NET 8.0.31；预热 10,000 次，5 次采样，每次 50,000 次，实际并发为 1。两边在本轮构建及测试结束后顺序测量。原始输出包含程序集 MVID，见 [修复前](performance-before.json) 和 [修复后](performance-after.json)；复现方法见 [探针说明](../../../benchmarks/ReflectionAccessorProbe/README.md)。

| 场景 | 修复前 ns/op | 修复后 ns/op | 修复前 B/op | 修复后 B/op |
| --- | ---: | ---: | ---: | ---: |
| object-getter | 72.816 | 52.858 | 24 | 24 |
| readable-properties | 2902.968 | 1917.364 | 368 | 296 |
| writable-properties | 6719.946 | 5599.198 | 824 | 752 |
| dynamic-getter | 721.170 | 230.000 | 80 | 24 |
| dynamic-setter | 765.926 | 360.178 | 56 | 0 |
| dynamic-create-and-read | 879.586 | 680.156 | 112 | 128 |
| typed-getter-new-api | 新 API | 14.328 | 新 API | 0 |

本样本中，强类型 int getter 为 0 B/op，旧 object getter 为 24 B/op；可读与可写属性列表每次均少分配 72 字节；可回收动态实例重复读取从 80 降到 24 B/op，重复写入从 56 降到 0 B/op。动态实例创建并首次读取从 112 增到 128 B/op，增加 16 字节，用于换取后续访问复用。

耗时是本机短探针观察值，未设置发布阈值；不将这组数据外推为所有类型、属性数量或并发场景的固定加速比例。多属性实例会按需创建字典；本轮性能样本为单属性动态实例。

## 文件归属

生产变更位于 `Lambdas.cs`、核心反射的属性枚举/成员路径/严格构造绑定/强类型访问器，以及反射子包的 `TypeVisit` 和 `DynamicBase`。新增测试归属遵循 `test-project-boundary`：核心功能进入 `Bing.Utils.Tests`，子包专属行为进入 `Bing.Utils.Reflection.Tests`。使用说明同步到 `docs/modules/Bing.Utils.Reflection.md`，缓存登记同步到 `docs/quality/reflection-cache-inventory.json`。
