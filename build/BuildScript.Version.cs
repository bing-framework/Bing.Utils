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

            var isCi = false;
            var isTagged = false;
            if (!context.BuildServers().IsLocalBuild)
            {
                isCi = true;
                var isTagAppveyor = context.BuildServers().AppVeyor().IsTag;
                if ((context.BuildServers().RunningOn == BuildServerType.AppVeyor && isTagAppveyor) ||
                    (context.BuildServers().RunningOn == BuildServerType.TravisCI && string.IsNullOrWhiteSpace(context.BuildServers().Travis().TagName)))
                {
                    isTagged = true;
                }
            }

            // 如果没有创建标签，则添加时间戳
            if (!isTagged)
                suffix += (isCi ? "preview-" : "dv-") + CreateStamp();
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