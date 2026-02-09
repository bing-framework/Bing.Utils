# 模块：Bing.Utils.Drawing.ImageSharp

## 1. 模块定位
- 目标：提供基于 `SixLabors.ImageSharp` 的图片加载/转换与基础处理（目前包含：加载 FromFile/FromStream/FromBytes/FromBase64String/FromDataUrl；输出 ToBytes/ToBase64String/ToDataUrl；设置透明度 SetOpacity）。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | Bing.Drawing.ImageSharpHelper.FromFile/FromStream/FromBytes/FromBase64String/FromDataUrl
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | Bing.Drawing.ImageSharpHelper.ToBytes/ToBase64String/ToDataUrl
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | Bing.Drawing.ImageSharpHelper.SetOpacity
- 非目标：
    - 不覆盖 System.Drawing 相关的验证码/位图算法能力（这些在 `Bing.Utils.Drawing` 模块中）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj | 仅引用 SixLabors.ImageSharp
    - 不提供图像缩放/裁剪/滤镜矩阵/复杂效果的封装（当前仅看到 SetOpacity，其余能力由调用方直接使用 ImageSharp API）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | 仅包含 SetOpacity
- 适用场景：
    - 在不依赖 `System.Drawing` 的情况下加载图片并生成 bytes/base64/dataUrl 输出。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | From* 返回 SixLabors.ImageSharp.Image
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | ToBytes/ToBase64String/ToDataUrl
    - 需要用统一方式把透明度应用到图像副本（不修改原图引用）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | SetOpacity（CloneAs<Rgba32> + Mutate.Opacity）
- 不适用场景：
    - 需要“可诊断失败原因”的加载场景（多数 From* 在异常时直接吞掉并返回 default/null）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | FromFile/FromBytes/FromBase64String/FromStream 的 catch { return default; }

## 2. 目录结构
    src/Bing.Utils.Drawing.ImageSharp/
        Bing/Drawing/（ImageSharpHelper 分部类：Load/Convert/基础处理）
    tests/Bing.Utils.Drawing.Tests/（待确认：仓库中未见 ImageSharp 对应测试工程）
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | namespace Bing.Drawing
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | namespace Bing.Drawing
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | namespace Bing.Drawing

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `ImageSharpHelper.FromFile(string)` | 从文件加载图片；失败返回 `default` | `filePath` | `Image?` | 无（内部捕获异常） | 
| `ImageSharpHelper.FromFile<TPixel>(string)` | 从文件加载图片（指定像素格式）；失败返回 `default` | `filePath` | `Image<TPixel>?` | 无（内部捕获异常） | 
| `ImageSharpHelper.FromStream(Stream)` | 从流加载图片；失败返回 `default` | `stream` | `Image?` | `ArgumentNullException`（stream 为 null） | 
| `ImageSharpHelper.FromStream<TPixel>(Stream)` | 从流加载图片（指定像素格式）；失败返回 `default` | `stream` | `Image<TPixel>?` | `ArgumentNullException`（stream 为 null） | 
| `ImageSharpHelper.FromBytes(byte[])` | 从字节数组加载图片；失败返回 `default` | `bytes` | `Image?` | `ArgumentNullException`（bytes 为 null） | 
| `ImageSharpHelper.FromBytes<TPixel>(byte[])` | 从字节数组加载图片（指定像素格式）；失败返回 `default` | `bytes` | `Image<TPixel>?` | `ArgumentNullException`（bytes 为 null） | 
| `ImageSharpHelper.FromBase64String(string)` | 从 base64 字符串加载图片；失败返回 `default` | `base64String` | `Image?` | 无（内部捕获异常） | 
| `ImageSharpHelper.FromBase64String<TPixel>(string)` | 从 base64 字符串加载图片（指定像素格式）；失败返回 `default` | `base64String` | `Image<TPixel>?` | 无（内部捕获异常） | 
| `ImageSharpHelper.FromDataUrl(string)` | 从 DataUrl 加载图片；格式不匹配或失败返回 `default` | `dataUrl` | `Image?` |  | 
| `ImageSharpHelper.FromDataUrl<TPixel>(string)` | 从 DataUrl 加载图片（指定像素格式）；格式不匹配或失败返回 `default` | `dataUrl` | `Image<TPixel>?` |  | 
| `ImageSharpHelper.ToBytes(Image, IImageFormat?)` | 将图像输出为 bytes；默认格式为 JPEG | `image`、`imageFormat` | `byte[]` | `ArgumentNullException` | 
| `ImageSharpHelper.ToBase64String(Image, IImageFormat?)` | 输出 base64；默认格式为 JPEG | `image`、`imageFormat` | `string` | `ArgumentNullException` | 
| `ImageSharpHelper.ToDataUrl(Image, IImageFormat?)` | 输出 DataUrl（MIME 来自 `imageFormat.DefaultMimeType`）；默认格式为 JPEG | `image`、`imageFormat` | `string` | `ArgumentNullException` | 
| `ImageSharpHelper.SetOpacity(Image, float)` | 克隆为 `Rgba32` 后设置透明度并返回新图像 | `image`、`opacity` | `Image` | `ArgumentNullException`、`ArgumentOutOfRangeException`（0..1） | 

