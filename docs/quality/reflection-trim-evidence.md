# 反射 trimming 验证证据

更新时间：2026-10-13

## 当前候选验证

2026-10-13 在 SDK 8.0.425 下新鲜发布 net8.0/win-x64、self-contained、NativeAOT、partial、portable PDB 探针，原生 exe 退出码为 0，并输出元数据、特性回退、byref 签名、ref return 和泛型 variance 标记。补齐生产契约后诊断报告为 260 条、244 条仓库源、218 个仓库指纹。相对冻结历史基线新增的 24 个探针指纹已获用户批准，加入新版 363 项、429 次上限的基线和责任清单；复核已有发布日志，五项严格门禁全部通过，新增 ID、新增仓库指纹和未解析数均为 0。详细差异见 [基线复核清单](reflection-nativeaot-baseline-review.md)。

以下各节保留各自日期的历史证据。“本轮”“当前”、421/405/339 等计数不是最新候选的验证结果；历史基线保留用于发现漂移，未以当前报告覆盖。远程 CI 和本次未执行的完整部署矩阵统一标记为待确认。

基线文件已集中到 `benchmarks/ReflectionTrimProbe/baselines/`，其中四份当前配置供 CI 读取，`history/20261013-before-approval/` 保存旧 339 项快照。下文历史文件位置不再作为当前命令入口；当前配置、操作命令与目录整理方案见 [反射验证目录与基线](reflection-validation-layout.md)。

## Linker root 窄化子项（2026-10-08）

主探针的 `TrimParameterlessSample` root 已从 `preserve="all"` 收窄为
`preserve="nothing"` 加显式 `System.Void .ctor()` 方法签名。临时隔离的 net8.0/win-x64
`TrimMode=full` smoke 发布成功，外部加载发布后的 `ReflectionTrimProbe.dll` 验证：类型和无参
构造函数存在，未声明属性和方法均已被裁剪；运行入口输出
`reflection-trim-root-narrowing-ok`。`TrimMode=partial` 对应用程序集公共成员的保留策略不会
证明成员收窄，因此只记录为辅助发布检查。其它主探针类型随后也按同一方式完成了显式 root 拆分，
TRIM-2 的剩余内容转为完整矩阵和资源边界验证。

同一隔离 smoke 还验证了 `TrimBaseSample`、`TrimDerivedSample`、`TrimGenericBase<T>`、
`TrimGenericDerived` 和 `TrimStatus` 的 `preserve="nothing"` root：发布 DLL 仍保留基类链、
闭合泛型基类、泛型定义及枚举底层类型，运行入口输出
`reflection-trim-type-root-narrowing-ok`。接口/集合关系随后分别通过 `preserve="nothing"` 的
`ITrimContract`、`TrimImplementation` 和 `TrimCollection` smoke 验证，`GetInterfaces()`/泛型接口
闭包扫描仍保持有效。

`TrimSample` 随后完成成员级拆分：`preserve="nothing"` 下显式保留
`System.Void .ctor(System.Int32)`、`Value` 属性、`Execute` 方法及
`System.ComponentModel.DescriptionAttribute`。隔离 full-trim smoke 输出
`reflection-trim-member-root-narrowing-ok`；外部读取发布 DLL 确认类型描述、属性描述、getter/setter、
构造函数和 `Execute` 存在，未声明属性/方法被裁剪。该证据验证 linker descriptor 语义，不替代主探针
完整构造、路径、方法缓存和 NativeAOT 矩阵；主探针 root 已全部进入显式成员保留，剩余工作是
完整矩阵、复杂程序集图和资源边界验证。

闭合泛型 `TrimGenericSample<T>` 也完成成员级拆分：`preserve="nothing"` 下保留构造函数、
`Value` 属性和 `Execute` 方法。隔离 full-trim smoke 输出
`reflection-trim-generic-member-root-narrowing-ok`；外部将定义闭合为 `TrimGenericSample<int>`，
确认构造、属性和方法均可解析，未声明方法已被裁剪。该证据只覆盖泛型成员 root 的 linker 语义，
不替代主探针的完整泛型批量构造和 NativeAOT 运行矩阵。

值类型 `TrimValueSample` 也改为 `preserve="nothing"`，仅保留 `System.Int32` 构造函数和
`Value` 属性。隔离 full-trim smoke 输出 `reflection-trim-value-root-narrowing-ok`，外部通过
`ConstructorInfo.Invoke` 创建装箱值并调用 `PropertyInfo.SetValue`，确认属性值可写回为 `4`。
值类型的其它字段保留行为未被纳入本轮契约，完整主探针仍需复核 ref setter 与 NativeAOT 装箱路径。

成员路径中间节点 `TrimContainer` 也完成拆分：`preserve="nothing"` 下保留
`System.Void .ctor(ReflectionTrimProbe.TrimSample)` 和 `Child` 属性。隔离 full-trim smoke 输出
`reflection-trim-container-root-narrowing-ok`，外部构造中间对象后读取嵌套 `Child.Value=17`，并确认
未声明属性已被裁剪。该证据覆盖中间节点元数据，不替代主探针完整路径 setter 和非公开路径矩阵。

`TrimFieldSample` 的 root 进一步拆分为构造函数、`PublicField`、`PrivateField` 和
`ReadPrivateField`，仍使用 `preserve="nothing"`。隔离 full-trim smoke 输出
`reflection-trim-field-root-narrowing-ok`；外部同时读写公开/私有字段，方法返回值为 `24`，并确认
未声明属性已被裁剪。这只验证显式 `IncludeNonPublic` 所需字段元数据已被 root，不扩大为默认公开访问权限。

非公开属性路径 `TrimPrivateContainer`/`TrimPrivateNode` 随后完成拆分：两者均使用
`preserve="nothing"`，显式保留中间 `Child`、私有 `Value`、构造函数和 `ReadValue`。
隔离 full-trim smoke 输出 `reflection-trim-private-path-root-narrowing-ok`；显式非公开访问将
`Value` 写为 `32` 并读回 `32`，同时确认无关属性被裁剪。该验证保持默认路径拒绝非公开成员的边界，
不扩大 `IncludeNonPublic` 的默认权限。

成员分类 root 随后完成拆分：`TrimMemberBase`、`TrimMemberDerived` 和 `TrimMemberSample` 均改为
`preserve="nothing"`，显式保留虚属性/虚方法、分类探针所需的属性、字段、普通方法和异步方法。
隔离 full-trim smoke 输出 `reflection-trim-member-root-narrowing-ok`；外部读取发布 DLL 确认
7 个无关属性/字段/方法已裁剪。该证据覆盖成员分类和基类签名回溯使用的元数据，不替代主探针完整
NativeAOT 发布矩阵。

无成员的接口契约 `ITrimContract` 与实现 `TrimImplementation` 也改为 `preserve="nothing"`。
隔离 full-trim smoke 保持接口赋值和接口枚举关系，并输出 `external-member-interface-pruning-ok`；
该项只验证类型/接口闭包，不扩大到集合实现。

`TrimCollection` 的集合关系 root 已单独验证并改为 `preserve="nothing"`。隔离 full-trim smoke
输出 `reflection-trim-collection-root-narrowing-ok`，`IEnumerable<int>` 赋值和接口扫描均保持有效；
外部程序集模型随后也完成独立的 `preserve="nothing"` 验证。

外部 `ReflectionTrimProbe.Models.UnrootedModel` root 已从 `preserve="all"` 收窄为无参构造函数和
私有 `ReflectedValue` 属性。隔离 net8.0/win-x64 full-trim smoke 通过程序集名称加载该模型，完成
属性写入/读取并输出 `reflection-trim-cross-assembly-root-narrowing-ok`；外部读取发布 DLL 的元数据
确认构造函数和非公开属性均保留。

外部 `ReflectionTrimProbe.PluginModels.PluginModel`/`PluginNode` root 同样改为 `preserve="nothing"`，
显式保留 `Int32` 构造函数、私有 `Child`/`Secret` 属性。隔离 smoke 沿跨程序集私有路径写入 `52`
并读回，外部元数据检查确认两级路径成员存在。该证据关闭主探针原有三个 `preserve="all"` 根，
但不替代更多程序集图、未 root 插件和资源容量边界矩阵。

## 参数化方法签名 root（2026-10-08）

`TrimSample` 和 `TrimGenericSample<T>` 的无参 `Execute` root 已改为精确的
`System.Void Execute()` 签名；`TrimMemberSample` 增加同名 `Execute(string)` 重载，而
`TrimmerRoots.xml` 只声明 `Execute(System.Int32)`。为避免 trimmed 程序中的通用
`Type.GetMethod` 查询扩大保留集合，隔离 smoke 直接调用 `Execute(int)`，full-trim 运行输出
`reflection-trim-signature-root-ok`；外部读取发布 DLL 确认 `Execute(Int32)` 存在且
`Execute(String)` 已被裁剪。该证据关闭一个参数化 signature root 子项，不代表所有复杂
重载、泛型约束和跨程序集 root 组合都已完成。

同一第二程序集的构造函数边界另以独立显式 `IsTrimmable` full-trim smoke 验证：
`CompositeSignatureRootModel` 增加 `System.Void .ctor(System.String)`，root descriptor
只声明 `System.Void .ctor(System.Int32)`；宿主输出
`reflection-trim-constructor-signature-root-ok`，外部元数据确认无参和字符串构造函数已裁剪。
正式多程序集 host 本轮未重新发布，不能把该结果扩大为 NativeAOT 或完整插件矩阵结论。

## 已验证范围

`benchmarks/ReflectionTrimProbe` 覆盖需要保留元数据的公共 CLR 路径：

