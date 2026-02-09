# 模块：Bing.Utils.Drawing.SkiaSharp

## 1. 模块定位
- 目标：提供基于 `SkiaSharp` 的图片加载/转换与基础处理（目前包含：加载 FromFile/FromStream/FromBytes/FromBase64String/FromDataUrl；输出 ToBytes/ToBase64String/ToDataUrl；设置透明度 SetOpacity；`SKEncodedImageFormat` 的 MIME 类型扩展）。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | Bing.Drawing.SkiaSharpHelper.FromFile/FromStream/FromBytes/FromBase64String/FromDataUrl
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | Bing.Drawing.SkiaSharpHelper.ToBytes/ToBase64String/ToDataUrl
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | Bing.Drawing.SkiaSharpHelper.SetOpacity
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs | Bing.Drawing.SKEncodedImageFormatExtensions.GetMimeType
- 非目标：
    - 不覆盖 System.Drawing 相关的验证码/位图算法能力（这些在 `Bing.Utils.Drawing` 模块中）。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj | 仅引用 SkiaSharp
    - 不提供更完整的图像处理 API 面封装（除 SetOpacity 外，其它处理需调用方直接使用 SkiaSharp API）。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | 仅包含 SetOpacity
- 适用场景：
    - 需要基于 SkiaSharp 加载图片并输出 bytes/base64/dataUrl。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | From* 返回 SKImage
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | ToBytes/ToBase64String/ToDataUrl（默认 Png, Quality=100）
    - 需要对图像设置统一透明度并输出一个新 `SKImage`。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity（SKBitmap.FromImage + 新 SKBitmap + FromBitmap）
- 不适用场景：
    - 需要“可诊断失败原因”的加载场景（From* 多数捕获异常后返回 default/null）。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | catch { return default; }

## 2. 目录结构
    src/Bing.Utils.Drawing.SkiaSharp/
        Bing/Drawing/（SkiaSharpHelper 分部类：Load/Convert/基础处理）
        Bing/Drawing/Extensions/（SKEncodedImageFormatExtensions）
    tests/Bing.Utils.Drawing.Tests/（待确认：仓库中未见 SkiaSharp 对应测试工程）
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | namespace Bing.Drawing
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | namespace Bing.Drawing
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs | namespace Bing.Drawing

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `SkiaSharpHelper.FromFile(string)` | 从文件加载图片（内部先 ReadAllBytes 再 FromEncodedData）；失败返回 `default` | `filePath` | `SKImage?` | 无（内部捕获异常） | 
| `SkiaSharpHelper.FromStream(Stream)` | 从流加载图片；失败返回 `default` | `stream` | `SKImage?` | `ArgumentNullException`（stream 为 null） | 
| `SkiaSharpHelper.FromBytes(byte[])` | 从字节数组加载图片；失败返回 `default` | `bytes` | `SKImage?` | `ArgumentNullException`（bytes 为 null） | 
| `SkiaSharpHelper.FromBase64String(string)` | 从 base64 字符串加载图片；失败返回 `default` | `base64String` | `SKImage?` | 无（内部捕获异常） | 
| `SkiaSharpHelper.FromDataUrl(string)` | 从 DataUrl 加载图片；格式不匹配或失败返回 `default` | `dataUrl` | `SKImage?` |  | 
| `SkiaSharpHelper.ToBytes(SKImage, (Format,Quality)?)` | 将图像输出为 bytes；默认 `Png,100` | `image`、`imageFormat` | `byte[]` | `ArgumentNullException` | 
| `SkiaSharpHelper.ToBase64String(SKImage, (Format,Quality)?)` | 输出 base64；默认 `Png,100` | `image`、`imageFormat` | `string` | `ArgumentNullException` | 
| `SkiaSharpHelper.ToDataUrl(SKImage, (Format,Quality)?)` | 输出 DataUrl（使用 `SKEncodedImageFormatExtensions.GetMimeType` 拼接）；默认 `Png,100` | `image`、`imageFormat` | `string` | `ArgumentNullException` | 
| `SkiaSharpHelper.SetOpacity(SKImage, float)` | 逐像素设置 alpha（返回新 `SKImage`） | `image`、`opacity` | `SKImage` | `ArgumentNullException`、`ArgumentOutOfRangeException`（0..1） | 
| `SKEncodedImageFormatExtensions.GetMimeType(SKEncodedImageFormat)` | 获取 MIME 类型字符串 | `format` | `string` |  | 

