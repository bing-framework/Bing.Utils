using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bing.Http.Extensions;

/// <summary>
/// 会话对象序列化扩展的测试。
/// </summary>
[Trait("Bing.Http", "SessionExtensions")]
public class SessionExtensionsTest
{
    /// <summary>
    /// 验证会话对象可经 JSON 序列化往返。
    /// </summary>
    [Fact]
    public void SetAndGet_ShouldRoundTripJsonValue()
    {
        var session = new InMemorySession();

        session.Set("value", new SessionValue { Name = "Bing", Count = 2 });

        var result = session.Get<SessionValue>("value");
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Bing");
        result.Count.ShouldBe(2);
    }

    /// <summary>
    /// 验证写入 null 后读取返回默认值。
    /// </summary>
    [Fact]
    public void Set_WhenValueIsNull_ShouldReadAsDefault()
    {
        var session = new InMemorySession();

        session.Set<string>("value", null);

        session.Get<string>("value").ShouldBeNull();
    }

    /// <summary>
    /// 验证空白会话键不会写入内容。
    /// </summary>
    /// <param name="key">为 null、空字符串或空白的会话键。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Set_WhenKeyIsNullOrWhitespace_ShouldIgnoreValue(string key)
    {
        var session = new InMemorySession();

        session.Set(key, "value");

        session.Keys.ShouldBeEmpty();
    }

    /// <summary>
    /// 用于会话序列化往返测试的对象。
    /// </summary>
    public sealed class SessionValue
    {
        /// <summary>
        /// 获取或设置测试名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置测试计数。
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// 用于会话扩展测试的内存存储。
    /// </summary>
    /// <remarks>
    /// 以字节数组模拟框架 SetString 和 GetString 使用的存储。
    /// </remarks>
    private sealed class InMemorySession : ISession
    {
        /// <summary>
        /// 保存当前测试会话中各键对应的序列化字节。
        /// </summary>
        private readonly Dictionary<string, byte[]> _values = new();

        /// <inheritdoc />
        public bool IsAvailable => true;

        /// <inheritdoc />
        public string Id => "test-session";

        /// <inheritdoc />
        public IEnumerable<string> Keys => _values.Keys;

        /// <inheritdoc />
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc />
        public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value);

        /// <inheritdoc />
        public void Set(string key, byte[] value) => _values[key] = value;

        /// <inheritdoc />
        public void Remove(string key) => _values.Remove(key);

        /// <inheritdoc />
        public void Clear() => _values.Clear();
    }
}
