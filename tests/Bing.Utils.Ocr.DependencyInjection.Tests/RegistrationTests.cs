using Bing.Ocr;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bing.Utils.Ocr.DependencyInjection.Tests;

public class RegistrationTests
{
    [Fact]
    public async Task Register_SingletonsSnapshotOptionsAndContainerOwnership()
    {
        var services = new ServiceCollection();
        var options = new OcrServiceOptions { MaxInputBytes = 2 };
        var provider = new Provider();
        services.AddBingOcr(options).AddOcrProvider(_ => provider).AddOcrImagePreprocessor(_ => new Preprocessor());
        options.MaxInputBytes = 1;
        using (var container = services.BuildServiceProvider())
        {
            var service = container.GetRequiredService<IOcrService>();
            Assert.Same(service, container.GetRequiredService<IOcrService>());
            await service.RecognizeAsync(new MemoryStream(new byte[] { 1,2 }));
        }
        Assert.True(provider.Disposed);
    }
    [Fact]
    public void ExternalInstance_RemainsOwnedByCaller()
    {
        var provider = new Provider();
        var services = new ServiceCollection().AddBingOcr();
        services.AddSingleton<IOcrProvider>(provider);
        services.AddOcrImagePreprocessor(_ => new Preprocessor());
        using (var container = services.BuildServiceProvider()) container.GetRequiredService<IOcrService>();
        Assert.False(provider.Disposed);
    }
    [Fact]
    public void MissingOrDuplicateComponents_AreRejected()
    {
        var services = new ServiceCollection().AddBingOcr();
        Assert.Throws<InvalidOperationException>(() => services.AddBingOcr());
        using (var incomplete = services.BuildServiceProvider())
            Assert.Throws<InvalidOperationException>(() => incomplete.GetRequiredService<IOcrService>());
        services.AddOcrProvider(_ => new Provider());
        Assert.Throws<InvalidOperationException>(() => services.AddOcrProvider(_ => new Provider()));
        services.AddOcrImagePreprocessor(_ => new Preprocessor());
        services.AddSingleton<IOcrProvider>(new Provider());
        using var duplicated = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => duplicated.GetRequiredService<IOcrService>());
    }
    private sealed class Provider : IOcrProvider, IDisposable
    {
        public string Name => "test"; public bool Disposed;
        public void ValidateOptions(OcrOptions options) { }
        public void Dispose() => Disposed = true;
        public Task<OcrProviderResult> RecognizeAsync(PreparedOcrImage image,OcrOptions options,CancellationToken token = default)
            => Task.FromResult(new OcrProviderResult(Array.Empty<OcrLine>()));
    }
    private sealed class Preprocessor : IOcrImagePreprocessor
    {
        public PreparedOcrImage Prepare(byte[] bytes,OcrOptions options,OcrServiceOptions limits,CancellationToken token = default)
            => new(bytes,1,1,1,1,new OcrTransform());
    }
}
