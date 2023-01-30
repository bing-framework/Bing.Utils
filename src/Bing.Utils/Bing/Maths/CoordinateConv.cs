namespace Bing.Maths;

/// <summary>
/// 坐标转换，UMD魔法代码。
/// </summary>
public static class CoordinateConv
{
    /// <summary>
    /// 坐标转换参数：(火星坐标系与百度坐标系转换的中间量)
    /// </summary>
    private const double X_PI = 3.1415926535897932384626433832795 * 3000.0 / 180.0;

    /// <summary>
    /// 坐标转换参数：π
    /// </summary>
    private const double PI = 3.1415926535897932384626433832795D;

    /// <summary>
    /// 地球半径（Krasovsky 1940）
    /// </summary>
    private const double RADIUS = 6378245.0D;

    /// <summary>
    /// 修正参数（偏率ee）
    /// </summary>
    private const double CORRECTION_PARAM = 0.00669342162296594323D;

    /// <summary>
    /// 判断坐标是否在国外。<br />
    /// 火星坐标系（GCJ-02）只对国内有效，国外无需转换。
    /// </summary>
    /// <param name="lng">经度</param>
    /// <param name="lat">纬度</param>
    /// <returns>坐标是否在国外</returns>
    public static bool OutOfChina(double lng, double lat)
    {
        return (lng < 72.004 || lng > 137.8347) || (lat < 0.8293 || lat > 55.8271);
    }

    #region 地球坐标系(WGS84)

    /// <summary>
    /// 将 地球坐标系（WGS84） 转换为 火星坐标系（GCJ-02）
    /// </summary>
    /// <param name="lng">经度坐标</param>
    /// <param name="lat">纬度坐标</param>
    /// <returns>火星坐标系（GCJ-02）</returns>
    // ReSharper disable once InconsistentNaming
    public static Coordinate WGS84ToGCJ02(double lng, double lat)
    {
        return OutOfChina(lng, lat) ? new Coordinate(lng, lat) : new Coordinate(lng, lat).Offset(Offset(lng, lat, true));
    }

    /// <summary>
    /// 将 地球坐标系（WGS84） 转换为 百度坐标系（BD-09）
    /// </summary>
    /// <param name="lng">经度坐标</param>
    /// <param name="lat">纬度坐标</param>
    /// <returns>百度坐标系（BD-09）</returns>
    // ReSharper disable once InconsistentNaming
    public static Coordinate WGS84ToBD09(double lng, double lat)
    {
        var gcj02 = WGS84ToGCJ02(lng, lat);
        return GCJ02ToBD09(gcj02.Lng, gcj02.Lat);
    }

    #endregion

    #region 火星坐标系(GCJ-02)

    /// <summary>
    /// 将 火星坐标系（GCJ-02） 转换为 地球坐标系(WGS84)
    /// </summary>
    /// <param name="lng">经度坐标</param>
    /// <param name="lat">纬度坐标</param>
    /// <returns>地球坐标系(WGS84)</returns>
    // ReSharper disable once InconsistentNaming
    public static Coordinate GCJ02ToWGS84(double lng, double lat)
    {
        return new Coordinate(lng, lat).Offset(Offset(lng, lat, false));
    }

    /// <summary>
    /// 将 火星坐标系（GCJ-02） 转换为 百度坐标系(BD-09)
    /// </summary>
    /// <param name="lng"></param>
    /// <param name="lat"></param>
    /// <returns>百度坐标系(BD-09)</returns>
    public static Coordinate GCJ02ToBD09(double lng, double lat)
    {
        var z = Math.Sqrt(lng * lng + lat * lat) + 0.00002 * Math.Sin(lat * X_PI);
        var theta = Math.Atan2(lat, lng) + 0.000003 * Math.Cos(lng * X_PI);
        var bdLng = z * Math.Cos(theta) + 0.0065;
        var bdLat = z * Math.Sin(theta) + 0.006;
        return new Coordinate(bdLng, bdLat);
    }

    #endregion

    #region 百度坐标系(BD-09)

