using System.Net;
using System.Net.Http;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;
#if NET6_0_OR_GREATER
using System.Diagnostics;
using System.Diagnostics.Metrics;
#endif

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// 验证诊断只导出有限元数据且不会改变请求结果。
/// </summary>
public class HttpDiagnosticsTest
{
    /// <summary>
    /// 真正的请求管线发出发送和完成事件，观察失败不改变成功结果。
    /// </summary>
    [Fact]
    public async Task DiagnosticsObserveRealPipelineWithoutExportingSecrets()
    {
        var events = new List<HttpDiagnosticEvent>(); var failures = 0;
        using var diagnostics = new HttpDiagnostics(e => { events.Add(e); throw new Exception("observer"); }, _ => failures++);
        using var client = new HttpClient(new Handler());
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/private?token=secret");
        request.UseInterceptor(diagnostics.CreateInterceptor()).ObserveCompletion(diagnostics.RecordCompletion);
        Assert.Equal("ok", await request.GetResultAsync());
        Assert.Equal(new[] { "attempt", "completion" }, events.Select(e => e.Kind));
        Assert.All(events, e => Assert.Equal(HttpStatusCode.OK, e.StatusCode));
        Assert.Equal(2, failures);
        diagnostics.RecordRetry(new HttpRetryScheduled(1, TimeSpan.FromMilliseconds(10), HttpStatusCode.ServiceUnavailable, null));
        Assert.Equal("retry", events.Last().Kind);
        Assert.DoesNotContain(typeof(HttpDiagnosticEvent).GetProperties(), p => p.Name.Contains("Uri") || p.Name.Contains("Exception") || p.Name.Contains("Headers"));
        diagnostics.Dispose(); diagnostics.RecordCompletion(new HttpRequestCompletion(HttpStatusCode.OK, 1, TimeSpan.Zero, HttpRequestOutcome.Success, null));
        Assert.Equal(3, events.Count);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// 外部 Activity 监听器失败不会覆盖 HTTP 成功结果。
    /// </summary>
    [Fact]
    public async Task ListenerFailureDoesNotReplaceRequestResult()
    {
        var name = "Bing.Test." + Guid.NewGuid().ToString("N"); var errors = 0;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _ => throw new InvalidOperationException("listener")
        };
        ActivitySource.AddActivityListener(listener);
        using var diagnostics = new HttpDiagnostics(onObserverError: _ => errors++, name: name);
        using var client = new HttpClient(new Handler());
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/");
        request.UseInterceptor(diagnostics.CreateInterceptor());
        Assert.Equal("ok", await request.GetResultAsync()); Assert.Equal(1, errors);
    }

    /// <summary>
    /// 现代框架发送 span 与总耗时指标可被标准监听器消费。
    /// </summary>
    [Fact]
    public async Task ModernInstrumentationEmitsActivitiesAndMeasurements()
    {
        var name = "Bing.Test." + Guid.NewGuid().ToString("N");
        var spans = new List<Activity>(); var counters = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, current) => { if (instrument.Meter.Name == name) current.EnableMeasurementEvents(instrument); };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, _, _) => counters.Add(instrument.Name));
        meterListener.SetMeasurementEventCallback<double>((instrument, _, _, _) => counters.Add(instrument.Name));
        meterListener.Start();
        using var diagnostics = new HttpDiagnostics(name: name);
        using var client = new HttpClient(new Handler());
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/?secret=1");
        request.UseInterceptor(diagnostics.CreateInterceptor()).ObserveCompletion(diagnostics.RecordCompletion);
        await request.GetResultAsync();
        Assert.Single(spans); Assert.Equal("http.send", spans[0].OperationName);
        Assert.DoesNotContain(spans[0].Tags, pair => pair.Value?.Contains("secret") == true);
        Assert.Contains("bing.http.requests", counters); Assert.Contains("bing.http.duration", counters);
    }
#endif
    /// <summary>
    /// 避免外部网络的响应处理器。
    /// </summary>
    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
    }
}
