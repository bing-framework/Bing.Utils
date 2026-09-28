# Bing.Utils.Extra.Drawing.SkiaSharp

## 包职责

该包为 `Bing.Utils.Extra` 的 Emoji 目录提供可选 SkiaSharp 渲染适配，不改变核心包的无图像依赖设计。

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

`Render` 接受 Unicode 清单中的规范或兼容序列，并使用目录中的规范形式测量和绘制。输入不是完整 Emoji 时抛出 `ArgumentException`；指定字体文件不存在时抛出 `FileNotFoundException`；显式字体缺少完整序列字形时抛出 `InvalidDataException`。返回的 `SKImage` 由调用方负责释放。

未指定字体时使用 SkiaSharp 默认字体。彩色 Emoji、ZWJ、肤色、旗帜和键帽序列的最终显示由字体和 SkiaSharp 后端决定；本包不携带字体文件，不承诺跨平台字形一致性。

包依赖 `Bing.Utils.Extra` 和 `SkiaSharp 2.88.9`，支持 netstandard2.0、.NET 6、.NET 7 和 .NET 8。
