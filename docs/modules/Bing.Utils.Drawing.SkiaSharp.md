# Bing.Utils.Drawing.SkiaSharp
## 1. 包职责（Scope）
- 解决的问题
- 提供基于 `SkiaSharp` 的图像处理辅助能力，包括：加载/保存/转换、缩放/裁剪/旋转/翻转、灰度/黑白/滤色/底片、颜色矩阵、亮度/对比度/饱和度、文字水印、验证码渲染（全量 `CaptchaOptions` 支持）、OCR 预处理、GPS 元数据清理、颜色空间转换、ICO、扭曲/冲蚀。
- 不解决的问题（Out of Scope）
- 不提供 System.Drawing 兼容层（由 `Bing.Utils.Drawing` 提供）。

## 2. 核心类型与扩展方法

### 2.1 加载与转换
- `SkiaSharpHelper.FromFile / FromStream / FromBytes / FromBase64String / FromDataUrl`
- `SkiaSharpHelper.ToBytes / ToBase64String / ToDataUrl / ToStream`

### 2.2 几何变换
- `Resize` / `Crop` / `Rotate` / `FlipHorizontal` / `FlipVertical`
- `MakeThumbnail(SKImage, w, h, ThumbnailMode)`
- `MakeThumbnail(byte[], w, h, ThumbnailMode)`
- `MakeThumbnail(sourcePath, destPath, w, h, ThumbnailMode)`
- `ScaleImage`

### 2.3 颜色与滤镜
- `Gray` / `ToBlackWhiteImage` / `FilterColor` / `Plate`
- `SetBrightness` / `SetContrast` / `SetSaturation`
- `ApplyColorMatrix(float[,])` / `PerPixelProcess`

### 2.4 水印
- `AddImageWatermark`：图片水印
- `AddTextWatermark`：文字水印（使用 SKTypeface）

### 2.5 颜色转换适配
- `SkiaSharpHelper.ToRgbColor(SKColor)` / `ToSKColor(RgbColor)`
- `SkiaSharpHelper.ToHsl(SKColor)` / `FromHsl(HslColor)`
- `SkiaSharpHelper.ToHex(SKColor)` / `FromHex(string)`

### 2.6 验证码（全量 CaptchaOptions 支持）
- `GetCaptchaCode(int length)` / `GetCaptchaCode(int length, CaptchaType)`
- `CreateCaptchaImage(string code, CaptchaOptions? options)`：全量配置
- `CreateCaptchaImage(int length, out string code, CaptchaType, CaptchaOptions?)`
- 支持：Width/Height/FontSize/FontWidth/HasBorder/NoiseLineCount/NoisePointCount/RandomPosition/RandomColor/RandomItalic/RandomRotation/MaxRotationDegrees/RandomSeed/BackgroundA/BackgroundR/G/B

### 2.7 OCR 预处理
- `ToGrayArray2D` / `ToBinaryArray2D` / `CreateImageFromGrayArray` / `CreateImageFromBinaryArray`
- `Binaryzation` / `DeepenForeground` / `ClearGrayRange`
- `ClearNoiseByNeighborCount` / `ClearNoiseByArea`
- `TrimToContent` / `GetVerticalProjection` / `GetHorizontalProjection` / `SplitByVerticalProjection`

### 2.8 高级效果
- `ToIcoStream` / `TwistImage` / `SetErosionEffect`

### 2.9 元数据清理
- `DeleteCoordinate(byte[] source, ImageMetadataOptions?)`
- `DeleteCoordinate(Stream input, ImageMetadataOptions?)`
- `DeleteCoordinate(Stream input, Stream output, ImageMetadataOptions?, bool leaveOpen)`
- `DeleteCoordinate(string filePath, ImageMetadataOptions?)`
- `DeleteCoordinate(string sourcePath, string destPath, ImageMetadataOptions?)`
- 支持 JPEG APP1 和 PNG eXIf 容器级 GPS IFD 清除

## 3. 使用示例（最小可运行）

```csharp
// 缩略图
using var image = SkiaSharpHelper.FromFile("photo.jpg");
using var thumb = SkiaSharpHelper.MakeThumbnail(image!, 200, 200, ThumbnailMode.FixedBoth);

// 验证码（全量配置）
var options = new CaptchaOptions
{
    FontSize = 30,
    Width = 200,
    Height = 60,
    RandomSeed = 42,
    NoiseLineCount = 5,
    BackgroundR = 255, BackgroundG = 255, BackgroundB = 255
};
using var captcha = SkiaSharpHelper.CreateCaptchaImage("AB12", options);

// 颜色转换
var hex = SkiaSharpHelper.ToHex(new SKColor(255, 0, 0)); // "#FF0000"
var hsl = SkiaSharpHelper.ToHsl(new SKColor(255, 0, 0));

// GPS 元数据清理
var cleaned = SkiaSharpHelper.DeleteCoordinate(jpegBytes);
```

## 4. 性能与线程安全说明
- 大部分方法返回新图像（通过 SKBitmap 复制），分配成本与图像尺寸相关。
- 方法本身无全局可变状态；并发安全取决于调用方是否共享同一 SKImage 实例。
- 格式跟踪使用 `ConditionalWeakTable`，线程安全。

## 5. 异常与日志策略
- `ArgumentNullException`：空图像/流/路径/验证码文本
- `ArgumentOutOfRangeException`：非法尺寸/角度/阈值/质量/不透明度
- `NotSupportedException`：DeleteCoordinate 遇不支持格式
- 当前 API 对非法输入不返回默认值，直接抛异常。

## 6. 与其他子包的关系
- 依赖 `SkiaSharp 2.88.9`
- 共享 `Bing.Utils.Drawing.Shared` 中的纯算法和公共类型（通过 csproj Compile Include 链接）
- 不依赖 `System.Drawing`、`SixLabors.ImageSharp`

## 7. 测试映射
- 测试项目：`tests/Bing.Utils.Drawing.SkiaSharp.Tests`
- 当前测试基线：140 条，覆盖验证码、缩略图、加载/转换、几何变换、OCR 预处理、高级效果、元数据清理、颜色转换适配

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略（net8.0/net7.0/net6.0/netstandard2.0）
- LangVersion 固定为 10.0
- `SkiaSharp 2.88.9`，升级需回归格式支持差异

