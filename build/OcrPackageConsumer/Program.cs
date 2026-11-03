using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bing.Ocr;
using Bing.Ocr.PaddleServing;
using Bing.Ocr.SkiaSharp;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

/// <summary>从 NuGet 包调用四个模块；netstandard2.0 编译同一入口以检查兼容资产。</summary>
public static class Program
{
    public static async Task Main()
    {
        using var client = new HttpClient(new LocalProtocol());
        var services = new ServiceCollection();
        services.AddBingOcr();
        services.AddOcrProvider(_ => new PaddleServingOcrProvider(client, new PaddleServingOptions()));
        services.AddOcrImagePreprocessor(_ => new SkiaOcrImagePreprocessor());
        using var container = services.BuildServiceProvider();
        using var bitmap = new SKBitmap(16, 12);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.White);
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var input = new MemoryStream(encoded.ToArray());
        var result = await container.GetRequiredService<IOcrService>().RecognizeAsync(input);
        if (result.Status != OcrStatus.NoTextDetected || result.ImageWidth != 16 || !input.CanRead)
            throw new InvalidOperationException("包消费识别链路不符合契约。");
        Console.WriteLine("OCR 四包消费通过：" + typeof(OcrService).Assembly.GetName().Version);
    }

    private sealed class LocalProtocol : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (request.RequestUri!.AbsolutePath != "/ocr") throw new InvalidOperationException("协议路径错误。");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"errorCode\":0,\"result\":{\"ocrResults\":[{\"prunedResult\":{\"input_img_shape\":[12,16],\"rec_texts\":[],\"rec_scores\":[],\"rec_polys\":[]}}]}}")
            });
        }
    }
}
