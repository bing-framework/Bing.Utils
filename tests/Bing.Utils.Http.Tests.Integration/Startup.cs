using Bing.Helpers;
using Bing.Http;
using Bing.Http.Clients;
using Bing.Serialization.SystemTextJson;
using Bing.Utils.Http.Tests.Integration.Controllers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Bing.Utils.Http.Tests.Integration;

/// <summary>
/// 配置集成测试使用的测试主机、HTTP 客户端及日志。
/// </summary>
public class Startup
{
    /// <summary>
    /// 配置使用内存测试服务器的控制器请求管道。
    /// </summary>
    /// <param name="hostBuilder">集成测试使用的主机构建器。</param>
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
        {
            webHostBuilder.UseTestServer()
                .Configure(t =>
                {
                    // 仅测试宿主使用：异常响应沿用控制器的错误结构与 JSON 配置。
                    t.UseExceptionHandler(error => error.Run(async context =>
                    {
                        var options = context.RequestServices
                            .GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>();
                        var response = new Test7Controller.ErrorResponse
                        {
                            Code = StatusCodes.Status500InternalServerError,
                            Message = "error:500"
                        };
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(response, options.Value.JsonSerializerOptions),
                            Encoding.UTF8, context.RequestAborted);
                    }));
                    t.UseRouting();
                    t.UseAuthentication();
                    t.UseAuthorization();
                    t.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                });
        });
    }

    /// <summary>
    /// 提供进程退出事件的空处理程序。
    /// </summary>
    /// <param name="sender">事件发送方。</param>
    /// <param name="e">进程退出事件参数。</param>
    private void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
    }

    /// <summary>
    /// 注册控制器、日期转换器及使用测试服务器的 HTTP 客户端。
    /// </summary>
    /// <param name="services">集成测试使用的服务集合。</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        RegisterHttpContextAccessor(services);
        services.Configure((Microsoft.AspNetCore.Mvc.JsonOptions options) =>
        {
            options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
        });
        services.AddTransient<IHttpClient>(t =>
        {
            var client = new HttpClientService();
            client.SetHttpClient(t.GetService<IHost>().GetTestClient());
            return client;
        });
    }

    /// <summary>
    /// 注册并设置测试使用的 HTTP 上下文访问器。
    /// </summary>
    /// <param name="services">接收上下文访问器注册的服务集合。</param>
    private void RegisterHttpContextAccessor(IServiceCollection services)
    {
        var httpContextAccessor = new HttpContextAccessor();
        services.TryAddSingleton<IHttpContextAccessor>(httpContextAccessor);
        Web.HttpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 将日志输出转发到当前 xUnit 测试。
    /// </summary>
    /// <param name="loggerFactory">用于添加测试日志提供程序的日志工厂。</param>
    /// <param name="accessor">当前测试输出助手的访问器。</param>
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor)
    {
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, (s, logLevel) => logLevel >= LogLevel.Trace));
    }
}
