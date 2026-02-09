# 模块：Bing.Utils（核心）

## 1. 模块定位
- 目标：提供 Bing.Utils 的核心通用工具能力（参数校验、对象/字符串扩展、Json、线程同步、URL/参数构造、签名/加密、日志等），供上层/其他模块复用；证据：src/Bing.Utils/Bing/Helpers/Check.cs | Check.NotNull，src/Bing.Utils/Json/JsonHelper.cs | JsonHelper.ToJson，src/Bing.Utils/Bing/Threading/Asyncs/AsyncLock.cs | AsyncLock.LockAsync。
- 非目标：不提供具体业务领域封装；对外部系统的集成能力（例如特定 HTTP API、特定图像引擎）应由独立模块承担；待确认：是否存在对业务模块的直接引用（未在本次 core 扫描中发现）。
- 适用场景：
    - 统一 guard clause 参数校验与异常提示；证据：src/Bing.Utils/Bing/Helpers/Check.cs | Check.NotNullOrWhiteSpace。
    - 常用对象/字符串扩展（转换、克隆、抽取/过滤等）；证据：src/Bing.Utils/Bing/Extensions/ObjectExtensions.cs | ObjectExtensions.AsOrDefault，src/Bing.Utils/Bing/Extensions/Bases/StringExtensions.cs | StringExtensions.ExtractAround。
    - JSON 序列化/反序列化与简单格式判断；证据：src/Bing.Utils/Json/JsonHelper.cs | JsonHelper.ToObject，src/Bing.Utils/Json/JsonExtensions.cs | JsonExtensions.ToJson。
    - URL 查询参数构造与拼接；证据：src/Bing.Utils/Bing/Utils/Parameters/UrlParameterBuilder.cs | UrlParameterBuilder.Result，src/Bing.Utils/Bing/Helpers/Url.cs | Url.Join。
    - 简化异常捕获与函数式组合（Try/Match/Recover/Linq 风格扩展）；证据：src/Bing.Utils/Bing/Exceptions/Try.cs | Try.LiftException，src/Bing.Utils/Bing/Exceptions/TryExtensions.cs | TryExtensions.SelectMany。
- 不适用场景：
    - 对性能极敏感的热点路径（例如高频反射/大对象深拷贝/大量 JSON）；应结合 benchmark/缓存策略评估；待确认：当前仓库是否为这些 API 提供了针对性 benchmark。
    - 对加密合规有强要求的场景（密钥管理、算法策略、国密等）；本模块提供的是通用加解密/签名工具；证据：src/Bing.Utils/Bing/Helpers/Encrypt.cs | Encrypt.Rsa2Sign。

