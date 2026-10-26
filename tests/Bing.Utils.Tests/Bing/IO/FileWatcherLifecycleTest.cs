using System.Collections.Concurrent;

using System.Threading;

namespace Bing.IO;

/// <summary>
/// 验证文件监控的合并、溢出恢复和停止屏障。
/// </summary>
[Trait("Bing.IO", "FileWatcher.Lifecycle")]
public class FileWatcherLifecycleTest
{
    [Fact]
    public async Task DebounceMergesRepeatedNotificationsWithinBoundedQueue()
    {
        using var directory = new TemporaryDirectory();
        var options = new FileWatcherOptions
        {
            ScanInterval = TimeSpan.FromSeconds(5),
            DebounceInterval = TimeSpan.FromMilliseconds(150)
        };
        await using var watcher = new FileWatcher(new[] { directory.Path }, options,
            _ => EmptySnapshot());
        var events = new ConcurrentQueue<FileWatcherEventArgs>();
        var received = new TaskCompletionSource<FileWatcherEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.EventHandler += (_, args) =>
        {
            events.Enqueue(args);
            received.TrySetResult(args);
        };
        watcher.Start();

        var filePath = Path.Combine(directory.Path, "item.txt");
        watcher.NotifyForTesting(directory.Path,
            new FileWatcherEventArgs(WatcherChangeTypes.Created, filePath, "item.txt", null, null));
        watcher.NotifyForTesting(directory.Path,
            new FileWatcherEventArgs(WatcherChangeTypes.Changed, filePath, "item.txt", null, null));

        var merged = await Wait(received.Task, TimeSpan.FromSeconds(5));
        await Task.Delay(250);

        Assert.Equal(WatcherChangeTypes.Created | WatcherChangeTypes.Changed, merged.ChangeTypes);
        Assert.Single(events);
        await watcher.StopAsync();
    }

    [Fact]
    public async Task BufferOverflowReportsErrorAndPublishesSnapshotDifference()
    {
        using var directory = new TemporaryDirectory();
        var snapshotCalls = 0;
        var initialSnapshot = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var filePath = Path.Combine(directory.Path, "after-overflow.txt");
        var secondPath = Path.Combine(directory.Path, "after-overflow-2.txt");
        var changes = new ConcurrentQueue<FileWatcherEventArgs>();
        var watcher = new FileWatcher(new[] { directory.Path }, new FileWatcherOptions
        {
            ScanInterval = TimeSpan.FromSeconds(5), DebounceInterval = TimeSpan.FromSeconds(1), MaxPendingEvents = 1
        }, _ =>
        {
            if (Interlocked.Increment(ref snapshotCalls) == 1)
            {
                initialSnapshot.TrySetResult(true);
                return EmptySnapshot();
            }

            return new Dictionary<string, FileWatcherSnapshotEntry>(StringComparer.Ordinal)
            {
                [filePath] = new FileWatcherSnapshotEntry(3, DateTime.UnixEpoch),
                [secondPath] = new FileWatcherSnapshotEntry(4, DateTime.UnixEpoch)
            };
        });
        await using (watcher)
        {
            var created = new TaskCompletionSource<FileWatcherEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            var overflow = new TaskCompletionSource<FileWatcherErrorEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            watcher.EventHandler += (_, args) => { changes.Enqueue(args); created.TrySetResult(args); };
            watcher.Error += (_, args) =>
            {
                if (args.IsBufferOverflow)
                {
                    overflow.TrySetResult(args);
                }
            };
            watcher.Start();

            await Wait(initialSnapshot.Task, TimeSpan.FromSeconds(5));
            var rescan = watcher.NotifyOverflowForTestingAsync(directory.Path);
            var error = await Wait(overflow.Task, TimeSpan.FromSeconds(5));
            await Wait(rescan, TimeSpan.FromSeconds(5));
            var change = await Wait(created.Task, TimeSpan.FromSeconds(5));

            Assert.IsType<InternalBufferOverflowException>(error.Exception);
            Assert.Equal(filePath, change.FullPath);
            Assert.Equal(WatcherChangeTypes.Created, change.ChangeTypes);
            Assert.Equal(2, changes.Count);
        }
    }

    [Fact]
    public async Task DebounceQueueLimitRequestsSnapshotRescan()
    {
        using var directory = new TemporaryDirectory();
        var snapshotCalls = 0;
        var initialSnapshot = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var rescanned = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var watcher = new FileWatcher(new[] { directory.Path }, new FileWatcherOptions
        {
            ScanInterval = TimeSpan.FromSeconds(5),
            DebounceInterval = TimeSpan.FromSeconds(1),
            MaxPendingEvents = 1
        }, _ =>
        {
            if (Interlocked.Increment(ref snapshotCalls) == 1)
            {
                initialSnapshot.TrySetResult(true);
            }
            else
            {
                rescanned.TrySetResult(true);
            }

            return EmptySnapshot();
        });
        var overflow = new TaskCompletionSource<FileWatcherErrorEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.Error += (_, args) =>
        {
            if (args.IsBufferOverflow)
            {
                overflow.TrySetResult(args);
            }
        };
        watcher.Start();
        await Wait(initialSnapshot.Task, TimeSpan.FromSeconds(5));

        watcher.NotifyForTesting(directory.Path,
            new FileWatcherEventArgs(WatcherChangeTypes.Created, Path.Combine(directory.Path, "one.txt"),
                "one.txt", null, null));
        watcher.NotifyForTesting(directory.Path,
            new FileWatcherEventArgs(WatcherChangeTypes.Created, Path.Combine(directory.Path, "two.txt"),
                "two.txt", null, null));

        Assert.True((await Wait(overflow.Task, TimeSpan.FromSeconds(5))).IsBufferOverflow);
        await Wait(rescanned.Task, TimeSpan.FromSeconds(5));
        await watcher.StopAsync();
    }

