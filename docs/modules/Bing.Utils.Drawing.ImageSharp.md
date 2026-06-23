# Bing.Utils.Drawing.ImageSharp
## 1. 包职责（Scope）
- 解决的问题
- 提供基于 `SixLabors.ImageSharp` 的图像处理辅助能力，包括：加载/保存/转换、缩放/裁剪/旋转/翻转、灰度/黑白/滤色/底片、颜色矩阵、亮度/对比度/饱和度、水印、ICO、扭曲/冲蚀等高级效果、OCR 预处理、验证码渲染、GPS 元数据清理、颜色空间转换。
- 不解决的问题（Out of Scope）
- 不提供 System.Drawing 兼容层（由 `Bing.Utils.Drawing` 提供）。
- 不提供 TTF/OTF 字体光栅化（当前依赖集无字体渲染包）。中文验证码使用内嵌点阵。

## 2. 核心类型与扩展方法

### 2.1 加载与转换
- `ImageSharpHelper.FromFile / FromStream / FromBytes / FromBase64String / FromDataUrl`：从各种来源加载图像。
- `ImageSharpHelper.ToBytes / ToBase64String / ToDataUrl`：编码输出。
- `ImageSharpHelper.ToStream`：转换为内存流（调用方负责释放）。
- 格式跟踪：内部通过 `ConditionalWeakTable` 跟踪来源格式，默认输出 PNG。

### 2.2 几何变换
- `Resize` / `Crop` / `Rotate` / `FlipHorizontal` / `FlipVertical`
- `MakeThumbnail(Image, w, h, ThumbnailMode)`：固定宽/高/双向/裁剪模式
- `MakeThumbnail(byte[], w, h, ThumbnailMode)`：字节数组便利重载
- `MakeThumbnail(sourcePath, destPath, w, h, ThumbnailMode)`：文件路径便利重载
- `ScaleImage`：等比缩放居中放置

### 2.3 颜色与滤镜
- `Gray` / `ToBlackWhiteImage` / `FilterColor` / `Plate`（底片）
- `SetBrightness` / `SetContrast` / `SetSaturation`
- `ApplyColorMatrix(float[,])`：通用颜色矩阵
- `PerPixelProcess`：逐像素处理

### 2.4 颜色转换适配
- `ImageSharpHelper.ToRgbColor(Rgba32)` / `ToRgba32(RgbColor)`：与 Shared `RgbColor` 互转
- `ImageSharpHelper.ToHsl(Rgba32)` / `FromHsl(HslColor)`：HSL 互转
- `ImageSharpHelper.ToHex(Rgba32)` / `FromHex(string)`：Hex 互转

### 2.5 验证码
- `GetCaptchaCode(int length, CaptchaType captchaType)`：按类型生成验证码文本
- `CreateCaptchaImage(string code, CaptchaOptions? options)`：绘制验证码图片
- `CreateCaptchaImage(int length, out string code, CaptchaType, CaptchaOptions?)`：组合重载
- 支持 ASCII 点阵和中文 16x16 点阵（不依赖系统字体）
- 中文模式遇未知字符抛 `NotSupportedException`

### 2.6 OCR 预处理
- `ToGrayArray2D` / `ToBinaryArray2D`：图像转 `byte[,]` 矩阵
- `CreateImageFromGrayArray` / `CreateImageFromBinaryArray`：矩阵转图像
- `Binaryzation` / `DeepenForeground` / `ClearGrayRange`
- `ClearNoiseByNeighborCount` / `ClearNoiseByArea`
- `TrimToContent` / `GetVerticalProjection` / `GetHorizontalProjection` / `SplitByVerticalProjection`

### 2.7 高级效果
- `ToIcoStream`：多尺寸 ICO 输出
- `TwistImage`：正弦扭曲
- `SetErosionEffect`：冲蚀效果

### 2.8 元数据清理
- `DeleteCoordinate(byte[] source, ImageMetadataOptions?)`：删除 GPS 元数据
- `DeleteCoordinate(Stream input, ImageMetadataOptions?)`：流输入
- `DeleteCoordinate(Stream input, Stream output, ImageMetadataOptions?, bool leaveOpen)`：流到流
- `DeleteCoordinate(string filePath, ImageMetadataOptions?)`：覆盖文件
- `DeleteCoordinate(string sourcePath, string destPath, ImageMetadataOptions?)`：另存为
- 支持 JPEG APP1 和 PNG eXIf 容器级 GPS IFD 清除

## 3. 使用示例（最小可运行）

```csharp
// 缩略图
using var image = ImageSharpHelper.FromFile("photo.jpg");
using var thumb = ImageSharpHelper.MakeThumbnail(image, 200, 200, ThumbnailMode.FixedBoth);

// 验证码
var code = ImageSharpHelper.GetCaptchaCode(6, CaptchaType.Number);
using var captcha = ImageSharpHelper.CreateCaptchaImage(code, new CaptchaOptions { FontSize = 30 });

// 颜色转换
var hex = ImageSharpHelper.ToHex(new Rgba32(255, 0, 0)); // "#FF0000"
var hsl = ImageSharpHelper.ToHsl(new Rgba32(255, 0, 0));

// GPS 元数据清理
var cleaned = ImageSharpHelper.DeleteCoordinate(jpegBytes);

// OCR 预处理
var gray = ImageSharpHelper.ToGrayArray2D(image);
var binary = ImageSharpHelper.ToBinaryArray2D(image, 128);
```

## 4. 性能与线程安全说明
- 大部分方法返回新图像（Clone），分配成本与图像尺寸相关。
- 方法本身无全局可变状态；并发安全取决于调用方是否共享同一图像实例。
- `ConditionalWeakTable` 用于格式跟踪，线程安全。

## 5. 异常与日志策略
- `ArgumentNullException`：空图像/流/路径
- `ArgumentOutOfRangeException`：非法尺寸/角度/阈值/质量
- `NotSupportedException`：DeleteCoordinate 遇不支持格式；中文验证码遇未知字符
- 当前 API 对非法输入不返回默认值，直接抛异常。

## 6. 与其他子包的关系
- 依赖 `SixLabors.ImageSharp 2.1.11`
- 共享 `Bing.Utils.Drawing.Shared` 中的纯算法和公共类型（通过 csproj Compile Include 链接）
- 不依赖 `System.Drawing`、`SkiaSharp`
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

