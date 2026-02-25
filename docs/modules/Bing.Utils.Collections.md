# Bing.Utils.Collections
## 1. 包职责（Scope）
- 解决的问题
- 提供数组/集合/字典的安全转换、便捷操作与只读包装工具。[证据] `src/Bing.Utils.Collections/Bing.Utils.Collections.csproj:3` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:6` [证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:6`
- 不解决的问题（Out of Scope）
- 不提供缓存中间件、持久化容器或并发集合框架（仅工具层 API）。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:33` [证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:100`

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `Arrays.Empty<T>()`、`Arrays.ToArraySafety(...)`、`Arrays.GetLength(Array)`。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:30` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:60` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:206`
- `Dicts.AddValueOrUpdate`、`AddValueIfNotExist`、`AddRange`。[证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:28` [证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:68` [证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:100`
- 输入输出约定
- `ToArraySafety` 统一“空安全”：`count <= 0` 返回空数组，`src == null` 返回默认值填充数组或空数组。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:62` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:65` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:107`
- `Dicts` 多数方法要求外部传入有效字典实例；针对委托参数有空检查。[证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:30` [证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:33`
- 边界行为（null、空集合、非法参数）
- `ToArraySafety<T>(Array, int)` 在元素无法转换时抛 `InvalidCastException`（显式强转）。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:150`
- `GetLength(Array)` 在 `array == null` 时抛 `ArgumentNullException`。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:206`
- `AddValueOrUpdate` 对空委托抛 `ArgumentNullException`。[证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:31` [证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:33`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
var result = Arrays.ToArraySafety(new List<int> { 1, 2, 3, 4, 5 }, 3);
Assert.Equal(new[] { 1, 2, 3 }, result);
```
[证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:56` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:59` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:63`
- 示例2：进阶用法
```csharp
var list = new List<string> { "A", "B" };
var result = Arrays.ToArraySafety(list, 4);
Assert.Equal(new[] { "A", "B", null, null }, result);
```
[证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:73` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:76` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:80`
- 示例3：常见错误与修正
```csharp
Array array = new object[] { "not a number" };
Assert.Throws<InvalidCastException>(() => Arrays.ToArraySafety<int>(array, 1));
// 修正：确保源元素可转换为目标类型
```
[证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:216` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:219`

## 4. 性能与线程安全说明
- 是否分配敏感
- `ToArraySafety` 会分配新数组（按 count 或源长度）。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:64` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:142` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:183`
- 是否线程安全
- API 为静态无共享状态，逻辑本身可重入；但传入集合若在外部并发修改，结果由调用方保证。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:6` [证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:6`
- 是否可并发调用
- 可并发调用，前提是输入对象线程安全或只读。

## 5. 异常与日志策略
- 抛出哪些异常
- 类型转换失败时可能抛 `InvalidCastException`。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:150`
- 委托参数为空抛 `ArgumentNullException`。[证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:31` [证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:33`
- 什么时候返回默认值而不是抛异常
- 对 `null` 源集合偏向返回空数组/默认值数组。[证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:65` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:107` [证据] `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:143`

## 6. 与其他子包的关系
- 依赖的包
- 直接依赖 `Bing.Utils`。[证据] `src/Bing.Utils.Collections/references.props:4`
- 被哪些包复用
- 当前 `src` 目录内未发现其他子包直接 `ProjectReference` 到 `Bing.Utils.Collections`（主要由测试项目消费）。[证据] `src/Bing.Utils.Collections/references.props:4`

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 项目：`tests/Bing.Utils.Collections.Tests`。[证据] `tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj:11`
- 类：`ArraysTest`，覆盖空数组、空源、count 边界、类型转换异常等关键路径。[证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:10` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:89` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:105` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:213`
- 未覆盖风险点
- `Dicts` 系列 API 的独立单测不足（TODO）。[证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:6`

## 8. 版本与兼容性注意事项
- 跟随公共目标框架策略：`net8.0;net7.0;net6.0;netstandard2.0`。[证据] `common.props:3`
- 包通过 `references.props` 解耦核心依赖，新增依赖建议继续放入 props 文件统一管理。[证据] `src/Bing.Utils.Collections/Bing.Utils.Collections.csproj:10`