> API 证据：
> - src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | SkiaSharpHelper.From*
> - src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | SkiaSharpHelper.ToBytes/ToBase64String/ToDataUrl（默认 Png,100）
> - src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SkiaSharpHelper.SetOpacity
> - src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs | GetMimeType

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Drawing.SkiaSharpHelper` | SkiaSharp 适配的图片加载/转换/基础处理入口 | 是（纯静态方法），但图像对象本身非线程安全 | `From*` 多数失败返回 `default`；`SetOpacity` 返回新图像实例 | 
| `Bing.Drawing.SKEncodedImageFormatExtensions` | `SKEncodedImageFormat` 的 MIME 映射 | 是（纯静态方法） | 返回值为完整 MIME（如 `image/png`） | 

> 核心类型证据：
> - src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SkiaSharpHelper
> - src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs | SKEncodedImageFormatExtensions

## 5. 依赖关系
- 直接依赖：
- 直接依赖：
    - `SkiaSharp`（2.88.9）
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj | PackageReference SkiaSharp 2.88.9
- 可选依赖：无（当前 csproj 未见条件依赖）。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj | 仅一个 PackageReference
- 禁止依赖：待确认（未见本模块显式“禁止依赖”约束）。

## 6. 关键实现说明
### 6.1 算法/流程
- DataUrl 解析：使用 `ImageDataUrl` 正则匹配允许的图片 MIME（bmp/emf/exif/gif/icon/jpeg/png/tiff/wmf），成功后取 `DATA` 分组并走 `FromBase64String`。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | ImageDataUrl/FromDataUrl
- 文件加载：`FromFile` 先 `File.ReadAllBytes(filePath)`，再 `SKImage.FromEncodedData(bytes)`。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | FromFile
- 输出格式默认值：`ToBytes/ToBase64String/ToDataUrl` 在 `imageFormat` 为空时默认 `(SKEncodedImageFormat.Png, 100)`。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | imageFormat ?? (Png,100)
- 透明度：`SetOpacity` 将 `SKImage` 转为 `SKBitmap`，逐像素读取并写入新 `SKBitmap`，最后 `SKImage.FromBitmap(output)` 返回。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity

### 6.2 边界与异常处理
- `FromStream`：`stream == null` 抛 `ArgumentNullException`；其它异常吞掉并返回 `default`。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | FromStream
- `FromBytes`：`bytes == null` 抛 `ArgumentNullException`；其它异常吞掉并返回 `default`。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | FromBytes
- `ToBytes/ToBase64String/ToDataUrl`：`image is null` 抛 `ArgumentNullException`。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | ToBytes/ToBase64String/ToDataUrl
- `SetOpacity`：
    - `image is null` 抛 `ArgumentNullException`；`opacity` 不在 0..1 抛 `ArgumentOutOfRangeException`。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity
- 待确认/潜在问题：`ToDataUrl` 当前拼接为 `data:image/{format.Format.GetMimeType()};base64,...`，而 `GetMimeType` 返回的是完整 MIME（如 `image/png`），可能导致 `data:image/image/png;base64,...` 这样的重复前缀。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | ToDataUrl（$"data:image/{format.Format.GetMimeType()}..."）
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs | GetMimeType 返回 "image/png" 等
- 待确认：`SetOpacity` 会把所有像素 alpha 统一设为 `0xFF*opacity`，而不是在原 alpha 基础上做乘法；是否符合预期需验证。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity（color.WithAlpha((byte)(0xFF * opacity))）

## 7. 性能与复杂度
- 时间复杂度：
    - `SetOpacity` 为逐像素遍历，复杂度约为 $O(w\times h)$。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity（双层 for 遍历宽高）
- 空间复杂度：
    - `SetOpacity` 会创建一个同尺寸的 `SKBitmap` 作为输出，额外占用一份像素内存。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | new SKBitmap(width,height,...)
    - `FromFile` 先整文件读入 `byte[]`，对大文件会有额外内存占用。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | File.ReadAllBytes
    - `ToBytes/ToBase64String` 会把编码后的数据转为 `byte[]` 并可能再转 base64（内存占用与输出大小相关）。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | Encode(...).ToArray() + Convert.ToBase64String
- 大数据量表现：待确认（仓库未见该模块的 benchmark）。
- Benchmark 链接：待确认。

## 8. 测试策略
- 单测覆盖点：待确认（仓库中未检索到 `Bing.Utils.Drawing.SkiaSharp` 对应测试工程/用例）。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj | 当前模块存在但 tests 未见引用/工程
- 边界用例（建议补充）：
    - `From*` 返回 default 的分支：非法路径/损坏图片/无效 base64/无效 dataUrl。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | 多处 catch { return default; }
    - `ToDataUrl` 的拼接格式（避免 `data:image/image/png`）与 MIME 映射正确性。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | ToDataUrl
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs | GetMimeType
    - `SetOpacity` 的 alpha 行为（覆盖/乘法）与性能回归。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity
- 回归用例（建议补充）：默认输出格式（Png,100）与 DataUrl 输出一致性。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | imageFormat ?? (Png,100)

## 9. 版本与兼容性
- 当前版本：1.5.0
    - 证据：version.props | VersionPrefix=1.5.0
- 依赖版本：`SkiaSharp` 2.88.9
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj | PackageReference SkiaSharp 2.88.9
- 破坏性变更：待确认（当前文档基于仓库现状未梳理历史版本差异）。
- 升级建议：
    - 调用方显式管理 `SKImage/SKBitmap/SKData` 等对象的释放（该模块返回 `SKImage`，不封装生命周期）。
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | 返回 SKImage
        - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | Encode(...).ToArray()（涉及 SKData）

## 10. 使用示例
```csharp
using SkiaSharp;
using Bing.Drawing;