    /// <summary>
    /// 将 百度坐标系（BD-09）转换为 火星坐标系（GCJ-02），即 百度 转 谷歌、高德
    /// </summary>
    /// <param name="lng">经度坐标</param>
    /// <param name="lat">纬度坐标</param>
    /// <returns>火星坐标系（GCJ-02）</returns>
    // ReSharper disable once InconsistentNaming
    public static Coordinate BD09ToGCJ02(double lng, double lat)
    {
        var x = lng - 0.0065;
        var y = lat - 0.006;
        var z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * X_PI);
        var theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * X_PI);
        var ggLng = z * Math.Cos(theta);
        var ggLat = z * Math.Sin(theta);
        return new Coordinate(ggLng, ggLat);
    }

    /// <summary>
    /// 将 百度坐标系（BD-09）转换为 地球坐标系（WGS84）
    /// </summary>
    /// <param name="lng">经度坐标</param>
    /// <param name="lat">纬度坐标</param>
    /// <returns>地球坐标系（WGS84）</returns>
    // ReSharper disable once InconsistentNaming
    public static Coordinate BD09ToWGS84(double lng, double lat)
    {
        var gcj02 = BD09ToGCJ02(lng, lat);
        return GCJ02ToWGS84(gcj02.Lng, gcj02.Lat);
    }

    #endregion

    /// <summary>
    /// WGS84 与 火星坐标系（GCJ-02）转换的偏移算法（非精确）
    /// </summary>
    /// <param name="lng">经度坐标</param>
    /// <param name="lat">纬度坐标</param>
    /// <param name="isPlus">是否正向偏移：WGS84转GCJ-02使用正向，否则使用反向</param>
    /// <returns>偏移坐标</returns>
    private static Coordinate Offset(double lng, double lat, bool isPlus)
    {
        var dLng = TransformLng(lng - 105.0, lat - 35.0);
        var dLat = TransformLat(lng - 105.0, lat - 35.0);

        var magic = Math.Sin(lat / 180.0 * PI);
        magic = 1 - CORRECTION_PARAM * magic * magic;
        var sqrtMagic = Math.Sqrt(magic);

        dLng = (dLng * 180.0) / (RADIUS / sqrtMagic * Math.Cos(lat / 180.0 * PI) * PI);
        dLat = (dLat * 180.0) / ((RADIUS * (1 - CORRECTION_PARAM)) / (magic * sqrtMagic) * PI);

        if (false == isPlus)
        {
            dLng = -dLng;
            dLat = -dLat;
        }

        return new Coordinate(dLng, dLat);
    }

    /// <summary>
    /// 转换经度
    /// </summary>
    /// <param name="lng">经度坐标</param>
    /// <param name="lat">纬度坐标</param>
    private static double TransformLng(double lng, double lat)
    {
        var ret = 300.0 + lng + 2.0 * lat + 0.1 * lng * lng + 0.1 * lng * lat + 0.1 * Math.Sqrt(Math.Abs(lng));
        ret += (20.0 * Math.Sin(6.0 * lng * PI) + 20.0 * Math.Sin(2.0 * lng * PI)) * 2.0 / 3.0;
        ret += (20.0 * Math.Sin(lng * PI) + 40.0 * Math.Sin(lng / 3.0 * PI)) * 2.0 / 3.0;
        ret += (150.0 * Math.Sin(lng / 12.0 * PI) + 300.0 * Math.Sin(lng / 30.0 * PI)) * 2.0 / 3.0;
        return ret;
    }

    /// <summary>
    /// 转换纬度
    /// </summary>
    /// <param name="lng">经度坐标</param>
    /// <param name="lat">纬度坐标</param>
    private static double TransformLat(double lng, double lat)
    {
        var ret = -100.0 + 2.0 * lng + 3.0 * lat + 0.2 * lat * lat + 0.1 * lng * lat + 0.2 * Math.Sqrt(Math.Abs(lng));
        ret += (20.0 * Math.Sin(6.0 * lng * PI) + 20.0 * Math.Sin(2.0 * lng * PI)) * 2.0 / 3.0;
        ret += (20.0 * Math.Sin(lat * PI) + 40.0 * Math.Sin(lat / 3.0 * PI)) * 2.0 / 3.0;
        ret += (160.0 * Math.Sin(lat / 12.0 * PI) + 320 * Math.Sin(lat * PI / 30.0)) * 2.0 / 3.0;
        return ret;
    }

    /// <summary>
    /// 坐标
    /// </summary>
    public struct Coordinate
    {
        /// <summary>
        /// 经度
        /// </summary>
        public double Lng { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        public double Lat { get; set; }

        /// <summary>
        /// 初始化一个<see cref="Coordinate"/>类型的实例
        /// </summary>
        /// <param name="lng">经度</param>
        /// <param name="lat">纬度</param>
        public Coordinate(double lng, double lat)
        {
            Lng = lng;
            Lat = lat;
        }

        /// <summary>
        /// 当前坐标偏移指定坐标
        /// </summary>
        /// <param name="offset">偏移量</param>
        public Coordinate Offset(Coordinate offset)
        {
            Lng += offset.Lng;
            Lat += offset.Lat;
            return this;
        }
    }
}