using System.Dynamic;
using Bing.Extensions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="ExpandoObjectExtensions"/> 单元测试
/// </summary>
public class ExpandoObjectExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // AddProperty
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AddProperty_NewProperty_AddsSuccessfully()
    {
        dynamic eo = new ExpandoObject();
        ((ExpandoObject)eo).AddProperty("Name", "Alice");
        ((string)eo.Name).ShouldBe("Alice");
    }

    [Fact]
    public void AddProperty_DuplicateProperty_ThrowsArgumentException()
    {
        dynamic eo = new ExpandoObject();
        ((ExpandoObject)eo).AddProperty("Age", 30);
        Should.Throw<ArgumentException>(() => ((ExpandoObject)eo).AddProperty("Age", 31));
    }

    [Fact]
    public void AddProperty_NullValue_AddsPropertyWithNull()
    {
        dynamic eo = new ExpandoObject();
        ((ExpandoObject)eo).AddProperty("Data", (object?)null);
        ((object?)eo.Data).ShouldBeNull();
    }

    // ─────────────────────────────────────────────────────────────────
    // SetProperty
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SetProperty_NewProperty_AddsWithoutException()
    {
        dynamic eo = new ExpandoObject();
        ((ExpandoObject)eo).SetProperty("Score", 100);
        ((int)eo.Score).ShouldBe(100);
    }

    [Fact]
    public void SetProperty_ExistingProperty_UpdatesValue()
    {
        dynamic eo = new ExpandoObject();
        ((ExpandoObject)eo).SetProperty("Score", 100);
        ((ExpandoObject)eo).SetProperty("Score", 200);
        ((int)eo.Score).ShouldBe(200);
    }

    // ─────────────────────────────────────────────────────────────────
    // GetProperty
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetProperty_ExistingProperty_ReturnsValue()
    {
        var eo = new ExpandoObject();
        eo.AddProperty("City", "Beijing");
        eo.GetProperty("City").ShouldBe("Beijing");
    }

    [Fact]
    public void GetProperty_MissingProperty_ThrowsArgumentNullException()
    {
        var eo = new ExpandoObject();
        Should.Throw<ArgumentNullException>(() => eo.GetProperty("Missing"));
    }

    // ─────────────────────────────────────────────────────────────────
    // GetProperties
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetProperties_Empty_ReturnsEmptyList()
    {
        var eo = new ExpandoObject();
        eo.GetProperties().ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_MultipleProperties_ReturnsAllNames()
    {
        var eo = new ExpandoObject();
        eo.AddProperty("A", 1);
        eo.AddProperty("B", 2);
        eo.AddProperty("C", 3);
        var names = eo.GetProperties();
        names.Count.ShouldBe(3);
        names.ShouldContain("A");
        names.ShouldContain("B");
        names.ShouldContain("C");
    }

    // ─────────────────────────────────────────────────────────────────
    // RemoveProperty
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveProperty_ExistingProperty_RemovesIt()
    {
        var eo = new ExpandoObject();
        eo.AddProperty("Temp", "value");
        eo.RemoveProperty("Temp");
        eo.GetProperties().ShouldNotContain("Temp");
    }

    [Fact]
    public void RemoveProperty_MissingProperty_ThrowsArgumentNullException()
    {
        var eo = new ExpandoObject();
        Should.Throw<ArgumentNullException>(() => eo.RemoveProperty("Ghost"));
    }

    // ─────────────────────────────────────────────────────────────────
    // ToDataTable
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDataTable_NullInput_ReturnsNull()
    {
        IEnumerable<ExpandoObject>? list = null;
        list.ToDataTable().ShouldBeNull();
    }

    [Fact]
    public void ToDataTable_EmptyList_ReturnsEmptyDataTable()
    {
        var list = Enumerable.Empty<ExpandoObject>();
        var table = list.ToDataTable();
        table.ShouldNotBeNull();
        table!.Rows.Count.ShouldBe(0);
        table.Columns.Count.ShouldBe(0);
    }

    [Fact]
    public void ToDataTable_WithObjects_ColumnsMatchPropertyNames()
    {
        var list = new List<ExpandoObject>();
        var obj1 = new ExpandoObject();
        obj1.AddProperty("Id", 1);
        obj1.AddProperty("Name", "Alice");
        list.Add(obj1);

        var table = list.ToDataTable()!;
        table.Columns.Contains("Id").ShouldBeTrue();
        table.Columns.Contains("Name").ShouldBeTrue();
    }

    [Fact]
    public void ToDataTable_WithObjects_RowValuesAreCorrect()
    {
        var list = new List<ExpandoObject>();
        var obj1 = new ExpandoObject();
        obj1.AddProperty("Id", 42);
        obj1.AddProperty("Name", "Bob");
        list.Add(obj1);

        var table = list.ToDataTable()!;
        table.Rows.Count.ShouldBe(1);
        // ToDataTable 将属性值以 object 存储，用 ToString() 比较即可
        table.Rows[0]["Id"].ToString().ShouldBe("42");
        table.Rows[0]["Name"].ShouldBe("Bob");
    }

    [Fact]
    public void ToDataTable_MultipleObjects_AllRowsPresent()
    {
        var list = new List<ExpandoObject>();
        for (var i = 1; i <= 3; i++)
        {
            var obj = new ExpandoObject();
            obj.AddProperty("Id", i);
            list.Add(obj);
        }

        var table = list.ToDataTable()!;
        table.Rows.Count.ShouldBe(3);
    }
}
