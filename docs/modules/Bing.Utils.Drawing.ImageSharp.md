# Bing.Utils.Drawing.ImageSharp
## 1. 包职责（Scope）
- 解决的问题
- 提供基于 `SixLabors.ImageSharp` 的图像处理辅助能力，当前核心公开能力为透明度设置。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj:5` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:10` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:21`
- 不解决的问题（Out of Scope）
- 不承担 `System.Drawing` 兼容层或验证码业务封装（由 `Bing.Utils.Drawing` 提供）。

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `ImageSharpHelper.SetOpacity(Image image, float opacity)`。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:21`
- 输入输出约定
- 输入原图 `image`，返回克隆后的新图像，不直接修改输入对象引用。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:28` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:30`
- 边界行为（null、空集合、非法参数）
- `image == null` 抛 `ArgumentNullException`。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:23` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:24`
- `opacity` 不在 `[0,1]` 抛 `ArgumentOutOfRangeException`。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:25` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:26`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
using SixLabors.ImageSharp;
using Bing.Drawing;

using var image = Image.Load("input.png");
using var output = ImageSharpHelper.SetOpacity(image, 0.5f);
output.Save("output.png");
```
[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:21` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:29`
- 示例2：进阶用法
```csharp
using var image = Image.Load("input.png");
foreach (var alpha in new[] { 0.2f, 0.5f, 0.8f })
{
    using var tmp = ImageSharpHelper.SetOpacity(image, alpha);
    tmp.Save($"output-{alpha}.png");
}
```
[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:29`
- 示例3：常见错误与修正
```csharp
Assert.Throws<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetOpacity(image, 1.5f));
// 修正：opacity 保持在 [0,1]
```
[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:25` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:26`
- TODO：以上示例尚无仓库现成测试用例可直接引用。

## 4. 性能与线程安全说明
- 是否分配敏感
- `CloneAs<Rgba32>()` 会复制图像数据，分配成本与图像尺寸相关。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:28`
- 是否线程安全
- 方法本身无全局可变状态；并发安全取决于调用方是否共享同一 `Image` 实例。
- 是否可并发调用
- 可并发调用；建议每次调用使用独立图像对象。

## 5. 异常与日志策略
- 抛出哪些异常
- `ArgumentNullException`、`ArgumentOutOfRangeException`。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:24` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:26`
- 什么时候返回默认值而不是抛异常
- 当前 API 对非法输入不返回默认值，直接抛异常。

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `SixLabors.ImageSharp`，未声明对 `Bing.Utils` 的项目引用。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj:15`
- 被哪些包复用
- 当前 `src` 层未发现其他子包直接引用此包。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- TODO：仓库内未发现 `Bing.Utils.Drawing.ImageSharp` 的直接测试项目或测试类。
- 未覆盖风险点
- 透明度边界、不同像素格式兼容性、并发处理性能均缺少测试覆盖（TODO）。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- 当前固定 `SixLabors.ImageSharp` 版本为 `2.1.11`，升级需回归编码器/像素格式行为。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj:15`