- 严格公共构造函数绑定；
- 公共属性 getter/setter 委托；
- 按名称和参数类型的方法签名查询；
- 闭合泛型模型 `TrimGenericSample<int>` 的构造、属性和方法元数据；
- `TrimContainer.Child.Value` 两层成员路径读取和写入；
- 公共字段读取/写入，以及显式 `BindingFlags.NonPublic` 的私有字段读取/写入；
- `TypeVisit` 字段/属性扫描和 `TypeReflections.GetPublicInstanceProperties` 类型缓存入口；
- 成员路径默认拒绝私有属性，显式 `IncludeNonPublic` 后允许私有属性读取/写入。
- 独立 `ReflectionTrimProbe.Models` 程序集中的私有属性，通过独立 linker root 后读取；
- 独立 `ReflectionTrimProbe.PluginModels` 程序集中的带参数构造、私有中间属性和嵌套私有路径读写，通过第二组 linker root 后验证。
- 独立 `ReflectionTrimProbe.UnrootedModels` 程序集不加入 `TrimmerRoots.xml`，也不再作为主探针的项目引用；framework-dependent 验证仅在独立 probe 执行前把它暂存到目标目录，trim/AOT 发布不携带该外部插件，避免验证入口改变 linker 根集合。
- 独立 `ReflectionTrimProbe.UnrootedModelsHost` 故意把插件标记为 `IsTrimmable`，只通过程序集名称加载，并用 `TrimRoots.xml` 仅保留插件类型身份，模拟程序集存在但成员元数据未被 root 的部署边界。
- 独立 `ReflectionTrimPluginLoadProbe` 不引用该模型程序集，使用可回收 `AssemblyLoadContext` 分别覆盖路径加载、`AssemblyDependencyResolver` 和字节数组加载，并检查 `TypeVisit` 字段/属性、继承属性、getter-only/setter-only/静态属性过滤、索引器过滤、`TypeReflections` 公共属性缓存及可读/可写属性筛选、无参/带参构造函数签名及 `ConstructorInfo`/构造参数特性、`Reflections.GetFields/GetProperties/GetMethods/GetPropertyOrField` 集合和对象包装入口、泛型 `Reflections.GetFieldValue<T>/SetFieldValue<T>` 字段读写（包含基类/派生类隐藏同名字段按泛型声明类型分流）、`Reflections.GetField/GetProperty/GetMethod<T>` 严格入口、`Reflections` 运行时 `Type attributeType` 的单项/集合/必需单项/必需集合入口（覆盖 `Type`、`TypeInfo`、`FieldInfo`、`MemberInfo`）、`TypeReflections` 运行时特性选项和 `ParameterInfo` 入口（覆盖继承、歧义处理、单项/集合/必需单项/必需集合/存在性）、`Reflections`/`TypeReflections` 类型和属性描述、普通和泛型方法签名/闭合调用、`GetMethodBySignature`/`IsSignatureCompatible`/`GetBaseMethod`、直接类型谓词和反向特性判断、类型/方法全名、完整接口图、直接接口过滤、`GetBaseClasses`、公共常量递归、`FindImplementTypes` 实现类型扫描、`GetInstancesByInterface` 实例扫描以及私有成员路径；四种产物目录中 framework-dependent 三种方式均输出 `metadata-preserved`，partial/full trim 与 NativeAOT 三种方式未携带插件时均输出 `assembly-trimmed`；workflow 另将未 root 插件作为独立部署物放入 partial/full/NativeAOT 宿主输出旁路目录，三种加载方式均输出 `metadata-preserved`。
- 同一 probe 支持显式期望边界值；partial/full 的可裁剪插件宿主分别输出成员已裁剪的 DLL，旁路部署后 path/resolver/bytes 三种方式均输出 `member-trimmed`，未携带插件的原始产物仍要求 `assembly-trimmed`；NativeAOT 可裁剪插件宿主则在原生 exe 内按程序集/类型名称反射并断言同一 `member-trimmed` 边界。
- `DescriptionAttribute` 类型、属性、方法和参数元数据读取；同时覆盖 `Reflections`、`TypeReflections` 和 `Types` 的描述、泛型/非泛型存在性、单项、required 及特性集合入口。NativeAOT 下由属性入口切换到 `MemberInfo` 公开 API，避免第三方反射器的动态代码生成。
- 成员元数据分类入口：成员和参数的数值/元组/结构判断、异步/可见/重写方法判断、虚拟/抽象属性判断，以及 `TypeVisit.GetProperties`/`Exclude` 的表达式属性筛选。
- `TypeReflections` 的 `MemberInfo` 类型分类入口：布尔、日期、数值、枚举、结构、原始、值类型和集合判断；同时验证 nullable 成员在默认选项与 `IgnoreNullable` 选项下的差异。
- 主探针类型关系入口：`Reflections.IsCollection`/`IsGenericCollection`、`GetTopBaseType`、两种 `GetBaseClasses`、`IsAssignableTo(Type, Type)` 和 `GetFullNameWithAssemblyName`，同时覆盖数组、泛型集合、接口、协变赋值和停止类型边界。
- 接口泛型参数入口：`InterfaceOptions.Default` 的精确闭合接口匹配、`IgnoreGenericArgs` 对开放/闭合接口定义的匹配，以及 `Types` 与 `TypeReflections` 的泛型重载。
- NativeAOT 动态代码边界：`TypeVisit.CreateDynamicType` 和 `TypeVisit.CreateInstance(IDictionary<string, object>)` 在不支持动态代码的运行时均明确抛出 `PlatformNotSupportedException`；动态代码可用的未裁剪/trim 运行时继续执行正常探针。
- 构造入口矩阵：主探针在当前 net8 NativeAOT 配置下通过 `TypeVisit.CreateInstances<T>`、运行时类型批量构造、强类型批量构造和两个 `CreateGenericInstance<T>` 重载并校验实例结果；独立插件 probe 通过 `TypeVisit.CreateInstance(Type, string)` 与 `CreateInstances(IEnumerable<Type>, params object[])` 构造动态加载的未 root 模型类型。
- 方法结构和直接类型谓词入口：主探针验证 `GetBaseMethod`、`GetMethodBySignature`、`IsSignatureCompatible`、泛型/非泛型 `IsAttributeNotDefined`，以及 `Types` 的数值、原始、静态和匿名类型判断；四种部署产物均执行成功。
- `TypeJudge` 入口：主探针验证数值类型泛型/`TypeInfo` 重载、nullable/枚举 `TypeInfo` 重载和泛型实现判断；未 root 插件 probe 对同一组不依赖动态 Emit 的入口执行旁路验证。
- 方法/成员查询重载：主探针验证 `TypeVisit.GetField<T,TField>`、`GetProperty<T,TProperty>`、`IsDescriptionDefined` 和 `GetDescriptionOr`；未 root 插件 probe 验证带 `BindingFlags` 的 `Reflections.GetProperty`、普通方法绑定标志查询和泛型参数签名查询。
- 未 root 插件成员类型扩展：独立 probe 验证元组字段、结构字段，以及 `MemberInfo`/`ParameterInfo` 的数值、元组和结构分类；resolver 复用默认框架程序集上下文，避免 `DescriptionAttribute` 类型身份分裂。
- 未 root 插件类型关系扩展：独立 probe 验证 `TypeReflections` 的继承、泛型实现、原始泛型类型/参数和枚举底层类型入口。
- 未 root 插件类型关系扩展：独立 probe 进一步验证 `Reflections.IsAssignableTo`、`IsBaseOn`、`IsDeriveClassFrom`、开放泛型可赋值、集合元素类型、顶级基类、停止类型基类列表和程序集限定全名入口。
- 未 root 插件泛型继承扩展：独立 probe 新增 `UnrootedGenericBase<int>`/派生模型，验证开放和闭合 `Reflections.GetRawTypeFromGenericClass`。
- 未 root 插件基础类型/程序集扩展：独立 probe 验证 `AssemblyVisit` 文件/产品版本读取、`Reflections.GetUnderlyingType` 的 `Type`/`TypeInfo` 重载和 `TypeConv.GetNonNullableType` 的 nullable 数组/集合归一化。
- 未 root 插件成员特性扩展：`PublicField` 增加 `DescriptionAttribute`，独立 probe 验证 `Reflections`/`TypeReflections` 的字段描述、单项、集合、required 和存在性入口。
- 未 root 插件描述/显示名称扩展：`PublicProperty` 同时带 `DescriptionAttribute` 和 `DisplayNameAttribute`，独立 probe 验证 `TypeReflections` 的描述优先和显示名称回退，以及 `Reflections` 的显示名称优先。
- 未 root 插件特性继承扩展：新增带描述特性的基类和无自身特性的派生模型，独立 probe 验证默认不继承、显式 `ReflectionOptions.Inherit` 的描述/单项/集合/存在性；接口继承保持 CLR 不自动继承语义。
- 未 root 插件成员路径安全边界：默认选项拒绝私有成员路径的存在性、读取和写入，显式 `IncludeNonPublic` 后才允许访问。

同一个探针还包含四条部署边界：

- framework-dependent 运行时使用 `EnableTrimAnalyzer` 构建并执行未裁剪契约探针；
- self-contained partial trim 验证 getter/setter、字段和成员路径读写；
- self-contained full trim 验证未 root 程序集边界与显式 root 模型；
- NativeAOT 只验证构造、属性/字段特性与 `PropertyInfo`/`FieldInfo` 直接访问、方法查找和成员路径读写，并断言动态 Emit 抛出
  `PlatformNotSupportedException`，不把 `Expression.Compile` 误纳入 AOT 支持范围。

