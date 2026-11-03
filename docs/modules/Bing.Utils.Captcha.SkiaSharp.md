# Bing.Utils.Captcha.SkiaSharp

实际实现：[SkiaSharpCaptchaRenderer](../../src/Bing.Utils.Captcha.SkiaSharp/SkiaSharpCaptchaRenderer.cs)。依赖核心包及 Drawing.SkiaSharp，四个目标框架与核心一致。

默认 PNG 160×60。构造时复制并验证配置，拒绝超过 512×256 的尺寸、过量噪声和未实现的绘图设置；编码后检查字节上限，不反复编码。字体只加载一次，每个请求独立画布和画笔。Dispose 等待在途绘图结束后释放自有字体；旧 SkiaSharpHelper 新增字体重载借用调用方字体，不释放它。

```csharp
using Bing.Captcha.SkiaSharp;
using Bing.Drawing;

using var renderer = new SkiaSharpCaptchaRenderer(new SkiaSharpCaptchaRendererOptions
{
    Type = CaptchaType.ChineseChar,
    FontPath = configuredFontPath
});
var png = renderer.Render("中文验证");
```

中文必须配置字体，并在构造时覆盖完整白名单。建议核心服务与渲染器选择相同 Type 和 IgnoreAsciiCase。渲染器实现 `ICaptchaRendererValidator`，服务构造时验证渲染字符集覆盖所有实际生成字符，不兼容配置在解析服务时抛 ArgumentException；兼容超集允许使用。字体覆盖按实际字形检查，不能用大小写折叠替代缺失字形。

服务不在每次请求中搜索系统字体，字符集也仅初始化一次。实际 PNG/尺寸/中文/所有权及服务配置兼容测试位于专属 Integration 项目，环境字体变量为 `BING_CAPTCHA_TEST_FONT`。完整接入和错误映射见 [生命周期](Bing.Utils.Captcha.md)。
