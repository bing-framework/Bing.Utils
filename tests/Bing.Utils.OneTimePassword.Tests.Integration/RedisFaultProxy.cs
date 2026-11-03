using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Bing.Utils.OneTimePassword.Tests.Integration;

/// <summary>
/// 在真实 Redis 连接前转发 TCP 流量，并可通过释放实例模拟连接中断。
/// </summary>
internal sealed class RedisFaultProxy : IAsyncDisposable
{
    private readonly EndPoint _upstream;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ConcurrentDictionary<long, ConnectionPair> _connections = new();
    private readonly List<Task> _forwardingTasks = new();
    private readonly Task _acceptLoop;
    private readonly object _disposeSync = new();
    private Task? _disposeTask;
    private long _nextConnectionId;

    public RedisFaultProxy(EndPoint upstream)
    {
        _upstream = upstream ?? throw new ArgumentNullException(nameof(upstream));
        if (_upstream is not DnsEndPoint and not IPEndPoint)
            throw new ArgumentException("上游地址必须是 DNS 或 IP 终结点。", nameof(upstream));

        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        EndPoint = (IPEndPoint)_listener.LocalEndpoint;
        _acceptLoop = AcceptLoopAsync(_shutdown.Token);
    }

    public IPEndPoint EndPoint { get; }

    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                client.Dispose();
                break;
            }

            var id = Interlocked.Increment(ref _nextConnectionId);
            var pair = new ConnectionPair(client);
            if (!_connections.TryAdd(id, pair))
            {
                pair.Dispose();
                continue;
            }

            _forwardingTasks.Add(ForwardConnectionAsync(id, pair, cancellationToken));
        }
    }

    private async Task ForwardConnectionAsync(long id, ConnectionPair pair, CancellationToken cancellationToken)
    {
        TcpClient? upstreamClient = null;
        try
        {
            upstreamClient = new TcpClient();
            pair.SetUpstream(upstreamClient);
            await ConnectAsync(upstreamClient, _upstream, cancellationToken).ConfigureAwait(false);

            using var relayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var clientToUpstream = CopyAsync(pair.Client.GetStream(), upstreamClient.GetStream(), relayCancellation.Token);
            var upstreamToClient = CopyAsync(upstreamClient.GetStream(), pair.Client.GetStream(), relayCancellation.Token);
            await Task.WhenAny(clientToUpstream, upstreamToClient).ConfigureAwait(false);

            relayCancellation.Cancel();
            pair.Dispose();
            await Task.WhenAll(clientToUpstream, upstreamToClient).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            pair.Dispose();
            _connections.TryRemove(id, out _);
        }
    }

    private static async Task ConnectAsync(TcpClient client, EndPoint endpoint, CancellationToken cancellationToken)
    {
        switch (endpoint)
        {
            case DnsEndPoint dns:
                await client.ConnectAsync(dns.Host, dns.Port, cancellationToken).ConfigureAwait(false);
                break;
            case IPEndPoint ip:
                await client.ConnectAsync(ip.Address, ip.Port, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentException("上游地址必须是 DNS 或 IP 终结点。", nameof(endpoint));
        }
    }

    private static async Task CopyAsync(NetworkStream source, NetworkStream destination, CancellationToken cancellationToken)
    {
        try
        {
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task DisposeCoreAsync()
    {
        _shutdown.Cancel();
        _listener.Stop();

        foreach (var connection in _connections.Values)
            connection.Dispose();

        await _acceptLoop.ConfigureAwait(false);
        await Task.WhenAll(_forwardingTasks).ConfigureAwait(false);
        _shutdown.Dispose();
    }

    private sealed class ConnectionPair : IDisposable
    {
        private readonly object _sync = new();
        private TcpClient? _upstream;
        private bool _disposed;

        public ConnectionPair(TcpClient client) => Client = client;

        public TcpClient Client { get; }

        public void SetUpstream(TcpClient upstream)
        {
            lock (_sync)
            {
                if (_disposed)
                    upstream.Dispose();
                else
                    _upstream = upstream;
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;
                Client.Dispose();
                _upstream?.Dispose();
            }
        }
    }
}