## 未 root 接口、继承和常量扫描复核（2026-10-06）

`ReflectionTrimProbe.UnrootedModels` 现在同时包含公共基类、继承属性、基类常量、派生类常量、
带参构造函数、getter-only/setter-only 属性和索引器，
以及继承基接口的 `IUnrootedBoundaryContract`。独立插件 probe 在模型程序集成功加载时调用
`Reflections.GetBaseClasses`、`Reflections.GetDirectInterfaceTypes`、
`Reflections.GetPublicConstantsRecursively`，并继续调用 `Reflections.GetInterfaceTypes`、
`Reflections.FindImplementTypes` 和 `Reflections.GetInstancesByInterface<T>`，并使用
`TypeVisit.GetMatchingConstructor` 验证带参构造函数，使用 `TypeVisit.GetProperties` 的三种访问模式
验证 setter-only 不访问空 getter、静态属性和索引器被排除；同时调用
`Reflections.GetField/GetProperty/GetMethod<T>` 以及类型/方法全名入口；对类型、属性、方法和方法参数分别调用
`TypeReflections.IsAttributeDefined` 的泛型/非泛型重载，并通过 `Reflections`、`TypeReflections`、`Types` 的特性单项、required
和集合入口读取 `DescriptionAttribute`，再将继承属性通过成员路径写入后读回，确认继承链、
直接接口过滤、常量递归、接口元数据、实现类型扫描和实例化扫描均符合预期；泛型 `Echo<T>` 方法通过
签名查找、闭合和调用验证了泛型参数数量、返回类型和参数类型元数据。framework-dependent
目录的 path/resolver/bytes 三种加载方式均输出 `metadata-preserved`；重新生成的 self-contained
partial/full trim 和 NativeAOT 目录主探针分别输出 `reflection-trim-ok`、`reflection-aot-metadata-ok`，
插件三种加载方式均输出 `assembly-trimmed`。因此该入口已纳入矩阵，但未 root 程序集被裁剪移除时仍只验证
明确的缺失边界，不代表任意插件类型扫描在 AOT 下可用。

## 覆盖方法解析复核（2026-10-07）

未 root 模型新增 `UnrootedBoundaryBase.ComputeBaseValue` 虚方法及
`UnrootedBoundaryModel.ComputeBaseValue` 覆盖实现。独立插件 probe 通过
`TypeVisit.FindMethod` 获取覆盖方法，再调用 `TypeVisit.GetBaseMethod`，校验返回方法的
`DeclaringType`、名称、参数/返回类型和调用结果。framework-dependent 隔离目录的
path、`AssemblyDependencyResolver`、字节数组三种加载方式均输出
`reflection-plugin-load-boundary:metadata-preserved`；该验证只补齐基类方法正向解析分支，
不把未显式 linker root 的任意程序集宣称为 trimming/NativeAOT 安全。

在同一模型上，probe 还验证 override 方法的 `TypeVisit.IsVisible` 与
`MethodInfo.IsOverridden`，以及基类方法的 `TypeVisit.IsVisibleAndVirtual` 和非重写结果，
确保可见性、虚拟性和重写判断不会因独立程序集加载方式改变。

基类的 `InheritedProperty` 同时作为公共虚属性参与验证。probe 检查
`PropertyInfo.IsVirtual` 为 true、`PropertyInfo.IsAbstract` 为 false，且
`TypeVisit.IsVisibleAndVirtual` 为 true；三种加载方式下原有继承属性的读写和值断言仍通过，
没有把静态、只写或索引器属性加入无参数读取集合。

基类另增加抽象只读属性 `AbstractBoundaryProperty`，派生边界模型和继承特性夹具分别提供实现。
probe 直接读取基类 `PropertyInfo`，确认 `IsAbstract` 和 `IsVisibleAndVirtual`，再读取派生实现并
校验 `IsAbstract == false` 与实例值；`PropertyAccessOptions.Both` 仍按契约排除只读属性，
可读属性和对象包装集合的既有断言保持通过。

## MemberInfo 字段类型谓词复核（2026-10-07）

`Reflections.IsBool/IsEnum/IsDate/IsInt/IsNumber(MemberInfo)` 现在对
`FieldInfo` 使用 `FieldType` 判断，不再把字段统一当作未支持的成员类型返回 `false`；枚举入口同时
支持可空枚举。未 root 模型新增布尔、枚举、可空枚举和浮点字段，独立 probe 在 path、
`AssemblyDependencyResolver`、字节数组三种加载方式下均输出 `metadata-preserved`；主包严格契约
回归 `15/15` 通过。

## 集合与对象包装入口复核（2026-10-06）

独立插件 probe 又增加了 `Reflections.GetFields`、`GetProperties`、`GetMethods`、
`GetPropertyOrField`、`GetPublicProperties`，以及 `TypeReflections.GetPublicReadableInstanceProperties` 和
`GetPublicWritableInstanceProperties` 的检查，并继续断言 getter-only、setter-only、静态属性和索引器边界。
`GetPublicProperties` 现在只投影可读的公共实例非索引器属性，避免无参数读取索引器或只写属性。
同一 probe 还通过动态闭合泛型调用覆盖 `Reflections.GetPropertyValue<T>`、
`SetPropertyValue<T>`，以及 `GetPropertyValueByPath`/`SetPropertyValueByPath` 的属性路径入口；
主探针则在所有发布模式执行路径入口，并仅在支持动态代码的分支执行委托访问器；同一分支额外调用
`Reflections.CreateInstance<T>`，验证核心包构造函数包装与 `TypeVisit` 绑定器保持一致。
主探针另以显式 linker root 覆盖值类型属性：CoreCLR/trim 分支通过 `GetValueSetterByRef<T>` 回写原始结构体，NativeAOT 分支只验证保留元数据下的装箱 `PropertyInfo.SetValue`，不把动态委托能力扩展到 AOT。
重新编译 probe 后，framework-dependent 目标目录的 path/resolver/bytes 三种加载方式均输出
`metadata-preserved`；重新生成的 self-contained partial/full trim 目标目录主探针（包含 `GetPublicProperties` 可读实例投影）均输出
`reflection-trim-ok`，独立 probe 三种方式均输出 `assembly-trimmed`。本轮重新生成的 NativeAOT
产物中未 root
插件同样三种方式均输出 `assembly-trimmed`。这补齐了未 root 模型的集合查询包装覆盖，但仍不等价于
任意插件程序集在裁剪或 NativeAOT 下可用。

## 独立插件部署复核（2026-10-07）

新增 `Stage-ExternalPlugin.ps1`，只把未 root 插件 DLL 和宿主托管程序集复制到独立旁路目录，
不修改原始发布目录，也不把插件加入宿主 linker roots。workflow 在 self-contained partial/full
trim 和 NativeAOT 三种宿主输出上分别执行该 staging，再通过 path/resolver/bytes 三种方式加载；
本机使用既有发布产物复核，三种模式均输出三行 `metadata-preserved`。原始发布目录仍保留
“未部署插件”三种 `assembly-trimmed` 结果，因此同时覆盖了独立部署成功和缺失边界；这不等价于
插件自身经过裁剪后的兼容性。

## 独立插件自身裁剪复核（2026-10-07）

新增 `ReflectionTrimProbe.UnrootedModelsHost` 作为最小裁剪宿主。插件项目显式设置
`IsTrimmable=true`，宿主只按程序集名称加载，不静态访问插件类型成员；`TrimRoots.xml` 仅保留
`UnrootedBoundaryModel` 的类型身份而不保留成员。partial/full 发布均保留插件 DLL，但其大小由
未裁剪的 6656 字节降为 5120 字节；将该 DLL 与对应托管宿主程序集放入旁路目录后，独立 probe
的 path/resolver/bytes 三种加载方式均输出 `member-trimmed`，并通过显式期望值检查。

NativeAOT 额外以 `PublishAot=true` 发布同一宿主，产物只包含原生 exe；执行
`ReflectionTrimProbe.UnrootedModelsHost.exe member-trimmed` 输出程序集身份和
`reflection-plugin-trim-boundary:member-trimmed`，退出码为 0。该检查在 NativeAOT
进程内部完成，不依赖外部 DLL 加载 probe，因此能区分“插件程序集被静态合并但成员元数据
被裁剪”与“外部插件 DLL 未随宿主部署”。

这证明了“程序集存在但成员缺失”与“程序集被裁剪移除”是两个可区分边界。该夹具是故意声明
可裁剪的测试插件，不代表未声明 `IsTrimmable` 的任意第三方插件会得到同样结果；NativeAOT
宿主现在也通过原生 exe 的动态类型查找断言成员缺失，形成独立发布契约，但未声明
`IsTrimmable` 的第三方插件和复杂 root 组合仍不在承诺范围内。

探针通过 `TrimmerRoots.xml` 显式保留正向测试模型的全部成员，模拟应用方为动态反射输入
提供 linker 保留信息的用法；第三个独立程序集故意不加入 root，用于观察缺少元数据时的边界。
常规 trim 分支不生成动态类型；AOT 分支只调用公共动态类型入口以确认它明确拒绝动态代码，
不把未保留的任意业务类型描述为 trimming 安全。插件加载 probe 与主探针分离，避免
验证入口本身改变被测程序集的 linker 根集合。

公共动态类型入口、动态对象入口、表达式委托入口和方法签名入口现在声明了
`RequiresDynamicCode`/`RequiresUnreferencedCode`；构造函数入口和类型缓存扫描入口声明
了相应的 `DynamicallyAccessedMembers` 需求。调用方在使用这些能力时会得到编译期提示。
本探针仍通过显式 roots 提供元数据，不把声明本身当作未保留输入的兼容证明。

