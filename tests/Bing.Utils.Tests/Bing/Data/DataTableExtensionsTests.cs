using System.Data;
using Bing.Data;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Data;

/// <summary>
/// 数据表行判断、对象映射及列结构创建测试。
/// </summary>
public class DataTableExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // HasRows
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证空数据表没有数据行。
    /// </summary>
    [Fact]
    public void HasRows_EmptyTable_ReturnsFalse()
    {
        var dt = new DataTable();
        dt.HasRows().ShouldBeFalse();
    }

    /// <summary>
    /// 验证包含数据行的数据表返回肯定结果。
    /// </summary>
    [Fact]
    public void HasRows_TableWithRows_ReturnsTrue()
    {
        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Rows.Add(1);
        dt.HasRows().ShouldBeTrue();
    }

    // ─────────────────────────────────────────────────────────────────
    // ToList<T>
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 数据表与对象映射使用的人员模型。
    /// </summary>
    private sealed class Person
    {
        /// <summary>
        /// 获取或设置人员标识符。
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 获取或设置人员姓名。
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 根据人员数据行创建测试数据表。
    /// </summary>
    /// <param name="rows">人员标识符和姓名组成的数据行。</param>
    /// <returns>包含人员标识符和姓名列的数据表。</returns>
    private static DataTable BuildPersonTable(params (int id, string name)[] rows)
    {
        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        foreach (var (id, name) in rows)
            dt.Rows.Add(id, name);
        return dt;
    }

    /// <summary>
    /// 验证空数据表引用转换为非空的空列表。
    /// </summary>
    [Fact]
    public void ToList_NullDataTable_ReturnsEmptyList()
    {
        var list = ((DataTable)null).ToList<Person>();
        list.ShouldNotBeNull();
        list.ShouldBeEmpty();
    }

    /// <summary>
    /// 验证空数据表转换为空列表。
    /// </summary>
    [Fact]
    public void ToList_EmptyDataTable_ReturnsEmptyList()
    {
        var dt = BuildPersonTable(); // no rows
        var list = dt.ToList<Person>();
        list.ShouldBeEmpty();
    }

    /// <summary>
    /// 验证单行数据映射为属性值一致的对象。
    /// </summary>
    [Fact]
    public void ToList_SingleRow_ReturnsMappedObject()
    {
        var dt = BuildPersonTable((1, "Alice"));
        var list = dt.ToList<Person>();
        list.Count.ShouldBe(1);
        list[0].Id.ShouldBe(1);
        list[0].Name.ShouldBe("Alice");
    }

    /// <summary>
    /// 验证多行数据转换后保留行数和指定字段值。
    /// </summary>
    [Fact]
    public void ToList_MultipleRows_ReturnsAllMappedObjects()
    {
        var dt = BuildPersonTable((1, "Alice"), (2, "Bob"), (3, "Charlie"));
        var list = dt.ToList<Person>();
        list.Count.ShouldBe(3);
        list[1].Name.ShouldBe("Bob");
    }

    // ─────────────────────────────────────────────────────────────────
    // DataTableHelper — CreateTable / CreateDataTable
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证根据列名创建具有对应列的数据表。
    /// </summary>
    [Fact]
    public void CreateTable_ValidColumns_ReturnsTableWithColumns()
    {
        var dt = DataTableHelper.CreateTable(["Name", "Age", "Email"]);
        dt.ShouldNotBeNull();
        dt.Columns.Count.ShouldBe(3);
        dt.Columns["Name"].ShouldNotBeNull();
    }

    /// <summary>
    /// 验证空列名列表创建数据表时返回空引用。
    /// </summary>
    [Fact]
    public void CreateTable_EmptyList_ReturnsNull()
    {
        var dt = DataTableHelper.CreateTable([]);
        dt.ShouldBeNull();
    }

    /// <summary>
    /// 验证未指定类型的列默认为字符串。
    /// </summary>
    [Fact]
    public void CreateDataTable_SimpleNames_AddsStringColumns()
    {
        var dt = DataTableHelper.CreateDataTable(new DataTable(), "col1,col2,col3");
        dt.Columns.Count.ShouldBe(3);
        dt.Columns["col1"].DataType.ShouldBe(typeof(string));
    }

    /// <summary>
    /// 验证带类型的列定义采用对应数据类型。
    /// </summary>
    [Fact]
    public void CreateDataTable_TypedNames_AddsCorrectTypes()
    {
        var dt = DataTableHelper.CreateDataTable(new DataTable(), "id|int,name|string,active|bool");
        dt.Columns["id"].DataType.ShouldBe(typeof(int));
        dt.Columns["active"].DataType.ShouldBe(typeof(bool));
    }

    // ─────────────────────────────────────────────────────────────────
    // DataTableHelper — ToDataTable<T>
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证空对象列表生成保留属性列的空数据表。
    /// </summary>
    [Fact]
    public void ToDataTable_EmptyList_ReturnsTableWithColumns()
    {
        var dt = DataTableHelper.ToDataTable<Person>([]);
        dt.ShouldNotBeNull();
        dt.Columns.Count.ShouldBe(2);
        dt.Rows.Count.ShouldBe(0);
    }

    /// <summary>
    /// 验证对象集合转换后保留行数和指定字段值。
    /// </summary>
    [Fact]
    public void ToDataTable_WithItems_ReturnsCorrectRows()
    {
        var people = new[] { new Person { Id = 1, Name = "Alice" }, new Person { Id = 2, Name = "Bob" } };
        var dt = DataTableHelper.ToDataTable(people);
        dt.Rows.Count.ShouldBe(2);
        dt.Rows[0]["Name"].ShouldBe("Alice");
    }
}
