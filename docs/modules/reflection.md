# 模块：Bing.Utils.Reflection

## 1. 模块定位
- 目标：提供反射相关的“类型/成员元数据访问”工具（如构造函数推断、属性/字段选择、方法签名匹配、类型判断扩展），并在非 NETSTANDARD 目标下提供基于 Reflection.Emit 的动态类型生成能力。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | TypeVisit.CreateInstance(...)
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs | TypeVisit.GetProperties(...) / PropertyAccessOptions
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.DynamicType.cs | TypeVisit.CreateDynamicType(...)
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | TypeFactory.CreateType(...)
- 非目标：不提供完整的“类型扫描/程序集扫描框架”、也不负责把反射结果序列化/映射到 DTO（本包更偏向基础反射工具与小型 helper）。
    - 证据：src/Bing.Utils.Reflection/Bing.Utils.Reflection.csproj | Description=反射操作类库（未声明扫描/映射框架）
- 适用场景：
    - 运行时按 Type + 构造参数创建实例（构造函数签名匹配）；目标不可实例化或找不到构造函数时抛出明确异常。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | CreateInstanceImpl(...)
        - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | Test_*_CreateInstance_*_With_WrongSort()
    - 从表达式树安全获取属性/字段元数据，或按 Getter/Setter 可见性筛选属性。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs | TypeVisit.GetProperty(...) / GetProperties(...)
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Fields.cs | TypeVisit.GetField(...) / GetFields(...)
    -（非 NETSTANDARD）在运行时基于属性字典生成 sealed 动态类型，并可创建对象后按属性名赋值。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | DefineDynamicAssembly/DefineType/DefineProperty
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | TypeVisit.CreateInstance(IDictionary<string,object>)
- 不适用场景：
    - 需要强类型表达式编译/代理织入的高级 AOP/动态代理（本包仅暴露基础反射 helper；动态类型基类仅提供属性 Get/Set）。
        - 证据：src/Bing.Utils.Reflection/Bing/Dynamic/DynamicBase.cs | DynamicBase.GetPropertyValue/SetPropertyValue

## 2. 目录结构
    src/Bing.Utils.Reflection/
        tests/Bing.Utils.Reflection.Tests/（反射子包测试）
        tests/BingUtilsUT/（存在与本模块相关用例）
        tests/Bing.Utils.Tests/（存在与本模块相关用例）
        - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | TypeVisit.CreateInstance(...)
        - 证据：tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs | AssemblyVisit.GetFileVersion/GetProductVersion

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `AssemblyVisit.GetFileVersion(Assembly)` | 读取程序集文件版本 | `assembly` | `string` | `ArgumentNullException`（assembly 为空时） | 
| `AssemblyVisit.GetProductVersion(Assembly)` | 读取产品版本并沿用正则替换 `+` 后的连续单词字符；任意构建元数据格式的完整截断待确认 | `assembly` | `string` | `ArgumentNullException`（assembly 为空时） |
| `TypeVisit.CreateInstance(Type, params object[])` | 按类型 + 构造参数创建实例（严格签名匹配） | `type`,`args` | `object` | `ArgumentNullException` / `ArgumentException` / `AmbiguousMatchException` / `InvalidOperationException` |
| `TypeVisit.CreateInstance<T>(params object[])` | 按泛型类型创建实例（使用严格构造绑定） | `args` | `T` | `ArgumentException` / `AmbiguousMatchException` / `InvalidOperationException` |
| `TypeVisit.CreateInstance<T>(Type, params object[])` | 按指定 Type 创建实例并验证可赋值给 `T` | `type`,`args` | `T` | `ArgumentNullException` / `ArgumentException` / `AmbiguousMatchException` / `InvalidOperationException` |
| `TypeVisit.HasParameterlessConstructor(Type)` | 推测是否存在无参构造函数 | `type` | `bool` | `ArgumentNullException` |
| `TypeVisit.GetProperties(Type, PropertyAccessOptions)` | 按访问器可见性筛选属性 | `type`,`accessOptions` | `IEnumerable<PropertyInfo>` | `ArgumentNullException` / `InvalidOperationException` |
| `TypeVisit.GetFields(Type)` | 获取字段列表 | `type` | `IEnumerable<FieldInfo>` | `ArgumentNullException` |
| `TypeVisit.GetMethodBySignature(Type, MethodInfo)` | 按签名从目标类型匹配方法（支持泛型参数位点比较） | `type`,`method` | `MethodInfo` | `ArgumentNullException` / `MissingMethodException` |
| `TypeVisit.FindMethodByReturnType(Type, string, Type, ...)` | 按名称、返回类型和参数签名查找方法；返回类型参与缓存 key | `type`,`name`,`returnType`,`bindingFlags`,`genericParameterCount`,`parameterSignatures` | `MethodInfo` | `ArgumentNullException` / `ArgumentException` / `MissingMethodException` / `AmbiguousMatchException` |
| `TypeVisit.GetMatchingConstructor(Type, Type[])` | 按参数类型匹配公共构造函数 | `type`,`constructorParameterTypes` | `ConstructorInfo` | `ArgumentNullException` / `ArgumentException` / `AmbiguousMatchException` |
| `TypeVisit.GetParameterlessConstructor(Type)` | 获取公共无参构造函数 | `type` | `ConstructorInfo` | `ArgumentNullException` / `ArgumentException` |
| `TypeMetaVisitExtensions.IsAsyncMethod(MethodInfo)` | 判断是否 Task/ValueTask（含泛型）返回类型 | `method` | `bool` |（无显式异常） |
| `TypeMetaVisitExtensions.IsNumeric/IsTupleType/IsStructType(...)` | 基于成员/参数实际类型进行类型判断 | `MemberInfo`/`ParameterInfo` | `bool` |（无显式异常） |
| `TypeVisit.CreateDynamicType(IDictionary<string,Type>)` |（非 NETSTANDARD）基于规范化属性字典生成动态类型，null 类型推断为 `object` | `properties` | `Type` | `ArgumentNullException` / `ArgumentException` / `InvalidOperationException` / `PlatformNotSupportedException` |
| `TypeVisit.CreateInstance(IDictionary<string,object>)` |（非 NETSTANDARD）基于属性值字典创建动态对象并赋值，null 值推断为 `object` | `values` | `object` | `ArgumentNullException` / `ArgumentException` / `MissingMemberException` / `InvalidOperationException` |

