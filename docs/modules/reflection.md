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
    - 运行时按 Type + 构造参数创建实例（构造函数签名匹配），且允许“找不到构造函数时返回 null”。
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
        tests/Bing.Utils.Reflection.Tests/（待确认：仓库中未找到该目录）
        tests/BingUtilsUT/（存在与本模块相关用例）
        tests/Bing.Utils.Tests/（存在与本模块相关用例）
        - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | TypeVisit.CreateInstance(...)
        - 证据：tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs | AssemblyVisit.GetFileVersion/GetProductVersion

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `AssemblyVisit.GetFileVersion(Assembly)` | 读取程序集文件版本 | `assembly` | `string` | `ArgumentNullException`（assembly 为空时） | 
| `AssemblyVisit.GetProductVersion(Assembly)` | 读取程序集产品版本；若含 `+` build metadata 则移除 | `assembly` | `string` | `ArgumentNullException`（assembly 为空时） | 
| `TypeVisit.CreateInstance(Type, params object[])` | 按类型 + 构造参数创建实例（签名匹配） | `type`,`args` | `object`（可能为 null） | `ArgumentNullException`（type 为空时） | 
| `TypeVisit.CreateInstance<T>(params object[])` | 按泛型类型创建实例（内部使用 `Types.Of<T>()`） | `args` | `T`（可能为 default/null） |（内部不显式抛出；构造函数未命中时返回 default） |
| `TypeVisit.CreateInstance<T>(Type, params object[])` | 按指定 Type 创建实例并尝试转换为 `T` | `type`,`args` | `T`（可能为 default/null） | `ArgumentNullException`（type 为空时） |
| `TypeVisit.HasParameterlessConstructor(Type)` | 推测是否存在无参构造函数 | `type` | `bool` | `ArgumentNullException` |
| `TypeVisit.GetProperties(Type, PropertyAccessOptions)` | 按访问器可见性筛选属性 | `type`,`accessOptions` | `IEnumerable<PropertyInfo>` | `ArgumentNullException` / `InvalidOperationException` |
| `TypeVisit.GetFields(Type)` | 获取字段列表 | `type` | `IEnumerable<FieldInfo>` | `ArgumentNullException` |
| `TypeVisit.GetMethodBySignature(Type, MethodInfo)` | 按签名从目标类型匹配方法（支持泛型参数位点比较） | `type`,`method` | `MethodInfo`（可能为 null） | `ArgumentNullException` |
| `TypeMetaVisitExtensions.IsAsyncMethod(MethodInfo)` | 判断是否 Task/ValueTask（含泛型）返回类型 | `method` | `bool` |（无显式异常） |
| `TypeMetaVisitExtensions.IsNumeric/IsTupleType/IsStructType(...)` | 基于成员/参数实际类型进行类型判断 | `MemberInfo`/`ParameterInfo` | `bool` |（无显式异常） |
| `TypeVisit.CreateDynamicType(IDictionary<string,Type>)` |（非 NETSTANDARD）基于属性字典生成动态类型 | `properties` | `Type` |（未显式 guard；入参为空/含异常值时行为待确认） |
| `TypeVisit.CreateInstance(IDictionary<string,object>)` |（非 NETSTANDARD）基于属性值字典创建动态对象并赋值 | `values` | `object` |（未显式 guard；values/value 为空时可能抛异常） |

