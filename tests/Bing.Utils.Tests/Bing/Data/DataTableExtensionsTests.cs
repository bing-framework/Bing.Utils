using System.Data;
using Bing.Data;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Data;

/// <summary>
/// DataTableExtensions — ToList / HasRows
/// DataTableHelper    — ToDataTable / CreateTable
/// </summary>
public class DataTableExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // HasRows
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void HasRows_EmptyTable_ReturnsFalse()
    {
        var dt = new DataTable();
        dt.HasRows().ShouldBeFalse();
    }

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

    private sealed class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static DataTable BuildPersonTable(params (int id, string name)[] rows)
    {
        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        foreach (var (id, name) in rows)
            dt.Rows.Add(id, name);
        return dt;
    }

    [Fact]
    public void ToList_NullDataTable_ReturnsEmptyList()
    {
        var list = ((DataTable)null).ToList<Person>();
        list.ShouldNotBeNull();
        list.ShouldBeEmpty();
    }

    [Fact]
    public void ToList_EmptyDataTable_ReturnsEmptyList()
    {
        var dt = BuildPersonTable(); // no rows
        var list = dt.ToList<Person>();
        list.ShouldBeEmpty();
    }

    [Fact]
    public void ToList_SingleRow_ReturnsMappedObject()
    {
        var dt = BuildPersonTable((1, "Alice"));
        var list = dt.ToList<Person>();
        list.Count.ShouldBe(1);
        list[0].Id.ShouldBe(1);
        list[0].Name.ShouldBe("Alice");
    }

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

    [Fact]
    public void CreateTable_ValidColumns_ReturnsTableWithColumns()
    {
        var dt = DataTableHelper.CreateTable(["Name", "Age", "Email"]);
        dt.ShouldNotBeNull();
        dt.Columns.Count.ShouldBe(3);
        dt.Columns["Name"].ShouldNotBeNull();
    }

    [Fact]
    public void CreateTable_EmptyList_ReturnsNull()
    {
        var dt = DataTableHelper.CreateTable([]);
        dt.ShouldBeNull();
    }

    [Fact]
    public void CreateDataTable_SimpleNames_AddsStringColumns()
    {
        var dt = DataTableHelper.CreateDataTable(new DataTable(), "col1,col2,col3");
        dt.Columns.Count.ShouldBe(3);
        dt.Columns["col1"].DataType.ShouldBe(typeof(string));
    }

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

    [Fact]
    public void ToDataTable_EmptyList_ReturnsTableWithColumns()
    {
        var dt = DataTableHelper.ToDataTable<Person>([]);
        dt.ShouldNotBeNull();
        dt.Columns.Count.ShouldBe(2);
        dt.Rows.Count.ShouldBe(0);
    }

    [Fact]
    public void ToDataTable_WithItems_ReturnsCorrectRows()
    {
        var people = new[] { new Person { Id = 1, Name = "Alice" }, new Person { Id = 2, Name = "Bob" } };
        var dt = DataTableHelper.ToDataTable(people);
        dt.Rows.Count.ShouldBe(2);
        dt.Rows[0]["Name"].ShouldBe("Alice");
    }
}
