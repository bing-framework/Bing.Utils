namespace Bing.IdUtils;

/// <summary>
/// 雪花ID
/// </summary>
public interface ISnowflakeId
{
    /// <summary>
    /// 获取Id
    /// </summary>
    /// <returns>生成的雪花标识。</returns>
    long NextId();

    /// <summary>
    /// 批量获取Id
    /// </summary>
    /// <param name="size">获取数量，最多10万个</param>
    /// <returns>按请求数量生成的雪花标识数组。</returns>
    long[] NextIds(uint size);
}