> API 证据：
> - src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | AssemblyVisit.GetFileVersion/GetProductVersion
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | TypeVisit.CreateInstance(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.Constructor.cs | TypeVisit.HasParameterlessConstructor(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs | TypeVisit.GetProperties(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Fields.cs | TypeVisit.GetFields(...)
> - src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs | TypeVisit.GetMethodBySignature(...)

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Reflection.AssemblyVisit` | 程序集版本信息访问 | 是（无状态） | 内部使用 `FileVersionInfo.GetVersionInfo(assembly.Location)` |
| `Bing.Reflection.TypeVisit` | Type/Member 的访问器工具集合（partial） | 是（无状态） | 多处依赖 `AspectCore.Extensions.Reflection` 进行调用/反射加速 |
| `Bing.Reflection.TypeMetaVisitExtensions` | 面向 `MemberInfo`/`ParameterInfo` 的类型判断与元数据扩展 | 是（无状态） | 内部把 `MemberInfo` 转换为“实际类型”后调用 `Bing.Reflection.Types.*` |
| `Bing.Dynamic.DynamicBase` | 动态类型基类：按属性名 Get/Set 值 | 实例级：读写属性本身线程安全取决于调用方 | 通过 `GetProperty(name).GetReflector()` 获取访问器；name 不存在会导致异常（见边界说明） |
| `Bing.Reflection.PropertyAccessOptions` | 属性筛选选项（Getter/Setter/Both） | 是（枚举） | 用于 `TypeVisit.GetProperties(...)` |
| `Bing.Reflection.PropertyReflectionHelper` | 内部属性筛选与访问校验 | 是（无状态） | `internal`，抛 `ArgumentException` 表示访问限制不满足 |
| `Bing.Reflection.TypeFactory` |（非 NETSTANDARD，internal）动态类型工厂 | 部分（有缓存；并发细节待确认） | 静态 `ConcurrentDictionary` 缓存；使用 Reflection.Emit 创建 sealed 类型并继承 `DynamicBase` |

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
    - `AspectCore.Extensions.Reflection`（用于 `GetReflector()` 调用与属性 Get/Set）
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | using AspectCore.Extensions.Reflection
        - 证据：src/Bing.Utils.Reflection/Bing/Dynamic/DynamicBase.cs | using AspectCore.Extensions.Reflection
    - `Bing.Extensions`（用于版本号字符串的正则替换扩展 `ReplaceWith`）
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | using Bing.Extensions / version.ReplaceWith(...)
- 禁止依赖：待确认（仓库未看到针对本模块的“禁止依赖”显式约束；如需补充请以架构规则或代码引用为证据）。

## 6. 关键实现说明
### 6.1 算法/流程
- 创建实例（`TypeVisit.CreateInstance`）：
    1) 若 `type == null` 抛 `ArgumentNullException`；
    2) 若无参数：尝试 `type.GetConstructor(Type.EmptyTypes)?.GetReflector().Invoke()`；
    3) 若有参数：以 `Types.Of(args)` 推导参数类型后尝试 `type.GetConstructor(paramTypes)?.GetReflector().Invoke(args)`；
    4) 构造函数未命中时返回 `null`（泛型重载通过 `AsOrDefault<T>()` 转 default/null）。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | CreateInstance/CreateInstanceImpl
    - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | WrongSort 返回 null 的断言

- 程序集版本读取（`AssemblyVisit`）：
    - 通过 `FileVersionInfo.GetVersionInfo(assembly.Location)` 读取文件版本与产品版本；若产品版本字符串包含 `+`，使用 `ReplaceWith(@"\\+(\\w+)?", "")` 移除构建元数据。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | V(...)/GetProductVersion(...)
    - 证据：tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs | 版本读取断言

- 动态类型生成（非 NETSTANDARD，`TypeFactory.CreateType`）：
    - 以属性签名字符串计算 MD5（`SignType`）作为缓存 key；
    - 通过 `AssemblyBuilder.DefineDynamicAssembly(...).DefineDynamicModule(...)` 创建模块；
    - 为每个属性生成私有字段与 get_/set_ 方法并 IL Emit；
    - 动态类型继承 `DynamicBase`，并通过 `ConcurrentDictionary` 缓存生成结果。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | DefineDynamicAssembly/DefineType/IL Emit
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.Utilities.cs | SignType(MD5)
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.DynamicType.cs | TypeVisit.CreateDynamicType

### 6.2 边界与异常处理
- `TypeVisit.CreateInstance(Type, ...)`：
    - `type == null`：抛 `ArgumentNullException`。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | CreateInstance(Type,...)
    - 构造函数不匹配：返回 `null`（而非抛异常）。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | GetConstructor(...)?
        - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | WrongSort 用例

- `TypeVisit.CreateInstance(IDictionary<string, object> values)`（非 NETSTANDARD）：
    - 未见对 `values == null` / `values` 中 value 为 null 的 guard；`_.Value.GetType()` 在 value 为 null 时可能触发 `NullReferenceException`（行为待确认）。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | values.ToDictionary(_ => _.Key, _ => _.Value.GetType())

- `DynamicBase.GetPropertyValue/SetPropertyValue`：
    - 未见对 `name` 空值、属性不存在的 guard；`_type.GetProperty(name)` 返回 null 时后续 `.GetReflector()` 可能触发异常（行为待确认）。
        - 证据：src/Bing.Utils.Reflection/Bing/Dynamic/DynamicBase.cs | GetProperty(name).GetReflector()

- `TypeVisit.GetProperties(Type, PropertyAccessOptions)`：
    - `type == null`：抛 `ArgumentNullException`；`accessOptions` 未知值：抛 `InvalidOperationException`。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs | GetProperties(Type, PropertyAccessOptions)

- `TypeVisit.GetMethodBySignature`：
    - `type/method == null`：抛 `ArgumentNullException`；未命中时返回 null。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs | GetMethodBySignature(...)

## 7. 性能与复杂度
- 时间复杂度：
    - `CreateInstance`：构造函数查找与参数类型推导主要依赖反射，通常与构造函数数量/参数数量相关（具体由运行时反射实现决定）。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | GetConstructor(...) / Types.Of(args)
    - `TypeFactory.CreateType`：生成签名 O(n)（n=属性数量），IL Emit 亦与属性数量线性相关；生成后命中缓存可近似 O(1) 获取。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | foreach properties / _generatedTypes
- 空间复杂度：
    - 动态类型会被缓存（`ConcurrentDictionary`），可能随不同属性签名增长。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | _generatedTypes
- 大数据量表现：待确认（仓库内未提供针对 Reflection 模块的基准测试用例）。
- Benchmark 链接：待补（benchmarks/Bing.Utils.Benchmark/ 中未检索到 Reflection 专项用例，待确认）。

## 8. 测试策略
- 单测覆盖点：
    - `TypeVisit.CreateInstance`：无参/有参创建、错误参数顺序返回 null、指定 Type 转换为泛型等。
        - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | Trait(TypeVisit.CreateInstance) + 多组用例
    - `AssemblyVisit.GetFileVersion/GetProductVersion`：读取版本并断言返回值。
        - 证据：tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs | Test_GetFileVersion_1/Test_GetProductVersion_1
- 边界用例：
    - 构造函数不匹配时返回 null（已覆盖）。
        - 证据：tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs | WrongSort
    - 动态类型创建（TypeFactory/动态 CreateInstance 字典重载）：待补（当前未找到对应单测）。
        - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | TypeFactory.CreateType
- 回归用例：建议补充（见“待办与改进”）。

> 测试工程目录备注：模板中的 tests/Bing.Utils.Reflection.Tests/ 在仓库中未找到，当前相关测试分散在其他测试工程中（见上方证据）。

## 9. 版本与兼容性
- 当前版本：1.5.0
    - 证据：version.props | VersionMajor/VersionMinor/VersionPatch
- 破坏性变更：待确认（本文件仅基于当前仓库状态，未对历史版本进行比对）。
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

// 2) 运行时创建实例（构造函数不匹配时可能返回 null）
var instance = TypeVisit.CreateInstance(typeof(string), new char[] { 'a', 'b' });

// 3) 从表达式获取属性元数据（Get/Set 可见性控制）
var props = TypeVisit.GetProperties<DateTime>(PropertyAccessOptions.Getters);

// 4)（非 NETSTANDARD）动态类型创建（仅展示 API，实际运行取决于目标框架）
// var dynamicType = TypeVisit.CreateDynamicType(new Dictionary<string, Type> { ["Name"] = typeof(string) });
// var obj = TypeVisit.CreateInstance(new Dictionary<string, object> { ["Name"] = "Alice" });
```

## 11. 待办与改进
- [ ] 补充 Reflection 模块独立测试工程（待确认是否计划新增 tests/Bing.Utils.Reflection.Tests）。
    - 证据：docs/modules/reflection.md | 目录结构声明 tests/Bing.Utils.Reflection.Tests（当前未找到）
- [ ] 为动态类型生成与字典 CreateInstance 增加单测：`values == null`、value 为 null、属性不存在等边界。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs | CreateInstance(IDictionary<string,object>) 无 guard
    - 证据：src/Bing.Utils.Reflection/Bing/Dynamic/DynamicBase.cs | GetProperty(name) 无 guard
- [ ] 评估并发场景下 `TypeFactory.CreateType` 重复生成同签名类型的可能性（待确认是否需要加锁/双检）。
    - 证据：src/Bing.Utils.Reflection/Bing/Reflection/TypeFactory/TypeFactory.cs | TryGetValue 后直接 DefineType

## 12. 证据定位（汇总）
- src/Bing.Utils.Reflection/Bing.Utils.Reflection.csproj | 包描述（反射操作类库）
- src/Bing.Utils.Reflection/dependency.props | ProjectReference -> Bing.Utils
- src/Bing.Utils.Reflection/references.props | 为空（用途待确认）
- src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs | AssemblyVisit.GetFileVersion/GetProductVersion/V
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

## 13. 待确认
- tests/Bing.Utils.Reflection.Tests/ 是否应存在（模板中列出但仓库未找到）。
- src/Bing.Utils.Reflection/references.props 为空是否符合预期、是否由 common.props 提供依赖注入。
- TypeFactory 并发创建同签名类型的行为是否需要额外同步保证。