## 裁剪注解流转复核（2026-10-07）

属性扫描缓存的 `ConditionalWeakTable` 回调不再从未标注的键参数读取类型，改为使用已标注入口捕获的类型；`TypeVisit.GetFields(Type)` 和构造函数扩展入口分别声明公共字段、公共构造函数需求，属性选择器扩展的泛型参数也同步声明属性需求。net8 trim analyzer 构建后，反射模块源代码不再产生对应的 `IL2067`/`IL2091` 诊断；运行时探针仍输出 `reflection-trim-ok`，176 个反射模块测试全部通过。

## 单文件程序集版本复核（2026-10-07）

`AssemblyVisit` 不再把 `Assembly.Location` 传给 `FileVersionInfo`；文件版本优先读取
`AssemblyFileVersionAttribute`，产品版本优先读取 `AssemblyInformationalVersionAttribute`，缺失时回退到程序集名称版本。这样 single-file 程序集的空路径不会触发运行时异常，也消除了反射模块的 `IL3000`。补丁后的 NativeAOT 单次发布日志共 197 条诊断（181 条仓库源、14 条依赖/工具链、2 条生成物），反射模块源文件诊断为 0；schema v2 检查在固定 SDK 8.0.425、net8.0/win-x64、partial/AOT 上下文下通过，原生探针仍正常生成并运行。

## 动态类型契约与版本回退复核（2026-10-07）

新增动态程序集夹具验证缺少版本特性时回退到程序集名称版本；trim 主探针实际调用
`AssemblyVisit.GetFileVersion`/`GetProductVersion`，self-contained partial 产物
`verification-partial-after-typefactory-flow/ReflectionTrimProbe.exe` 输出
`reflection-trim-ok`。属性筛选 helper、构造可实例化检查、`SchemaEntry`/`SchemaCache`/`BuildType`
链路均补齐 `DynamicallyAccessedMembers`、`RequiresUnreferencedCode` 和
`RequiresDynamicCode` 流转，动态基类只保留无法静态表达的局部抑制。

NativeAOT 产物 `verification-aot-after-contract-flow/ReflectionTrimProbe.exe` 输出
`reflection-aot-metadata-ok`。对应 schema v2 报告
`current-round-nativeaot-after-contract-flow-report.json` 检查通过，共 75 条诊断：72 条
仓库源、1 条生成物、2 条依赖/工具链，0 条未解析；反射模块生产源诊断为 0。该数字是本轮
契约传播后的新鲜报告，不替代 checked-in 的 421 条历史指纹基线。

## 本机复核

当前 SDK 由仓库 `global.json` 固定为 `8.0.425`。self-contained 与 NativeAOT 使用联网还原的 `win-x64`
runtime/compiler pack；所有命令均显式限定单一 TFM。当前 SDK 不允许
`SelfContained=false` 与 `PublishTrimmed=true` 组合，直接发布会报告 `NETSDK1102`，因此
framework-dependent 分支改为 trim analyzer 构建加未裁剪契约执行：

```powershell
dotnet build benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  -c Release -f net8.0 -p:TargetFrameworks=net8.0 `
  -p:EnableTrimAnalyzer=true -p:TrimMode=partial -p:DebugType=None
dotnet benchmarks/ReflectionTrimProbe/bin/Release/net8.0/ReflectionTrimProbe.dll
dotnet build benchmarks/ReflectionTrimProbe.UnrootedModels/ReflectionTrimProbe.UnrootedModels.csproj `
  -c Release -f net8.0
Copy-Item benchmarks/ReflectionTrimProbe.UnrootedModels/bin/Release/net8.0/ReflectionTrimProbe.UnrootedModels.dll `
  benchmarks/ReflectionTrimProbe/bin/Release/net8.0 -Force
dotnet benchmarks/ReflectionTrimPluginLoadProbe/bin/Release/net8.0/ReflectionTrimPluginLoadProbe.dll `
  benchmarks/ReflectionTrimProbe/bin/Release/net8.0
```

结果：输出 `reflection-trim-ok`，退出码 0；将 `ReflectionTrimProbe.UnrootedModels.dll` 暂存到该目标目录后，独立插件 probe
额外输出 path/resolver/bytes 三行 `metadata-preserved`。该分支只验证框架依赖运行时契约和 trim analyzer 诊断，
不代表链接器已经删除了元数据。

未限定 TFM 的多目标发布会尝试 trimming `netstandard2.0` 并触发 SDK `NETSDK1124`，因此
CI 和本机命令均固定为 net8.0。

self-contained trim：

```powershell
dotnet restore benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  -p:TargetFrameworks=net8.0 -r win-x64
dotnet publish benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  --no-restore -c Release -f net8.0 -r win-x64 --self-contained true `
  -p:TargetFrameworks=net8.0 -p:PublishTrimmed=true -p:TrimMode=partial -p:DebugType=None `
  -p:PublishDir=benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-partial/
./benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-partial/ReflectionTrimProbe.exe
```

结果：输出 `reflection-trim-ok`，退出码 0；该 trim 目标目录不携带未 root 外部插件，独立插件 probe 的 path/resolver/bytes
三行均输出 `assembly-trimmed`。新增字段、私有成员选项、路径写入以及两个显式 root 外部程序集模型断言均通过；
未 root 外部插件因未随 trim 产物发布而由独立 probe 观察为 `assembly-trimmed`，不能据此推导任意插件安全。

self-contained full trim：

```powershell
dotnet publish benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  --no-restore -c Release -f net8.0 -r win-x64 --self-contained true `
  -p:TargetFrameworks=net8.0 -p:PublishTrimmed=true -p:TrimMode=full -p:DebugType=None `
  -p:PublishDir=benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-full/
./benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-full/ReflectionTrimProbe.exe
```

结果：输出 `reflection-trim-ok`，退出码 0；该 trim 目标目录不携带未 root 外部插件，独立插件 probe 的 path/resolver/bytes
三行均输出 `assembly-trimmed`。显式 root 模型仍通过，未 root 外部插件没有随 trim 产物发布。

独立插件加载 probe 对每个发布目录使用同一命令，仅替换目标目录；每次输出 path、resolver、bytes 三种加载结果：

```powershell
dotnet benchmarks/ReflectionTrimPluginLoadProbe/bin/Release/net8.0/ReflectionTrimPluginLoadProbe.dll `
  benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-full
```

framework-dependent 目标目录三种方式均输出 `metadata-preserved`；partial/full trim 和 NativeAOT 目标目录三种方式均输出
`assembly-trimmed`。partial/full 使用独立 publish 目录，避免前一发布模式的 DLL 残留污染结果。

NativeAOT metadata probe：

```powershell
dotnet restore benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  -p:TargetFrameworks=net8.0 -r win-x64 -p:PublishAot=true
dotnet publish benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  --no-restore -c Release -f net8.0 -r win-x64 --self-contained true `
  -p:TargetFrameworks=net8.0 -p:PublishAot=true -p:PublishTrimmed=true `
  -p:TrimMode=partial -p:DebugType=None
./benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/native/ReflectionTrimProbe.exe
```

结果：输出 `reflection-aot-metadata-ok`，退出码 0；独立插件 probe 的
path/resolver/bytes 三行均输出 `assembly-trimmed`。显式 root 的两个外部程序集、
`Reflections`/`TypeReflections` 属性特性公开 API 回退、字段、私有成员选项和路径写入均通过；未 root 外部插件没有随 NativeAOT 产物发布，独立 probe 观察为 `assembly-trimmed`。NativeAOT 原生 exe 位于
`win-x64/native`。本轮按与 CI 相同的 SDK/TFM/RID/Trim/AOT 发布参数，并在还原阶段显式带 `PublishAot=true`，从干净副本重新生成并运行 NativeAOT exe；当前日志报告 15 个 `IL/ILC` 诊断 ID、421 条诊断、0 条未解析，其中 405 条归类为仓库源、14 条为依赖/工具链、2 条为生成物。`Build-NativeAotSourceMap.ps1` 从 PE 元数据、Portable PDB 和方法签名生成源码映射，额外匹配 4 条 ILC 符号诊断；其余来源仍以实际文件位置分类，不冒充依赖/工具链警告为仓库指纹。旧 CI-like 的 13/196/180 结果、7/35 隔离报告和 398 条聚合记录仅作为历史证据保留，不能混用作当前仓库指纹基线。其余警告不影响本探针退出码，但说明尚未形成整个库的 trimming/AOT 无警告契约。

本节中 `421` 条诊断是版本入口修复前生成并提交的历史基线，不应与上面的最新复核数字混用；基线仍保留用于 CI 兼容比较，补丁后的同配置报告为 `197` 条且反射模块源诊断为 `0`。

## 类型谓词入口复核（2026-10-07）

主探针新增固定 root 模型 `TrimImplementation`、`TrimCollection`、`TrimStatus` 和 `ITrimContract`，覆盖接口定义、集合识别、元组/可空/枚举判断、泛型接口实现解析以及枚举底层类型读取。入口验证不改变默认非公开成员访问策略。

framework-dependent trim analyzer 构建：

```powershell
dotnet build benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  -c Release -f net8.0 -p:TargetFrameworks=net8.0 `
  -p:EnableTrimAnalyzer=true -p:TrimMode=partial -p:DebugType=None --no-restore
dotnet benchmarks/ReflectionTrimProbe/bin/Release/net8.0/ReflectionTrimProbe.dll
```

结果：构建 0 个错误，探针输出 `reflection-trim-ok`，退出码 0。新增入口没有产生新的反射模块生产源诊断。

self-contained partial/full trim：

