using System.Collections.Generic;

namespace BingUtilsUT.Samples;

public class NormalValueTypeClass
{
    public int Int16V1 { get; set; }
    public int Int16V2;
    public int? Int16V3 { get; set; }
    public int? Int16V4;

    public bool BooleanV1 { get; set; }
    public bool BooleanV2;

    public bool? BooleanV3 { get; set; }
    public bool? BooleanV4;

    public DateTime DateTimeV1 { get; set; }
    public DateTime DateTimeV2;

    public DateTime? DateTimeV3 { get; set; }
    public DateTime? DateTimeV4;

    public string Str { get; set; }

    public Int16Enum Int99V1 { get; set; }
    public Int16Enum Int99V2;

    public Int16Enum? Int99V3 { get; set; }
    public Int16Enum? Int99V4;

    public List<Int32Enum> Int32EnumV1 { get; set; }
    public List<Int32Enum> Int32EnumV2;

    public List<Int32Enum?> Int32EnumV3 { get; set; }
    public List<Int32Enum?> Int32EnumV4;

    public Dictionary<string, Int32Enum> Int32EnumD1 { get; set; }
    public Dictionary<string, Int32Enum> Int32EnumD2;

    public Int32Enum[] Int32EnumA1 { get; set; }
    public Int32Enum[] Int32EnumA2;

    public Int32Enum?[] Int32EnumA3 { get; set; }
    public Int32Enum?[] Int32EnumA4;
}