using System.Collections.Generic;
using System.Xml;

namespace Bing.Utils.Comments.Tests
{
    /// <summary>
    /// 测试结构化注释模型。
    /// </summary>
    public class CsCommentsModelTest
    {
        /// <summary>
        /// 测试目的：验证 value、未知元素、原始 XML 和异常 cref 均被保留。
        /// </summary>
        [Fact]
        public void CsComments_XmlNode_PreservesValueAndRawElements()
        {
            // Arrange
            var document = new XmlDocument();
            document.LoadXml("<member><value>属性值</value><permission cref=\"T:System.Security.PermissionSet\">权限。</permission><exception cref=\"T:System.InvalidOperationException\">异常。</exception></member>");

            // Act
            var comments = new CsComments(document.DocumentElement);

            // Assert
            Assert.Equal("属性值", comments.Summary);
            Assert.Equal("属性值", comments.Value);
            Assert.Contains("<permission", comments.RawXml);
            Assert.Equal(new[] { "value", "permission", "exception" },
                comments.Elements.Select(element => element.Name).ToArray());
            Assert.Equal("System.Security.PermissionSet", comments.Elements[1].GetAttribute("cref").Substring(2));
            Assert.Equal("System.InvalidOperationException", comments.Exception[0].Cref);
            Assert.Equal("T:System.InvalidOperationException", comments.Exception[0].RawCref);
            Assert.Empty(comments.UnresolvedInheritdoc);
        }

        /// <summary>
        /// 测试目的：验证没有标准前缀的异常 cref 不会被错误截断。
        /// </summary>
        [Fact]
        public void CsCommentsException_UnprefixedCref_PreservesOriginalValue()
        {
            // Arrange
            var document = new XmlDocument();
            document.LoadXml("<member><exception cref=\"Invalid cref\">说明。</exception></member>");

            // Act
            var comments = new CsComments(document.DocumentElement);

            // Assert
            Assert.Equal("Invalid cref", comments.Exception[0].Cref);
            Assert.Equal("Invalid cref", comments.Exception[0].RawCref);
        }

        /// <summary>
        /// 测试目的：验证混合 XML 节点的类型、顺序、命名空间和原始内容均被保留。
        /// </summary>
        [Fact]
        public void CsCommentElement_MixedNodes_PreservesNodeOrderAndContent()
        {
            // Arrange
            var document = new XmlDocument();
            document.LoadXml("<member><summary xmlns:doc=\"urn:test\">前<![CDATA[CDATA内容]]><!--注释--><?note value?><doc:mark code=\"1\">标签</doc:mark>后</summary></member>");

            // Act
            var element = new CsComments(document.DocumentElement).Elements[0];

            // Assert
            Assert.Equal(new[]
            {
                XmlNodeType.Text,
                XmlNodeType.CDATA,
                XmlNodeType.Comment,
                XmlNodeType.ProcessingInstruction,
                XmlNodeType.Element,
                XmlNodeType.Text
            }, element.Nodes.Select(node => node.NodeType).ToArray());
            Assert.Equal("CDATA内容", element.Nodes[1].Text);
            Assert.Equal("注释", element.Nodes[2].Text);
            Assert.Equal("value", element.Nodes[3].Text);
            Assert.Equal("doc:mark", element.Nodes[4].Name);
            Assert.Equal("1", element.Nodes[4].GetAttribute("code"));
            Assert.Contains("<![CDATA[CDATA内容]]>", element.InnerXml);
        }

        /// <summary>
        /// 测试目的：验证参数集合对重复名称、空名称和 @ 前缀保持兼容处理。
        /// </summary>
        [Fact]
        public void CsCommentsParamCollection_DuplicateAndAtNames_UsesNormalizedLastValue()
        {
            // Arrange
            var document = new XmlDocument();
            document.LoadXml("<member><param name=\"value\">第一项</param><param name=\"@value\">第二项</param><param>无名称</param></member>");

            // Act
            var comments = new CsComments(document.DocumentElement);

            // Assert
            Assert.Equal("第二项", comments.Param["value"].Text);
            Assert.Same(comments.Param["value"], comments.Param["@value"]);
            Assert.Null(comments.Param[""]);
        }

        /// <summary>
        /// 测试目的：验证新增只读视图不会被兼容集合和数组的修改污染。
        /// </summary>
        [Fact]
        public void CsComments_ReadOnlyViews_RemainStableAfterLegacyMutation()
        {
            // Arrange
            var document = new XmlDocument();
            document.LoadXml("<member><param name=\"value\">原始参数</param><typeparam name=\"T\">类型参数</typeparam><exception cref=\"T:System.InvalidOperationException\">异常</exception></member>");
            var comments = new CsComments(document.DocumentElement);

            // Act
            comments.Param["value"] = CsCommentsParam.Empty;
            comments.TypeParam["T"] = CsCommentsParam.Empty;
            comments.Exception[0] = null;

            // Assert
            Assert.Equal("原始参数", comments.Parameters["value"].Text);
            Assert.Equal("类型参数", comments.TypeParameters["T"].Text);
            Assert.NotNull(comments.Exceptions[0]);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, CsCommentsParam>)comments.Parameters).Add("other", CsCommentsParam.Empty));
        }
    }
}