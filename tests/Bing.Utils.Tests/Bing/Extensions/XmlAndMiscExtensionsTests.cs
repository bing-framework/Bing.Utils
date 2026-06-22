using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Bing.Extensions;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Extensions;

// ──────────────────────────────────────────────────────────────────────────────
//  Helper types for XML serialization tests
// ──────────────────────────────────────────────────────────────────────────────

[XmlRoot("Person")]
public class XmlTestPerson
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
//  XmlExtensions tests  (string/XDocument → T, XmlNode ↔ XElement)
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="XmlExtensions"/> 扩展方法：
/// FromXml / Deserialize / ToXElement / ToXmlNode
/// </summary>
public class XmlExtensionsTests
{
    private const string PersonXml = "<Person><Name>Alice</Name><Age>30</Age></Person>";

    // ── FromXml ───────────────────────────────────────────────────────────────

    [Fact]
    public void FromXml_ValidXml_DeserializesObject()
    {
        var person = PersonXml.FromXml<XmlTestPerson>();
        person.ShouldNotBeNull();
        person.Name.ShouldBe("Alice");
        person.Age.ShouldBe(30);
    }

    [Fact]
    public void FromXml_DefaultValues_WhenFieldsMissing()
    {
        var p = "<Person></Person>".FromXml<XmlTestPerson>();
        p.ShouldNotBeNull();
        p.Name.ShouldBeNull();
        p.Age.ShouldBe(0);
    }

    // ── Deserialize (XDocument) ───────────────────────────────────────────────

    [Fact]
    public void Deserialize_XDocument_DeserializesObject()
    {
        var xdoc = XDocument.Parse(PersonXml);
        var person = xdoc.Deserialize<XmlTestPerson>();
        person.ShouldNotBeNull();
        person.Name.ShouldBe("Alice");
        person.Age.ShouldBe(30);
    }

    [Fact]
    public void Deserialize_XDocument_EmptyDocument_DefaultValues()
    {
        var xdoc = XDocument.Parse("<Person></Person>");
        var person = xdoc.Deserialize<XmlTestPerson>();
        person.ShouldNotBeNull();
        person.Age.ShouldBe(0);
    }

    // ── ToXElement / ToXmlNode ────────────────────────────────────────────────

    [Fact]
    public void ToXElement_FromXmlDocument_ReturnsXElementWithSameName()
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml("<root id=\"42\"><child/></root>");
        var el = xmlDoc.DocumentElement!.ToXElement();
        el.ShouldNotBeNull();
        el.Name.LocalName.ShouldBe("root");
    }

    [Fact]
    public void ToXElement_PreservesAttributes()
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml("<root id=\"42\"/>");
        var el = xmlDoc.DocumentElement!.ToXElement();
        el.ShouldNotBeNull();
        var idAttr = el.Attribute("id");
        idAttr.ShouldNotBeNull();
        idAttr!.Value.ShouldBe("42");
    }

    [Fact]
    public void ToXmlNode_FromXElement_ReturnsXmlDocumentWithSameName()
    {
        var xel = XElement.Parse("<root id=\"7\"><child/></root>");
        var node = (XmlDocument)xel.ToXmlNode();
        node.ShouldNotBeNull();
        node.DocumentElement!.Name.ShouldBe("root");
    }

    [Fact]
    public void ToXElement_ToXmlNode_RoundTrip_PreservesChildNodes()
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml("<root><child name=\"c1\"/></root>");
        var xel = xmlDoc.DocumentElement!.ToXElement();
        var backDoc = (XmlDocument)xel.ToXmlNode();
        backDoc.DocumentElement!.ChildNodes.Count.ShouldBe(1);
        backDoc.DocumentElement.ChildNodes[0]!.Name.ShouldBe("child");
    }
}

