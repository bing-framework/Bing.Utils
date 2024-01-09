using Bing.Helpers;
using Bing.Http;
using Bing.Http.Clients;
using Bing.Serialization.SystemTextJson;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Bing.Utils.Http.Tests.Integration;

/// <summary>
/// 启动配置
/// </summary>
public class Startup
{
    /// <summary>
    /// 配置主机
    /// </summary>
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
        {
            webHostBuilder.UseTestServer()
                .Configure(t =>
                {
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
    /// 进程退出时释放日志实例，用于解决Seq无法写入的问题
    /// </summary>
    private void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
    }

    /// <summary>
    /// 配置服务
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        RegisterHttpContextAccessor(services);
        services.Configure( ( Microsoft.AspNetCore.Mvc.JsonOptions options ) => {
            options.JsonSerializerOptions.Converters.Add( new DateTimeJsonConverter() );
            options.JsonSerializerOptions.Converters.Add( new NullableDateTimeJsonConverter() );
        } );
        services.AddTransient<IHttpClient>(t =>
        {
            var client = new HttpClientService();
            client.SetHttpClient(t.GetService<IHost>().GetTestClient());
            return client;
        });
    }

    /// <summary>
    /// 注册Http上下文访问器
    /// </summary>
    private void RegisterHttpContextAccessor( IServiceCollection services ) {
        var httpContextAccessor = new HttpContextAccessor();
        services.TryAddSingleton<IHttpContextAccessor>( httpContextAccessor );
        Web.HttpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 配置日志提供程序
    /// </summary>
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor)
    {
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, (s, logLevel) => logLevel >= LogLevel.Trace));
    }
}
