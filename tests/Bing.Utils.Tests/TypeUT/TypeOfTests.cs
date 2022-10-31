using System.Collections;
using Bing.Reflection;
using Bing.Utils.Tests.Samples;
using System.Linq;

namespace Bing.Utils.Tests.TypeUT;

[Trait("TypeUT ", "TypeOfTests")]
public class TypeOfTests
{
    /// <summary>
    /// 测试 - 获取给定类型 - 单个值类型
    /// </summary>
    [Fact]
    public void Test_Of_SingleValueType()
    {
        Types.Of<int>().ShouldBe(typeof(int));
        Types.Of<int>(TypeOfOptions.Underlying).ShouldBe(typeof(int));
        Types.Of<int?>().ShouldBe(typeof(int?));
        Types.Of<int?>(TypeOfOptions.Underlying).ShouldBe(typeof(int));
    }

    /// <summary>
    /// 测试 - 获取给定类型 - 单个枚举类型
    /// </summary>
    [Fact]
    public void Test_Of_SingleEnumType()
    {
        Types.Of<Int16Enum>().ShouldBe(typeof(Int16Enum));
        Types.Of<Int16Enum>(TypeOfOptions.Underlying).ShouldBe(typeof(Int16Enum));
        Types.Of<Int16Enum?>().ShouldBe(typeof(Int16Enum?));
        Types.Of<Int16Enum?>(TypeOfOptions.Underlying).ShouldBe(typeof(Int16Enum));
    }

    /// <summary>
    /// 测试 - 获取给定类型 - 多个值类型
    /// </summary>
    [Fact]
    public void Test_Of_MultiValueType()
    {
        int? a = 0;
        long? b = 1;
        string c = "2";
        DateTime d = DateTime.Now;
        Exception e = new ArgumentNullException();

        var types = Types.Of(new object[] { a, b, c, d, e }).ToList();

        types.Count.ShouldBe(5);
        types[0].ShouldBe(typeof(int));
        types[1].ShouldBe(typeof(long));
        types[2].ShouldBe(typeof(string));
        types[3].ShouldBe(typeof(DateTime));
        types[4].ShouldBe(typeof(ArgumentNullException));

        types = Types.Of(new object[] { a, b, c, d, e }, TypeOfOptions.Underlying).ToList();

        types.Count.ShouldBe(5);
        types[0].ShouldBe(typeof(int));
        types[1].ShouldBe(typeof(long));
        types[2].ShouldBe(typeof(string));
        types[3].ShouldBe(typeof(DateTime));
        types[4].ShouldBe(typeof(ArgumentNullException));
    }

    /// <summary>
    /// 测试 - 获取给定类型 - 多个值类型 可能为空
    /// </summary>
    [Fact]
    public void Test_Of_MultiValueTypeWithNull()
    {
        int? a = 0;
        long? b = 1;
        string c = "2";
        DateTime d = DateTime.Now;
        Exception e = new ArgumentNullException();
        object f = null;

        // null 将会被过滤
        var types = Types.Of(new object[] {a, b, c, d, e, f}).ToList();

        types.Count.ShouldBe(5);
        types[0].ShouldBe(typeof(int));
        types[1].ShouldBe(typeof(long));
        types[2].ShouldBe(typeof(string));
        types[3].ShouldBe(typeof(DateTime));
        types[4].ShouldBe(typeof(ArgumentNullException));
        //types[5].ShouldBeNull();

        types = Types.Of(new object[] {a, b, c, d, e, f}, TypeOfOptions.Underlying).ToList();

        types.Count.ShouldBe(5);
        types[0].ShouldBe(typeof(int));
        types[1].ShouldBe(typeof(long));
        types[2].ShouldBe(typeof(string));
        types[3].ShouldBe(typeof(DateTime));
        types[4].ShouldBe(typeof(ArgumentNullException));
        //types[5].ShouldBeNull();
    }