> API 证据：
> - src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | ImageSharpHelper.From*
> - src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | ImageSharpHelper.ToBytes/ToBase64String/ToDataUrl（默认 JpegFormat.Instance）
> - src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | ImageSharpHelper.SetOpacity

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Drawing.ImageSharpHelper` | ImageSharp 适配的图片加载/转换/基础处理入口 | 是（纯静态方法），但图像对象本身非线程安全 | `From*` 多数失败返回 `default`；`SetOpacity` 返回新图像实例 | 

> 核心类型证据：
> - src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | ImageSharpHelper

## 5. 依赖关系
- 直接依赖：
- 直接依赖：
    - `SixLabors.ImageSharp`（2.1.11）
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj | PackageReference SixLabors.ImageSharp 2.1.11
- 可选依赖：无（当前 csproj 未见条件依赖）。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj | 仅一个 PackageReference
- 禁止依赖：待确认（未见本模块显式“禁止依赖”约束）。

## 6. 关键实现说明
### 6.1 算法/流程
- DataUrl 解析：使用 `ImageDataUrl` 正则匹配允许的图片 MIME（bmp/emf/exif/gif/icon/jpeg/png/tiff/wmf），成功后取 `DATA` 分组并走 `FromBase64String`。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | ImageDataUrl/FromDataUrl
- 加载失败策略：
    - `FromFile/FromBytes/FromBase64String/FromStream`：捕获所有异常并返回 `default`（其中 Stream/Bytes 的 null 会先抛 `ArgumentNullException`）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | FromFile/FromBytes/FromBase64String/FromStream
- 默认输出格式：
    - `ToBytes/ToBase64String/ToDataUrl` 在 `imageFormat` 为空时默认使用 `JpegFormat.Instance`。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | imageFormat ??= JpegFormat.Instance
- 透明度：
    - `SetOpacity`：校验 `opacity` 在 0..1；将原图 `CloneAs<Rgba32>()` 生成副本，并在副本上 `Mutate(o => o.Opacity(opacity))`，返回副本。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | SetOpacity

### 6.2 边界与异常处理
- `FromStream/FromStream<TPixel>`：`stream == null` 抛 `ArgumentNullException`；其他异常吞掉并返回 `default`。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | FromStream/FromStream<TPixel>
- `FromBytes/FromBytes<TPixel>`：`bytes == null` 抛 `ArgumentNullException`；其他异常吞掉并返回 `default`。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | FromBytes/FromBytes<TPixel>
- `ToBytes/ToBase64String/ToDataUrl`：`image is null` 抛 `ArgumentNullException`。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | ToBytes/ToBase64String/ToDataUrl
- `SetOpacity`：
    - `image is null` 抛 `ArgumentNullException`；`opacity` 不在 0..1 抛 `ArgumentOutOfRangeException`。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | SetOpacity
- 待确认：`FromFile` 对路径非法/不存在的诊断信息被吞掉（返回 null）；若需要可观测性，可能需要新增 Try 模式 + out Exception 或日志回调。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | FromFile catch { return default; }

## 7. 性能与复杂度
- 时间复杂度：
    - `SetOpacity` 对整图进行像素处理（由 ImageSharp 内部实现，复杂度与像素数相关，通常可视为 $O(w\times h)$）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | SetOpacity（Mutate.Opacity）
- 空间复杂度：
    - `SetOpacity` 会克隆新图像（额外占用一份图像内存）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | CloneAs<Rgba32>()
    - `ToBytes/ToBase64String` 会把输出写入内存流并转成 `byte[]`/base64（内存占用与输出大小相关）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | new MemoryStream() + ToArray()
- 大数据量表现：待确认（仓库未见该模块的 benchmark）。
- Benchmark 链接：待确认。

## 8. 测试策略
- 单测覆盖点：待确认（仓库中未检索到 `Bing.Utils.Drawing.ImageSharp` 对应测试工程/用例）。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj | 当前模块存在但 tests 未见引用/工程
- 边界用例（建议补充）：
    - `From*` 返回 default 的分支：非法路径/损坏图片/无效 base64/无效 dataUrl。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | 多处 catch { return default; }
    - `SetOpacity` 的参数范围与返回图像是否为新实例。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | SetOpacity
- 回归用例（建议补充）：`ToDataUrl` 的 MIME 与输出格式一致性（默认 JPEG）。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | imageFormat ??= JpegFormat.Instance + DefaultMimeType

## 9. 版本与兼容性
- 当前版本：1.5.0
    - 证据：version.props | VersionPrefix=1.5.0
- 依赖版本：`SixLabors.ImageSharp` 2.1.11
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj | PackageReference SixLabors.ImageSharp 2.1.11
- 破坏性变更：待确认（当前文档基于仓库现状未梳理历史版本差异）。
- 升级建议：
    - 调用方显式管理 `Image` 的释放（ImageSharp 的 `Image` 通常需要 Dispose，具体以其类型契约为准）。
        - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | 返回 SixLabors.ImageSharp.Image（未封装释放）

## 10. 使用示例
```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Bing.Drawing;

