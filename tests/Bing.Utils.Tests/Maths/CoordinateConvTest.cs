using Bing.Maths;

namespace Bing.Utils.Tests.Maths;

public class CoordinateConvTest
{
    /// <summary>
    /// 经度
    /// </summary>
    private const double BASE_LNG = 113.331073;

    /// <summary>
    /// 纬度
    /// </summary>
    private const double BASE_LAT = 23.112214;

    [Theory]
    [InlineData(113.32453135630236, 23.106450240552665)]// 113.32453135630236,23.106450240552665
    public void Test_BD09ToGCJ02(double lng, double lat)
    {
        var result = CoordinateConv.BD09ToGCJ02(BASE_LNG, BASE_LAT);
        Assert.Equal(lng, result.Lng);
        Assert.Equal(lat, result.Lat);
    }

    [Theory]
    [InlineData(113.31911008504576, 23.109052036366514)]// 113.31911008504576,23.109052036366514
    public void Test_BD09ToWGS84(double lng, double lat)
    {
        var result = CoordinateConv.BD09ToWGS84(BASE_LNG, BASE_LAT);
        Assert.Equal(lng, result.Lng);
        Assert.Equal(lat, result.Lat);
    }

    [Theory]
    [InlineData(113.33650811893553, 23.109627055413704)]// 113.33650811893553,23.109627055413704
    public void Test_WGS84ToGCJ02(double lng, double lat)
    {
        var result = CoordinateConv.WGS84ToGCJ02(BASE_LNG, BASE_LAT);
        Assert.Equal(lng, result.Lng);
        Assert.Equal(lat, result.Lat);
    }

    [Theory]
    [InlineData(113.34306682663143, 23.115290783030787)]// 113.34306682663143,23.115290783030787
    public void Test_WGS84ToBD09(double lng, double lat)
    {
        var result = CoordinateConv.WGS84ToBD09(BASE_LNG, BASE_LAT);
        Assert.Equal(lng, result.Lng);
        Assert.Equal(lat, result.Lat);
    }

    [Theory]
    [InlineData(113.32563788106448, 23.1148009445863)]// 113.32563788106448,23.1148009445863
    public void Test_GCJ02ToWGS84(double lng, double lat)
    {
        var result = CoordinateConv.GCJ02ToWGS84(BASE_LNG, BASE_LAT);
        Assert.Equal(lng, result.Lng);
        Assert.Equal(lat, result.Lat);
    }

    [Theory]
    [InlineData(113.33762320331894, 23.117908090871566)]// 113.33762320331894,23.117908090871566
    public void Test_GCJ02ToBD09(double lng, double lat)
    {
        var result = CoordinateConv.GCJ02ToBD09(BASE_LNG, BASE_LAT);
        Assert.Equal(lng, result.Lng);
        Assert.Equal(lat, result.Lat);
    }
}