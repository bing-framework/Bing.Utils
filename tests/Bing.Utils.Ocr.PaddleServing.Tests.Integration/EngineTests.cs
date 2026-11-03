using Bing.Ocr;
using Bing.Ocr.PaddleServing;
using Bing.Ocr.SkiaSharp;
using SkiaSharp;
using Xunit;

namespace Bing.Utils.Ocr.PaddleServing.Tests.Integration;

/// <summary>仅在配置真实端点时运行；配置存在但不可达时必须失败。</summary>
public sealed class OcrEngineFactAttribute : FactAttribute
{
    public OcrEngineFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OCR_ENDPOINT")))
            Skip="BLOCKED_EXTERNAL: 未配置 OCR_ENDPOINT，真实引擎尚未验证。";
    }
}
public class EngineTests
{
    [OcrEngineFact, Trait("Category","OcrEngine")]
    public async Task RealEngine_RecognizesEnglishChineseBlankAndRegion()
    {
        var endpoint=Environment.GetEnvironmentVariable("OCR_ENDPOINT")!;
        var expectedProfile=Environment.GetEnvironmentVariable("OCR_MODEL_PROFILE");
        using var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(60)};
        var provider=new PaddleServingOcrProvider(client,new PaddleServingOptions{Endpoint=new Uri(endpoint),ExpectedModelProfileId=expectedProfile});
        var service=new OcrService(provider,new SkiaOcrImagePreprocessor(),new OcrServiceOptions{Timeout=TimeSpan.FromSeconds(60)});
        using var typeface=LoadFont();
        var bytes=Image(typeface,"BING OCR 12345","中文识别测试");
        using var input=new MemoryStream(bytes);
        var result=await service.RecognizeAsync(input);
        var compact=result.Text.Replace(" ","").Replace("\n","");
        Assert.Contains("12345",compact);
        Assert.Contains("中文",compact);
        if(!string.IsNullOrWhiteSpace(expectedProfile)) Assert.True(result.ModelIdentityVerified);
        using var regionInput=new MemoryStream(bytes);
        var region=await service.RecognizeAsync(regionInput,new OcrOptions{Region=new OcrRectangle(0,0,700,90)});
        Assert.Contains("12345",region.Text.Replace(" ",""));
        Assert.DoesNotContain("中文",region.Text);
        Assert.All(region.Lines.SelectMany(x=>x.Polygon),p=>Assert.InRange(p.Y,0,90));
        using var blank=new MemoryStream(Image(typeface,"",""));
        Assert.Equal(OcrStatus.NoTextDetected,(await service.RecognizeAsync(blank)).Status);
    }

    private static SKTypeface LoadFont()
    {
        var path=Environment.GetEnvironmentVariable("OCR_FONT_PATH");
        Assert.False(string.IsNullOrWhiteSpace(path),"真实中文冒烟需要 OCR_FONT_PATH 指向中文字体。");
        var font=SKTypeface.FromFile(path!) ?? throw new InvalidOperationException("无法加载 OCR_FONT_PATH 字体。");
        using var check=new SKPaint{Typeface=font};
        Assert.True(check.ContainsGlyphs("中文识别测试"),"字体必须覆盖中文样本文字。");
        return font;
    }
    private static byte[] Image(SKTypeface font,string first,string second)
    {
        using var bitmap=new SKBitmap(700,180); using var canvas=new SKCanvas(bitmap); canvas.Clear(SKColors.White);
        using var paint=new SKPaint{Typeface=font,TextSize=40,Color=SKColors.Black,IsAntialias=true};
        canvas.DrawText(first,25,60,paint); canvas.DrawText(second,25,145,paint);
        using var image=SKImage.FromBitmap(bitmap); using var png=image.Encode(SKEncodedImageFormat.Png,100); return png.ToArray();
    }
}
