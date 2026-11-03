using Bing.Captcha;
using Bing.Captcha.SkiaSharp;
using System.Diagnostics;

if (args.Contains("--capacity"))
{
    await CapacityProbe.RunAsync();
    return;
}

// 有限样本，只报告本次测量，不作为发布阈值或历史对比。
using var renderer = new SkiaSharpCaptchaRenderer();
using var store = new MemoryCaptchaStore();
var keys = new StaticCaptchaKeyProvider("probe", new[] { new CaptchaKey("probe", Bing.Security.Randomness.SecurityRandom.GetBytes(32)) });
var service = new CaptchaService(store, renderer, keys);
foreach(var concurrency in new[]{1,8})
{
    var allocated=GC.GetTotalAllocatedBytes(true);var watch=Stopwatch.StartNew();long totalBytes=0,createTicks=0,verifyTicks=0;
    await Parallel.ForEachAsync(Enumerable.Range(0,100),new ParallelOptions {MaxDegreeOfParallelism=concurrency},async(i,ct)=>
    {
        var context=new CaptchaContext("login","probe",i.ToString());
        var start=Stopwatch.GetTimestamp();
        var challenge=await service.CreateAsync(context,ct);
        Interlocked.Add(ref createTicks,Stopwatch.GetTimestamp()-start);
        Interlocked.Add(ref totalBytes,challenge.Image.ByteLength);
        start=Stopwatch.GetTimestamp();
        await service.VerifyAsync(context,challenge.CaptchaId,"wrong-answer",ct);
        Interlocked.Add(ref verifyTicks,Stopwatch.GetTimestamp()-start);
    });
    Console.WriteLine($"concurrency={concurrency} count=100 elapsed_ms={watch.Elapsed.TotalMilliseconds:F2} create_mean_ms={createTicks*1000.0/Stopwatch.Frequency/100:F3} verify_mean_ms={verifyTicks*1000.0/Stopwatch.Frequency/100:F3} managed_allocated_bytes={GC.GetTotalAllocatedBytes(true)-allocated} mean_png_bytes={totalBytes/100}");
}