// 1) 加载 + 透明度
using var image = ImageSharpHelper.FromFile("input.png");
if (image is null)
    return;

using var opacityImage = ImageSharpHelper.SetOpacity(image, 0.5f);

// 2) 输出 DataUrl（指定 PNG）
var dataUrl = ImageSharpHelper.ToDataUrl(opacityImage, PngFormat.Instance);

// 3) 输出 bytes（默认 JPEG）
var bytes = ImageSharpHelper.ToBytes(opacityImage);
```

## 11. 待办与改进
- [ ] 增加 `Bing.Utils.Drawing.ImageSharp` 的单元测试工程与用例（覆盖 From*/To*/SetOpacity 的正常与异常路径）。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | From*
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | To*
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | SetOpacity
- [ ] 为 `From*` 提供可选的失败诊断能力（例如 TryXxx + out Exception，或可注入日志回调），避免“吞异常”导致排障困难。
    - 证据：src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | catch { return default; }

## 12. 证据定位（汇总）
- src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj | 包描述、SixLabors.ImageSharp 依赖
- src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Load.cs | FromFile/FromStream/FromBytes/FromBase64String/FromDataUrl + ImageDataUrl 正则
- src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.Convert.cs | ToBytes/ToBase64String/ToDataUrl（默认 JpegFormat.Instance）
- src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs | SetOpacity（CloneAs<Rgba32> + Mutate.Opacity）
- version.props | VersionPrefix=1.5.0
