# 模块：Bing.Utils.Drawing

## 1. 模块定位
- 目标：提供基于 `System.Drawing` 的图片/位图处理工具集，覆盖验证码生成、缩略图/缩放、常用格式转换（Bytes/Base64/DataUrl/Ico）、基础图像效果（亮度/对比度/柔边/双色调/黑白等）与像素级处理扩展。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | Bing.Drawing.CaptchaBuilder（GetCode/CreateImage）
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.cs | Bing.Drawing.ImageHelper（MakeThumbnail/ScaleImage/Rotate/Gray/...）
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Load.cs | Bing.Drawing.ImageHelper.FromBytes/FromDataUrl
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Convert.cs | Bing.Drawing.ImageHelper.ToBytes/ToBase64String/ToDataUrl/ToIcoStream
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ColorMatrices.cs | Bing.Drawing.ColorMatrices（ApplyMatrix/CreateBrightnessFilter/...）
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | Bing.Drawing.ImageEffect（SoftEdge）
    - 证据：src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs | Bing.Extensions.BitmapExtensions（灰度/二值化/滤波等大量扩展）
- 非目标：
    - 不提供跨平台图像引擎的抽象层与多后端统一（该能力拆分在 `Bing.Utils.Drawing.ImageSharp` / `Bing.Utils.Drawing.SkiaSharp` 等独立包，详见对应模块文档）。
        - 证据：src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj | PackageId=Bing.Utils.Drawing（当前包仅基于 System.Drawing.Common）
    - 不提供“完整图像处理流水线/作业编排/分布式处理”等平台能力（当前以函数/扩展方法为主）。
        - 证据：src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs | static 扩展方法集合
- 适用场景：
    - 需要在服务端生成验证码图片（字符串生成 + 绘制干扰线/点/倾斜/随机颜色等）。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | CreateImage(...)/DrawDisorderLine/DrawDisorderPoint/RandomItalic
    - 需要进行常见的图片转换与输出（Bytes/Base64/DataUrl/ICO）。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Convert.cs | ToBytes/ToBase64String/ToDataUrl/ToIcoStream
        - 证据：src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs | ToBase64String/ToBase64StringWithPrefix/ToBytes
    - 对 `Bitmap` 做像素级处理或做简单效果（亮度/对比度/黑白/双色调/柔边）。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | PerPixelProcess/SetBrightness/SetContrast
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ColorEffect.cs | ReplaceColor/SetBlackWhiteEffect/SetDuotoneEffect
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | SetSoftEdgeEffect
- 不适用场景：
    - 需要严格的跨平台一致性/高性能 GPU 加速/无 `System.Drawing` 依赖的场景（本模块依赖 `System.Drawing.Common`，非 Windows 场景通常还需要系统级依赖，具体以运行环境为准）。
        - 证据：src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj | PackageReference System.Drawing.Common
        - 证据：src/Bing.Utils.Drawing/README.md | Ubuntu/Docker 需安装 libgdiplus

