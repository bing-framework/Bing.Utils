using System.IO;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Scripting.Attributes;

namespace BuildScript
{
    // 构建脚本 - 版本号
    [Reference("System.Xml.XmlDocument, System.Xml, Version=4.0.0.0, Culture=neutral, publicKeyToken=b77a5c561934e089")]
    public partial class BuildScript
    {
        /// <summary>
        /// 获取构建版本信息
        /// </summary>
        public BuildVersion FetchBuildVersion(ITaskContext context)
        {
            var content = File.ReadAllText(RootDirectory.CombineWith("./version.props"));

            XmlDocument doc = new();
            doc.LoadXml(content);

            var versionMajor = doc.DocumentElement!.SelectSingleNode("/Project/PropertyGroup/VersionMajor")!.InnerText;
            var versionMinor = doc.DocumentElement.SelectSingleNode("/Project/PropertyGroup/VersionMinor")!.InnerText;
            var versionPatch = doc.DocumentElement.SelectSingleNode("/Project/PropertyGroup/VersionPatch")!.InnerText;
            var versionQuality = doc.DocumentElement.SelectSingleNode("/Project/PropertyGroup/VersionQuality")!.InnerText;
            versionQuality = string.IsNullOrWhiteSpace(versionQuality) ? null : versionQuality;

            var suffix = versionQuality;

            switch (Env)
            {
                case "pred":
                    suffix += $"preview-{CreateStamp()}";
                    break;
                case "prod":
                    break;
            }

            suffix = string.IsNullOrWhiteSpace(suffix) ? null : suffix;

            var version = new BuildVersion
            {
                Major = int.Parse(versionMajor),
                Minor = int.Parse(versionMinor),
                Patch = int.Parse(versionPatch),
                Quality = versionQuality,
                Suffix = suffix
            };
            return version;
        }
    }
}