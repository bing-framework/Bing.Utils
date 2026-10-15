using System.Data;
using Bing.Tests.Samples;

namespace Bing.Data;

/// <summary>
/// 数据表映射学生模型的测试。
/// </summary>
public class DataTableHelperTest
{
    /// <summary>
    /// 验证数据表首行映射为字段值一致的学生模型。
    /// </summary>
    [Fact]
    public void Test_ToList()
    {
        var dt = new DataTable("StudentSample");
        //Columns Header
        dt.Columns.Add("StudentId", typeof(int));
        dt.Columns.Add("StudentName", typeof(string));
        dt.Columns.Add("Address", typeof(string));
        dt.Columns.Add("MobileNo", typeof(string));
        //Data
        dt.Rows.Add(1, "Manish", "Hyderabad", "0000000000");
        dt.Rows.Add(2, "Venkat", "Hyderabad", "111111111");
        dt.Rows.Add(3, "Namit", "Pune", "1222222222");
        dt.Rows.Add(4, "Abhinav", "Bhagalpur", "3333333333");
        var result = DataTableHelper.ToList<StudentSample>(dt);
        Assert.Equal(result[0].StudentId, dt.Rows[0]["StudentId"]);
        Assert.Equal(result[0].StudentName, dt.Rows[0]["StudentName"]);
        Assert.Equal(result[0].Address, dt.Rows[0]["Address"]);
        Assert.Equal(result[0].MobileNo, dt.Rows[0]["MobileNo"]);
    }
}
