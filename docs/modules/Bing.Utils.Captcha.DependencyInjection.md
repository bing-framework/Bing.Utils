# Bing.Utils.Captcha.DependencyInjection

实际实现：[CaptchaServiceCollectionExtensions](../../src/Bing.Utils.Captcha.DependencyInjection/CaptchaServiceCollectionExtensions.cs)。依赖 Captcha 和现有 DI Abstractions 8.0.2，不依赖 Redis 或 SkiaSharp。

`AddBingCaptcha(options)` 注册单例服务并复制配置；`AddMemoryCaptchaStore(options)` 选择有界内存；`AddCaptchaStore(factory)` 和 `AddCaptchaRenderer(factory)` 用于适配器。每种角色必须恰好注册一个，密钥提供器由宿主显式配置，不自动生成。

启动时解析 `ICaptchaService` 完成配置、组件和字体验证。DI 工厂创建的资源由容器释放；通过外部实例注册的资源由宿主管理。核心服务不释放注入对象。请勿在 singleton 工厂捕获 scoped 服务。

可运行示例见 [CaptchaWeb](../../samples/CaptchaWeb/README.md)，最小注册代码及密钥轮换见 [核心模块](Bing.Utils.Captcha.md)。DI 单元断言位于 Bing.Utils.Captcha.DependencyInjection.Tests；真实 Web 宿主协作位于 Bing.Utils.Captcha.Tests.Integration。
