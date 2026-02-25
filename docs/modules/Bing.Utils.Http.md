# Bing.Utils.Http
## 1. 包职责（Scope）
- 解决的问题
- 提供 Web 运行时上下文辅助：请求/响应读取、参数提取、编码解码、Cookie、下载输出等。[证据] `src/Bing.Utils.Http/Bing.Utils.Http.csproj:3` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:17` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:410`
- 提供网络地址与 IPv4/IPv6 相关工具（位于 `Bing.Net.*` 目录）。[证据] `src/Bing.Utils.Http/Bing/Net/IPv6/IPv6CidrCalculator.cs:1`
- 不解决的问题（Out of Scope）
- 不作为完整 HTTP API SDK（更偏宿主内辅助工具），复杂业务协议编排需上层处理。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:70` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:103`

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `Web`：`GetParam`、`UrlEncode/UrlDecode`、`GetCookie/SetCookie/RemoveCookie`、`DownloadAsync`、`Redirect`。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:410` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:449` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:539` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:788` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:809` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:760`
- 输入输出约定
- 全局静态入口，依赖 `HttpContextAccessor` 提供上下文。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:70` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:79`
- 多处采用“无上下文返回空字符串/空集合”策略（如 `Body`、`GetFiles`）。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:197` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:372`
- 边界行为（null、空集合、非法参数）
- `GetParam(name)` 的 `name` 为空白抛 `ArgumentException`。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:413`
- `UrlEncode(url)` 的 `url` 为空返回空字符串；非法编码名抛 `ArgumentException`。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:461` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:470`
- `DownloadAsync(byte[],...)` 在空字节/空文件名时抛 `ArgumentException`。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:763` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:765`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
var token = Web.AccessToken; // Bearer 头自动提取
token.ShouldBe("abc123");
```
[证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:171` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:183` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:186`
- 示例2：进阶用法
```csharp
var encoded = Web.UrlEncode("http://example.com", isUpper: true);
var decoded = Web.UrlDecode(encoded);
decoded.ShouldBe("http://example.com");
```
[证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:476` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:482` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:567` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:574`
- 示例3：常见错误与修正
```csharp
Should.Throw<ArgumentException>(() => Web.GetParam(" "));
Should.Throw<ArgumentNullException>(() => Web.UrlDecode("abc", (Encoding)null));
// 修正：参数名非空白，编码对象非 null
```
[证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:447` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:450` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:610` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:617`

## 4. 性能与线程安全说明
- 是否分配敏感
- `Body`/`DownloadAsync` 会读写流和字节数组，属于分配敏感路径。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:201` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:775`
- 是否线程安全
- `Web` 使用全局可变静态属性（`HttpContextAccessor`/`Environment`），配置期不是无锁线程安全语义。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:70` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:89`
- 是否可并发调用
- 读方法可并发，但应避免并发覆盖全局静态依赖；建议在宿主启动期一次性配置。

## 5. 异常与日志策略
- 抛出哪些异常
- 参数错误抛 `ArgumentException/ArgumentNullException`；上下文不可用抛 `InvalidOperationException`。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:413` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:488` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:563`
- 什么时候返回默认值而不是抛异常
- `Body` 无请求时返回空字符串；`UrlEncode/UrlDecode` 输入空字符串返回空字符串；`GetFiles` 无文件返回空列表。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:197` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:485` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:530` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:372`

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `Bing.Utils`。[证据] `src/Bing.Utils.Http/references.props:4`
- 按目标框架依赖 ASP.NET Core 组件（`FrameworkReference`/`PackageReference`）。[证据] `src/Bing.Utils.Http/dependency.props:15` [证据] `src/Bing.Utils.Http/dependency.props:32`
- 被哪些包复用
- 当前 `src` 层未见其他子包直接引用 `Bing.Utils.Http`（主要由测试或业务侧引用）。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 单测项目：`tests/Bing.Utils.Http.Tests`。[证据] `tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj:11`
- 集成测试项目：`tests/Bing.Utils.Http.Tests.Integration`。[证据] `tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj:40`
- 关键类：`WebTest` 覆盖参数、Cookie、编码、下载、重定向、异常路径；`HttpClientServiceTest.*` 覆盖集成调用。[证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:12` [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Post.cs:1`
- 未覆盖风险点
- 全局静态上下文并发覆盖场景缺少明确并发测试（TODO）。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- `dependency.props` 里按 TFM 分支配置差异较大，升级框架时应回归条件分支行为。[证据] `src/Bing.Utils.Http/dependency.props:3` [证据] `src/Bing.Utils.Http/dependency.props:31`

