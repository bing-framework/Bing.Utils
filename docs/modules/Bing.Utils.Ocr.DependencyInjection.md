# Bing.Utils.Ocr.DependencyInjection

扩展方法位于 `Microsoft.Extensions.DependencyInjection` 命名空间。包只依赖核心与 DI 抽象，无默认具体引擎。

```csharp
services.AddBingOcr(new OcrServiceOptions { MaxConcurrency = 1, MaxQueuedRequests = 8 });
services.AddSingleton(_ => new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
{
    Timeout = Timeout.InfiniteTimeSpan
});
services.AddOcrProvider(sp => new PaddleServingOcrProvider(
    sp.GetRequiredService<HttpClient>(), new PaddleServingOptions
    {
        Endpoint = new Uri("http://127.0.0.1:8080/"),
        ExpectedModelProfileId = verifiedProfile
    }));
services.AddOcrImagePreprocessor(_ => new SkiaOcrImagePreprocessor());
```

`AddBingOcr` 在注册时固定配置快照，首次解析时要求恰好一个 Provider 和预处理器。重复注册立即报错，缺失组件在解析时明确报错。三个实例均 singleton；工厂不得捕获 scoped 依赖。启动时解析 `IOcrService` 可提前发现配置问题。

工厂创建的可释放组件由容器管理。通过标准 `AddSingleton<IOcrProvider>(existingInstance)` 等方式注册的外部实例由调用方管理。若需要不同模型或容量配置，显式创建不同服务容器/服务实例，首版不提供隐式路由。

仓库 Flubu 打包规则已为 OCR DI 包添加例外，保留其他 DI 项目的既有规则。独立 NuGet 消费验证覆盖本包和另外三个 OCR 包。
