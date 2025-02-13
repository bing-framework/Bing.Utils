namespace BingUtilsUT.Samples;

[Description("NormalClassOne")]
[DisplayName("NormalClassDisplayNameOne")]
[Display(Name = "NormalDescription", Description = "NormalClassDisplayOne")]
public class NormalDescriptionClass
{
    [Description("PropertyOne")]
    [DisplayName("PropertyDisplayNameOne")]
    [Display(Name = "PropertyOne", Description = "PropertyDisplayOne")]
    public string PropertyOne { get; set; }

    [Description("FieldOne")]
    [Display(Name = "FieldOne", Description = "FieldDisplayOne")]
    public string FieldOne;

    [Description("MethodOne")]
    [DisplayName("MethodDisplayNameOne")]
    [Display(Name = "MethodOne", Description = "MethodDisplayOne")]
    public string MethodOne(
        [Description("ArgOne")] [Display(Name = "ParamName", Description = "ParamDesc")]
        string argOne
    ) => string.Empty;
}

public class NormalDescriptionWrapper : NormalDescriptionClass { }

[DisplayName("NormalClassDisplayNameTwo")]
[Display(Name = "NormalDescription", Description = "NormalClassDisplayTwo")]
public class NormalDescriptionOrClass
{
    [DisplayName("PropertyDisplayNameTwo")]
    [Display(Name = "PropertyTwo", Description = "PropertyDisplayTwo")]
    public string PropertyTwo { get; set; }

    [Display(Name = "FieldTwo", Description = "FieldDisplayTwo")]
    public string FieldTwo;

    [DisplayName("MethodDisplayNameTwo")]
    [Display(Name = "MethodTwo", Description = "MethodDisplayTwo")]
    public string MethodTwo(
        [Display(Name = "ParamName", Description = "ParamDesc")]
        string argTwo
    ) => string.Empty;
}

[Description("NormalClassThree")]
public class NormalDisplayNameOrClass
{
    [Description("PropertyThree")] public string PropertyThree { get; set; }

    [Description("FieldThree")]
    public string FieldThree;

    [Description("MethodThree")]
    public string MethodThree([Description("ArgThree")] string argThree) => string.Empty;
}