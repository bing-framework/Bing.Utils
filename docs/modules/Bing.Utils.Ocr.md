# Bing.Utils.Ocr

为中英文单张 PNG、JPEG、BMP 提供统一识别入口。核心包不依赖 SkiaSharp 或 Paddle；通过图像预处理器和 Provider 组合。四包均提供 net8.0、net7.0、net6.0、netstandard2.0 资产。本次只生成本地包，没有发布 NuGet。

## 接入

```csharp
using Bing.Ocr;
using Bing.Ocr.PaddleServing;
using Bing.Ocr.SkiaSharp;

using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
{
    Timeout = Timeout.InfiniteTimeSpan
};
var provider = new PaddleServingOcrProvider(client, new PaddleServingOptions
{
    Endpoint = new Uri("http://127.0.0.1:8080/"),
    // 使用部署准备阶段记录的 profile；不设置时兼容普通官方服务，身份未验证。
    ExpectedModelProfileId = "替换为 model-manifest 对应的部署身份"
});
var service = new OcrService(provider, new SkiaOcrImagePreprocessor());
using var image = File.OpenRead("sample.png");
OcrResult result = await service.RecognizeAsync(image, new OcrOptions
{
    LanguageHint = "zh-Hans+en",
    Preprocessing = OcrPreprocessingPreset.Standard,
    LowConfidenceThreshold = 0.6
});
Console.WriteLine(result.Text);
```

同一 `OcrService` 应复用。另有 `RecognizeFileAsync(path, options, cancellationToken)`，它负责关闭自行打开的文件。流入口从当前位置读取，支持不可 Seek 流，始终保持调用方流打开。

## 结果与坐标

`OcrResult` 是只读结果：`Text`、`Lines`、`ImageWidth/Height`、`Provider`、`ModelProfileId`、`ModelIdentityVerified`、`Warnings`、`Elapsed`。集合和图像字节均防御性复制。

每行保留原文、可空识别分数、顺时针凸四点框和 `IsLowConfidence`。低分行仍出现在全文，分数不是实际正确率。全文按 Provider 顺序以 LF 拼接，保留行内空格、标点和大小写。没有文字时返回 `NoTextDetected`，引擎故障抛异常。

坐标始终对应 EXIF 校正后的完整图片，范围为 `[0,width] × [0,height]`。ROI 用相同坐标系，必须完整位于图内。ROI、显式旋转和缩放之后的框会映回完整图；调用方不需要二次修正。默认不缩放，旋转只接受 0、90、180、270。

## 限制与取消

| 配置 | 默认 |
| --- | --- |
| 输入 / 处理图 | 10 MiB / 20 MiB |
| 像素 / 单边 | 20,000,000 / 8192 |
| 活动 / 等待请求 | 1 / 8 |
| 总期限 | 30 秒，含排队和读取 |
| Provider HTTP 响应 | 4 MiB |

有界准入在读取图片前执行，满载立即抛 `OcrBusyException`。等待支持取消；调用方取消返回 `OperationCanceledException`，总期限返回 `OcrTimeoutException`。原生图像操作在步骤间检查取消，单个原生调用无法硬中断；自定义扩展必须协作响应令牌。远端开始的推理可能继续运行，私有宿主会保留其容量直到完成或工作进程被回收。

`OcrInputException.Code` 表达输入错误；`OcrCapabilityException` 表达不支持的语言或格式；`OcrProviderException` 表达协议或服务故障。PDF、多帧图片、多页文档、表格结构和业务字段提取不属于首版。

## 扩展与资源所有权

`IOcrImagePreprocessor.Prepare` 返回编码图、处理前后尺寸及 `ToOriginal` 逆变换；`IOcrProvider` 先校验能力，再返回处理图坐标的文字行。扩展被共享调用，应线程安全且遵守输入/输出上限。

服务借用 Provider 和预处理器；Paddle Provider 借用 HttpClient，均不释放借用组件。每次请求自行释放缓冲流、HTTP 消息和 Skia 对象。DI 工厂创建的可释放实例由容器释放；外部实例由调用方释放。

## 验证与相关入口

- [SkiaSharp 图像适配](Bing.Utils.Ocr.SkiaSharp.md)
- [PaddleServing 协议](Bing.Utils.Ocr.PaddleServing.md)
- [DI 注册](Bing.Utils.Ocr.DependencyInjection.md)
- [部署与离线准备](../../deploy/ocr/README.md)
- [可运行 Web 示例](../../samples/OcrWeb/README.md)
- [CER/P95 测量](../../benchmarks/OcrProbe/README.md)
- [实施证据](../../ai_docs/tasks/2026-10-29-ocr/execution.md)
- [安全修复与部署验证](../../ai_docs/tasks/2026-10-29-ocr/fix-report.md)

`build/Test-Ocr.ps1` 的 Unit、Integration、PackConsumer、Engine、Host、Resource、Final 阶段各自生成 UTF-8 日志和结果 JSON。Engine 必须使用真实环境，`-RequireEngine` 会在缺少环境变量时失败；普通单测不下载模型。
