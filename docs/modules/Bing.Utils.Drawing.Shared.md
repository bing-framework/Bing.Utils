# Bing.Utils.Drawing.Shared

## 功能说明

`Bing.Utils.Drawing.Shared` 是 `System.Drawing`、ImageSharp 和 SkiaSharp 图像后端共用的契约与纯算法层。它定义统一处理参数、颜色值、验证码参数、二值矩阵操作及内部图像管线，不直接负责解码或渲染具体图像格式。

## 核心实现与关键文件

- 统一处理契约：`src/Bing.Utils.Drawing.Shared/Bing/Drawing/ImageProcessingOptions.cs`，定义裁剪、缩放、旋转、翻转、水印、标注、压缩、资源限制和处理结果。
- 颜色模型与转换：`RgbColor.cs`、`HslColor.cs`、`ColorConversion.cs`、`Bing/Conversions/ColorConv.cs`。
- 验证码公共参数：`CaptchaOptions.cs`、`CaptchaType.cs`，生成算法由具体后端实现。
- 二值矩阵兼容入口：`BinaryMatrixHelper.cs`；连通域、投影、降噪、腐蚀等实现位于 `Bing/Drawing/Internal`。
- 元数据清理与编码检查：`ImageMetadataOptions.cs`、`Internal/EncodedImageInspector.cs`、`Internal/EncodedImageSanitizer.cs`。
- 统一处理顺序由 `ImageProcessOptions` 约定：方向纠正、裁剪、缩放、旋转、翻转、边框、水印、标注、编码。

## 对外接口与用法

```csharp
using Bing.Drawing;

var color = ColorConversion.ParseHex("#336699");
var hsl = ColorConversion.RgbToHsl(color);

var options = new ImageProcessOptions {
    Resize = new ImageResizeOptions {
        Width = 800,
        Height = 600,
        Mode = ImageResizeMode.Contain
    },
    Format = ImageOutputFormat.Jpeg,
    Quality = 85,
    RemoveMetadata = true
};
```

`ImageProcessOptions` 只描述处理请求；实际执行应调用 ImageSharp 或 SkiaSharp 模块公开的处理入口。

## 依赖与配置

- 目标框架继承 `common.props`：`net8.0;net7.0;net6.0;netstandard2.0`。
- 项目不依赖具体图像引擎；ImageSharp、SkiaSharp 和 `System.Drawing` 项目通过 `ProjectReference` 引用它。
- 默认资源限制包括 20 MiB 输入、4000 万像素、单边 16384 像素、1 帧和最多 16 张合成图；调用方可通过 `ImageProcessingLimits` 调整。

## 已知限制与注意事项

- `Internal` 下类型不是公共兼容契约，不应由业务代码直接依赖。
- 共享层不验证字体文件是否部署，也不提供具体后端的格式编解码能力。
- `ColorConv.RgbToHsb/HsbToRgb` 为历史兼容名称，实际采用 HSL 语义。
- 默认 `MaxFrames = 1`，多帧/动态图像默认不在处理范围内。