```powershell
dotnet publish benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  --no-restore -c Release -f net8.0 -r win-x64 --self-contained true `
  -p:TargetFrameworks=net8.0 -p:PublishTrimmed=true -p:TrimMode=partial `
  -p:DebugType=None -p:PublishDir=benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-type-predicates-partial/
./benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-type-predicates-partial/ReflectionTrimProbe.exe

dotnet publish benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  --no-restore -c Release -f net8.0 -r win-x64 --self-contained true `
  -p:TargetFrameworks=net8.0 -p:PublishTrimmed=true -p:TrimMode=full `
  -p:DebugType=None -p:PublishDir=benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-type-predicates-full/
./benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-type-predicates-full/ReflectionTrimProbe.exe
```

两种产物均输出 `reflection-trim-ok`，退出码 0。

NativeAOT metadata probe：

```powershell
dotnet restore benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  -r win-x64 -p:PublishAot=true -p:TargetFrameworks=net8.0 --ignore-failed-sources
dotnet publish benchmarks/ReflectionTrimProbe/ReflectionTrimProbe.csproj `
  --no-restore -c Release -f net8.0 -r win-x64 --self-contained true `
  -p:TargetFrameworks=net8.0 -p:PublishAot=true -p:PublishTrimmed=true `
  -p:TrimMode=full -p:DebugType=None `
  -p:PublishDir=benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-type-predicates-nativeaot/
./benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-type-predicates-nativeaot/ReflectionTrimProbe.exe
```

结果：NativeAOT 发布 0 错误，原生探针输出 `reflection-aot-metadata-ok`，退出码 0。发布日志仍包含既有依赖/工具链和动态代码边界诊断；本轮新增类型谓词没有引入新的反射模块生产源诊断。

## 构造与异步类型入口复核（2026-10-07）

主探针新增固定 root 的 `TrimParameterlessSample`，覆盖
`TypeVisit.HasParameterlessConstructor`、`TypeVisit.GetParameterlessConstructor` 及对应 `Type` 扩展入口；
同时覆盖 `TypeReflections` 和 `TypeInfo` 扩展的 `Task`、`Task<T>`、`ValueTask`、`ValueTask<T>` 判定。
framework-dependent trim analyzer、self-contained partial/full trim 均输出 `reflection-trim-ok`，NativeAOT
原生探针输出 `reflection-aot-metadata-ok`，退出码均为 0；新增入口没有产生反射模块生产源诊断。

## NativeAOT 外部插件 EXE anchor 复核（2026-10-07）

AOT 发布目录只生成 `ReflectionTrimProbe.exe`，不会生成 `ReflectionTrimProbe.dll`。此前外部插件 staging
步骤按 DLL 查找 host 文件，无法覆盖真实 NativeAOT 输出。本轮将 workflow 的 host 输入改为原生 EXE，
`ReflectionTrimPluginLoadProbe` 在 resolver 模式下按文件存在性选择 `ReflectionTrimProbe.dll` 或
`ReflectionTrimProbe.exe`。

本机使用本轮新生成的 `publish-round2-nativeaot` 目录，将未 root 插件显式旁路部署后执行：

```powershell
./benchmarks/ReflectionTrimPluginLoadProbe/Stage-ExternalPlugin.ps1 `
  -PluginAssemblyPath benchmarks/ReflectionTrimProbe.UnrootedModels/bin/Release/net8.0/ReflectionTrimProbe.UnrootedModels.dll `
  -HostAssemblyPath benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-round2-nativeaot/ReflectionTrimProbe.exe `
  -OutputDirectory benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-round2-nativeaot-external
dotnet benchmarks/ReflectionTrimPluginLoadProbe/bin/Release/net8.0/ReflectionTrimPluginLoadProbe.dll `
  benchmarks/ReflectionTrimProbe/bin/Release/net8.0/win-x64/publish-round2-nativeaot-external --expect metadata-preserved
```

结果：path/resolver/bytes 三种加载方式均输出 `metadata-preserved`，退出码 0；partial/full 的 managed
host DLL 旁路部署也通过。未将插件放入发布目录时仍输出 `assembly-trimmed`，因此该修复只补齐显式旁路部署边界，
不把任意未 root 插件宣称为 AOT 安全。

## 成员分类与表达式筛选入口复核（2026-10-07）

主探针新增 `TrimMemberSample`、`TrimMemberBase` 和 `TrimMemberDerived`，并通过显式 linker root 保留其字段、属性、方法和参数元数据。新增断言覆盖：

- `MemberInfo`/`ParameterInfo` 的数值、元组和结构类型扩展判断；
- `MethodInfo` 的异步、可见、虚方法和重写判断；
- `PropertyInfo` 的虚拟、可见虚拟和抽象判断，包含继承重写属性；
- `TypeVisit.GetProperties<T>` 的表达式选择和 `TypeVisit.Exclude<T>` 的表达式排除。

验证命令使用同一份主探针源码依次构建并执行未裁剪、self-contained partial trim、self-contained full trim 和 NativeAOT 产物：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT: reflection-aot-metadata-ok
```

四种产物均退出码为 0。该结果证明新增入口在显式保留目标元数据的部署边界下可用；仍不代表未保留 linker root 的任意业务类型或整个反射库已具备 AOT 兼容性。

## TypeReflections 成员分类入口复核（2026-10-07）

主探针在上一轮成员夹具上继续增加 nullable 和集合属性，直接调用 `TypeReflections` 的
`MemberInfo` 分类入口：`IsBoolean`、`IsDateTime`、`IsNumeric`、`IsEnum`、`IsStruct`、
`IsPrimitive`、`IsValueType` 和 `IsCollection`。验证同时包含非 nullable 类型、nullable 类型的默认行为，
以及 `TypeIsOptions.IgnoreNullable` 下的基础类型判断。

本轮新生成的四种产物执行结果为：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT: reflection-aot-metadata-ok
```

四种产物均退出码为 0。该证据只覆盖显式 linker root 的测试模型，未将 `MemberInfo` 分类入口推广为
任意未保留业务类型在 trimming/NativeAOT 下的兼容承诺。

## 类型关系、泛型原始类型与值类型入口复核（2026-10-07）

主探针新增 `TrimBaseSample`、`TrimDerivedSample`、`TrimGenericBase<int>`、`TrimGenericDerived` 和
`TrimCollection` root，覆盖 `Types.Of` 的 owner/underlying 选项、对象数组中的 null 过滤、默认值和默认值判断、
`Types`/`TypeReflections`/`Reflections` 的类型派生与泛型实现判断、`GetRawTypeFromGenericType`/
`GetRawTypeArgsFromGenericType`、`GetRawTypeFromGenericClass`、可空基础类型和数组/集合元素类型入口。

本轮修复两个核心匹配缺陷：开放泛型按泛型定义匹配，闭合泛型按完整构造类型精确匹配；因此
`Base<int>` 能命中，`Base<string>` 不会误命中。核心 `TypesAndTypeJudgeTest` 筛选用例为 18/18，
并用错误类型参数断言 `false/null` 防止只比较泛型定义造成过宽匹配。

四种产物执行结果为：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT: reflection-aot-metadata-ok
```

以上结果只证明显式 linker root 的模型在对应部署边界可用；未显式保留的第三方类型、更多泛型方差和复杂
继承组合仍属于剩余矩阵 TODO。

## 闭合泛型赋值与协变入口复核（2026-10-07）

本轮修复 `Reflections.IsGenericAssignableFrom` 的闭合泛型分支：目标为闭合泛型时直接使用 CLR
`Type.IsAssignableFrom`，保留闭合泛型基类及泛型协变/逆变规则；目标为开放泛型定义时仍遍历来源类型的
基类和接口并按 `GetGenericTypeDefinition()` 匹配。主探针新增以下正向/反向断言：

- `IEnumerable<object>` 可由 `List<string>` 填充；
- `IEnumerable<string>` 不接受 `List<object>`；
- `IComparer<string>` 可由 `IComparer<object>` 填充，反向赋值不成立；
- 闭合泛型基类 `Base<int>` 可由其派生类填充，错误类型参数不会命中；
- 开放泛型 `IEnumerable<>` 仍可识别 `List<int>`。

核心严格契约测试为 `14/14`。本轮重新生成的四种产物执行结果为：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT: reflection-aot-metadata-ok
```

该证据只覆盖 CLR 已定义的闭合泛型赋值规则和显式保留的探针入口；更多泛型方差组合、泛型约束推断和
未显式 linker roots 的业务类型仍属于剩余矩阵 TODO。

## 主探针类型关系入口复核（2026-10-07）

主 `ReflectionTrimProbe` 本轮补齐了显式 root 模型上的类型关系入口：
`Reflections.IsCollection`、`IsGenericCollection`、`GetTopBaseType`、两种
`GetBaseClasses`（包括 `includeObject` 和停止类型）、`IsAssignableTo(Type, Type)` 以及
`GetFullNameWithAssemblyName`。断言同时覆盖数组、泛型集合、非集合类型、接口顶级类型、协变可赋值和反向拒绝，
并检查基类链顺序及停止边界。

本轮四种产物执行结果：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT: reflection-aot-metadata-ok
```

这些断言只证明已 root 的主探针模型在对应部署边界可用；未显式 linker roots、更多泛型约束/方差组合和未调用的公开入口仍属于剩余矩阵 TODO。

## 接口泛型参数入口复核（2026-10-07）

主探针新增 `TrimCollection : List<int>` 的接口判定断言：默认选项要求完整闭合接口相等，
`IEnumerable<string>` 不会误命中 `IEnumerable<int>`；`IgnoreGenericArgs` 则允许按同一泛型接口定义匹配，
同时验证开放 `IEnumerable<>` 目标和 `Types`/`TypeReflections` 的泛型入口。

本轮四种产物执行结果：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT: reflection-aot-metadata-ok
```