    /// <summary>
    /// 测试 - 获取给定类型 - 多个枚举类型 可能为空
    /// </summary>
    [Fact]
    public void Test_Of_MultiValueTypeWithEnumAndNull()
    {
        int? a = 0;
        long? b = 1;
        string c = "2";
        DateTime d = DateTime.Now;
        Exception e = new ArgumentNullException();
        object f = null;
        Int16Enum g = Int16Enum.A;
        Int32Enum? h = null;
        Int64Enum? i = Int64Enum.C;

        // null 将会被过滤
        var types = Types.Of(new object[] {a, b, c, d, e, f, g, h, i}).ToList();

        types.Count.ShouldBe(7);
        types[0].ShouldBe(typeof(int));
        types[1].ShouldBe(typeof(long));
        types[2].ShouldBe(typeof(string));
        types[3].ShouldBe(typeof(DateTime));
        types[4].ShouldBe(typeof(ArgumentNullException));
        //types[5].ShouldBeNull();
        types[5].ShouldBe(typeof(Int16Enum));
        //types[7].ShouldBeNull();
        types[6].ShouldBe(typeof(Int64Enum));

        types = Types.Of(new object[] {a, b, c, d, e, f, g, h, i}, TypeOfOptions.Underlying).ToList();

        types.Count.ShouldBe(7);
        types[0].ShouldBe(typeof(int));
        types[1].ShouldBe(typeof(long));
        types[2].ShouldBe(typeof(string));
        types[3].ShouldBe(typeof(DateTime));
        types[4].ShouldBe(typeof(ArgumentNullException));
        //types[5].ShouldBeNull();
        types[5].ShouldBe(typeof(Int16Enum));
        //types[7].ShouldBeNull();
        types[6].ShouldBe(typeof(Int64Enum));
    }

