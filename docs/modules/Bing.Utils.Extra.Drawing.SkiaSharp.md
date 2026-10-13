# Bing.Utils.Extra.Drawing.SkiaSharp

## 功能说明

该包为 `Bing.Utils.Extra` 的 Emoji 目录提供可选 SkiaSharp 渲染适配，不改变核心包的无图像依赖设计。

## 核心实现与关键文件

- `src/Bing.Utils.Extra.Drawing.SkiaSharp/Bing/Extra/Emoji/Drawing/EmojiRenderer.cs`：校验 Emoji、选择字体、测量字形并渲染 `SKImage`。
- `src/Bing.Utils.Extra.Drawing.SkiaSharp/Bing/Extra/Emoji/Drawing/EmojiRenderOptions.cs`：字体路径或字体族、字号、边距、背景色和前景色配置。
- 输入识别复用 `Bing.Utils.Extra` 的固定 Emoji 目录，不自行注册或猜测新序列。

## 对外接口与用法示例

入口位于 `Bing.Extra.Emoji.Drawing`：

- `EmojiRenderer.Render`：将一个完整、受支持的 Emoji 序列生成 `SKImage`。
- `EmojiRenderOptions`：配置字体文件或字体名称、字号、内边距、背景色和非彩色字形颜色。

```csharp
using Bing.Extra.Emoji.Drawing;
using SkiaSharp;

using var image = EmojiRenderer.Render("👍🏽", new EmojiRenderOptions
{
    FontPath = "NotoColorEmoji.ttf",
    FontSize = 96,
    Padding = 8
});

using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
var bytes = encoded.ToArray();
```

## 依赖与配置

- 依赖 `Bing.Utils.Extra` 和 `SkiaSharp 2.88.9`。
- 目标框架继承 `common.props`：`net8.0;net7.0;net6.0;netstandard2.0`。
- 本包不携带字体；需要稳定字形时应通过 `FontPath` 部署并指定调用方字体。
- 渲染测试位于 `tests/Bing.Utils.Extra.Drawing.SkiaSharp.Tests/Bing/Extra/Emoji/Drawing/EmojiRendererTests.cs`。

## 已知限制与注意事项

`Render` 接受 Unicode 清单中的规范或兼容序列，并使用目录中的规范形式测量和绘制。输入不是完整 Emoji 时抛出 `ArgumentException`；指定字体文件不存在时抛出 `FileNotFoundException`；显式字体缺少完整序列字形时抛出 `InvalidDataException`。返回的 `SKImage` 由调用方负责释放。

未指定字体时使用 SkiaSharp 默认字体。彩色 Emoji、ZWJ、肤色、旗帜和键帽序列的最终显示由字体和 SkiaSharp 后端决定；本包不携带字体文件，不承诺跨平台字形一致性。
