using Bing.Captcha;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bing.Utils.Captcha.DependencyInjection.Tests;

public sealed class RegistrationTests
{
    private sealed class Renderer : ICaptchaRenderer
    {
        public CaptchaImage Render(string text,CancellationToken cancellationToken=default) => new(new byte[]{137,80,78,71,13,10,26,10},160,60);
    }

    [Fact]
    public async Task Registration_RequiresUniqueComponents_AndDisposesOwnedStore()
    {
        var services=new ServiceCollection();services.AddBingCaptcha();
        using(var missing=services.BuildServiceProvider()) Assert.Throws<InvalidOperationException>(()=>missing.GetRequiredService<ICaptchaService>());
        services.AddSingleton<ICaptchaKeyProvider>(new StaticCaptchaKeyProvider("test",new[]{new CaptchaKey("test",new byte[32])}));
        services.AddCaptchaRenderer(_=>new Renderer());services.AddMemoryCaptchaStore();
        Assert.Throws<InvalidOperationException>(()=>services.AddMemoryCaptchaStore());
        using var provider=services.BuildServiceProvider();Assert.NotNull(provider.GetRequiredService<ICaptchaService>());
        var store=provider.GetRequiredService<ICaptchaStore>();provider.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(()=>store.TryTakeAsync(new CaptchaContext("login","session","form"),"id"));
    }

    [Fact]
    public async Task Registration_SnapshotsOptions_AndLeavesExternalInstancesOwnedByHost()
    {
        using var store=new MemoryCaptchaStore();
        var options=new CaptchaServiceOptions { AllowedPurposes=new[]{"login"} };
        var services=new ServiceCollection();services.AddBingCaptcha(options);
        services.AddSingleton<ICaptchaStore>(store);services.AddCaptchaRenderer(_=>new Renderer());
        services.AddSingleton<ICaptchaKeyProvider>(new StaticCaptchaKeyProvider("test",new[]{new CaptchaKey("test",new byte[32])}));
        options.AllowedPurposes[0]="other";
        var context=new CaptchaContext("login","session","form");string id;
        using(var provider=services.BuildServiceProvider())
            id=(await provider.GetRequiredService<ICaptchaService>().CreateAsync(context)).CaptchaId;
        Assert.NotNull(await store.TryTakeAsync(context,id));
    }
}