    /// <summary>
    /// 测试 - 获取给定类型 - 多个值类型 泛型
    /// </summary>
    [Fact]
    public void Test_Of_MultiValueTypeViaGeneric()
    {
        var ts01 = Types.Of<int>();
        var ts02 = Types.Of<int, int?>().ToList();
        var ts03 = Types.Of<int, int?, long?>().ToList();
        var ts04 = Types.Of<int, int?, long?, Int16Enum>().ToList();
        var ts05 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions>().ToList();
        var ts06 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string>().ToList();
        var ts07 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type>().ToList();
        var ts08 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?>().ToList();
        var ts09 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime>().ToList();
        var ts10 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?>().ToList();
        var ts11 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable>().ToList();
        var ts12 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int>().ToList();
        var ts13 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int, Guid?>().ToList();
        var ts14 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int, Guid?, long>().ToList();
        var ts15 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int, Guid?, long, char[]>().ToList();
        var ts16 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int, Guid?, long, char[], object>().ToList();

        ts01.ShouldBe(typeof(int));

        ts02.Count.ShouldBe(2);
        ts02[0].ShouldBe(typeof(int));
        ts02[1].ShouldBe(typeof(int?));

        ts03.Count.ShouldBe(3);
        ts03[0].ShouldBe(typeof(int));
        ts03[1].ShouldBe(typeof(int?));
        ts03[2].ShouldBe(typeof(long?));

        ts04.Count.ShouldBe(4);
        ts04[0].ShouldBe(typeof(int));
        ts04[1].ShouldBe(typeof(int?));
        ts04[2].ShouldBe(typeof(long?));
        ts04[3].ShouldBe(typeof(Int16Enum));

        ts05.Count.ShouldBe(5);
        ts05[0].ShouldBe(typeof(int));
        ts05[1].ShouldBe(typeof(int?));
        ts05[2].ShouldBe(typeof(long?));
        ts05[3].ShouldBe(typeof(Int16Enum));
        ts05[4].ShouldBe(typeof(StringSplitOptions));

        ts06.Count.ShouldBe(6);
        ts06[0].ShouldBe(typeof(int));
        ts06[1].ShouldBe(typeof(int?));
        ts06[2].ShouldBe(typeof(long?));
        ts06[3].ShouldBe(typeof(Int16Enum));
        ts06[4].ShouldBe(typeof(StringSplitOptions));
        ts06[5].ShouldBe(typeof(string));

        ts07.Count.ShouldBe(7);
        ts07[0].ShouldBe(typeof(int));
        ts07[1].ShouldBe(typeof(int?));
        ts07[2].ShouldBe(typeof(long?));
        ts07[3].ShouldBe(typeof(Int16Enum));
        ts07[4].ShouldBe(typeof(StringSplitOptions));
        ts07[5].ShouldBe(typeof(string));
        ts07[6].ShouldBe(typeof(Type));

        ts08.Count.ShouldBe(8);
        ts08[0].ShouldBe(typeof(int));
        ts08[1].ShouldBe(typeof(int?));
        ts08[2].ShouldBe(typeof(long?));
        ts08[3].ShouldBe(typeof(Int16Enum));
        ts08[4].ShouldBe(typeof(StringSplitOptions));
        ts08[5].ShouldBe(typeof(string));
        ts08[6].ShouldBe(typeof(Type));
        ts08[7].ShouldBe(typeof(Int64Enum?));

        ts09.Count.ShouldBe(9);
        ts09[0].ShouldBe(typeof(int));
        ts09[1].ShouldBe(typeof(int?));
        ts09[2].ShouldBe(typeof(long?));
        ts09[3].ShouldBe(typeof(Int16Enum));
        ts09[4].ShouldBe(typeof(StringSplitOptions));
        ts09[5].ShouldBe(typeof(string));
        ts09[6].ShouldBe(typeof(Type));
        ts09[7].ShouldBe(typeof(Int64Enum?));
        ts09[8].ShouldBe(typeof(DateTime));

        ts10.Count.ShouldBe(10);
        ts10[0].ShouldBe(typeof(int));
        ts10[1].ShouldBe(typeof(int?));
        ts10[2].ShouldBe(typeof(long?));
        ts10[3].ShouldBe(typeof(Int16Enum));
        ts10[4].ShouldBe(typeof(StringSplitOptions));
        ts10[5].ShouldBe(typeof(string));
        ts10[6].ShouldBe(typeof(Type));
        ts10[7].ShouldBe(typeof(Int64Enum?));
        ts10[8].ShouldBe(typeof(DateTime));
        ts10[9].ShouldBe(typeof(char?));

        ts11.Count.ShouldBe(11);
        ts11[0].ShouldBe(typeof(int));
        ts11[1].ShouldBe(typeof(int?));
        ts11[2].ShouldBe(typeof(long?));
        ts11[3].ShouldBe(typeof(Int16Enum));
        ts11[4].ShouldBe(typeof(StringSplitOptions));
        ts11[5].ShouldBe(typeof(string));
        ts11[6].ShouldBe(typeof(Type));
        ts11[7].ShouldBe(typeof(Int64Enum?));
        ts11[8].ShouldBe(typeof(DateTime));
        ts11[9].ShouldBe(typeof(char?));
        ts11[10].ShouldBe(typeof(IEnumerable));

        ts12.Count.ShouldBe(12);
        ts12[0].ShouldBe(typeof(int));
        ts12[1].ShouldBe(typeof(int?));
        ts12[2].ShouldBe(typeof(long?));
        ts12[3].ShouldBe(typeof(Int16Enum));
        ts12[4].ShouldBe(typeof(StringSplitOptions));
        ts12[5].ShouldBe(typeof(string));
        ts12[6].ShouldBe(typeof(Type));
        ts12[7].ShouldBe(typeof(Int64Enum?));
        ts12[8].ShouldBe(typeof(DateTime));
        ts12[9].ShouldBe(typeof(char?));
        ts12[10].ShouldBe(typeof(IEnumerable));
        ts12[11].ShouldBe(typeof(int));

        ts13.Count.ShouldBe(13);
        ts13[0].ShouldBe(typeof(int));
        ts13[1].ShouldBe(typeof(int?));
        ts13[2].ShouldBe(typeof(long?));
        ts13[3].ShouldBe(typeof(Int16Enum));
        ts13[4].ShouldBe(typeof(StringSplitOptions));
        ts13[5].ShouldBe(typeof(string));
        ts13[6].ShouldBe(typeof(Type));
        ts13[7].ShouldBe(typeof(Int64Enum?));
        ts13[8].ShouldBe(typeof(DateTime));
        ts13[9].ShouldBe(typeof(char?));
        ts13[10].ShouldBe(typeof(IEnumerable));
        ts13[11].ShouldBe(typeof(int));
        ts13[12].ShouldBe(typeof(Guid?));

        ts14.Count.ShouldBe(14);
        ts14[0].ShouldBe(typeof(int));
        ts14[1].ShouldBe(typeof(int?));
        ts14[2].ShouldBe(typeof(long?));
        ts14[3].ShouldBe(typeof(Int16Enum));
        ts14[4].ShouldBe(typeof(StringSplitOptions));
        ts14[5].ShouldBe(typeof(string));
        ts14[6].ShouldBe(typeof(Type));
        ts14[7].ShouldBe(typeof(Int64Enum?));
        ts14[8].ShouldBe(typeof(DateTime));
        ts14[9].ShouldBe(typeof(char?));
        ts14[10].ShouldBe(typeof(IEnumerable));
        ts14[11].ShouldBe(typeof(int));
        ts14[12].ShouldBe(typeof(Guid?));
        ts14[13].ShouldBe(typeof(long));

        ts15.Count.ShouldBe(15);
        ts15[0].ShouldBe(typeof(int));
        ts15[1].ShouldBe(typeof(int?));
        ts15[2].ShouldBe(typeof(long?));
        ts15[3].ShouldBe(typeof(Int16Enum));
        ts15[4].ShouldBe(typeof(StringSplitOptions));
        ts15[5].ShouldBe(typeof(string));
        ts15[6].ShouldBe(typeof(Type));
        ts15[7].ShouldBe(typeof(Int64Enum?));
        ts15[8].ShouldBe(typeof(DateTime));
        ts15[9].ShouldBe(typeof(char?));
        ts15[10].ShouldBe(typeof(IEnumerable));
        ts15[11].ShouldBe(typeof(int));
        ts15[12].ShouldBe(typeof(Guid?));
        ts15[13].ShouldBe(typeof(long));
        ts15[14].ShouldBe(typeof(char[]));

        ts16.Count.ShouldBe(16);
        ts16[0].ShouldBe(typeof(int));
        ts16[1].ShouldBe(typeof(int?));
        ts16[2].ShouldBe(typeof(long?));
        ts16[3].ShouldBe(typeof(Int16Enum));
        ts16[4].ShouldBe(typeof(StringSplitOptions));
        ts16[5].ShouldBe(typeof(string));
        ts16[6].ShouldBe(typeof(Type));
        ts16[7].ShouldBe(typeof(Int64Enum?));
        ts16[8].ShouldBe(typeof(DateTime));
        ts16[9].ShouldBe(typeof(char?));
        ts16[10].ShouldBe(typeof(IEnumerable));
        ts16[11].ShouldBe(typeof(int));
        ts16[12].ShouldBe(typeof(Guid?));
        ts16[13].ShouldBe(typeof(long));
        ts16[14].ShouldBe(typeof(char[]));
        ts16[15].ShouldBe(typeof(object));
    }

