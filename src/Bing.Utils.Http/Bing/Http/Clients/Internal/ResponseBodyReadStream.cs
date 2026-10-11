using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Bing.Http.Clients.Internal;

/// <summary>

/// 只读响应流：计数并限制实际读取量，预读 BOM/空白且不缓冲完整正文。

/// </summary>
internal sealed class ResponseBodyReadStream : Stream
{
    /// <summary>
    /// 接收后续响应正文数据的底层流。
    /// </summary>
    private readonly Stream _inner;
    /// <summary>
    /// 允许读取的正文最大字节数；为空时不限制。
    /// </summary>
    private readonly long? _limit;
    /// <summary>
    /// 暂存首次读取时跳过的 Unicode 空白字节。
    /// </summary>
    private byte[] _prefix = ArrayPool<byte>.Shared.Rent(4096);
    /// <summary>
    /// 前缀缓冲区中下一个待读取字节的位置。
    /// </summary>
    private int _prefixOffset;
    /// <summary>
    /// 前缀缓冲区当前有效字节数。
    /// </summary>
    private int _prefixCount;
    /// <summary>
    /// 从响应正文累计读取的字节数。
    /// </summary>
    private long _bytesRead;

    /// <summary>

    /// 初始化一个<see cref="ResponseBodyReadStream"/>类型的实例。

    /// </summary>
    public ResponseBodyReadStream(Stream inner, long? limit) { _inner = inner; _limit = limit; }

    /// <summary>

    /// 移除 UTF-8 BOM 和前置 JSON 空白；返回是否存在非空白正文。

    /// </summary>
    public async Task<bool> InitializeAsync(CancellationToken token)
    {
        _prefixCount = await ReadSourceAsync(_prefix, 0, 4096, token).ConfigureAwait(false);
        if (_prefixCount != 0 && _prefix[0] == 0xEF)
        {
            while (_prefixCount < 3)
            {
                var read = await ReadSourceAsync(_prefix, _prefixCount, 3 - _prefixCount, token).ConfigureAwait(false);
                if (read == 0) break;
                _prefixCount += read;
            }
            if (_prefixCount >= 3 && _prefix[1] == 0xBB && _prefix[2] == 0xBF) _prefixOffset = 3;
        }
        while (true)
        {
            while (_prefixOffset < _prefixCount)
            {
                var value = _prefix[_prefixOffset];
                if (value >= 0x80 || value == 0x0B || value == 0x0C)
                {
                    // 旧转换将全 Unicode 空白视为空正文；非 JSON 空白前缀不能被悄悄移除。
                    await ReadUnicodeWhitespaceAsync(token).ConfigureAwait(false);
                    return false;
                }
                if (value != 0x20 && value != 0x09 && value != 0x0A && value != 0x0D) return true;
                _prefixOffset++;
            }
            _prefixOffset = 0;
            _prefixCount = await ReadSourceAsync(_prefix, 0, 4096, token).ConfigureAwait(false);
            if (_prefixCount == 0) return false;
        }
    }

    /// <summary>

    /// 读取并跳过 JSON 正文开头的 Unicode 空白。

    /// </summary>
    private async Task ReadUnicodeWhitespaceAsync(CancellationToken token)
    {
        var decoder = Encoding.UTF8.GetDecoder();
        var characters = ArrayPool<char>.Shared.Rent(4097);
        try
        {
            while (true)
            {
                var count = decoder.GetChars(_prefix, _prefixOffset, _prefixCount - _prefixOffset,
                    characters, 0, _prefixCount == 0);
                for (var i = 0; i < count; i++)
                    if (!char.IsWhiteSpace(characters[i]))
                        throw new JsonException("A JSON response cannot start with non-JSON whitespace or an invalid token.");
                if (_prefixCount == 0) return;
                _prefixOffset = 0;
                _prefixCount = await ReadSourceAsync(_prefix, 0, 4096, token).ConfigureAwait(false);
            }
        }
        finally { ArrayPool<char>.Shared.Return(characters); }
    }

    /// <summary>

    /// 从已暂存前缀读取数据到调用方缓冲区。

    /// </summary>
    private int ReadPrefix(byte[] buffer, int offset, int count)
    {
        var copied = Math.Min(count, _prefixCount - _prefixOffset);
        if (copied != 0)
        {
            Buffer.BlockCopy(_prefix, _prefixOffset, buffer, offset, copied);
            _prefixOffset += copied;
        }
        return copied;
    }

    /// <summary>

    /// 累计读取字节并检查正文大小限制。

    /// </summary>
    private void CountRead(int count)
    {
        _bytesRead += count;
        if (_limit.HasValue && _bytesRead > _limit.Value)
            throw new HttpResponseSizeLimitException(_limit.Value);
    }

    /// <summary>

    /// 读取底层响应流并累计正文字节数。

    /// </summary>
    private async Task<int> ReadSourceAsync(byte[] buffer, int offset, int count, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var read = await _inner.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
        CountRead(read);
        return read;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var copied = ReadPrefix(buffer, offset, count);
        if (copied != 0 || count == 0) return copied;
        var read = _inner.Read(buffer, offset, count);
        CountRead(read);
        return read;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var copied = ReadPrefix(buffer, offset, count);
        return copied != 0 || count == 0 ? Task.FromResult(copied) : ReadSourceAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _prefix != null)
        {
            ArrayPool<byte>.Shared.Return(_prefix);
            _prefix = null;
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override bool CanRead => _prefix != null && _inner.CanRead;
    /// <inheritdoc />
    public override bool CanSeek => false;
    /// <inheritdoc />
    public override bool CanWrite => false;
    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc />
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    /// <inheritdoc />
    public override void Flush() { }
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();
    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
