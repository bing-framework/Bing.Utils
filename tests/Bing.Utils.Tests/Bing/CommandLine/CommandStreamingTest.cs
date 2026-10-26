using System.Runtime.InteropServices;
using System.Threading;

namespace Bing.CommandLine;

public class CommandStreamingTest
{
    [Fact]
    public async Task InputAndLiveConsumersAreIndependentOfCaptureLimits()
    {
        var options = Script("$line=[Console]::ReadLine(); [Console]::Out.Write($line); [Console]::Error.Write('error');", "read line; printf '%s' \"$line\"; printf error >&2");
        var stdout = new StringBuilder(); var stderr = new StringBuilder();
        options.MaxStandardOutputChars = 2; options.MaxStandardErrorChars = 1;
        options.StandardInput = async (writer, token) => { token.ThrowIfCancellationRequested(); await writer.WriteLineAsync(new string('x', 9000)); };
        options.OnStandardOutput = (chunk, token) => { Assert.InRange(chunk.Length, 1, 4096); stdout.Append(chunk); return Task.CompletedTask; };
        options.OnStandardError = (chunk, token) => { stderr.Append(chunk); return Task.CompletedTask; };
        var result = await CommandRunner.RunAsync(options);
        Assert.Equal(new string('x', 9000), stdout.ToString()); Assert.Equal("error", stderr.ToString());
        Assert.Equal("xx", result.StandardOutput); Assert.Equal("e", result.StandardError); Assert.True(result.OutputComplete);
    }
    [Fact]
    public async Task ConsumerFailureTerminatesProcessAndPreservesException()
    {
        var options = Script("[Console]::Out.WriteLine('ready'); Start-Sleep -Seconds 15", "printf 'ready\n'; sleep 15");
        var error = new IOException("consumer");
        options.OnStandardOutput = (_, _) => Task.FromException(error);
        Assert.Same(error, await Assert.ThrowsAsync<IOException>(() => CommandRunner.RunAsync(options)));
    }
    [Fact]
    public async Task CancellationWaitsForLiveConsumerFinally()
    {
        var options = Script("[Console]::Out.WriteLine('ready'); Start-Sleep -Seconds 15", "printf 'ready\n'; sleep 15");
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); var finished = false;
        using var cts = new CancellationTokenSource();
        options.OnStandardOutput = async (_, token) => { entered.SetResult(true); try { await Task.Delay(Timeout.Infinite, token); } finally { finished = true; } };
        var operation = CommandRunner.RunAsync(options, cts.Token); await entered.Task; cts.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(cts.Token, error.CancellationToken); Assert.True(finished);
    }
    private static CommandOptions Script(string windows, string unix)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new CommandOptions { FileName = "powershell.exe", Arguments = new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand",
                Convert.ToBase64String(Encoding.Unicode.GetBytes("[Console]::InputEncoding=[Text.Encoding]::UTF8;[Console]::OutputEncoding=[Text.Encoding]::UTF8;" + windows)) }, Timeout = TimeSpan.FromSeconds(25), OutputDrainTimeout = TimeSpan.FromSeconds(5) };
        return new CommandOptions { FileName = "/bin/sh", Arguments = new[] { "-c", unix }, Timeout = TimeSpan.FromSeconds(25), OutputDrainTimeout = TimeSpan.FromSeconds(5) };
    }
}