## 2. 目录结构
    src/Bing.Utils.Drawing/
        Bing/Conversions/（颜色转换 ColorConv）
        Bing/Drawing/（Captcha/ImageHelper/效果/矩阵/枚举）
        Bing/Drawing/Extensions/（Bitmap/Color 扩展）
        Bing/Extensions/（Bitmap/Image 扩展，部分包含 unsafe/图像算法）
    tests/Bing.Utils.Drawing.Tests/（待确认：仓库中未见该测试工程）
    - 证据：src/Bing.Utils.Drawing/Bing/Conversions/ColorConv.cs | namespace Bing.Conversions
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | namespace Bing.Drawing
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | namespace Bing.Drawing
    - 证据：src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs | namespace Bing.Extensions

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `CaptchaBuilder.GetCode(...)` | 生成验证码字符串（数字/字母数字/汉字） | `length`、`captchaType` | `string` | `ArgumentOutOfRangeException` | 
| `CaptchaBuilder.CreateImage(string)` / `CreateImage(int,out string,...)` | 绘制验证码图片（可选干扰线/点/随机颜色/倾斜） | `code` 或 `length` 等 | `Bitmap` | `ArgumentNullException` / `ArgumentOutOfRangeException` | 
| `ImageHelper.MakeThumbnail(...)` | 生成缩略图（裁剪/定宽/定高/固定宽高） | `Image/bytes/path` + `width/height/mode` | `Image`/`void` | 见实现（GDI+ 相关异常待确认） | 
| `ImageHelper.ScaleImage(...)` | 缩放图像以适配目标宽高 | `image`、`width`、`height` | `Image` | 返回 `null`（image 为 null 或宽高非法） | 
| `ImageHelper.FromBytes/FromBase64String/FromDataUrl` | 从 bytes/base64/dataUrl 加载图片 | `bytes` / `string` | `Image` | `ArgumentNullException`（FromBytes） | 
| `ImageHelper.ToBytes/ToBase64String/ToDataUrl` | 输出 bytes/base64/dataUrl | `Image/Bitmap`、`ImageFormat` | `byte[]`/`string` | `ArgumentNullException` | 
| `ImageHelper.ToIcoStream(...)` | 输出 ICO（内部以 PNG 写入 ICO 结构） | `image`、`size` | `MemoryStream` | 见实现（待确认） | 
| `ImageHelper.DeleteCoordinate(...)` | 删除图片经纬度信息（覆盖或另存） | `filePath`、`savePath` 或 `Image` | `void` | 见实现（待确认） | 
| `ColorMatrices.ApplyMatrix/Create*Filter` | 颜色矩阵应用与常见滤镜矩阵构建（亮度/对比度/饱和度/灰度等） | `Image/ColorMatrix` 或 `amount` | `void`/`ColorMatrix` | `ArgumentOutOfRangeException` | 
| `Bing.Drawing.BitmapExtensions.SetBrightness/SetContrast/PerPixelProcess` | `Bitmap` 亮度/对比度与逐像素处理（并行） | `percentage`、`Func<Color,Color>` | `void` | `NotSupportedException`（像素格式不支持） | 
| `ColorExtensions.GetGrayScale/IsSimilarColors/...` | `Color` 灰度/相似度/混色等 | 见方法 | 见方法 |  | 
| `ColorEffect.ReplaceColor/SetBlackWhiteEffect/SetDuotoneEffect/SetErosionEffect` | 基于逐像素处理的颜色/黑白/双色调/冲蚀效果 | `Bitmap` + 参数 | `void` | 见实现（待确认） | 
| `ImageEffect.SetSoftEdgeEffect/CreateSoftEdgeBitmap` | 柔化边缘（基于 Alpha 蒙层 + 腐蚀/模糊） | `Bitmap`、`radius` | `void`/`Bitmap` | `NotSupportedException`（像素格式不支持） | 
| `Bing.Extensions.ImageExtensions.*` | `Image` 的 Base64/DataUrl/Bytes/缩放扩展（委托给 ImageHelper） | 见方法 | 见方法 |  | 
| `Bing.Conversions.ColorConv.*` | RGB/HSB 转换、sRGB/LinearRGB 转换、Hex/RGB 字符串输出 | 见方法 | `float[]/Color/string/double` |  | 