// ──────────────────────────────────────────────────────────────────────────────
//  XmlNodeExtensions tests
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="XmlNodeExtensions"/> 扩展方法：
/// CreateChildNode / CreateCDataSection / GetCdataSection /
/// GetAttribute / GetAttributeValueOrNull / SetAttribute
/// </summary>
public class XmlNodeExtensionsTests
{
    private static XmlDocument CreateDoc(string xml = "<root/>")
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return doc;
    }

    // ── CreateChildNode ───────────────────────────────────────────────────────

    [Fact]
    public void CreateChildNode_AppendsChildElement()
    {
        var doc = CreateDoc();
        var child = doc.DocumentElement!.CreateChildNode("item");
        child.ShouldNotBeNull();
        child.Name.ShouldBe("item");
        doc.DocumentElement.ChildNodes.Count.ShouldBe(1);
    }

    [Fact]
    public void CreateChildNode_MultipleChildren_AllAppended()
    {
        var doc = CreateDoc();
        doc.DocumentElement!.CreateChildNode("a");
        doc.DocumentElement.CreateChildNode("b");
        doc.DocumentElement.ChildNodes.Count.ShouldBe(2);
    }

    // ── CreateCDataSection / GetCdataSection ──────────────────────────────────

    [Fact]
    public void CreateCDataSection_AppendsSection()
    {
        var doc = CreateDoc();
        var cdata = doc.DocumentElement!.CreateCDataSection("hello cdata");
        cdata.ShouldNotBeNull();
    }

    [Fact]
    public void GetCdataSection_ReturnsCDataContent()
    {
        var doc = CreateDoc();
        doc.DocumentElement!.CreateCDataSection("my data");
        doc.DocumentElement.GetCdataSection().ShouldBe("my data");
    }

    [Fact]
    public void GetCdataSection_EmptyCData_ReturnsEmpty()
    {
        var doc = CreateDoc();
        doc.DocumentElement!.CreateCDataSection();
        doc.DocumentElement.GetCdataSection().ShouldBe(string.Empty);
    }

    [Fact]
    public void GetCdataSection_NoCData_ReturnsNull()
    {
        var doc = CreateDoc();
        doc.DocumentElement!.GetCdataSection().ShouldBeNull();
    }

    // ── GetAttribute ─────────────────────────────────────────────────────────

    [Fact]
    public void GetAttribute_ExistingAttribute_ReturnsValue()
    {
        var doc = CreateDoc("<root id=\"99\"/>");
        XmlNode node = doc.DocumentElement!;
        node.GetAttribute("id").ShouldBe("99");
    }

    [Fact]
    public void GetAttribute_MissingAttribute_ReturnsDefault()
    {
        var doc = CreateDoc("<root/>");
        XmlNode node = doc.DocumentElement!;
        node.GetAttribute("missing", "fallback").ShouldBe("fallback");
    }

    [Fact]
    public void GetAttribute_MissingAttribute_DefaultNull_ReturnsNull()
    {
        var doc = CreateDoc("<root/>");
        XmlNode node = doc.DocumentElement!;
        node.GetAttribute("missing").ShouldBeNull();
    }

    [Fact]
    public void GetAttribute_Typed_ExistingAttribute_ReturnsConvertedValue()
    {
        var doc = CreateDoc("<root count=\"5\"/>");
        XmlNode node = doc.DocumentElement!;
        node.GetAttribute<int>("count").ShouldBe(5);
    }

    [Fact]
    public void GetAttribute_Typed_MissingAttribute_ReturnsDefault()
    {
        var doc = CreateDoc("<root/>");
        XmlNode node = doc.DocumentElement!;
        node.GetAttribute<int>("missing", 42).ShouldBe(42);
    }

    // ── GetAttributeValueOrNull ───────────────────────────────────────────────

    [Fact]
    public void GetAttributeValueOrNull_ExistingAttribute_ReturnsValue()
    {
        var doc = CreateDoc("<root key=\"v\"/>");
        XmlNode node = doc.DocumentElement!;
        node.GetAttributeValueOrNull("key").ShouldBe("v");
    }

    [Fact]
    public void GetAttributeValueOrNull_MissingAttribute_ReturnsNull()
    {
        var doc = CreateDoc("<root key=\"v\"/>");
        XmlNode node = doc.DocumentElement!;
        node.GetAttributeValueOrNull("other").ShouldBeNull();
    }

    [Fact]
    public void GetAttributeValueOrNull_NoAttributes_ThrowsArgumentNullException()
    {
        var doc = new XmlDocument();
        doc.LoadXml("<root>text</root>");
        var textNode = doc.DocumentElement!.FirstChild!; // XmlText node, Attributes = null
        Should.Throw<ArgumentNullException>(() => textNode.GetAttributeValueOrNull("x"));
    }

    // ── SetAttribute ─────────────────────────────────────────────────────────

    [Fact]
    public void SetAttribute_NewAttribute_AddsAttribute()
    {
        var doc = CreateDoc();
        XmlNode node = doc.DocumentElement!;
        node.SetAttribute("lang", "zh-CN");
        node.GetAttribute("lang").ShouldBe("zh-CN");
    }

    [Fact]
    public void SetAttribute_ExistingAttribute_UpdatesValue()
    {
        var doc = CreateDoc("<root v=\"old\"/>");
        XmlNode node = doc.DocumentElement!;
        node.SetAttribute("v", "new");
        node.GetAttribute("v").ShouldBe("new");
    }

    [Fact]
    public void SetAttribute_ObjectOverload_ConvertsToString()
    {
        var doc = CreateDoc();
        XmlNode node = doc.DocumentElement!;
        node.SetAttribute("num", (object)123);
        node.GetAttribute("num").ShouldBe("123");
    }
}

