using System;
using System.IO;
using System.Threading.Tasks;
using Bing.Security.Randomness;
using Shouldly;
using Xunit;

namespace Bing.Security.Cryptography;

public class AesGcmFilePathTests
{
    [Fact]
    public async Task EncryptFileAsync_WhenPathsDifferOnlyByCase_ShouldUsePlatformSemantics()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Security.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "source.bin");
        var destination = Path.Combine(directory, "SOURCE.BIN");
        await File.WriteAllBytesAsync(source, new byte[] { 1, 2, 3 });
        try
        {
            var directoryIsCaseSensitive = !File.Exists(destination);
            if (directoryIsCaseSensitive)
            {
                await AesGcmStreamEncryption.EncryptFileAsync(source, destination, SecurityRandom.GetBytes(32));
                File.Exists(destination).ShouldBeTrue();
            }
            else
            {
                await Should.ThrowAsync<ArgumentException>(() => AesGcmStreamEncryption.EncryptFileAsync(source, destination, SecurityRandom.GetBytes(32)));
                (await File.ReadAllBytesAsync(source)).ShouldBe(new byte[] { 1, 2, 3 });
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}