该结果只覆盖显式 root 的 `List<int>` 接口图；更多接口继承层级、泛型约束和未 root 插件仍属于剩余矩阵 TODO。

## 动态对象 AOT 边界复核（2026-10-07）

主探针新增 `TypeVisit.CreateInstance(IDictionary<string, object>)` 调用。该入口通过
`TypeFactory.CreateObject` 使用 Reflection.Emit 生成动态类型，因此在 NativeAOT 中必须和
`TypeVisit.CreateDynamicType` 保持一致，明确抛出 `PlatformNotSupportedException`，不能静默返回空对象或
退化为不完整实例。四种产物结果如下：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:       reflection-trim-ok
NativeAOT:  reflection-aot-metadata-ok
```

这项验证只确认动态对象包装的 AOT 拒绝契约，不代表动态 Emit 或表达式编译在 NativeAOT 下可用。

## 批量与动态加载类型构造复核（2026-10-07）

主探针在四种产物中调用 `TypeVisit.CreateInstances<TrimSample>`、运行时类型批量
`CreateInstances(IEnumerable<Type>, params object[])`、强类型运行时批量重载，以及两个
`CreateGenericInstance<T>` 重载，并验证返回实例值分别为 `1`、`2`、`3`、`4`、`5`、`6`。
当前 net8 NativeAOT 运行时实际支持这些已保留构造入口，结果如下：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT:  reflection-aot-metadata-ok
```

独立插件 probe 还对通过 path/resolver/bytes 加载的 `UnrootedBoundaryModel` 调用
`TypeVisit.CreateInstance(type, "typevisit-boundary")` 和
`TypeVisit.CreateInstances(new[] { type }, "typevisit-batch-boundary")`，三种 managed
加载方式均返回 `metadata-preserved`；未携带插件的 partial/full 目录三种方式均返回
`assembly-trimmed`。这些结果只证明当前 SDK、TFM、RID 和已保留模型下的行为；
`RequiresDynamicCode` 契约仍要求调用方对其他 AOT 配置进行独立验证。

## 方法签名与直接类型谓词复核（2026-10-07）

主探针在成员分类夹具上新增 `TypeVisit.GetBaseMethod`、
`TypeVisit.GetMethodBySignature` 和 `TypeVisit.IsSignatureCompatible`，并同时验证
`TypeMetaVisitExtensions.IsAttributeNotDefined<T>` 与非泛型 `IsAttributeNotDefined(Type)`。
类型关系夹具新增直接调用 `Types.IsNumericType`、`IsPrimitiveType`、`IsStaticType` 和
`IsAnonymousType`，包含 nullable、静态工具类型、编译器生成匿名类型以及普通业务类型的正反断言。

本轮四种产物执行结果为：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT:  reflection-aot-metadata-ok
```

四种产物均退出码为 0。该证据只覆盖显式 linker root 的固定模型和当前 net8.0/win-x64
发布参数；它补齐了主探针的入口回归，不代表未保留业务类型、更多签名边界或动态代码委托在
所有 trimming/NativeAOT 组合下均可用。

未 root 插件 probe 另外在 framework-dependent managed 目录执行同一组方法结构和直接类型谓词断言，
path/resolver/bytes 三种加载方式均输出 `metadata-preserved`；当前未在这三种入口上新增 partial/full
或 NativeAOT 的正向插件成员保留承诺，仍需遵守插件是否随产物发布及 linker root 的边界。

## TypeJudge 重载复核（2026-10-07）

主探针新增 `TypeJudge.IsNumericType<T>`、`IsNumericType(TypeInfo)`、
`IsNullableType(TypeInfo)`、`IsEnumType(TypeInfo, bool)` 和
`IsGenericImplementation(Type, Type)` 断言，覆盖 nullable、枚举和 `List<int>` 的泛型接口实现。
未 root 插件 probe 对固定 `int`/`int?`/`List<int>` 入口执行相同的公开 API 验证。

本轮主探针结果如下：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT:  reflection-aot-metadata-ok
```

四种产物均退出码为 0；插件 managed path/resolver/bytes 三种加载方式均输出
`metadata-preserved`。这组证据只覆盖当前 net8.0/win-x64 和显式 root/旁路插件模型，
不替代其他 TFM、未保留插件组合或动态代码入口的独立验证。

## 方法与成员查询重载复核（2026-10-07）

主探针在固定 `TrimMemberSample` 上通过表达式调用
`TypeVisit.GetField<TrimMemberSample, int>` 和 `GetProperty<TrimMemberSample, int>`，
并验证无 `DescriptionAttribute` 时 `IsDescriptionDefined` 为 false、
`GetDescriptionOr` 返回调用方默认值。未 root 插件 probe 额外使用
`BindingFlags.Instance | BindingFlags.NonPublic` 读取 `ReflectedValue`，并通过
`TypeVisit.FindMethod` 的绑定标志重载和 `MethodParameterSignature` 泛型签名重载查找
`Execute(int)` 与 `Echo<T>(T)`。

本轮主探针结果如下：

```text
untrimmed: reflection-trim-ok
partial:   reflection-trim-ok
full:      reflection-trim-ok
NativeAOT:  reflection-aot-metadata-ok
```

主探针四种产物退出码均为 0；插件 managed path/resolver/bytes 三种加载方式均输出
`metadata-preserved`。这些断言只覆盖已保留模型和当前 net8.0/win-x64 配置，非公开访问仍受
显式 `BindingFlags` 控制，未扩大默认 public 访问边界。

## 未 root 插件成员类型扩展（2026-10-07）

未 root 模型新增 `TupleField` 和 `StructField`，独立加载 probe 对公共字段、公共属性和
`Execute(int)` 参数执行 `IsNumeric`，并对元组/结构字段执行 `IsTupleType`、`IsStructType`。
resolver 加载上下文对 `System`、`System.*`、`Microsoft.*` 和 `netstandard` 程序集复用默认
上下文，避免裁剪目录中的框架程序集与 probe 的特性类型身份分裂。

本轮四种宿主目录的结果均为：

```text
untrimmed path/resolver/bytes: metadata-preserved
partial   path/resolver/bytes: metadata-preserved
full      path/resolver/bytes: metadata-preserved
NativeAOT path/resolver/bytes: metadata-preserved
```

这项证据仍只覆盖当前 net8.0/win-x64 和现有插件模型；更多未 root 程序集、框架程序集版本
组合和未覆盖公开入口仍属于剩余矩阵 TODO。

## 未 root 插件类型关系扩展（2026-10-07）

独立加载 probe 对未 root 模型验证 `TypeReflections.IsTypeBasedOn`、
`IsTypeDerivedFrom`，并以 `List<int>`/`IEnumerable<>` 验证泛型实现、原始泛型类型和参数解析，
同时验证 `GetEnumUnderlyingType(typeof(DayOfWeek))`。四种宿主目录均保持：

```text
untrimmed path/resolver/bytes: metadata-preserved
partial   path/resolver/bytes: metadata-preserved
full      path/resolver/bytes: metadata-preserved
NativeAOT path/resolver/bytes: metadata-preserved
```

这些断言只覆盖当前 net8.0/win-x64 和现有插件模型，未覆盖的动态程序集组合仍属于剩余矩阵 TODO。
更新后的故意可裁剪插件 partial/full/NativeAOT 宿主仍输出 `member-trimmed`；NativeAOT 宿主按设计
只生成原生 EXE，不输出可旁路复制的独立插件 DLL。

## 未 root 插件成员路径安全边界（2026-10-07）

独立 probe 在未 root 模型的私有 `ReflectedValue` 上验证：默认选项下
`HasMemberPath`、`TryGetMemberValueByPath` 和 `TrySetMemberValueByPath` 均返回失败；
启用 `IncludeNonPublic` 后路径存在性和读取/写入均成功。四种正向宿主目录的
path/resolver/bytes 加载均保持 `metadata-preserved`，可裁剪插件的 partial/full/NativeAOT
边界继续保持 `member-trimmed`。

## 未 root 插件类型关系入口扩展（2026-10-07）

独立 probe 在未 root 模型上验证 `IsAssignableTo`、`IsBaseOn`、
`IsDeriveClassFrom`、开放泛型可赋值、集合元素类型、顶级基类、停止类型基类列表
和程序集限定全名。未裁剪、partial/full trim 与 NativeAOT 宿主的 path/resolver/bytes
正向加载均输出 `metadata-preserved`；故意可裁剪插件的 partial/full/NativeAOT 边界
均输出 `member-trimmed`。

## 未 root 插件泛型继承入口扩展（2026-10-07）

独立 probe 在未 root 程序集中新增 `UnrootedGenericBase<int>`/派生模型，分别以开放
泛型定义和闭合泛型类型调用 `Reflections.GetRawTypeFromGenericClass`，两种结果均解析为
`System.Int32`。未裁剪、partial/full trim 与 NativeAOT 宿主的 path/resolver/bytes
正向加载均输出 `metadata-preserved`；故意可裁剪插件的 partial/full/NativeAOT 边界
均输出 `member-trimmed`。

## 未 root 插件基础类型和程序集入口扩展（2026-10-07）

独立 probe 在未 root 程序集加载后读取文件/产品版本，并验证
`Reflections.GetUnderlyingType` 的 `Type`/`TypeInfo` 重载，以及
`TypeConv.GetNonNullableType` 对 nullable 数组和 `List<int?>` 的归一化。
未裁剪、partial/full trim 与 NativeAOT 宿主的 path/resolver/bytes 正向加载均输出
`metadata-preserved`；故意可裁剪插件的 partial/full/NativeAOT 边界均输出
`member-trimmed`。