## 2. 目录结构
    src/Bing.Utils/
    tests/Bing.Utils.Tests/

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `Bing.Helpers.Check.NotNull` / `NotNullOrWhiteSpace` | 参数校验（guard clause），验证失败抛出参数异常；证据：src/Bing.Utils/Bing/Helpers/Check.cs  Check.NotNullOrWhiteSpace | value/parameterName/maxLength/minLength | 校验通过返回原值 | `ArgumentNullException` / `ArgumentException` |
| `Bing.ObjectExtensions.As<T>` / `TryAs<T>` | 对象强制转换与 Try 风格转换；证据：src/Bing.Utils/Bing/Extensions/ObjectExtensions.cs  ObjectExtensions.TryAs | object | `T` / `bool` | 无（TryAs 不抛；As 可能抛 `InvalidCastException`） |
| `Bing.Extensions.ObjectExtensions.ClonePropertyFrom` / `ClonePropertyTo` | 通过反射拷贝字段/属性值（支持排除名单）；证据：src/Bing.Utils/Bing/Extensions/Bases/ObjectExtensions.cs  ObjectExtensions.ClonePropertyFrom | destination/source/excludeName | 成功复制数量 | 内部吞掉反射异常（调用方无感知） |
| `Bing.Extensions.StringExtensions.ExtractAround` | 从指定索引附近截取字符串（带边界检查）；证据：src/Bing.Utils/Bing/Extensions/Bases/StringExtensions.cs  StringExtensions.ExtractAround | value/index/left/right | string | `IndexOutOfRangeException` |
| `Bing.Utils.Json.JsonHelper.ToJson` / `ToObject<T>` | 使用 Newtonsoft.Json 进行序列化/反序列化；证据：src/Bing.Utils/Json/JsonHelper.cs  JsonHelper.ToObject | target/json | string / `T` | 由 Newtonsoft.Json 抛出的异常（如循环引用序列化异常） |
| `Bing.Utils.Json.JsonExtensions.ToJson` / `ToObject<T>` | JsonHelper 的扩展封装（string/object 扩展方法）；证据：src/Bing.Utils/Json/JsonExtensions.cs  JsonExtensions.ToJson | this string/this object | string / `T` | 由 JsonHelper/Json.NET 决定 |
| `Bing.Exceptions.Try.LiftValue` / `LiftException` | 将值/异常提升为 `Try<T>`（Success/Failure）；证据：src/Bing.Utils/Bing/Exceptions/Try.cs  Try.LiftValue | value/exception/cause | `Try<T>` | 无（Lift 本身不抛） |
| `Bing.Exceptions.TryExtensions.Select/SelectMany/Where` | 为 `Try<T>` 提供 LINQ 风格组合；证据：src/Bing.Utils/Bing/Exceptions/TryExtensions.cs  TryExtensions.SelectMany | source/selector/convert | `Try<TResult>` | `ArgumentNullException` / `InvalidOperationException`（Where 失败时提升异常） |
| `Bing.Utils.Parameters.UrlParameterBuilder`（`Add/Result/JoinUrl`） | 构造并拼接 URL 查询参数；证据：src/Bing.Utils/Bing/Utils/Parameters/UrlParameterBuilder.cs  UrlParameterBuilder.JoinUrl | key/value/url/isSort/isUrlEncode/encoding | string / dictionary | `ArgumentNullException`（JoinUrl 传入 url 为空时由 Url.Join 抛出） |
| `Bing.Utils.Signatures.SignManager.Sign` / `Verify` | 基于 RSA2（SHA256）对参数串签名/验签；证据：src/Bing.Utils/Bing/Utils/Signatures/SignManager.cs  SignManager.Sign | sign | string / bool | 由底层加密实现决定 |
| `Bing.Logging.LogHelper.Initialize/For/CreateLogger` | 统一日志入口，基于 `ILoggerFactory` 创建并缓存 logger；证据：src/Bing.Utils/Bing/Logging/LogHelper.cs  LogHelper.Initialize | ILoggerFactory/category/type | `LogHelper<T>` / `ILogger` | `ArgumentNullException` |
| `Bing.Threading.Asyncs.AsyncLock.LockAsync` | 基于异步信号量实现的异步锁；证据：src/Bing.Utils/Bing/Threading/Asyncs/AsyncLock.cs  AsyncLock.LockAsync | - | `Task<AsyncLock.Releaser>` | 无 |
| `Bing.Threading.OneTimeRunner.Run` | 确保某个操作在对象生命周期内只执行一次（含并发保护）；证据：src/Bing.Utils/Bing/Threading/OneTimeRunner.cs  OneTimeRunner.Run | Action | void | 无 |
| `Bing.Threading.TaskCache.TrueResult/FalseResult` | 复用常用 `Task<bool>`，减少分配；证据：src/Bing.Utils/Bing/Threading/TaskCache.cs  TaskCache.TrueResult | - | `Task<bool>` | 无 |
| `Bing.Helpers.Url.Combine/Join` | URL 片段合并与查询参数拼接；证据：src/Bing.Utils/Bing/Helpers/Url.cs  Url.Combine | urls/url/param | string / Uri | `ArgumentNullException` / `UriFormatException` |
| `Bing.Helpers.Encrypt`（`Md5By32/DesEncrypt/Rsa2Sign/Rsa2Verify`） | 通用加解密与签名验签工具；证据：src/Bing.Utils/Bing/Helpers/Encrypt.cs  Encrypt.Rsa2Verify | value/key/publicKey/sign/encoding | string / bool | 由加密实现决定（部分方法对空输入返回空字符串/false） |

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Helpers.Check` | 统一参数校验入口（NotNull/Length/AssignableTo 等） | 是（无共享可变状态） | 验证失败抛异常；证据：src/Bing.Utils/Bing/Helpers/Check.cs  Check.NotNull |
| `Bing.ObjectExtensions` | object 扩展（As/TryAs/IsOn 等） | 是（纯方法） | 部分方法用 try/catch 返回默认值；证据：src/Bing.Utils/Bing/Extensions/ObjectExtensions.cs  ObjectExtensions.AsOrDefault |
| `Bing.Extensions.StringExtensions` | string 扩展集合（抽取/过滤/移除/反转等） | 是（纯方法） | 部分方法会抛出索引/参数异常；证据：src/Bing.Utils/Bing/Extensions/Bases/StringExtensions.cs  StringExtensions.ExtractAround |
| `Bing.Utils.Json.JsonHelper` / `JsonExtensions` | Json 序列化/反序列化与扩展方法 | 是（无共享状态） | 使用 Newtonsoft.Json；循环引用序列化会抛异常；证据：src/Bing.Utils/Json/JsonHelper.cs  JsonHelper.ToJson |
| `Bing.Exceptions.Try` / `Try<T>` / `Success<T>` / `Failure<T>` | Try 模式与函数式错误处理（Match/Recover/Tap/Bind） | 是（对象不可变/无共享状态） | `Failure<T>.Value` 访问会重抛；证据：src/Bing.Utils/Bing/Exceptions/Try`1.Failure.cs  Failure<T>.Value |
| `Bing.Utils.Parameters.ParameterBuilder` / `UrlParameterBuilder` | 参数管理与 URL 参数拼装（排序/编码/格式化） | 否（内部持有可变字典） | 适合作为短生命周期 builder 使用；证据：src/Bing.Utils/Bing/Utils/Parameters/ParameterBuilder.cs  ParameterBuilder.Add |
| `Bing.Utils.Signatures.SignManager` / `SignKey` | 基于 builder 输出进行 RSA2 签名/验签 | 取决于使用方式（实例级可变 builder） | `SignManager.Add` 会累积参数；证据：src/Bing.Utils/Bing/Utils/Signatures/SignManager.cs  SignManager.Add |
| `Bing.Logging.LogHelper` | 日志工厂包装与 logger 缓存 | 部分（内部使用 ConcurrentDictionary；Initialize 改变工厂） | 建议启动时先 Initialize；证据：src/Bing.Utils/Bing/Logging/LogHelper.cs  LogHelper.Initialize |
| `Bing.Threading.Asyncs.AsyncLock` / `AsyncSemaphore` | 异步同步原语（锁/信号量） | 是（内部用锁保护队列） | LockAsync 返回可释放 token；证据：src/Bing.Utils/Bing/Threading/Asyncs/AsyncSemaphore.cs  AsyncSemaphore.WaitAsync |
| `Bing.Threading.OneTimeRunner` | 一次性执行器（并发场景只执行一次） | 是（volatile + lock） | 适合静态字段防重复初始化；证据：src/Bing.Utils/Bing/Threading/OneTimeRunner.cs  OneTimeRunner.Run |
| `Bing.Helpers.Url` | URL 合并、拼接、域名解析等 | 是（无共享状态） | Join 对空 url 抛异常；证据：src/Bing.Utils/Bing/Helpers/Url.cs  Url.Join |
| `Bing.Helpers.Encrypt` | 加解密/签名验签（MD5/DES/AES/RSA/HMAC 等） | 待确认（包含静态可变配置，如 DesKey） | `DesKey` 为静态可写属性；证据：src/Bing.Utils/Bing/Helpers/Encrypt.cs  Encrypt.DesKey |

