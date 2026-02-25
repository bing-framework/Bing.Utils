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
- 是否分配敏感
- `GetFullyQualifiedName` 在泛型类型场景会构造 `StringBuilder` 和递归字符串拼接，有一定分配开销。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:33` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:41`
- 是否线程安全
- 主要 API 为静态纯函数，无共享可变状态。
- 是否可并发调用
- 可并发调用。

## 5. 异常与日志策略
- 抛出哪些异常
- `ArgumentNullException`（空 `Type` 入参）与 `InvalidOperationException`（不支持的成员类型）。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.cs:19` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:25`
- 什么时候返回默认值而不是抛异常
- 当前展示 API 偏显式失败（抛异常），不做默认值吞错。

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `Bing.Utils` 核心包。[证据] `src/Bing.Utils.Reflection/dependency.props:3`
- 被哪些包复用
- 当前 `src` 层未见其他子包直接引用 `Bing.Utils.Reflection`。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 测试项目通过聚合工程引用本包：`tests/Bing.Utils.Tests`、`tests/BingUtilsUT`。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:17` [证据] `tests/BingUtilsUT/BingUtilsUT.csproj:16`
- 关键类：`TypeIsNumericTypeTest`、`TypeIsTupleTest`、`ReflectionsTest`（以 `Bing.Reflection` 命名空间验证反射行为）。[证据] `tests/BingUtilsUT/TypeUT/TypeIsNumericTypeTest.cs:6` [证据] `tests/BingUtilsUT/TypeUT/TypeIsTupleTest.cs:6` [证据] `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsTest.cs:8`
- 未覆盖风险点
- `TypeMetaVisitExtensions`（`MemberInfo`/`ParameterInfo` 扩展）缺少独立精确单测映射（TODO）。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- 包内部对核心 `Types` 能力有依赖，升级 `Bing.Utils` 时需联动回归类型判定语义。[证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:46`