// 1) 加载
using var image = SkiaSharpHelper.FromFile("input.png");
if (image is null)
    return;

// 2) 设置透明度（返回新 SKImage）
using var opacityImage = SkiaSharpHelper.SetOpacity(image, 0.5f);

// 3) 输出 bytes（默认 Png, Quality=100）
var bytes = SkiaSharpHelper.ToBytes(opacityImage);

// 4) 输出 DataUrl（注意：当前实现可能拼出重复 image/ 前缀，建议先补测试确认）
var dataUrl = SkiaSharpHelper.ToDataUrl(opacityImage);
```

## 11. 待办与改进
- [ ] 增加 `Bing.Utils.Drawing.SkiaSharp` 的单元测试工程与用例（覆盖 From*/To*/SetOpacity 与异常/边界路径）。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | From*
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | To*
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity
- [ ] 修正/确认 `ToDataUrl` 的 MIME 拼接逻辑（避免 `data:image/image/png`），并补充回归测试。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | ToDataUrl
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs | GetMimeType
- [ ] 评估 `SetOpacity` 的 alpha 语义（覆盖 vs 乘法保留原 alpha），并用测试锁定期望。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity
- [ ] 评估 `FromFile` 是否需要避免 `ReadAllBytes`（对大文件减少内存占用），并补充性能/内存基准。
    - 证据：src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | File.ReadAllBytes

## 12. 证据定位（汇总）
- src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj | 包描述、SkiaSharp 依赖
- src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs | FromFile/FromStream/FromBytes/FromBase64String/FromDataUrl + ImageDataUrl 正则
- src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Convert.cs | ToBytes/ToBase64String/ToDataUrl（默认 Png,100）
- src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs | SetOpacity（逐像素设置 alpha）
- src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs | GetMimeType（返回完整 MIME）
- version.props | VersionPrefix=1.5.0