## 5. 依赖关系
- 直接依赖：
    - `Newtonsoft.Json`（Json 序列化/反序列化）；证据：src/Bing.Utils/Json/JsonHelper.cs | JsonHelper.ToObject。
    - `Microsoft.Extensions.Logging.Abstractions`（日志接口与 NullLogger）；证据：src/Bing.Utils/Bing/Logging/LogHelper.cs | LogHelper.Initialize。
    - `AspectCore.Extensions.Reflection`（反射特性读取 reflector）；证据：src/Bing.Utils/Bing/Reflection/Reflections/Reflections.Attributes.cs | Reflections.GetAttribute。
    - `System.ComponentModel.Annotations`（DisplayAttribute 等）；证据：src/Bing.Utils/Bing/Reflection/Reflections/Reflections.Description.cs | Reflections.GetDisplayNameOrDescription。
- 可选依赖：
    - `System.Text.Json`（netstandard2.0 条件引用）；待确认：当前 core 代码中是否存在直接使用点。
    - `System.Text.Encoding.CodePages`（引用于项目）；待确认：当前 core 代码中是否存在 `CodePagesEncodingProvider` 注册/使用点。
- 禁止依赖：
    - 图像引擎/图像处理库（应由 drawing 系列模块提供）。
    - 特定业务系统 SDK（应由 integrations/专用模块承担）。