> API 证据：
> - src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | CaptchaBuilder.GetCode/CreateImage
> - src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.cs | ImageHelper.MakeThumbnail/ScaleImage/DeleteCoordinate/Rotate/Gray/...
> - src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Load.cs | ImageHelper.FromBytes/FromBase64String/FromDataUrl
> - src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Convert.cs | ImageHelper.ToBytes/ToBase64String/ToDataUrl/ToIcoStream
> - src/Bing.Utils.Drawing/Bing/Drawing/ColorMatrices.cs | ColorMatrices.ApplyMatrix/CreateBrightnessFilter/CreateContrastFilter/CreateSaturationFilter/CreateGrayScaleFilter
> - src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | SetBrightness/SetContrast/PerPixelProcess
> - src/Bing.Utils.Drawing/Bing/Drawing/Extensions/ColorExtensions.cs | ColorExtensions.*
> - src/Bing.Utils.Drawing/Bing/Drawing/ColorEffect.cs | ColorEffect.*
> - src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | ImageEffect.*
> - src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs | ImageExtensions.*
> - src/Bing.Utils.Drawing/Bing/Conversions/ColorConv.cs | ColorConv.*

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Drawing.CaptchaBuilder` | 验证码生成（字符串 + 图片绘制） | 否（实例可变属性） | 内部使用随机数与 `System.Drawing` 绘制 | 
| `Bing.Drawing.ImageHelper` | 图片辅助（缩略图/缩放/坐标删除/旋转/滤镜等） | 是（纯静态方法），但图像对象本身非线程安全 | 多数方法基于 GDI+，部分方法无显式 guard | 
| `Bing.Drawing.ColorMatrices` | 构建/应用颜色矩阵滤镜 | 是（纯静态方法） | `ApplyMatrix` 会直接修改输入图像 | 
| `Bing.Drawing.ColorEffect` | 颜色效果（替换色/黑白/双色调/冲蚀） | 否（无状态但依赖可变 Bitmap） | 基于逐像素处理扩展 | 
| `Bing.Drawing.ImageEffect` | 图片效果（柔边等） | 否（无状态但依赖可变 Bitmap） | 对像素格式有要求 | 
| `Bing.Drawing.BitmapExtensions` | `Bitmap` 扩展（逐像素处理、亮度/对比度） | 否（操作可变 Bitmap） | 使用 `Parallel.For` + 内存拷贝 | 
| `Bing.Extensions.BitmapExtensions` | `Bitmap` 扩展（灰度/二值化/形态学/滤波等，含 unsafe） | 否（操作可变 Bitmap） | 需要 `AllowUnsafeBlocks`；大量算法 O(w*h) | 
| `Bing.Conversions.ColorConv` | 颜色空间与字符串表示转换 | 是（纯静态方法） | 依赖 `System.Drawing.Color` | 
| `Bing.Extensions.ImageExtensions` | `Image` 扩展（委托 ImageHelper） | 否（操作可变 Image） | 便于链式调用 | 

> 核心类型证据：
> - src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | CaptchaBuilder
> - src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.cs | ImageHelper
> - src/Bing.Utils.Drawing/Bing/Drawing/ColorMatrices.cs | ColorMatrices
> - src/Bing.Utils.Drawing/Bing/Drawing/ColorEffect.cs | ColorEffect
> - src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | ImageEffect
> - src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | Bing.Drawing.BitmapExtensions
> - src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs | Bing.Extensions.BitmapExtensions（含 unsafe）
> - src/Bing.Utils.Drawing/Bing/Conversions/ColorConv.cs | ColorConv
> - src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs | ImageExtensions

## 5. 依赖关系
- 直接依赖：
    - `Bing.Utils`（项目引用）
        - 证据：src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj | ProjectReference ..\\Bing.Utils\\Bing.Utils.csproj
    - `System.Drawing.Common`（GDI+ 图像处理）
        - 证据：src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj | PackageReference System.Drawing.Common
- 可选依赖：
    - 非 Windows 环境的系统依赖（用于 System.Drawing/GDI+ 运行时能力）。
        - 证据：src/Bing.Utils.Drawing/README.md | Ubuntu/Docker 安装 libc6-dev/libgdiplus
- 禁止依赖：待确认（未见本模块显式“禁止依赖”约束）。

## 6. 关键实现说明
### 6.1 算法/流程
- 缩略图生成（`ImageHelper.MakeThumbnail`）：根据 `ThumbnailMode` 决定裁剪/等比缩放逻辑，然后通过 `Graphics.DrawImage` 绘制到新 `Bitmap`。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.cs | MakeThumbnail(...)（mode 分支 + DrawImage）
- 验证码图片生成（`CaptchaBuilder.CreateImage`）：计算画布大小 → 清屏 → 可选边框/干扰线/干扰点 → 绘制字符（可随机位置/颜色/倾斜）。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | CreateImage(string)（DrawBorder/DrawDisorderLine/DrawDisorderPoint/DrawText）
- 逐像素处理（`Bing.Drawing.BitmapExtensions.PerPixelProcess`）：锁定位图 → 拷贝像素到 `byte[]` → 并行遍历并调用回调 → 写回像素 → 解锁。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | PerPixelProcess(...)
- 柔边效果（`ImageEffect.SetSoftEdgeEffect`）：读取 Alpha 蒙层 → 腐蚀（AlphaErode）→ 模糊（AlphaBlur）→ 将蒙层映射回 Alpha 通道。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | SetSoftEdgeEffect(byte[]...)/AlphaErode/AlphaBlur/ApplySoftEdgeAlphaMask

### 6.2 边界与异常处理
- 参数 guard：
    - `CaptchaBuilder.GetCode`：`length <= 0` 抛 `ArgumentOutOfRangeException`。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | GetCode
    - `CaptchaBuilder.CreateImage(string)`：`code` 为空抛 `ArgumentNullException`。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | CreateImage(string)
    - `ImageHelper.FromBytes`：`bytes == null` 抛 `ArgumentNullException`；`FromDataUrl` 为空或不匹配正则时返回 `default`。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Load.cs | FromBytes/FromDataUrl/ImageDataUrl
    - `ImageHelper.ToBytes/ToBase64String/ToDataUrl`：`image/bitmap` 为 null 抛 `ArgumentNullException`。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Convert.cs | ToBytes/ToBase64String/ToDataUrl
- 像素格式限制：
    - `Bing.Drawing.BitmapExtensions.PerPixelProcess` 仅支持 `Format32bppArgb` 与 `Format24bppRgb`，否则抛 `NotSupportedException`。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | PerPixelProcess（pixelFormat 判断）
    - `ImageEffect.SetSoftEdgeEffect` 要求 `Format32bppArgb`，否则抛 `NotSupportedException`。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | SetSoftEdgeEffect(Bitmap,float)
- 数值范围：
    - `ColorMatrices.CreateBrightnessFilter/CreateContrastFilter/CreateSaturationFilter`：`amount < 0` 抛 `ArgumentOutOfRangeException`；`CreateGrayScaleFilter` 要求 `0..1`。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ColorMatrices.cs | CreateBrightnessFilter/CreateContrastFilter/CreateSaturationFilter/CreateGrayScaleFilter
- 待确认：`ImageHelper.FromBytes/FromBase64String` 内部 `using var ms = new MemoryStream(...)` 后直接 `Image.FromStream(ms)` 返回 `Image`，流被释放后图像对象后续行为是否始终安全需验证。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Load.cs | FromBytes/FromBase64String
- 待确认：`ImageHelper.MakeThumbnail` 捕获异常后 `throw e;` 可能导致堆栈信息丢失（建议 `throw;`）。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.cs | MakeThumbnail(Image,int,int,ThumbnailMode)（catch(Exception e){ throw e; }）

## 7. 性能与复杂度
- 时间复杂度：
    - 多数像素级处理为 $O(w\times h)$（逐像素遍历/卷积/腐蚀/模糊）。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | PerPixelProcess（双层循环遍历像素）
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | AlphaErode/AlphaBlur（遍历 rows/cols + iteration）
- 空间复杂度：
    - 常见实现会额外分配整图 `byte[]` 或二维数组（`byte[,]` / `Color[,]`）。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | PerPixelProcess（new byte[total]）
        - 证据：src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs | ToGrayArray2D/ToPixelArray2D（new byte[width,height] / new Color[width,height]）
- 大数据量表现：
    - `PerPixelProcess` 与部分算法使用 `Parallel.For`，在大图上可能提高吞吐，但也会带来较高内存拷贝与并行调度开销。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | Parallel.For(0, rows, ...)
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | Parallel.For(...)
- Benchmark 链接：待补充（当前仓库未见 Drawing 模块基准用例，待确认）。

## 8. 测试策略
- 单测覆盖点：待确认（当前仓库未见 Drawing 模块对应测试工程）。
- 边界用例（建议补充）：
    - `ImageHelper.FromBytes/FromBase64String` 的“返回 Image 后释放 Stream”的生命周期验证。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Load.cs | FromBytes/FromBase64String
    - `PerPixelProcess` 的像素格式边界与异常信息。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | PerPixelProcess
    - `ColorMatrices.Create*Filter` 的参数范围验证。
        - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ColorMatrices.cs | CreateGrayScaleFilter/CreateBrightnessFilter/...
- 回归用例（建议补充）：验证码生成的随机性边界（线/点/倾斜/随机颜色）与输出尺寸稳定性。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | RandomLineCount/RandomPointPercent/RandomItalic/RandomColor

## 9. 版本与兼容性
- 当前版本：1.5.0
    - 证据：version.props | VersionPrefix=1.5.0
- 兼容性说明：
    - 依赖 `System.Drawing.Common`，在 Ubuntu/Docker 环境可能需要安装 `libgdiplus` 等系统包（具体以运行环境为准）。
        - 证据：src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj | System.Drawing.Common 6.0.0
        - 证据：src/Bing.Utils.Drawing/README.md | apt install libc6-dev/libgdiplus
- 破坏性变更：待确认（当前文档基于仓库现状未梳理历史版本差异）。
- 升级建议：
    - 如在非 Windows 环境使用，建议在部署阶段验证 `System.Drawing` 运行时依赖与关键算法输出一致性。
        - 证据：src/Bing.Utils.Drawing/README.md | Ubuntu/Docker 部署说明

## 10. 使用示例
```csharp
using System.Drawing;
using System.Drawing.Imaging;
using Bing.Drawing;
using Bing.Extensions;

