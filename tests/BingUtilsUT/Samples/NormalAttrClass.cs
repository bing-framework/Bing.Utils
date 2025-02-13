namespace BingUtilsUT.Samples;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ModelOneAttribute : Attribute { }

[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
public class ModelTwoAttribute : Attribute { }

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ModelThreeAttribute : Attribute { }
    
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ModelFourAttribute : Attribute { }

[ModelOne]
[ModelOne]
[ModelThree]
public class NormalWithAttrClass
{
    [ModelOne] [ModelTwo] public string Nice { get; set; }
        
    public int Index { get; set; }

    [ModelOne]
    [ModelTwo]
    public string Good;

    [ModelOne]
    [ModelTwo]
    public string GetAwesome()
    {
        return string.Empty;
    }

    [ModelOne]
    [ModelTwo]
    public NormalWithAttrClass() { }

    public NormalWithAttrClass([ModelTwo] [ModelThree] string value)
    {
        Nice = value;
    }

    public NormalWithAttrClass(string value, int index)
    {
        Nice = value;
        Index = index;
    }
}

public class NormalWithoutAttrClass
{
    public string Nice { get; set; }

    public string Good;

    public string GetAwesome()
    {
        return string.Empty;
    }

    public NormalWithoutAttrClass() { }

    public NormalWithoutAttrClass(string value)
    {
        Nice = value;
    }
}

public class NormalWithAttrClassWrapper : NormalWithAttrClass
{
    public NormalWithAttrClassWrapper() : base() { }

    public NormalWithAttrClassWrapper(string value) : base(value) { }
}

[ModelOne]
[ModelOne]
[ModelTwo]
[ModelThree]
public class NormalWithAttrClass2 { }

[ModelFour]
public class NormalWithAttrClassWrapper2 : NormalWithAttrClass2 { }