## 未 root 插件字段特性入口扩展（2026-10-07）

`PublicField` 现在带有 `DescriptionAttribute`。独立 probe 验证
`Reflections`/`TypeReflections` 对 `FieldInfo` 的描述、单项特性、特性集合、required
和存在性查询。未裁剪、partial/full trim 与 NativeAOT 宿主的 path/resolver/bytes
正向加载均输出 `metadata-preserved`；故意可裁剪插件的 partial/full/NativeAOT 边界
均输出 `member-trimmed`。

## 未 root 插件描述和显示名称入口扩展（2026-10-07）

`PublicProperty` 同时带有 `DescriptionAttribute` 和 `DisplayNameAttribute`。独立 probe
验证 `TypeReflections.GetDisplayName`、`GetDisplayNameOr`、
`GetDescriptionOrDisplayName` 的优先级，以及 `Reflections.GetDisplayNameOrDescription`
的显示名称优先级。未裁剪、partial/full trim 与 NativeAOT 宿主的 path/resolver/bytes
正向加载均输出 `metadata-preserved`；故意可裁剪插件的 partial/full/NativeAOT 边界
均输出 `member-trimmed`。

## 未 root 插件特性继承入口扩展（2026-10-07）

新增带 `DescriptionAttribute` 的 `UnrootedBoundaryBase` 和无自身特性的
`UnrootedInheritanceModel`。独立 probe 验证默认选项返回派生类型名，显式
`ReflectionOptions.Inherit` 返回基类描述，并正确返回单项、集合和存在性结果；同时记录
接口继承保持 CLR 不自动继承特性的语义。正向 path/resolver/bytes 加载均为
`metadata-preserved`，可裁剪插件 partial/full/NativeAOT 边界均为 `member-trimmed`。

## NativeAOT 警告基线

`NativeAotWarningBaseline.txt` 固化当前 15 个历史 `IL/ILC` 诊断 ID，
`Check-NativeAotWarnings.ps1` 从发布日志提取并比较新增/消失 ID，同时在 schema v2 JSON 报告中保留
原始/规范化位置、行列号、项目上下文、TFM、完整消息、消息指纹、来源分类和未解析原始行；CI 默认传入
`-FailOnUnparsedDiagnostic`，将未解析行作为独立失败条件。仅显式诊断排障输入允许旁路。最新 CI-like 日志包含 13 个 ID、196 个诊断出现次数，
全部解析，未解析数量为 0；旧 CI-like 日志的 ID 均属于 15 个历史基线。当前固定 SDK 8.0.425 的干净 AOT 日志同样为 15 个 ID、421 条诊断和 0 条未解析，严格 ID 比较无新增。若提供同一发布配置下的上一份 JSON 报告并传入
`-DiagnosticBaselinePath ... -FailOnNewRepositoryDiagnostic`，还会按仓库源 `(code, messageFingerprint, file)`
计数识别同一 ID 下新增的仓库诊断；传入 `-DiagnosticAllowlistPath -FailOnUnapprovedRepositoryDiagnostic` 后，还会按规范化仓库文件、诊断 ID、消息指纹、`maxCount`、非空 `owner` 和 `reason` 执行可审计的所有权 allowlist 门禁；同时必须传入 `-DiagnosticContext`，报告会拒绝缺失或不一致的 SDK、TFM、RID、TrimMode
或日志聚合上下文，避免把不同构建配置误判为代码回归。外部依赖和未知来源不会被自动当作仓库代码。CI 默认加载 `NativeAotRepositoryDiagnosticAllowlist.json` 并传入 `-FailOnUnapprovedRepositoryDiagnostic`，同时加载 `NativeAotNonRepositoryDiagnosticOwnership.json` 并传入 `-FailOnUnownedNonRepositoryDiagnostic`；只有显式诊断排障输入允许责任门禁旁路。非仓库清单按 `(code, messageFingerprint, origin)` 记录生成物、依赖/工具链和未知来源的 `owner/reason/action`，不把绝对 NuGet 或 SDK 路径写入稳定 key。传入 `-SymbolMapPath` 后，检查器会消费由 `Build-NativeAotSourceMap.ps1` 根据实际 PE 元数据、Portable PDB 和方法签名生成的符号映射，先采用唯一文件映射，再按规范化重载签名选择唯一文件；仍无法匹配或缺少 PDB 的项继续标记为 unknown。旧 CI-like 日志包含 13 个 ID、196 条诊断且全部解析，180 条映射到仓库源文件，其中 5 条通过签名匹配；当时固定 SDK 8.0.425 的干净 AOT 发布包含 421 条诊断，其中 405 条归类为仓库源并形成 339 个指纹基线。当时 CI 默认加载该基线并上传报告、映射和候选文件，新增仓库指纹或未登记责任默认阻断；显式诊断排障输入才允许旁路。 当前工作流使用 baselines 下的 363 项配置，见 [目录与基线](reflection-validation-layout.md)。
CI 发布步骤会从实际 `dotnet --version` 生成同一 `DiagnosticContext`，并通过环境变量传给检查步骤；上传的 JSON 因而包含可复现的发布上下文，后续基线比较不能跨配置复用。

`Build-NativeAotDiagnosticBaseline.ps1` 可将 schema v2 报告压缩为可审查的仓库指纹基线：保留发布上下文、仓库诊断出现次数，以及规范化的 `code`、`messageFingerprint`、`file` 和 `maxCount`，不把外部依赖或未知来源写入仓库门禁。`Check-NativeAotWarnings.ps1` 同时接受完整 schema v2 报告和该紧凑 schema 1 基线，并继续执行上下文一致性、重复项和计数校验；检查器要求 `entryCount` 与 `entries` 数量、`repositoryDiagnosticOccurrenceCount` 与 `maxCount` 总和一致，异常基线直接拒绝；紧凑基线比较按键直接读取 `maxCount`，不会因异常大计数展开无界的临时记录。仓库 `global.json` 与 workflow 将 NativeAOT SDK 固定为 8.0.425，并在发布步骤检查实际版本；固定输入的干净 NativeAOT 发布已成功生成并运行原生 exe，产出 405 次仓库诊断、339 个指纹，历史 339 项基线现归档于 `benchmarks/ReflectionTrimProbe/baselines/history/20261013-before-approval/NativeAotRepositoryDiagnosticBaseline.json`，对应的 `NativeAotRepositoryDiagnosticAllowlist.json` 为每条指纹提供源码归属和审计理由，`NativeAotNonRepositoryDiagnosticOwnership.json` 为 16 条生成物/依赖/工具链诊断提供稳定来源归属、理由和后续动作。当前 CI 默认加载 `baselines/` 下的 363 项基线、仓库 allowlist 和非仓库责任清单进行上下文、指纹、责任字段比较并阻断新增或未登记诊断，只有显式诊断排障输入才允许漂移。

## CI 部署矩阵

Windows net8 job 会依次执行 framework-dependent trim analyzer 契约构建、self-contained
partial trim、self-contained full trim 和 NativeAOT metadata probe，并在四个产物目录运行独立插件加载 probe；partial/full/NativeAOT 产物随后各自通过 `Stage-ExternalPlugin.ps1` 创建旁路部署目录，验证保留元数据的独立插件三种加载方式；partial/full/NativeAOT 还发布可裁剪插件宿主并以 `member-trimmed` 期望值复核成员缺失边界。self-contained 与 NativeAOT 需要下载 `win-x64`
runtime/compiler pack；
其中 NativeAOT 旁路部署使用原生 `ReflectionTrimProbe.exe` 作为 host anchor，插件 probe 会在目标目录按存在性选择 EXE 或 managed DLL，避免要求 AOT 不会生成的 `ReflectionTrimProbe.dll`；
本机已完成四条命令和产物执行，CI 使用同一矩阵继续防回归；NativeAOT 发布后先生成 Portable PDB/PE 符号映射，再运行
警告检查；发布前还会运行
`Test-Check-NativeAotWarnings.ps1`，固定校验来源分类、未解析行、未解析门禁和仓库指纹门禁的负向路径。
检查步骤会加载版本化指纹基线和带 `owner/reason` 的责任 allowlist，并生成同一次发布的紧凑候选随报告上传；默认阻断新增仓库诊断或未登记责任，显式诊断排障输入才允许漂移。
手动 `workflow_dispatch` 传入非空 `baseline_ref` 时，独立 job 还会执行一次
`Run-ReflectionComparison.ps1 -BaselineRef ... -Rounds 1` 并上传结果；同时可选传入
`cold_start_max_time_ns`、`cold_start_max_bytes`、`steady_state_max_time_ns` 和
`steady_state_max_bytes`，由脚本分别校验 candidate 冷启动和稳态预算并生成
`cold-start-budget.json`、`steady-state-budget.json`；预算脚本还会校验两版本每轮的
7 个冷启动模式以及所有稳态场景均恰好产生一条结果。
手动运行只填写预算而不填写 `baseline_ref` 会显式失败。当前工作流在 PR 使用 base SHA 执行三轮比较；手动运行通过 `comparison_rounds` 选择 1–20 轮，默认一轮。不预设业务性能阈值；配置存在不代表远程 CI 已通过。

## 支持边界

- 这是显式元数据保留下的 trimming smoke，不是整个库的任意输入兼容承诺。
- `Reflection.Emit` 动态类型仍只在动态代码可用的 CLR 验证；NativeAOT 探针只覆盖元数据
  子集并确认动态 Emit 被拒绝，不能推导整个库或表达式编译入口在 AOT 下可用。