> API 证据：
> - src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | AssemblyVisit.GetFileVersion/GetProductVersion
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | TypeVisit.CreateInstance(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.Constructor.cs | TypeVisit.HasParameterlessConstructor(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs | TypeVisit.GetProperties(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Fields.cs | TypeVisit.GetFields(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs | TypeVisit.GetMethodBySignature(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Signatures.cs | TypeVisit.FindMethodByReturnType(...)

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Reflection.AssemblyVisit` | 程序集版本信息访问 | 是（无状态） | 从程序集版本特性读取，缺失时回退到程序集名称中的版本；不依赖文件位置 |
| `Bing.Reflection.TypeVisit` | Type/Member 的访问器工具集合（partial） | 是（无状态） | 构造函数、方法签名使用弱键惰性缓存；可回收类型旁路第三方强缓存 |
| `Bing.Reflection.TypeMetaVisitExtensions` | 面向 `MemberInfo`/`ParameterInfo` 的类型判断与元数据扩展 | 是（无状态） | 内部把 `MemberInfo` 转换为“实际类型”后调用 `Bing.Reflection.Types.*` |
| `Bing.Dynamic.DynamicBase` | 动态类型基类：按属性名 Get/Set 值 | 实例级：读写属性本身线程安全取决于调用方 | 只访问公开实例属性；属性解析使用弱键惰性缓存，缺失/索引器/init-only 写入给出明确异常 |
| `Bing.Reflection.PropertyAccessOptions` | 属性筛选选项（Getter/Setter/Both） | 是（枚举） | 用于 `TypeVisit.GetProperties(...)` |
| `Bing.Reflection.PropertyReflectionHelper` | 内部属性筛选与访问校验 | 是（无状态） | `internal`，抛 `ArgumentException` 表示访问限制不满足 |
| `Bing.Reflection.TypeFactory` |（非 NETSTANDARD，internal）动态类型工厂 | 并发安全 | 属性名排序、AssemblyQualifiedName/运行时句柄组成签名；`Lazy<Type>` 单次 Emit，存活类型容量受限并使用弱引用 |

> 核心类型证据：
> - src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | AssemblyVisit
> - src/Bing.Utils.Reflection/Bing/Dynamic/DynamicBase.cs | DynamicBase
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs | PropertyAccessOptions / PropertyReflectionHelper / TypeVisit.GetProperties
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | TypeFactory

## 5. 依赖关系
- 直接依赖：
    - `Bing.Utils`（项目引用，提供 `Bing.Reflection.Types`、`TypeClass`、`TypeReflections` 等基础能力）
        - 证据：src/Bing.Utils.Reflection/dependency.props | ProjectReference ..\\Bing.Utils\\Bing.Utils.csproj
        - 证据：src/Bing.Utils/Bing/Reflection/Types/Types.cs | Types
        - 证据：src/Bing.Utils/Bing/Reflection/TypeClass.cs | TypeClass
        - 证据：src/Bing.Utils/Bing/Reflection/TypeReflections/TypeReflections.Attributes.cs | ReflectionOptions / TypeReflections
- 可选依赖：
    - `Bing.Extensions`（用于版本号字符串的正则替换扩展 `ReplaceWith`）
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | using Bing.Extensions / version.ReplaceWith(...)
- 禁止依赖：待确认（仓库未看到针对本模块的“禁止依赖”显式约束；如需补充请以架构规则或代码引用为证据）。

## 6. 关键实现说明
### 6.1 算法/流程
- 创建实例（`TypeVisit.CreateInstance`）：
    1) 若 `type == null` 抛 `ArgumentNullException`；
    2) 由核心严格构造绑定器按精确类型、可赋值基类/接口和可空参数选择唯一公共构造函数；
    3) 不进行隐式数值转换，未命中或候选歧义分别抛出 `ArgumentException` 或 `AmbiguousMatchException`；
    4) 构造函数执行异常包装为 `InvalidOperationException` 并保留原始异常链。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | CreateInstance/CreateInstanceImpl
    - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | WrongSort 抛 `ArgumentException` 的断言

- 程序集版本读取（`AssemblyVisit`）：
    - 文件版本优先读取 `AssemblyFileVersionAttribute`，缺失时回退到 `Assembly.GetName().Version`；产品版本依次读取 `AssemblyInformationalVersionAttribute`、文件版本特性和程序集版本，均缺失时返回空字符串。产品版本沿用 `ReplaceWith(@"\+(\w+)?", "")` 正则替换，不承诺对任意格式的构建元数据完整截断。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | GetFileVersion/GetProductVersion
    - 证据：tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs | 版本读取断言

- 动态类型生成（非 NETSTANDARD，`TypeFactory.CreateType`）：
    - 按属性名排序并使用属性类型的程序集限定名和运行时句柄组成规范化缓存 key；
    - 通过 `AssemblyBuilder.DefineDynamicAssembly(...).DefineDynamicModule(...)` 创建模块；
    - 为每个属性生成私有字段与 get_/set_ 方法并 IL Emit；
    - 动态类型继承 `DynamicBase`，通过每模式 `Lazy<Type>` 避免并发重复 Emit，并以弱引用和 4096 个存活模式上限保护元数据资源。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | DefineDynamicAssembly/DefineType/IL Emit
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.Utilities.cs | SignType(MD5)
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.DynamicType.cs | TypeVisit.CreateDynamicType

### 6.2 边界与异常处理
- `TypeVisit.CreateInstance(Type, ...)`：
    - `type == null`：抛 `ArgumentNullException`。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | CreateInstance(Type,...)
    - 构造函数不匹配：抛出带参数上下文的 `ArgumentException`，接口/抽象类型也明确拒绝。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | StrictConstructorBinder
        - 证据：tests/Bing.Utils.Reflection.Tests/Bing/Reflection/TypeVisitConstructorBindingTests.cs | InvalidInputs/Assignability 用例

- `TypeVisit.CreateInstance(IDictionary<string, object> values)`（非 NETSTANDARD）：
    - 先取得单次字典快照；null 值推断为 `object`，null 字典、非法属性名或不可访问属性会抛出明确异常。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.Object.cs | CreateObject(...)

- `DynamicBase.GetPropertyValue/SetPropertyValue`：
    - 空名称、缺失属性、索引器和 init-only setter 均在入口给出明确异常；默认只访问公开实例属性。
        - 证据：src/Bing.Utils.Reflection/Bing/Dynamic/DynamicBase.cs | GetProperty/ValidateProperty

- `TypeVisit.GetProperties(Type, PropertyAccessOptions)`：
    - `type == null`：抛 `ArgumentNullException`；`accessOptions` 未知值：抛 `InvalidOperationException`。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs | GetProperties(Type, PropertyAccessOptions)

- `TypeVisit.GetMethodBySignature`：
    - `type/method == null`：抛 `ArgumentNullException`；未命中时抛 `MissingMethodException`。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs | GetMethodBySignature(...)

- `TypeVisit.FindMethod`：
    - 参数合法但没有唯一匹配时抛 `MissingMethodException`；多个候选时抛 `AmbiguousMatchException`。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Signatures.cs | ResolveMethodBySignature/ResolveNamedMethod

## 7. 性能与复杂度
- 时间复杂度：
    - `CreateInstance`：构造函数查找与参数类型推导主要依赖反射，通常与构造函数数量/参数数量相关（具体由运行时反射实现决定）。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | GetConstructor(...) / Types.Of(args)
    - `TypeFactory.CreateType`：模式规范化含属性名排序，通常为 O(n log n)（n=属性数量），IL Emit 与属性数量线性相关；已有字典身份快路径可复用模式，但具体分配和耗时以探针为准。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | foreach properties / _generatedTypes
- 空间复杂度：
    - 动态类型缓存按规范化属性签名受容量保护，并使用弱引用类型键；构造函数、方法签名、成员路径和属性元数据缓存达到容量后旁路解析，避免无界增长。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | _generatedTypes
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs | MethodSignatureCacheCapacity
        - 证据：src/Bing.Utils/Bing/Reflection/Reflections/Reflections.MemberPath.cs | MemberPathCacheCapacity
- 大数据量表现：尚无经批准的业务阈值；已有冷启动、稳态、并发缓存及动态 schema 探针，数据仅用于趋势观察。
- Benchmark 入口：`benchmarks/Bing.Utils.Benchmark/Benchmarks/ReflectionBenchmarks.cs`；比较探针：`benchmarks/ReflectionComparisonProbe/Program.cs`。

## 8. 测试策略
- 单测覆盖点：
    - `TypeVisit.CreateInstance`：无参/有参创建、错误参数顺序抛 `ArgumentException`、指定 Type 转换为泛型等。
        - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | Trait(TypeVisit.CreateInstance) + 多组用例
    - `AssemblyVisit.GetFileVersion/GetProductVersion`：读取版本并断言返回值。
        - 证据：tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs | Test_GetFileVersion_1/Test_GetProductVersion_1
- 边界用例：
    - 构造函数不匹配时抛 `ArgumentException`，并覆盖可赋值、null、歧义和无匹配路径。
        - 证据：tests/Bing.Utils.Reflection.Tests/Bing/Reflection/TypeVisitConstructorBindingTests.cs | CreateInstance/GetMatchingConstructor 用例
    - 动态类型创建（TypeFactory/动态 CreateInstance 字典重载）：覆盖 null 值、空 schema、255 属性边界、并发 Emit、存活容量和可回收程序集。
        - 证据：tests/Bing.Utils.Reflection.Tests/Bing/Reflection/DynamicTypeFactoryTests.cs
    - 回归用例：失败契约、部分类型加载、缓存容量旁路、并发首次访问、基线归档和四条 trim/AOT 部署边界均已有独立验证；业务冷启动阈值与未保留元数据的完整 NativeAOT/trimming 矩阵仍见“待办与改进”。

> 测试工程目录备注：反射子包测试位于 `tests/Bing.Utils.Reflection.Tests/`；核心包反射入口测试位于 `tests/Bing.Utils.Tests/`。

## 9. 版本与兼容性
- 当前版本：1.5.0
    - 证据：version.props | VersionMajor/VersionMinor/VersionPatch
- 破坏性变更：方法签名查找（`FindMethod`、`GetMethodBySignature`）未命中时抛 `MissingMethodException`；构造函数元数据查询（`GetMatchingConstructor`、`GetParameterlessConstructor`）未命中时抛带 `args` 参数名的 `ArgumentException`。需要探测时使用 `HasParameterlessConstructor` 或对相应异常做显式处理。
- 升级建议：
    - 若使用动态类型生成能力，需确认目标框架非 NETSTANDARD（该能力被 `#if !NETSTANDARD` 包围）。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | #if !NETSTANDARD
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.DynamicType.cs | #if !NETSTANDARD

## 10. 使用示例
```csharp
using System.Reflection;
using Bing.Reflection;

// 1) 读取程序集版本
var assembly = typeof(object).Assembly;
var fileVersion = AssemblyVisit.GetFileVersion(assembly);
var productVersion = AssemblyVisit.GetProductVersion(assembly);

// 2) 运行时创建实例（构造函数不匹配时抛出明确异常）
var instance = TypeVisit.CreateInstance(typeof(string), new char[] { 'a', 'b' });

// 3) 从表达式获取属性元数据（Get/Set 可见性控制）
var props = TypeVisit.GetProperties<DateTime>(PropertyAccessOptions.Getters);

// 4)（非 NETSTANDARD）动态类型创建（仅展示 API，实际运行取决于目标框架）
// var dynamicType = TypeVisit.CreateDynamicType(new Dictionary<string, Type> { ["Name"] = typeof(string) });
// var obj = TypeVisit.CreateInstance(new Dictionary<string, object> { ["Name"] = "Alice" });
```

## 11. 待办与改进
- [ ] 如业务要求降低首次调用成本，先确定机器规格、输入规模和冷启动预算，再针对构造/签名/路径入口优化。
- [ ] 补齐未显式 linker roots、多程序集输入模型和更多反射入口的 NativeAOT/trimming 端到端矩阵；当前已覆盖 net8.0 framework-dependent analyzer、self-contained partial/full trim、metadata-only NativeAOT、两个显式 root 外部程序集、一个仅在 framework-dependent 目标目录暂存的未 root 外部插件，以及独立可回收插件 probe 的路径、`AssemblyDependencyResolver`、字节数组加载和字段/属性/构造/方法/路径主要入口；仍未覆盖所有入口和更大规模的组合。
- [ ] 待确认：业务运行时间预算和正式发布阈值。工作流的 `baseline-comparison` 已使用 PR 基准 SHA，手动运行可指定 `baseline_ref`；该配置不代表远程 CI 已执行通过。

## 12. 证据定位（汇总）
- src/Bing.Utils.Reflection/Bing.Utils.Reflection.csproj | 包描述（反射操作类库）
- src/Bing.Utils.Reflection/dependency.props | ProjectReference -> Bing.Utils
- src/Bing.Utils.Reflection/references.props | 为空（用途待确认）
- src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | AssemblyVisit.GetFileVersion/GetProductVersion
- src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs | MemberVisitHelper / TypeMetaVisitExtensions.IsNumeric/IsTupleType/IsStructType
- src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | TypeVisit.CreateInstance(...) / CreateInstanceImpl
- src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs | TypeVisit.GetMethodBySignature/IsVisible...
- src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs | TypeVisit.GetProperties/GetProperty/Exclude/PropertyAccessOptions
- src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Fields.cs | TypeVisit.GetFields/GetField
- src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.Constructor.cs | HasParameterlessConstructor/GetParameterlessConstructor
- src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.DynamicType.cs | TypeVisit.CreateDynamicType (#if !NETSTANDARD)
- src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | TypeFactory.CreateType (Reflection.Emit + cache)
- src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.Utilities.cs | TypeFactory.SignType(MD5)
- src/Bing.Utils.Reflection/Bing/Dynamic/DynamicBase.cs | DynamicBase.GetPropertyValue/SetPropertyValue
- src/Bing.Utils/Bing/Reflection/Types/Types.cs | Types（核心包提供）
- src/Bing.Utils/Bing/Reflection/Types/Types.Of.cs | Types.Of<T>/TypeOfOptions
- src/Bing.Utils/Bing/Reflection/Types/Types.Is.cs | Types.IsTupleType/IsNumericType 等
- src/Bing.Utils/Bing/Reflection/TypeClass.cs | TypeClass.TaskClazz/ValueTaskClazz 等（供 IsAsyncMethod 使用）
- src/Bing.Utils/Bing/Reflection/TypeReflections/TypeReflections.Attributes.cs | ReflectionOptions/TypeReflections
- tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | TypeVisit.CreateInstance 覆盖
- tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs | AssemblyVisit 覆盖
- tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsStrictContractTests.cs | 部分类型加载失败夹具与严格失败契约
- docs/quality/reflection-contract-migration.md | 严格失败契约迁移矩阵与仓库消费者清单
- benchmarks/ReflectionComparisonProbe/Run-ReflectionComparison.ps1 | 支持 `-BaselineRef` 的干净 Git 基线归档和完整比较
- tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimProbe | net8.0 显式 linker roots 的 trimming、self-contained partial/full 和 NativeAOT metadata smoke，以及未 root 外部程序集边界探针
- docs/quality/reflection-trim-evidence.md | trimming 命令、结果和支持边界

## 13. 待确认
- 缓存达到容量后会旁路解析；高基数输入下的延迟和旁路比例需要结合业务观测设定告警。
- `src/Bing.Utils.Reflection/references.props` 为空是否符合预期、是否由 common.props 提供依赖注入。
- 常规 CLR、显式 linker roots 下的 self-contained partial/full trim 和 metadata-only NativeAOT smoke 已验证；当前 SDK 对 framework-dependent `PublishTrimmed` 返回 `NETSDK1102`，该路径只执行 trim analyzer 与未裁剪契约检查。旧 TFM 的 .NET Core 3.1.32/.NET 5.0.17 精准矩阵已通过（反射模块 159/159、核心反射筛选 444/444），不代表旧 TFM 全量解决方案测试或远程 CI 已通过。未保留元数据的完整矩阵仍未完成。
