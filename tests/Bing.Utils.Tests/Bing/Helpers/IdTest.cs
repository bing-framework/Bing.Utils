using Bing.IdUtils;
using System.Collections.Concurrent;

namespace Bing.Helpers;

/// <summary>
/// Id 测试
/// </summary>
public class IdTest
{
    /// <summary>
    /// 测试 CreateSnowflakeId 方法是否生成唯一的雪花ID
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_Should_Not_Generate_Duplicate_Ids()
    {
        // Arrange
        var idSet = new HashSet<long>();
        var snowflakeIdProvider = SnowflakeGenerator.Create(1);
        Id.Configure(() => snowflakeIdProvider);

        // Act
        for (var i = 0; i < 100000; i++)
        {
            var id = Id.CreateSnowflakeId();
            Assert.DoesNotContain(id, idSet);
            idSet.Add(id);
        }

        // Assert
        Assert.Equal(100000, idSet.Count);
    }

    /// <summary>
    /// 测试 ResetSnowflakeId 方法是否重置为默认的雪花ID生成函数
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_Should_Reset_To_Default_Provider()
    {
        // Arrange
        var defaultProvider = SnowflakeGenerator.Create(1);
        Id.Configure(() => defaultProvider);
        var defaultId = Id.CreateSnowflakeId();

        // Act
        Id.ResetSnowflakeId();
        var resetId = Id.CreateSnowflakeId();

        // Assert
        Assert.NotEqual(defaultId, resetId);
    }

    /// <summary>
    /// 多线程压测，测试 CreateSnowflakeId 方法是否生成唯一的雪花ID
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_Should_Not_Generate_Duplicate_Ids_Under_MultiThreading()
    {
        // Arrange
        var idSet = new ConcurrentDictionary<long, byte>();
        var snowflakeIdProvider = SnowflakeGenerator.Create(1);
        Id.Configure(() => snowflakeIdProvider);
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 10000; j++)
                {
                    var id = Id.CreateSnowflakeId();
                    Assert.False(idSet.ContainsKey(id), $"Duplicate ID found: {id}");
                    idSet[id] = 0;
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(100000, idSet.Count);
    }
}