- 未显式保留 linker roots 的任意业务类型，以及更大规模的 trimming 组合尚未完成矩阵验证；
  当前已覆盖普通字段/属性、私有字段/属性、闭合泛型模型、嵌套路径读写模型、两个由独立
  root 保留的外部程序集模型、一个未 root 的外部插件部署边界和一个故意声明可裁剪的成员缺失插件；同时用独立可回收
  `AssemblyLoadContext` probe 验证路径、`AssemblyDependencyResolver` 和字节数组插件加载。当前 NativeAOT 产物不携带该外部插件，不能推广为所有插件加载方式的固定行为。
- CI 在 Windows net8 job 中使用同一发布参数执行 framework-dependent analyzer、self-contained partial/full trim 和 NativeAOT 四种部署边界，并运行对应插件加载 probe；失败时会阻断该 job。

## 未声明 IsTrimmable 的第三方插件边界（2026-10-07）

新增 `ReflectionTrimProbe.NonTrimmablePluginModels` 独立程序集。该项目没有声明
`IsTrimmable`，不引用宿主探针或 Bing.Utils；`ReflectionTrimThirdPartyPluginProbe`
只通过程序集名称加载它，再使用反射工具验证类型、公共无参构造函数、字段、属性、方法和
`DescriptionAttribute` 元数据。

本机验证结果：

- framework-dependent 宿主：path/resolver/bytes 均为 `metadata-preserved`；目标目录不携带插件时三种方式均为 `assembly-trimmed`。
- self-contained partial trim 宿主旁路目录：path/resolver/bytes 均为 `metadata-preserved`。
- self-contained full trim 宿主旁路目录：path/resolver/bytes 均为 `metadata-preserved`。
- NativeAOT 宿主旁路目录：path/resolver/bytes 均为 `metadata-preserved`。

探针输出固定格式为 `reflection-third-party-plugin-boundary:<mode>:<result>`，workflow 已在四个宿主边界中执行该探针。该矩阵只证明一个不声明 `IsTrimmable`、仅依赖框架程序集的简单插件；真实第三方依赖、复杂 linker root 组合、插件版本冲突和插件自身的裁剪设置仍需单独验证，不能据此承诺任意动态加载程序集安全。

## 带独立依赖的第三方插件边界（2026-10-07）

在简单插件模型之外，新增未声明 `IsTrimmable` 的独立依赖程序集
`ReflectionTrimProbe.NonTrimmablePluginDependency`。插件的 `Execute` 方法通过反射加载的依赖
类型调用静态方法；探针同时检查依赖类型、`DescriptionAttribute` 和依赖方法返回值，避免只验证
插件本体而遗漏依赖解析。

`Stage-ExternalPlugin.ps1` 新增 `AdditionalAssemblyPath`，将依赖与插件、宿主放入同一旁路目录。
探针的 path/resolver/bytes 三种模式均在隔离可回收 `AssemblyLoadContext` 中按目标目录解析依赖：

- framework-dependent：三种方式均为 `metadata-preserved`。
- self-contained partial trim：三种方式均为 `metadata-preserved`。
- self-contained full trim：三种方式均为 `metadata-preserved`。
- NativeAOT 宿主旁路目录：三种方式均为 `metadata-preserved`。
- 故意不复制依赖程序集：三种方式均为 `member-trimmed`。

该矩阵只覆盖一个简单的一层依赖和当前 `net8.0/win-x64` 配置；依赖版本冲突、多级依赖、插件
自身裁剪和复杂 linker root 组合仍未形成通用兼容承诺。

## 两层独立依赖的第三方插件边界（2026-10-07）

在一层依赖之外，新增未声明 `IsTrimmable` 的第二层程序集
`ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency`。插件通过直接依赖调用第二层程序集，探针
在执行插件方法后再检查两层依赖的类型、`DescriptionAttribute`、静态方法和返回值，确保依赖解析不是
只在程序集存在性层面通过。

本机验证结果：

- framework-dependent 宿主：两层依赖均部署时，path/resolver/bytes 均为 `metadata-preserved`。
- framework-dependent 宿主：只部署直接依赖而缺少第二层依赖时，path/resolver/bytes 均为 `member-trimmed`。
- self-contained partial trim 宿主旁路目录：两层依赖的 path/resolver/bytes 均为 `metadata-preserved`。
- self-contained full trim 宿主旁路目录：两层依赖的 path/resolver/bytes 均为 `metadata-preserved`。
- NativeAOT 宿主旁路目录：两层依赖的 path/resolver/bytes 均为 `metadata-preserved`。

该矩阵把多级依赖解析扩展到两层，但仍未覆盖依赖版本冲突、更多层级、插件自身裁剪和复杂
linker root 组合，不能据此承诺任意动态加载程序集的通用 AOT 安全性。

## 同名替代程序集版本冲突边界（2026-10-07）

新增 `ReflectionTrimProbe.NonTrimmablePluginDependencyV2`，其输出程序集仍命名为
`ReflectionTrimProbe.NonTrimmablePluginDependency`，但程序集版本为 `2.0.0.0`。插件编译时引用
的是 `1.0.0.0`，探针通过 `version-conflict-v2` profile 检查隔离加载上下文实际装载的版本、描述
特性和方法返回值，避免只凭类型名称判断解析成功。

本机验证结果：

- framework-dependent 宿主：path/resolver/bytes 均加载 `2.0.0.0` 并为 `metadata-preserved`。
- self-contained partial trim 宿主旁路目录：path/resolver/bytes 均加载 `2.0.0.0` 并为 `metadata-preserved`。
- self-contained full trim 宿主旁路目录：path/resolver/bytes 均加载 `2.0.0.0` 并为 `metadata-preserved`。
- NativeAOT 宿主旁路目录：path/resolver/bytes 均加载 `2.0.0.0` 并为 `metadata-preserved`。

该结果证明当前隔离加载上下文在目标目录只有一个替代版本时不会错误回退到宿主版本；并存多个
同名版本、强名称签名冲突和更复杂 linker root 组合仍需单独验证。

## 同进程多版本插件隔离边界（2026-10-07）

新增 `--multi-version` probe 模式，同时创建两个可回收的 `AssemblyLoadContext`：第一个目录部署
依赖 `1.0.0.0` 及其第二层依赖，第二个目录部署同名依赖 `2.0.0.0`。两个上下文在同一进程中
保持存活，分别加载插件并验证依赖版本、插件类型身份和方法返回值，避免串行卸载掩盖版本污染。

本机验证结果：

- framework-dependent 宿主：path/resolver/bytes 三种并存加载方式均为 `metadata-preserved`。
- v1 上下文实际加载 `1.0.0.0`，v2 上下文实际加载 `2.0.0.0`，两个依赖程序集和插件类型均不共享引用身份。

workflow 已接入 framework、partial/full 和 NativeAOT 四组双目录 staging 及同进程 probe；四个
宿主的 path/resolver/bytes 均为 `metadata-preserved`。强名称签名冲突及更复杂多插件组合仍未覆盖。

## Framework-dependent PublishTrimmed 不支持边界（2026-10-07）

当前 SDK 8.0.425 在 `SelfContained=false` 且 `PublishTrimmed=true` 时返回 `NETSDK1102`，不会生成
可执行的裁剪发布物。本机已复现该失败；workflow 的 net8 验证 job 现在要求该命令非零退出且输出
包含 `NETSDK1102`，把 SDK 行为变化转化为显式契约。真正的链接器产物验证继续使用 self-contained
partial/full trim 与 NativeAOT；若未来 SDK 支持 framework-dependent trimming，需要删除负向断言并
恢复独立产物执行验证。

## 多插件资源拒绝边界（2026-10-08）

`ReflectionTrimThirdPartyPluginProbe` 的 `--multi-plugin` 与 `--multi-plugin-rounds` 现在最多接收
32 个部署目录。第 33 个目录在路径规范化和文件访问之前被拒绝，命令返回退出码 `2`，并输出
`reflection-third-party-plugin-boundary:multi-plugin:resource-rejected:contexts:33>32`；因此拒绝路径不会创建
任何 `AssemblyLoadContext`。新增 `Test-MultiPluginCapacity.ps1` 使用 33 个不存在目录执行隔离 probe，输出
`reflection-third-party-plugin-capacity:verified:max-contexts=32`；同一 probe 使用 9 个真实 profile 目录并以合法
v1 副本补齐到 32 个上下文，在新建临时 staging 中对 framework、partial trim、full trim 和 NativeAOT 四类宿主均执行
`--multi-plugin` 单轮及 `--multi-plugin-rounds 3`，每类宿主的 path/resolver/bytes 正向 marker 全部为
`metadata-preserved`，四类宿主均退出码 `0`，并输出
`reflection-third-party-plugin-capacity:positive-admission:verified:contexts=32:rounds=3:markers=9`。
该边界只约束验证探针的资源输入与当前夹具的上限准入，不替代生产插件宿主的容量策略、动态程序集卸载评估或性能预算。

## 多插件复杂图 20 轮边界（2026-10-08）

新增 `Test-MultiPluginLongRounds.ps1`，在 framework host staging 中强制接收 9 个 profile 目录并运行
`--multi-plugin-rounds 20`，在复用的 partial/full/NativeAOT host-output staging 中各运行 10 轮；每轮的
path/resolver/bytes 均输出 `metadata-preserved`，分别共 60/30 个 marker，脚本同时校验轮次完整性和 marker
数量，避免只检查退出码掩盖缺失分支。该边界覆盖四类宿主的当前 9 类复杂夹具，不替代更高轮次、生产容量策略
或性能预算。
