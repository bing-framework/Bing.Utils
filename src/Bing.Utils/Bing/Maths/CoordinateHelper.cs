namespace Bing.Maths;

/// <summary>
/// 坐标轴帮助类
/// </summary>
public class CoordinateHelper
{
    /// <summary>
    /// 地球半径（WGS-84椭圆球体）
    /// </summary>
    private const double RADIUS = 6378137.0D;

    /// <summary>
    /// 计算两个经纬度坐标的距离（单位：m）
    /// </summary>
    /// <param name="lngA">第一点经度</param>
    /// <param name="latA">第一点纬度</param>
    /// <param name="lngB">第二点经度</param>
    /// <param name="latB">第二点纬度</param>
    public static double CalcDistance(double lngA, double latA, double lngB, double latB)
    {
        // 所在经纬线
        var arcLngA = Arc(lngA);
        var arcLatA = Arc(latA);
        var arcLngB = Arc(lngB);
        var arcLatB = Arc(latB);
        // 经纬差
        var latDiffer = arcLatA - arcLatB;
        var lngDiffer = arcLngA - arcLngB;

        var distance = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(latDiffer / 2), 2) +Math.Cos(arcLatA) * Math.Cos(arcLatB) * Math.Pow(Math.Sin(lngDiffer / 2), 2))) * RADIUS;
        return Math.Round(distance, 2);
    }

    /// <summary>
    /// 经纬度转化成弧度
    /// </summary>
    /// <param name="line">经度/纬度</param>
    /// <returns>弧度</returns>
    private static double Arc(double line)
    {
        return line * Math.PI / 180d;
    }
}