## 6. 关键实现说明
### 6.1 算法/流程
- guard clause：`Check` 提供 Required/NotNull/NotNullOrEmpty 等校验，在失败时抛出明确异常；证据：src/Bing.Utils/Bing/Helpers/Check.cs | Check.Required。
- Try 模式：通过 `Try.LiftValue/LiftException` 构造 Success/Failure，并用 `Match/Recover` 等实现组合；证据：src/Bing.Utils/Bing/Exceptions/Try.cs | Try.LiftException，src/Bing.Utils/Bing/Exceptions/Try`1.Success.cs | Success<T>.Match。
- URL 参数输出：`UrlParameterBuilder` 内部委托 `ParameterBuilder`，统一排序/编码/格式化输出；证据：src/Bing.Utils/Bing/Utils/Parameters/UrlParameterBuilder.cs | UrlParameterBuilder.Result。
- 日志缓存：`LogHelper.For<T>` 使用 `ConcurrentDictionary<Type, object>` 缓存 `LogHelper<T>`；证据：src/Bing.Utils/Bing/Logging/LogHelper.cs | LogHelper.For。
- 异步锁：`AsyncLock` 基于 `AsyncSemaphore` 实现，返回 `Releaser` 以 `Dispose` 释放；证据：src/Bing.Utils/Bing/Threading/Asyncs/AsyncLock.cs | AsyncLock.Releaser.Dispose。

### 6.2 边界与异常处理
- Json：`JsonHelper.ToJson(null)` 返回空字符串；循环引用序列化会抛出 `JsonSerializationException`；证据：src/Bing.Utils/Json/JsonHelper.cs | JsonHelper.ToJson，tests/Bing.Utils.Tests/Json/JsonHelperTest.cs | JsonHelperTest.Test_Loop。
- 参数校验：`Check.Require<TException>` 在 message 为空时会抛 `ArgumentNullException`；证据：src/Bing.Utils/Bing/Helpers/Check.cs | Check.Required。
- 字符串扩展：`StringExtensions.ExtractAround` 在 index 超界时抛 `IndexOutOfRangeException`；证据：src/Bing.Utils/Bing/Extensions/Bases/StringExtensions.cs | StringExtensions.ExtractAround。
- URL 拼接：`Url.Join(string url, ...)` 当 url 为空时抛 `ArgumentNullException`；证据：src/Bing.Utils/Bing/Helpers/Url.cs | Url.Join。
- 异步信号量：构造函数对 initialCount 做大于 0 校验；证据：src/Bing.Utils/Bing/Threading/Asyncs/AsyncSemaphore.cs | AsyncSemaphore.AsyncSemaphore。

## 7. 性能与复杂度
- 时间复杂度：按具体方法而定。
    - `TaskCache.TrueResult/FalseResult`：$O(1)$；证据：src/Bing.Utils/Bing/Threading/TaskCache.cs | TaskCache.TrueResult。
    - `OneTimeRunner.Run`：无竞争时近似 $O(1)$；证据：src/Bing.Utils/Bing/Threading/OneTimeRunner.cs | OneTimeRunner.Run。
- 空间复杂度：按具体方法而定（多数为常量或与输入长度线性相关）。
    - `StringExtensions.ExtractLettersNumbers/ExtractNumbers` 通过 `StringBuilder` 聚合，分配与输出长度相关；证据：src/Bing.Utils/Bing/Extensions/Bases/StringExtensions.cs | StringExtensions.ExtractNumbers。
- 大数据量表现：待确认（当前未发现针对 core API 的性能基准数据/报告）。
- Benchmark 链接：benchmarks/Bing.Utils.Benchmark（待确认：是否覆盖 core 关键 API）。

## 8. 测试策略
- 单测覆盖点：
    - Json 序列化/反序列化、循环引用异常；证据：tests/Bing.Utils.Tests/Json/JsonHelperTest.cs | JsonHelperTest.Test_Loop。
    - 字符串扩展的提取/过滤等行为；证据：tests/Bing.Utils.Tests/Extensions/Base/StringExtensionsTest.cs | StringExtensionsTest.Test_ExtractAround。
    - 对象属性拷贝与强制转换；证据：tests/Bing.Utils.Tests/Extensions/Base/ObjectExtensionsTest.cs | ObjectExtensionsTest.Test_ClonePropertyFrom_SameType。
    - URL 参数构造、排序与 JoinUrl；证据：tests/Bing.Utils.Tests/Parameters/UrlParameterBuilderTest.cs | UrlParameterBuilderTest.Test_Result_Sort。
    - RSA2 签名输出一致性；证据：tests/Bing.Utils.Tests/Signatures/SignManagerTest.cs | SignManagerTest.Test_Sign_1。
    - 一次性运行器在并发下只执行一次；证据：tests/Bing.Utils.Tests/Threading/OneTimeRunnerTest.cs | OneTimeRunnerTest.Test_Run_MultipleThreads_ActionExecutedOnce。
    - 日志初始化、缓存与日志方法调用；证据：tests/Bing.Utils.Tests/Bing/Logging/LogHelperTest.cs | LogHelperTest.For_Should_ReturnLogHelperOfTInstance。
    - 参数校验异常与边界；证据：tests/Bing.Utils.Tests/Bing/Helpers/CheckTest.cs | CheckTest.NotNull_NullValue_ThrowsArgumentNullException。
- 边界用例：null/empty/whitespace、索引越界、URL 空值、循环引用对象图、并发执行。
- 回归用例：待确认（未在 core 模块文档中找到“历史 bug -> 用例”映射说明）。

## 9. 版本与兼容性
- 当前版本：1.5.0；证据：version.props | VersionPrefix（待确认：模块级版本是否与仓库统一版本完全一致）。
- 破坏性变更：待确认（未在本文件内建立与 ReleaseNotes 的映射）。
- 升级建议：
    - 若使用 `LogHelper`，建议在应用启动阶段先调用 `LogHelper.Instance.Initialize(ILoggerFactory)`；证据：src/Bing.Utils/Bing/Logging/LogHelper.cs | LogHelper.Initialize。
    - 若使用 `Encrypt.DesKey`（静态可写），注意全局状态影响；证据：src/Bing.Utils/Bing/Helpers/Encrypt.cs | Encrypt.DesKey。

## 10. 使用示例
```csharp
using Bing.Helpers;
using Bing.Logging;
using Bing.Utils.Json;
using Bing.Utils.Parameters;