// 1) 生成验证码
var builder = new CaptchaBuilder
{
    RandomLineCount = 2,
    RandomPointPercent = 2,
    RandomItalic = true,
    RandomColor = true,
    HasBorder = true
};
var captchaImage = builder.CreateImage(length: 4, out var code, captchaType: CaptchaType.NumberAndLetter);
var captchaDataUrl = captchaImage.ToBase64StringWithPrefix(ImageFormat.Png);

// 2) 缩略图
using var source = ImageHelper.FromFile("input.jpg");
using var thumb = ImageHelper.MakeThumbnail(source, 200, 200, ThumbnailMode.Cut);
var thumbBytes = thumb.ToBytes(ImageFormat.Jpeg);
```

## 11. 待办与改进
- [ ] 补充 `Bing.Utils.Drawing` 的单元测试工程与用例（当前仓库未见对应 tests 项目，待确认）。
- [ ] 评估并修正 `ImageHelper.MakeThumbnail` 中的 `throw e;`（避免丢失异常堆栈）。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.cs | MakeThumbnail(...)
- [ ] 为 `ImageHelper.FromBytes/FromBase64String` 增加生命周期验证与更稳妥的实现（如克隆图像后释放流），并补充回归测试。
    - 证据：src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Load.cs | FromBytes/FromBase64String
- [ ] 增加 Drawing 模块 benchmark（逐像素/卷积/柔边等算法在不同尺寸下的耗时与分配）。

## 12. 证据定位（汇总）
- src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj | 包信息、AllowUnsafeBlocks、System.Drawing.Common 依赖
- src/Bing.Utils.Drawing/README.md | Ubuntu/Docker 运行时依赖（libgdiplus 等）
- src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs | CaptchaBuilder（验证码生成）
- src/Bing.Utils.Drawing/Bing/Drawing/CaptchaType.cs | CaptchaType
- src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.cs | ImageHelper（缩略图/缩放/图像效果等）
- src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Load.cs | ImageHelper 加载（FromBytes/FromDataUrl）
- src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Convert.cs | ImageHelper 转换（ToBytes/Base64/DataUrl/Ico）
- src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Info.cs | ImageHelper 信息（GetImageExtension/GetCodecInfo）
- src/Bing.Utils.Drawing/Bing/Drawing/ColorMatrices.cs | ColorMatrices（ApplyMatrix + Create*Filter）
- src/Bing.Utils.Drawing/Bing/Drawing/ColorEffect.cs | ColorEffect（黑白/双色调/冲蚀等）
- src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs | ImageEffect（柔边）
- src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs | Bing.Drawing.BitmapExtensions（PerPixelProcess 等）
- src/Bing.Utils.Drawing/Bing/Drawing/Extensions/ColorExtensions.cs | ColorExtensions
- src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs | Bing.Extensions.BitmapExtensions（大量算法、unsafe/LockBits）
- src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs | ImageExtensions
- src/Bing.Utils.Drawing/Bing/Conversions/ColorConv.cs | ColorConv（颜色转换）
