using Bing.IdUtils;
using Bing.Utils.Develops;

namespace Bing.Utils.Tests.IdGenerators;


public class SnowflakeIdGeneratorTest : TestBase
{
    private readonly ISnowflakeId _worker;

    /// <summary>
    /// 测试初始化
    /// </summary>
    public SnowflakeIdGeneratorTest(ITestOutputHelper output) : base(output)
    {
        _worker = SnowflakeGenerator.Create(1);
    }

    [Fact]
    public void Test_Create()
    {
        var result = _worker.NextId();
        Output.WriteLine(result.ToString());
    }

    [Fact]
    public void Test_Create_100()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _worker.NextId();
            Output.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void Test_Create_1000()
    {
        for (int i = 0; i < 1000; i++)
        {
            var result = _worker.NextId();
            Output.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void Test_Create_10000()
    {
        for (int i = 0; i < 10000; i++)
        {
            var result = _worker.NextId();
            Output.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void Test_Create_10W()
    {
        Create(100000);
    }

    [Fact]
    public void Test_Create_Thread10_100W()
    {
        UnitTester.TestConcurrency(() =>
        {
            Create(1000000);
        }, 10);
        Output.WriteLine("数量：" + _set.Count);
    }

    private static object _lock = new object();

    private static HashSet<long> _set = new HashSet<long>();

    private void Create(long length)
    {
        for (int i = 0; i < length; i++)
        {
            var result = _worker.NextId();
            lock (_lock)
            {
                if (_set.Contains(result))
                {
                    Output.WriteLine("发现重复项：{0}", result);
                }
                else
                {
                    _set.Add(result);
                }
            }
        }
    }
}