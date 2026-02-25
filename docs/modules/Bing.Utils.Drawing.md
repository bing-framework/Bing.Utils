# Bing.Utils.Drawing
## 1. 包职责（Scope）
- 解决的问题
- 基于 `System.Drawing` 提供验证码生成与图像处理辅助。[证据] `src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:5` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:11`
- 不解决的问题（Out of Scope）
- 不提供前端渲染组件或跨平台图像引擎抽象层（ImageSharp/SkiaSharp 由独立子包承担）。[证据] `Bing.Utils.sln:79` [证据] `Bing.Utils.sln:81`

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `CaptchaBuilder.GetCode(int length, CaptchaType)`、`CreateImage(string code)`、`CreateImage(int length, out string code, CaptchaType)`。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:118` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:216` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:456`
- 输入输出约定
- 通过属性驱动生成策略（字体、颜色、干扰线/点、随机倾斜等）。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:35` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:85`
- 边界行为（null、空集合、非法参数）
- `length <= 0` 抛 `ArgumentOutOfRangeException`。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:120` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:121`
- `code` 为空白抛 `ArgumentNullException`。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:218` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:219`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
var builder = new CaptchaBuilder();
var code = builder.GetCode(10, CaptchaType.NumberAndLetter);
```
[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:11` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:18`
- 示例2：进阶用法
```csharp
builder.RandomPointPercent = 5;
builder.Height = 50;
builder.RandomColor = true;
using var image = builder.CreateImage(4, out var code, CaptchaType.ChineseChar);
```
[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:40` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:43`
- 示例3：常见错误与修正
```csharp
Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetCode(0));
Assert.Throws<ArgumentNullException>(() => builder.CreateImage(" "));
// 修正：保证 length > 0 且 code 非空白
```
[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:120` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:218`

## 4. 性能与线程安全说明
- 是否分配敏感
- 生成图片时会创建 `Bitmap`、`Graphics` 与多种绘图对象，属于分配敏感路径。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:224` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:225` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:359`
- 是否线程安全
- `CaptchaBuilder` 是有状态实例（属性可变），并包含静态 `Random` 字段，不建议多线程共享同一实例。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:18` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:35`
- 是否可并发调用
- 建议“每线程/每请求独立实例”并发调用。

## 5. 异常与日志策略
- 抛出哪些异常
- 参数非法抛 `ArgumentOutOfRangeException` / `ArgumentNullException`。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:121` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:219`
- 什么时候返回默认值而不是抛异常
- 当前核心 API 对非法输入倾向抛异常，不返回默认值。

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `Bing.Utils` 与 `System.Drawing.Common`。[证据] `src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:15` [证据] `src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:19`
- 被哪些包复用
- 当前 `src` 层未见其他子包直接引用 `Bing.Utils.Drawing`。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 项目：`tests/Bing.Utils.Tests` 引用本包。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:13`
- 类：`CaptchaBuilderTest`（验证码生成与图片生成冒烟）。[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:5`
- 未覆盖风险点
- 当前测试以“执行通过+输出”为主，断言强度有限（如像素正确性、并发一致性未覆盖）。[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:19` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:47`

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- Linux/Docker 需安装 `libgdiplus` 等系统依赖，否则运行时可能失败。[证据] `src/Bing.Utils.Drawing/README.md:2` [证据] `src/Bing.Utils.Drawing/README.md:6`