    /// <summary>
    /// 测试 - 获取给定类型 - 多个值类型 泛型 可空
    /// </summary>
    [Fact]
    public void Test_Of_MultiValueTypeViaGenericWithOfOptions()
    {
        var ts01 = Types.Of<int>(TypeOfOptions.Underlying);
        var ts02 = Types.Of<int, int?>(TypeOfOptions.Underlying).ToList();
        var ts03 = Types.Of<int, int?, long?>(TypeOfOptions.Underlying).ToList();
        var ts04 = Types.Of<int, int?, long?, Int16Enum>(TypeOfOptions.Underlying).ToList();
        var ts05 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions>(TypeOfOptions.Underlying).ToList();
        var ts06 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string>(TypeOfOptions.Underlying).ToList();
        var ts07 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type>(TypeOfOptions.Underlying).ToList();
        var ts08 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?>(TypeOfOptions.Underlying).ToList();
        var ts09 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime>(TypeOfOptions.Underlying).ToList();
        var ts10 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?>(TypeOfOptions.Underlying).ToList();
        var ts11 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable>(TypeOfOptions.Underlying).ToList();
        var ts12 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int>(TypeOfOptions.Underlying).ToList();
        var ts13 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int, Guid?>(TypeOfOptions.Underlying).ToList();
        var ts14 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int, Guid?, long>(TypeOfOptions.Underlying).ToList();
        var ts15 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int, Guid?, long, char[]>(TypeOfOptions.Underlying).ToList();
        var ts16 = Types.Of<int, int?, long?, Int16Enum, StringSplitOptions, string, Type, Int64Enum?, DateTime, char?, IEnumerable, int, Guid?, long, char[], object>(TypeOfOptions.Underlying).ToList();
            
            
        ts01.ShouldBe(typeof(int));

        ts02.Count.ShouldBe(2);
        ts02[0].ShouldBe(typeof(int));
        ts02[1].ShouldBe(typeof(int));

        ts03.Count.ShouldBe(3);
        ts03[0].ShouldBe(typeof(int));
        ts03[1].ShouldBe(typeof(int));
        ts03[2].ShouldBe(typeof(long));

        ts04.Count.ShouldBe(4);
        ts04[0].ShouldBe(typeof(int));
        ts04[1].ShouldBe(typeof(int));
        ts04[2].ShouldBe(typeof(long));
        ts04[3].ShouldBe(typeof(Int16Enum));

        ts05.Count.ShouldBe(5);
        ts05[0].ShouldBe(typeof(int));
        ts05[1].ShouldBe(typeof(int));
        ts05[2].ShouldBe(typeof(long));
        ts05[3].ShouldBe(typeof(Int16Enum));
        ts05[4].ShouldBe(typeof(StringSplitOptions));

        ts06.Count.ShouldBe(6);
        ts06[0].ShouldBe(typeof(int));
        ts06[1].ShouldBe(typeof(int));
        ts06[2].ShouldBe(typeof(long));
        ts06[3].ShouldBe(typeof(Int16Enum));
        ts06[4].ShouldBe(typeof(StringSplitOptions));
        ts06[5].ShouldBe(typeof(string));

        ts07.Count.ShouldBe(7);
        ts07[0].ShouldBe(typeof(int));
        ts07[1].ShouldBe(typeof(int));
        ts07[2].ShouldBe(typeof(long));
        ts07[3].ShouldBe(typeof(Int16Enum));
        ts07[4].ShouldBe(typeof(StringSplitOptions));
        ts07[5].ShouldBe(typeof(string));
        ts07[6].ShouldBe(typeof(Type));

        ts08.Count.ShouldBe(8);
        ts08[0].ShouldBe(typeof(int));
        ts08[1].ShouldBe(typeof(int));
        ts08[2].ShouldBe(typeof(long));
        ts08[3].ShouldBe(typeof(Int16Enum));
        ts08[4].ShouldBe(typeof(StringSplitOptions));
        ts08[5].ShouldBe(typeof(string));
        ts08[6].ShouldBe(typeof(Type));
        ts08[7].ShouldBe(typeof(Int64Enum));

        ts09.Count.ShouldBe(9);
        ts09[0].ShouldBe(typeof(int));
        ts09[1].ShouldBe(typeof(int));
        ts09[2].ShouldBe(typeof(long));
        ts09[3].ShouldBe(typeof(Int16Enum));
        ts09[4].ShouldBe(typeof(StringSplitOptions));
        ts09[5].ShouldBe(typeof(string));
        ts09[6].ShouldBe(typeof(Type));
        ts09[7].ShouldBe(typeof(Int64Enum));
        ts09[8].ShouldBe(typeof(DateTime));

        ts10.Count.ShouldBe(10);
        ts10[0].ShouldBe(typeof(int));
        ts10[1].ShouldBe(typeof(int));
        ts10[2].ShouldBe(typeof(long));
        ts10[3].ShouldBe(typeof(Int16Enum));
        ts10[4].ShouldBe(typeof(StringSplitOptions));
        ts10[5].ShouldBe(typeof(string));
        ts10[6].ShouldBe(typeof(Type));
        ts10[7].ShouldBe(typeof(Int64Enum));
        ts10[8].ShouldBe(typeof(DateTime));
        ts10[9].ShouldBe(typeof(char));

        ts11.Count.ShouldBe(11);
        ts11[0].ShouldBe(typeof(int));
        ts11[1].ShouldBe(typeof(int));
        ts11[2].ShouldBe(typeof(long));
        ts11[3].ShouldBe(typeof(Int16Enum));
        ts11[4].ShouldBe(typeof(StringSplitOptions));
        ts11[5].ShouldBe(typeof(string));
        ts11[6].ShouldBe(typeof(Type));
        ts11[7].ShouldBe(typeof(Int64Enum));
        ts11[8].ShouldBe(typeof(DateTime));
        ts11[9].ShouldBe(typeof(char));
        ts11[10].ShouldBe(typeof(IEnumerable));

        ts12.Count.ShouldBe(12);
        ts12[0].ShouldBe(typeof(int));
        ts12[1].ShouldBe(typeof(int));
        ts12[2].ShouldBe(typeof(long));
        ts12[3].ShouldBe(typeof(Int16Enum));
        ts12[4].ShouldBe(typeof(StringSplitOptions));
        ts12[5].ShouldBe(typeof(string));
        ts12[6].ShouldBe(typeof(Type));
        ts12[7].ShouldBe(typeof(Int64Enum));
        ts12[8].ShouldBe(typeof(DateTime));
        ts12[9].ShouldBe(typeof(char));
        ts12[10].ShouldBe(typeof(IEnumerable));
        ts12[11].ShouldBe(typeof(int));

        ts13.Count.ShouldBe(13);
        ts13[0].ShouldBe(typeof(int));
        ts13[1].ShouldBe(typeof(int));
        ts13[2].ShouldBe(typeof(long));
        ts13[3].ShouldBe(typeof(Int16Enum));
        ts13[4].ShouldBe(typeof(StringSplitOptions));
        ts13[5].ShouldBe(typeof(string));
        ts13[6].ShouldBe(typeof(Type));
        ts13[7].ShouldBe(typeof(Int64Enum));
        ts13[8].ShouldBe(typeof(DateTime));
        ts13[9].ShouldBe(typeof(char));
        ts13[10].ShouldBe(typeof(IEnumerable));
        ts13[11].ShouldBe(typeof(int));
        ts13[12].ShouldBe(typeof(Guid));

        ts14.Count.ShouldBe(14);
        ts14[0].ShouldBe(typeof(int));
        ts14[1].ShouldBe(typeof(int));
        ts14[2].ShouldBe(typeof(long));
        ts14[3].ShouldBe(typeof(Int16Enum));
        ts14[4].ShouldBe(typeof(StringSplitOptions));
        ts14[5].ShouldBe(typeof(string));
        ts14[6].ShouldBe(typeof(Type));
        ts14[7].ShouldBe(typeof(Int64Enum));
        ts14[8].ShouldBe(typeof(DateTime));
        ts14[9].ShouldBe(typeof(char));
        ts14[10].ShouldBe(typeof(IEnumerable));
        ts14[11].ShouldBe(typeof(int));
        ts14[12].ShouldBe(typeof(Guid));
        ts14[13].ShouldBe(typeof(long));

        ts15.Count.ShouldBe(15);
        ts15[0].ShouldBe(typeof(int));
        ts15[1].ShouldBe(typeof(int));
        ts15[2].ShouldBe(typeof(long));
        ts15[3].ShouldBe(typeof(Int16Enum));
        ts15[4].ShouldBe(typeof(StringSplitOptions));
        ts15[5].ShouldBe(typeof(string));
        ts15[6].ShouldBe(typeof(Type));
        ts15[7].ShouldBe(typeof(Int64Enum));
        ts15[8].ShouldBe(typeof(DateTime));
        ts15[9].ShouldBe(typeof(char));
        ts15[10].ShouldBe(typeof(IEnumerable));
        ts15[11].ShouldBe(typeof(int));
        ts15[12].ShouldBe(typeof(Guid));
        ts15[13].ShouldBe(typeof(long));
        ts15[14].ShouldBe(typeof(char[]));

        ts16.Count.ShouldBe(16);
        ts16[0].ShouldBe(typeof(int));
        ts16[1].ShouldBe(typeof(int));
        ts16[2].ShouldBe(typeof(long));
        ts16[3].ShouldBe(typeof(Int16Enum));
        ts16[4].ShouldBe(typeof(StringSplitOptions));
        ts16[5].ShouldBe(typeof(string));
        ts16[6].ShouldBe(typeof(Type));
        ts16[7].ShouldBe(typeof(Int64Enum));
        ts16[8].ShouldBe(typeof(DateTime));
        ts16[9].ShouldBe(typeof(char));
        ts16[10].ShouldBe(typeof(IEnumerable));
        ts16[11].ShouldBe(typeof(int));
        ts16[12].ShouldBe(typeof(Guid));
        ts16[13].ShouldBe(typeof(long));
        ts16[14].ShouldBe(typeof(char[]));
        ts16[15].ShouldBe(typeof(object));
    }
}