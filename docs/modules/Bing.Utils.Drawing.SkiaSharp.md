# Bing.Utils.Drawing.SkiaSharp
## 1. 包职责（Scope）
- 解决的问题
- 提供 SkiaSharp 图片格式到 MIME 的映射扩展。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj:5` [证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:10`
- 不解决的问题（Out of Scope）
- 不提供完整图像处理流水线（当前公开 API 主要是格式映射）。

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `SKEncodedImageFormatExtensions.GetMimeType(this SKEncodedImageFormat format)`。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:16`
- 输入输出约定
- 已知格式返回标准 MIME；未知格式回退 `application/octet-stream`。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:20` [证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:28`
- 边界行为（null、空集合、非法参数）
- 输入为枚举值，不涉及 null；未知枚举值走默认分支。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:18` [证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:28`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
var mime = SKEncodedImageFormat.Png.GetMimeType();
Assert.Equal("image/png", mime);
```
[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:21`
- 示例2：进阶用法
```csharp
var map = new Dictionary<SKEncodedImageFormat, string>
{
    [SKEncodedImageFormat.Jpeg] = SKEncodedImageFormat.Jpeg.GetMimeType(),
    [SKEncodedImageFormat.Webp] = SKEncodedImageFormat.Webp.GetMimeType()
};
```
[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:20` [证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:22`
- 示例3：常见错误与修正
```csharp
var unknown = (SKEncodedImageFormat)999;
var mime = unknown.GetMimeType(); // application/octet-stream
// 修正：上游业务应在调用前过滤非法格式值
```
[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:28`
- TODO：以上示例尚无仓库现成测试用例可直接引用。

## 4. 性能与线程安全说明
- 是否分配敏感
- 纯 `switch` 分支返回常量字符串，分配开销低。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:18`
- 是否线程安全
- 无共享可变状态，线程安全。
- 是否可并发调用
- 可并发调用。

## 5. 异常与日志策略
- 抛出哪些异常
- 当前方法无显式抛异常路径。
- 什么时候返回默认值而不是抛异常
- 未知格式返回 `application/octet-stream`，不抛异常。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:28`

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `SkiaSharp`，未声明对 `Bing.Utils` 的项目引用。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj:15`
- 被哪些包复用
- 当前 `src` 层未发现其他子包直接引用此包。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- TODO：仓库内未发现 `Bing.Utils.Drawing.SkiaSharp` 的直接测试项目或测试类。
- 未覆盖风险点
- MIME 映射正确性、未知枚举值兼容策略缺少自动化测试（TODO）。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- 当前固定 `SkiaSharp` 版本为 `2.88.9`，升级需回归格式支持差异。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj:15`