// ──────────────────────────────────────────────────────────────────────────────
//  IdentityExtensions tests
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="IdentityExtensions"/> 扩展方法：
/// GetValue / GetValues / RemoveClaim
/// </summary>
public class IdentityExtensionsTests
{
    private static ClaimsIdentity BuildIdentity()
    {
        var claims = new List<Claim>
        {
            new Claim("sub", "user1"),
            new Claim("age", "25"),
            new Claim("role", "admin"),
            new Claim("role", "editor"),
        };
        return new ClaimsIdentity(claims, "test");
    }

    // ── GetValue(string) ──────────────────────────────────────────────────────

    [Fact]
    public void GetValue_ExistingClaim_ReturnsValue()
    {
        var identity = BuildIdentity();
        identity.GetValue("sub").ShouldBe("user1");
    }

    [Fact]
    public void GetValue_MissingClaim_ReturnsEmpty()
    {
        var identity = BuildIdentity();
        identity.GetValue("nonexistent").ShouldBe(string.Empty);
    }

    // ── GetValue<T>(string) ───────────────────────────────────────────────────

    [Fact]
    public void GetValue_Typed_ReturnsConvertedValue()
    {
        var identity = BuildIdentity();
        identity.GetValue<int>("age").ShouldBe(25);
    }

    [Fact]
    public void GetValue_Typed_MissingClaim_ReturnsDefault()
    {
        var identity = BuildIdentity();
        identity.GetValue<int>("missing").ShouldBe(0);
    }

    // ── GetValues(string) ─────────────────────────────────────────────────────

    [Fact]
    public void GetValues_MultiValueClaim_ReturnsAll()
    {
        var identity = BuildIdentity();
        var roles = identity.GetValues("role");
        roles.ShouldNotBeNull();
        roles.Length.ShouldBe(2);
        roles.ShouldContain("admin");
        roles.ShouldContain("editor");
    }

    [Fact]
    public void GetValues_MissingClaim_ReturnsEmptyCollection()
    {
        var identity = BuildIdentity();
        var values = identity.GetValues("nope");
        values.ShouldNotBeNull();
        values.Length.ShouldBe(0);
    }

    // ── RemoveClaim ───────────────────────────────────────────────────────────

    [Fact]
    public void RemoveClaim_ExistingClaim_RemovesFirstMatch()
    {
        var identity = BuildIdentity();
        identity.RemoveClaim("role");
        // Two "role" claims: first one removed, second remains
        var remaining = identity.GetValues("role");
        remaining.Length.ShouldBe(1);
    }

    [Fact]
    public void RemoveClaim_OnlyClaim_RemovesIt()
    {
        var identity = BuildIdentity();
        identity.RemoveClaim("sub");
        identity.GetValue("sub").ShouldBe(string.Empty);
    }

    [Fact]
    public void RemoveClaim_MissingClaim_NoThrow()
    {
        var identity = BuildIdentity();
        Should.NotThrow(() => identity.RemoveClaim("nonexistent"));
    }
}

// ──────────────────────────────────────────────────────────────────────────────
//  ExpressionCopier tests  (ObjectExtensions.ExpressionCopy<T>)
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="ObjectExtensions.ExpressionCopy{T}"/> 表达式浅拷贝
/// </summary>
public class ExpressionCopierTests
{
    private class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public List<string> Tags { get; set; }
    }

    [Fact]
    public void ExpressionCopy_CopiesPropertyValues()
    {
        var src = new Person { Name = "Bob", Age = 30, Tags = new List<string> { "a" } };
        var copy = src.ExpressionCopy();
        copy.Name.ShouldBe("Bob");
        copy.Age.ShouldBe(30);
    }

    [Fact]
    public void ExpressionCopy_ReturnsDifferentInstance()
    {
        var src = new Person { Name = "Alice", Age = 25 };
        var copy = src.ExpressionCopy();
        ReferenceEquals(src, copy).ShouldBeFalse();
    }

    [Fact]
    public void ExpressionCopy_ShallowCopy_SharedReference()
    {
        var tags = new List<string> { "x", "y" };
        var src = new Person { Tags = tags };
        var copy = src.ExpressionCopy();
        // Shallow copy: Tags reference is the same object
        ReferenceEquals(src.Tags, copy.Tags).ShouldBeTrue();
    }

    [Fact]
    public void ExpressionCopy_MutatingCopy_DoesNotAffectSource()
    {
        var src = new Person { Name = "Carol", Age = 20 };
        var copy = src.ExpressionCopy();
        copy.Name = "Dave";
        copy.Age = 99;
        src.Name.ShouldBe("Carol");
        src.Age.ShouldBe(20);
    }
}