    [Fact]
    public async Task StopAsyncWaitsForInFlightCallbackAndRejectsLaterNotifications()
    {
        using var directory = new TemporaryDirectory();
        await using var watcher = new FileWatcher(new[] { directory.Path }, new FileWatcherOptions
        {
            ScanInterval = TimeSpan.FromSeconds(5)
        }, _ => EmptySnapshot());
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCount = 0;
        watcher.EventHandler += (_, _) =>
        {
            Interlocked.Increment(ref callbackCount);
            entered.TrySetResult(true);
            release.Task.GetAwaiter().GetResult();
        };
        watcher.Start();

        var notification = Task.Run(() => watcher.NotifyForTesting(directory.Path,
            new FileWatcherEventArgs(WatcherChangeTypes.Created, Path.Combine(directory.Path, "one.txt"),
                "one.txt", null, null)));
        await Wait(entered.Task, TimeSpan.FromSeconds(5));
        var stopping = watcher.StopAsync();
        try
        {
            Assert.False(stopping.IsCompleted);
        }
        finally
        {
            release.TrySetResult(true);
        }

        await Wait(notification, TimeSpan.FromSeconds(5));
        await Wait(stopping, TimeSpan.FromSeconds(5));
        watcher.NotifyForTesting(directory.Path,
            new FileWatcherEventArgs(WatcherChangeTypes.Created, Path.Combine(directory.Path, "two.txt"),
                "two.txt", null, null));

        Assert.Equal(1, callbackCount);
        Assert.False(watcher.IsWatching);
    }

    [Fact]
    public async Task ErrorSubscriberFailureDoesNotReenterErrorNotification()
    {
        using var directory = new TemporaryDirectory();
        await using var watcher = new FileWatcher(new[] { directory.Path }, new FileWatcherOptions(),
            _ => EmptySnapshot());
        var errorCount = 0;
        watcher.EventHandler += (_, _) => throw new InvalidOperationException("event failed");
        watcher.Error += (_, _) =>
        {
            errorCount++;
            throw new InvalidOperationException("error subscriber failed");
        };
        watcher.Start();

        watcher.NotifyForTesting(directory.Path,
            new FileWatcherEventArgs(WatcherChangeTypes.Changed, Path.Combine(directory.Path, "item.txt"),
                "item.txt", null, null));

        Assert.Equal(1, errorCount);
        await watcher.StopAsync();
    }

    [Fact]
    public async Task StartStopCanRepeatAndFilesystemEventsAreObserved()
    {
        using var directory = new TemporaryDirectory();
        await using var watcher = new FileWatcher(new[] { directory.Path }, new FileWatcherOptions
        {
            ScanInterval = TimeSpan.FromMilliseconds(50)
        });
        watcher.Start();
        watcher.Start();
        await watcher.StopAsync();
        await watcher.StopAsync();

        var received = new TaskCompletionSource<FileWatcherEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.EventHandler += (_, args) => received.TrySetResult(args);
        Assert.True(watcher.AddPath(directory.Path));
        watcher.Start();
        await WaitForWatcherAsync(watcher, directory.Path);
        var expectedPath = Path.Combine(directory.Path, "smoke.txt");
        File.WriteAllText(expectedPath, "watch", Encoding.UTF8);

        var observed = await Wait(received.Task, TimeSpan.FromSeconds(10));

        Assert.Equal(expectedPath, observed.FullPath);
        await watcher.StopAsync();
    }

    [Fact]
    public async Task ConcurrentPathChangesAndRepeatedLifecycleRemainUsable()
    {
        using var directory = new TemporaryDirectory();
        await using var watcher = new FileWatcher(Array.Empty<string>(), new FileWatcherOptions
        {
            ScanInterval = TimeSpan.FromMilliseconds(20)
        }, _ => EmptySnapshot());
        watcher.Start();

        await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            watcher.AddPath(directory.Path);
            watcher.DeletePath(directory.Path);
        })));

        watcher.DeletePath(directory.Path);
        Assert.True(watcher.AddPath(directory.Path));
        await WaitForWatcherAsync(watcher, directory.Path);
        await watcher.StopAsync();
        Assert.True(watcher.AddPath(directory.Path));
        watcher.Start();
        await WaitForWatcherAsync(watcher, directory.Path);
        await watcher.StopAsync();
    }

    private static async Task Wait(Task operation, TimeSpan timeout)
    {
        using var timer = new CancellationTokenSource();
        if (await Task.WhenAny(operation, Task.Delay(timeout, timer.Token)) != operation) throw new TimeoutException();
        timer.Cancel(); await operation;
    }
    private static async Task<T> Wait<T>(Task<T> operation, TimeSpan timeout)
    { await Wait((Task)operation, timeout); return await operation; }

    private static async Task WaitForWatcherAsync(FileWatcher watcher, string path)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!watcher.IsWatcherAttachedForTesting(path))
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private static IReadOnlyDictionary<string, FileWatcherSnapshotEntry> EmptySnapshot()
    {
        return new Dictionary<string, FileWatcherSnapshotEntry>(StringComparer.Ordinal);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bing-file-watcher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
