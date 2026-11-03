using Bing.Ocr;
using Bing.Ocr.PaddleServing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Bing.Utils.Ocr.PaddleServing.Tests.Integration;

public class HttpProviderTests
{
    private const string Valid="{\"errorCode\":0,\"modelProfileId\":\"profile\",\"result\":{\"ocrResults\":[{\"prunedResult\":{\"rec_texts\":[\"中文 A\"],\"rec_scores\":[0.2],\"rec_polys\":[[[0,0],[10,0],[10,5],[0,5]]],\"input_img_shape\":[10,20]}}]}}";
    private static PreparedOcrImage Image()=>new(new byte[]{1,2},20,10,20,10,new OcrTransform());

    [Fact]
    public async Task Request_UsesExplicitOptionsAndVerifiedIdentity()
    {
        var requests=0;
        using var server=new TestServer(new WebHostBuilder().Configure(app=>app.Run(async context=>{
            requests++;
            if(context.Request.Path=="/health/ready") { await context.Response.WriteAsync("{\"ready\":true,\"modelProfileId\":\"profile\"}"); return; }
            using var doc=await JsonDocument.ParseAsync(context.Request.Body);
            var root=doc.RootElement;
            Assert.Equal(1,root.GetProperty("fileType").GetInt32());
            Assert.False(root.GetProperty("useDocOrientationClassify").GetBoolean());
            Assert.False(root.GetProperty("useDocUnwarping").GetBoolean());
            Assert.False(root.GetProperty("visualize").GetBoolean());
            Assert.True(root.GetProperty("useTextlineOrientation").GetBoolean());
            Assert.Equal(0,root.GetProperty("textRecScoreThresh").GetDouble());
            Assert.Equal(new byte[]{1,2},Convert.FromBase64String(root.GetProperty("file").GetString()!));
            await context.Response.WriteAsync(Valid);
        })));
        using var client=server.CreateClient();
        var options=new PaddleServingOptions{Endpoint=client.BaseAddress!,ExpectedModelProfileId="profile"};
        var provider=new PaddleServingOcrProvider(client,options);
        options.ExpectedModelProfileId="changed";
        var result=await provider.RecognizeAsync(Image(),new OcrOptions());
        Assert.True(result.ModelIdentityVerified); Assert.Equal("profile",result.ModelProfileId);
        Assert.Equal(.2,result.Lines[0].Confidence); Assert.Equal(2,requests);
    }

    [Theory]
    [InlineData(429,typeof(OcrBusyException))] [InlineData(504,typeof(OcrTimeoutException))]
    [InlineData(500,typeof(OcrProviderException))] [InlineData(302,typeof(OcrProviderException))]
    public async Task HttpErrors_AreMappedWithoutRetry(int status,Type type)
    {
        var count=0;
        using var server=new TestServer(new WebHostBuilder().Configure(app=>app.Run(context=>{
            count++; context.Response.StatusCode=status; return Task.CompletedTask;
        })));
        using var client=server.CreateClient();
        var provider=new PaddleServingOcrProvider(client,new PaddleServingOptions{Endpoint=client.BaseAddress!});
        var error=await Record.ExceptionAsync(()=>provider.RecognizeAsync(Image(),new OcrOptions()));
        Assert.IsType(type,error); Assert.Equal(1,count);
    }

    [Fact]
    public async Task ResponseLimitAndMalformedJson_AreProviderFailures()
    {
        using var server=new TestServer(new WebHostBuilder().Configure(app=>app.Run(async context=>await context.Response.WriteAsync(new string('x',128)))));
        using var client=server.CreateClient();
        var limited=new PaddleServingOcrProvider(client,new PaddleServingOptions{Endpoint=client.BaseAddress!,MaxResponseBytes=16});
        await Assert.ThrowsAsync<OcrProviderException>(()=>limited.RecognizeAsync(Image(),new OcrOptions()));
        var malformed=new PaddleServingOcrProvider(client,new PaddleServingOptions{Endpoint=client.BaseAddress!});
        await Assert.ThrowsAsync<OcrProviderException>(()=>malformed.RecognizeAsync(Image(),new OcrOptions()));
    }

    [Fact]
    public async Task CallerCancellation_ReachesHttpTransport()
    {
        var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server=new TestServer(new WebHostBuilder().Configure(app=>app.Run(async context=>{
            entered.SetResult(true); await Task.Delay(Timeout.Infinite,context.RequestAborted);
        })));
        using var client=server.CreateClient();
        using var cts=new CancellationTokenSource();
        var provider=new PaddleServingOcrProvider(client,new PaddleServingOptions{Endpoint=client.BaseAddress!});
        var task=provider.RecognizeAsync(Image(),new OcrOptions(),cts.Token);
        await entered.Task; cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>task);
    }

    [Fact]
    public async Task UnsupportedLanguage_FailsBeforeHttp_AndClientIsBorrowed()
    {
        using var server=new TestServer(new WebHostBuilder().Configure(app=>app.Run(c=>c.Response.WriteAsync(Valid))));
        using var client=server.CreateClient();
        var provider=new PaddleServingOcrProvider(client,new PaddleServingOptions{Endpoint=client.BaseAddress!});
        await Assert.ThrowsAsync<OcrCapabilityException>(()=>provider.RecognizeAsync(Image(),new OcrOptions{LanguageHint="unsupported"}));
        Assert.True((await client.GetAsync("/")).IsSuccessStatusCode);
    }
}
