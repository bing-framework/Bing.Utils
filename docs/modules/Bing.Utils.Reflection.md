# Bing.Utils.Reflection
## 1. 包职责（Scope）
- 解决的问题
- 提供反射访问器与类型元数据扩展（`TypeVisit`/`TypeMetaVisitExtensions`/`AssemblyVisit`）。[证据] `src/Bing.Utils.Reflection/Bing.Utils.Reflection.csproj:3` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:10` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs:10`
- 提供基于 `MemberInfo`/`ParameterInfo` 的类型判断扩展（数值、元组、结构体等）。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:38` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:45`
- 不解决的问题（Out of Scope）
- 不承担 DI 容器和运行时代理框架职责（只提供反射工具层）。

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `TypeVisit.GetFullName(Type)`、`GetFullyQualifiedName(Type)`。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:17` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:29`
- `TypeMetaVisitExtensions.IsNumeric(MemberInfo/ParameterInfo)`、`IsTupleType(...)`、`IsStructType(...)`。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:45` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:62` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:79`
- 输入输出约定
- 以 `Type`/`MemberInfo` 为输入，返回字符串描述或布尔判定，不修改外部状态。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:58` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:46`
- 边界行为（null、空集合、非法参数）
- `GetFullName`/`GetFullyQualifiedName` 在 `type == null` 抛 `ArgumentNullException`。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:19` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:31`
- `MemberVisitHelper.GetActualType` 遇到不支持的 `MemberInfo` 类型抛 `InvalidOperationException`。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:25`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
Types.IsNumericType(typeof(int)).ShouldBeTrue();
Types.IsNumericType(typeof(decimal?)).ShouldBeFalse();
```
[证据] `tests/BingUtilsUT/TypeUT/TypeIsNumericTypeTest.cs:14` [证据] `tests/BingUtilsUT/TypeUT/TypeIsNumericTypeTest.cs:52`
- 示例2：进阶用法
```csharp
typeof((int, int)).IsTupleType().ShouldBeTrue();
typeof(object).IsTupleType().ShouldBeFalse();
```
[证据] `tests/BingUtilsUT/TypeUT/TypeIsTupleTest.cs:58` [证据] `tests/BingUtilsUT/TypeUT/TypeIsTupleTest.cs:65`
- 示例3：常见错误与修正
```csharp
Assert.Throws<ArgumentNullException>(() => TypeVisit.GetFullyQualifiedName(null));
// 修正：调用前判空
```
[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:31`
- 待确认：上述示例1/2主要来自 `Types.*` 用法，`Types` 定义位于 `Bing.Utils` 核心包，当前包更多是其反射入口包装层。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:46` [证据] `src/Bing.Utils/Bing/Reflection/Types/Types.Is.cs:199`

## 4. 性能与线程安全说明
- 构造函数、方法签名、属性元数据与委托、成员路径使用并发惰性缓存；类型相关缓存使用弱键，公开的元数据数组返回独立快照。
- 动态类型工厂对同一属性模式并发复用生成类型，按属性名稳定排序；最多缓存 4096 种仍存活的模式，每种类型最多 255 个属性。类型使用可回收程序集，调用方不再持有类型或实例后允许回收，再次使用相同模式可能重新生成类型。若跨加载上下文的同名属性类型无法保留运行时身份，生成请求会明确失败。
- `GetFullyQualifiedName` 在泛型类型场景会构造 `StringBuilder` 并递归拼接，仍有分配开销。
- 动态类型生成依赖 Reflection.Emit；运行时禁用动态代码时抛 `PlatformNotSupportedException`。`ReflectionTrimProbe` 历史记录在显式 linker roots 下验证了 net8.0 framework-dependent analyzer 与未裁剪契约检查、self-contained partial/full trim 和 NativeAOT metadata-only smoke，并覆盖普通、闭合泛型及嵌套成员路径，已配置 CI，远程执行结果待确认；另有独立未 root 程序集和独立可回收插件 probe，分别用路径、`AssemblyDependencyResolver`、字节数组三种加载方式验证 `TypeVisit` 字段/属性、`TypeReflections` 属性缓存、`Reflections`/`TypeReflections`/`Types` 的类型、属性、方法和参数特性描述/存在性/集合入口、构造、方法签名和成员路径在 framework/partial/full trim 与 NativeAOT 下的实际边界。动态类型、动态对象和表达式委托入口声明了相应的 `Requires*` 契约，仍不承诺它们在 NativeAOT 或未保留元数据的 trimming 场景可用。 framework-dependent `PublishTrimmed=true` 在固定 SDK 上预期失败并返回 `NETSDK1102`；当前验证状态见质量文档，历史发布记录不等同于本候选全部矩阵通过。
- 基准入口：`benchmarks/Bing.Utils.Benchmark` 中的 `ReflectionBenchmarks`；`--reflection-cold` 输出首次调用耗时与当前线程分配量，`--reflection-cache` 和 `--reflection-concurrent` 输出并发首次访问及重复访问的命中、未命中、等待、绕过、失败复用和实际构建次数，`--reflection-capacity` 用 1025 个唯一方法签名以及 1025 组唯一动态结构体映射验证 1024 条目准入及超限旁路，`--reflection-struct-map-steady` 以唯一动态结构体类型验证冷启动编译与稳态复用，并记录两阶段耗时/分配量；容量探针可通过 `--reflection-capacity-max-bypass=N`、`--reflection-capacity-max-time-us=N` 和 `--reflection-capacity-output=path` 增加可选预算及 UTF-8 JSON 证据。诊断仅在探针开启时计数；命中表示检查时已经存在成功构建的条目，等待表示已有尚未完成的条目，失败复用表示已有缓存的异常，未命中表示检查时尚未看到条目，不等于重复构建。方法签名的两级缓存只按公开请求记录外层查找，不把内部结构键查找重复计入命中率。`benchmarks/ReflectionComparisonProbe` 用同一运行时与输入对比固定基线和当前候选的稳态时间与分配量。
- 2026-10-05 本机 .NET 8.0.31 Release 对比：基线为未修改的 `49fd7fa0e9b50ef179a89ec07685cd37c9693622`，候选为本轮最终生产源码快照，缓存诊断关闭；两者的库与探针均按 Release/net8.0 构建。每场景预热 10000 次，再测 5 组各 50000 次；两个版本交替启动 5 轮，下表为进程间中位数，逐轮数据与完整源码归档见 `docs/quality/reflection-performance-evidence.md` 的最终复核。探针通过反射调用多数入口，数字仅适用于相同探针输入，不用作发布阈值。核心源码快照包含工作区已有改动，不能把全部性能差异归因于反射改动。

  | 场景 | 基线 ns/次 | 候选 ns/次 | 基线 B/次 | 候选 B/次 |
  | --- | ---: | ---: | ---: | ---: |
  | 构造函数 | 277.7 | 157.8 | 240 | 24 |
  | 属性 getter | 6.6 | 17.4 | 24 | 24 |
  | 属性 setter | 7.0 | 7.7 | 24 | 24 |
  | 方法签名 | 1034.3 | 175.5 | 448 | 88 |
  | 成员路径读取 | 779.1 | 369.4 | 472 | 200 |
  | 成员路径写入 | 707.4 | 357.2 | 408 | 144 |
  | 显式选项的成员路径读取 | 733.1 | 409.4 | 448 | 224 |
  | 显式选项的成员路径写入 | 633.0 | 376.2 | 384 | 168 |
  | 相同字典的动态类型缓存命中 | 1799.3 | 293.5 | 728 | 56 |
  | 全新字典的等价动态类型模式 | 1171.8 | 582.1 | 976 | 304.4 |

- 显式成员路径选项比默认选项多分配约 24 B/次，来自调用入口的选项快照。混合探针中 getter 耗时高于基线；独立进程 getter 的基线/候选中位数为 12.2/12.9 ns，均分配 24 B/次，未复现相同幅度的回退。独立显式选项写入为 460.8/343.8 ns，分配量为 384/168 B；此前显式选项写入回退也未被复现。各入口仍有进程波动，不能概括为全面提速。逐轮记录及快照位置见性能证据文档。
- 冷启动存在取舍：独立进程首次构造、方法签名和成员路径的候选耗时高于基线，动态类型首次生成低于基线。冷启动包含 JIT、缓存初始化及第一次扫描/编译开销，不能由稳态提速推导首次调用也更快。

- 并发冷启动探针中，32 次构造请求对应 1 次构造函数扫描和 1 次委托构建；8 次相同动态模式请求记录 1 次未命中、7 次等待、1 次 Emit，重复 8 次均为已完成构建的命中。缓存计数反映探针实际观察到的查找状态，五类计数之和等于请求数。失败构造请求连续两次查询计为 1 次未命中和 1 次失败复用，不再误记为等待；重复请求同时断言无额外扫描、编译或 Emit。

## 5. 异常与日志策略
- `TypeVisit.CreateInstance*` 无兼容公共构造函数或类型不可实例化时抛 `ArgumentException`；多个候选同样匹配时抛 `AmbiguousMatchException`；精确参数类型优先于可赋值类型。
- 核心包 `Reflections.CreateInstance<T>(Type/string, ...)` 与 `TypeVisit` 共用严格构造绑定器及弱键缓存，支持赋值兼容和 null 参数，不执行隐式数值转换或返回值转换。核心入口的目标类型不能赋值给 `T` 时，在执行构造函数前抛 `ArgumentException`；空白或无法解析的类型名称也明确拒绝。
- 构造函数本身抛异常时，外层 `InvalidOperationException.InnerException` 保留原始异常。
- 动态字典创建时，`null` 值生成 `object` 属性；非法属性名、类型或数量抛 `ArgumentException`，达到模式容量上限抛 `InvalidOperationException`。
- 成员路径默认只访问公共实例成员；仅显式设置 `IncludeNonPublic` 才允许非公开成员访问。
- `ConvertAssignedValue=false` 时仅允许直接赋值；`DBNull.Value` 写入 object/DBNull 等兼容目标会保留原对象，写入 string、可空数值等不兼容目标会返回 false 并保留原值。开启转换时仍将 DBNull 视为 null。
- `init` 属性视为初始化后不可写：设置器委托抛 `InvalidOperationException`，成员路径 `TrySet` 返回 `false`，`IncludeNonPublic` 不改变这一规则。
- `DynamicBase.SetPropertyValue` 同样拒绝写入 `init` 属性，失败时保留原值。
- 方法签名按 `ParameterPassingKind` 区分 `ref`、`out` 和 `in`；只读引用既识别 `ParameterInfo.IsIn`，也识别 `IsReadOnlyAttribute` required custom modifier，避免元数据缺少 `In` 标志时误判为 `ref`。
- 成员路径总长度最多 4096 字符（包含可选的类型名前缀），移除前缀后最多 64 层成员。超限时 `TryGet/TrySet/HasMemberPath` 返回 `false`，严格读取/写入入口抛 `ArgumentException`。
- 结构体字段和成员路径写入请使用 `ref` 重载。接受 `object` 的路径入口可修改调用方保留的装箱对象；直接传入结构体变量只修改临时装箱副本，不会回写原变量。
- 成员路径选项在调用开始时逐项读取为快照；调用方不应在捕获期间并发修改同一选项实例。捕获完成后的修改不会改变本次访问。嵌套结构体逐层回写时，用户定义的 getter/setter 抛异常会继续向外传播，已执行的副作用不会回滚。
- `TypeVisit.GetProperties(Getters/Setters/Both)` 只返回公共实例非索引器属性；`Setters/Both` 还排除只能在初始化期间设置的 `init` 属性。
- 普通构造函数、方法签名、成员路径和动态属性缓存均按类型加准入锁并在锁内二次检查；达到容量后旁路扫描/解析，不因并发 `Count` 竞态超出配置阈值。旁路会牺牲高基数输入的稳态延迟，需结合业务观测容量和告警。
- 方法名或 DynamicBase 属性名超过 1024 字符时绕过名称缓存，方法签名字符串键超过 8192 字符时绕过结构缓存；合法长名称仍可查询。方法签名的弱键外层缓存仍可复用结果，诊断按一次公开请求计数，不重复统计内部结构缓存。
- object 属性委托在缓存写入前拒绝 ref-return、pointer、byref-like、function-pointer 及开放泛型属性类型，抛 `NotSupportedException`；普通、静态和 ref 设置入口均执行该校验。
- 第三方特性反射器的私有令牌不可读取时，使用公开的 `AttributeType` 元数据判断类型，不执行特性构造函数。
- 可回收类型、成员和参数，以及携带可回收类型实参的封闭泛型方法，使用直接特性查询，避开第三方静态强引用缓存。检测同时覆盖 `DeclaringType` 和 `ReflectedType`，包括从可回收派生类型取得的普通基类成员。`IsAttributeDefined` 的默认与继承选项均不构造被查询特性；`GetAttribute/GetAttributes` 和 required 入口读取实例时仍执行特性构造函数，其异常会传播。
- 特性查询的 `attributeType` 必须是封闭的 `Attribute` 派生类型；普通与可回收元数据均对非法类型抛带参数名的 `ArgumentException`。`TypeReflections` 的各选项查询支持类型、字段、属性、方法和构造函数，事件等不支持的成员统一抛 `InvalidOperationException`；`Reflections` 的 `MemberInfo` 包装入口对这些不支持成员降级到 CLR 公共特性 API。所有 `GetRequiredAttributes` 集合重载都会物化结果，空集合与 `null` 一样抛出 `ArgumentException`。
- 未定义的 `ReflectionOptions` 或 `ReflectionAmbiguousOptions` 值抛 `ArgumentOutOfRangeException`，存在性查询不会因非法选项转入特性实例化路径。
- 方法签名区分 Value/Ref/Out/In，结构匹配和缓存身份包含参数及返回值的必需/可选自定义修饰符。`In` 表示元数据中的只读输入引用；不模拟 C# 调用点的 `ref readonly` 规则，也不执行泛型约束推断。函数指针等未覆盖的高级签名暂不承诺支持。
- 旧版 `Reflections.GetField/GetMethod/GetProperty` 在名称无效、成员不存在或重载歧义时会抛出明确异常，不再以 `null` 表示失败。
- 旧版实例化失败返回 `null/default` 的调用方需改为处理显式异常；值类型的按值属性设置器现在抛 `NotSupportedException`，请迁移到 `GetValueSetterByRef<T>`。
- 成员/参数的 `IsNumeric/IsTupleType/IsStructType` 和属性 `IsVirtual/IsAbstract` 的空参数现在统一抛出带参数名的 `ArgumentNullException`。
- 核心 `Reflections` 的类型、接口、集合和属性分类入口同样明确拒绝 null 输入；程序集列表或接口筛选列表包含 null 元素时抛 `ArgumentException`。`GetTopBaseType(typeof(object))` 仍返回 null，传入 null 类型则抛异常。

裁剪与 NativeAOT 的支持边界：`benchmarks/ReflectionTrimProbe` 历史记录在 net8.0、显式 linker roots、framework-dependent analyzer 与未裁剪契约检查、self-contained partial/full trim 和 NativeAOT metadata-only smoke 下验证构造、带参构造函数查询、属性访问、getter-only/setter-only/静态属性/索引器过滤、严格字段/属性/方法查询、类型/方法全名、普通/泛型方法签名和闭合调用、完整/直接接口图、基类、公共常量递归、实现类型扫描、实例化扫描、闭合泛型模型和嵌套成员路径；独立插件 probe 在不改变主探针 linker 根集合的前提下，记录未 root 程序集在三种加载方式下的实际结果，并直接覆盖类型/成员/参数的 `TypeReflections.IsAttributeDefined` 泛型与非泛型重载及 `Reflections`/`TypeReflections`/`Types` 特性单项、required 和集合包装。NativeAOT 日志检查器支持可选的未解析诊断门禁、仓库源指纹门禁和 ID 基线门禁，脚本默认只生成报告；工作流默认开启未解析、仓库指纹、仓库责任与非仓库责任门禁。普通元数据查询仍依赖调用方保留需要的类型与成员元数据。动态类型、动态对象、表达式委托和方法签名入口声明了相应的 `Requires*` 契约，构造和类型缓存扫描入口声明了 `DynamicallyAccessedMembers` 需求；动态类型生成入口依赖 Reflection.Emit，动态代码不可用时明确拒绝；不构成动态生成及表达式编译入口在 NativeAOT 或未保留元数据的 trimming 场景可用的支持承诺。 framework-dependent `PublishTrimmed=true` 在固定 SDK 上预期失败并返回 `NETSDK1102`；当前验证状态见质量文档，历史发布记录不等同于本候选全部矩阵通过。

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `Bing.Utils` 核心包。[证据] `src/Bing.Utils.Reflection/dependency.props:3`
- 被哪些包复用
- 当前 `src` 层未见其他子包直接引用 `Bing.Utils.Reflection`。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 本包专属测试位于 `tests/Bing.Utils.Reflection.Tests`，包括构造绑定、动态类型、方法签名和失败契约测试；核心包的特性、属性委托及成员路径测试位于 `tests/Bing.Utils.Tests/Bing/Reflection`。
- 新增回归类：`ReflectionFailureContractTests`、`MethodSignatureReadOnlyTests`、`TypeReflectionsCollectibleAttributeTests`、`ReflectionsMemberPathResourceTests`。缓存卸载测试同时覆盖字段、绑定标志元数据和筛选属性缓存。
- 本轮新增 `ReflectionsStrictContractTests`、`ReflectionLongNameTests` 和 `ReflectionRuntimeVerificationTests`；属性及特性既有回归补充不可装箱属性和非法枚举选项测试。测试按 test-project-boundary 规则归属核心包和反射子包。
- 旧运行时 CI 入口为 `.github/workflows/reflection.yml`，逐 TFM 验证 .NET 3.1/5/6/7/8。测试引用 netstandard 资产时不提供动态生成 API，相关条件编译应保留。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- 包内部对核心 `Types` 能力有依赖，升级 `Bing.Utils` 时需联动回归类型判定语义。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:46`
- 严格失败契约的版本升级步骤和仓库消费者清单见 `docs/quality/reflection-contract-migration.md`。

