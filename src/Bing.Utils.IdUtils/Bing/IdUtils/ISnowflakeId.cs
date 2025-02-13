namespace Bing.IdUtils;

/// <summary>
/// 雪花ID
/// </summary>
public interface ISnowflakeId
{
    /// <summary>
    /// 获取Id
    /// </summary>
    long NextId();

    /// <summary>
    /// 批量获取Id
    /// </summary>
    /// <param name="size">获取数量，最多10万个</param>
    long[] NextIds(uint size);
}