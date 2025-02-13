using Bing.Helpers;
using Bing.Utils.Develops;

namespace Bing.Utils.Tests.IdGenerators;

public class ObjectIdGeneratorTest : TestBase
{
    /// <summary>
    /// 测试初始化
    /// </summary>
    public ObjectIdGeneratorTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Test_Create()
    {
        var result = Id.CreateObjectId();
        Output.WriteLine(result.ToString());
    }

    [Fact]
    public void Test_Create_100()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = Id.CreateObjectId();
            Output.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void Test_Create_1000()
    {
        for (int i = 0; i < 1000; i++)
        {
            var result = Id.CreateObjectId();
            Output.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void Test_Create_10000()
    {
        for (int i = 0; i < 10000; i++)
        {
            var result = Id.CreateObjectId();
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

    private static HashSet<string> _set = new HashSet<string>();

    private void Create(long length)
    {
        for (int i = 0; i < length; i++)
        {
            var result = Id.CreateObjectId();
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