// 1) 参数校验（guard clause）
var name = Check.NotNullOrWhiteSpace("alice", nameof(name));

// 2) Json 序列化/反序列化
var json = JsonHelper.ToJson(new { Name = name }, camelCase: true);
var obj = JsonHelper.ToObject<Dictionary<string, object>>(json);

// 3) URL 参数构造
var url = new UrlParameterBuilder().Add("a", 1).Add("b", 2).JoinUrl("http://test.com");

// 4) 日志（需要先 Initialize）
// LogHelper.Instance.Initialize(loggerFactory);
// LogHelper.Instance.LogInformation("hello");
```

## 11. 待办与改进
- [ ] 为 core 关键 API（Json、参数构造、Try、反射、加密）补充/完善 benchmark（当前仅发现基准工程入口，覆盖情况待确认）。
- [ ] 梳理并补齐“破坏性变更/兼容性说明”与 ReleaseNotes 的对应关系（待确认）。

## 12. 证据定位
- src/Bing.Utils/Bing/Helpers/Check.cs | Check.NotNullOrWhiteSpace：提供 guard clause 参数校验，并在失败时抛出明确参数异常。
- src/Bing.Utils/Bing/Extensions/ObjectExtensions.cs | ObjectExtensions.TryAs：提供 Try 风格的强制转换，避免异常控制流。
- src/Bing.Utils/Bing/Extensions/Bases/StringExtensions.cs | StringExtensions.ExtractAround：提供字符串范围提取并包含越界保护。
- src/Bing.Utils/Json/JsonHelper.cs | JsonHelper.ToJson：提供基于 Json.NET 的序列化入口，null 输入返回空字符串。
- tests/Bing.Utils.Tests/Json/JsonHelperTest.cs | JsonHelperTest.Test_Loop：验证循环引用对象图序列化会抛出 JsonSerializationException。
- src/Bing.Utils/Bing/Exceptions/Try`1.Failure.cs | Failure<T>.Value：访问失败值会重抛捕获的异常。
- src/Bing.Utils/Bing/Exceptions/TryExtensions.cs | TryExtensions.SelectMany：为 Try 模式提供 LINQ 风格组合。
- src/Bing.Utils/Bing/Utils/Parameters/UrlParameterBuilder.cs | UrlParameterBuilder.JoinUrl：将参数串拼接到 URL，内部调用 Url.Join。
- src/Bing.Utils/Bing/Utils/Signatures/SignManager.cs | SignManager.Sign：对参数串进行 RSA2 签名。
- src/Bing.Utils/Bing/Helpers/Encrypt.cs | Encrypt.Rsa2Verify：提供 RSA2（SHA256）验签实现。
- src/Bing.Utils/Bing/Logging/LogHelper.cs | LogHelper.For<T>：基于 ConcurrentDictionary 缓存每个类型的 LogHelper<T>。
- src/Bing.Utils/Bing/Threading/OneTimeRunner.cs | OneTimeRunner.Run：通过 volatile + lock 确保操作只执行一次。

## 13. 待确认
- core 项目条件引用的 `System.Text.Json`、`System.Text.Encoding.CodePages` 是否存在明确使用点与场景说明（当前未在 `src/Bing.Utils` 扫描到典型调用）。
- benchmarks/Bing.Utils.Benchmark 是否覆盖 core 的关键 API（Json/参数构造/反射/加密/线程同步）。
- 模块级版本与兼容性说明（破坏性变更）是否已在 docs/ReleaseNotes.md 中有可引用条目。
