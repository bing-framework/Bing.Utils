using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlubuCore.Context;
using FlubuCore.Context.Attributes.BuildProperties;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.IO;
using FlubuCore.Scripting;
using FlubuCore.Scripting.Attributes;

namespace BuildScript
{
    /// <summary>
    /// 构建脚本
    /// </summary>
    [Include("./build/BuildVersion.cs")]
    public partial class BuildScript : DefaultBuildScript
    {
        /// <summary>
        /// 解决方案文件名
        /// </summary>
        [SolutionFileName]
        public string SolutionFileName { get; set; } = "Bing.Utils.sln";

        /// <summary>
        /// 构建配置
        /// </summary>
        [FromArg("c|configuration")]
        [BuildConfiguration]
        public string BuildConfiguration { get; set; } = "Release";

        /// <summary>
        /// Nuget推送地址
        /// </summary>
        [FromArg("nugetUrl")]
        public string NugetUrl { get; set; } = "https://api.nuget.org/v3/index.json";

        /// <summary>
        /// Nuget密钥
        /// </summary>
        [FromArg("nugetKey", "Nuget api key for publishing nuget packages.")]
        public string NugetApiKey { get; set; }

        /// <summary>
        /// 构建版本
        /// </summary>
        protected BuildVersion BuildVersion { get; set; }

        /// <summary>
        /// 源代码目录
        /// </summary>
        protected FullPath SourceDir => RootDirectory.CombineWith("src");

        /// <summary>
        /// 测试目录
        /// </summary>
        protected FullPath TestDir => RootDirectory.CombineWith("tests");

        /// <summary>
        /// 输出目录
        /// </summary>
        protected FullPath OutputDir => RootDirectory.CombineWith("nuget_packages");

        /// <summary>
        /// 项目文件列表
        /// </summary>
        protected List<FileFullPath> ProjectFiles { get; set; }

        /// <summary>
        /// 单元测试项目文件列表
        /// </summary>
        protected List<FileFullPath> UnitTestProjectFiles { get; set; }

        /// <summary>
        /// 集成测试项目文件列表
        /// </summary>
        protected List<FileFullPath> IntegrationTestProjectFiles { get; set; }

        /// <summary>
        /// 忽略测试项目文件列表
        /// </summary>
        protected List<FileFullPath> IgnoreTestProjectFiles { get; set; }

        /// <summary>
        /// 忽略打包项目文件列表
        /// </summary>
        protected List<FileFullPath> IgnorePackProjectFiles { get; set; }

        /// <summary>
        /// 获取集成测试项目文件列表
        /// </summary>
        private List<FileFullPath> GetIntegrationTestProjects()
        {
            return IntegrationTestProjectFiles.Where(x => IgnoreTestProjectFiles.Exists(p => p.FileName == x.FileName) == false).ToList();
        }

        /// <summary>
        /// 获取打包项目文件列表
        /// </summary>
        private List<FileFullPath> GetPackProjects()
        {
            return ProjectFiles.Where(x => IgnorePackProjectFiles.Exists(p => p.FileName == x.FileName) == false).ToList();
        }

        /// <summary>
        /// 构建前操作
        /// </summary>
        /// <param name="context">构建任务上下文</param>
        protected override void BeforeBuildExecution(ITaskContext context)
        {
            BuildVersion = FetchBuildVersion(context);
            ProjectFiles = context.GetFiles(SourceDir, "*/*.csproj");
            UnitTestProjectFiles = context.GetFiles(TestDir, "*/*.csproj");
            IntegrationTestProjectFiles = context.GetFiles(TestDir, "*/*.Tests.Integration.csproj");
            IgnoreTestProjectFiles = new List<FileFullPath>();
            IgnorePackProjectFiles = new List<FileFullPath>();
            AddIgnoreTestProjects(context);
            AddIgnorePackProjects(context);
        }

        /// <summary>
        /// 添加忽略测试项目文件列表
        /// </summary>
        private void AddIgnoreTestProjects(ITaskContext context)
        {
        }

        /// <summary>
        /// 添加忽略打包项目文件列表
        /// </summary>
        private void AddIgnorePackProjects(ITaskContext context)
        {
        }

        /// <summary>
        /// 配置构建目标
        /// </summary>
        protected override void ConfigureTargets(ITaskContext context)
        {
            var clean = Clean(context);
            var restore = Restore(context, clean);
            var build = Build(context, restore);
            var test = Test(context, build);
            var pack = Pack(context, clean);
            PublishNugetPackage(context, build, pack);
        }

        /// <summary>
        /// 清理解决方案
        /// </summary>
        private ITarget Clean(ITaskContext context)
        {
            return context.CreateTarget("clean")
                .SetDescription("Cleans the output of all projects in the solution.")
                .AddCoreTask(x => x.Clean().AddDirectoryToClean(OutputDir, true));
        }

        /// <summary>
        /// 还原包
        /// </summary>
        private ITarget Restore(ITaskContext context, params ITarget[] dependTargets)
        {
            return context.CreateTarget("restore")
                .SetDescription("Restores the dependencies and tools of all projects in the solution.")
                .DependsOn(dependTargets)
                .AddCoreTask(x => x.Restore());
        }

        /// <summary>
        /// 构建解决方案
        /// </summary>
        private ITarget Build(ITaskContext context, params ITarget[] dependTargets)
        {
            return context.CreateTarget("build")
                .SetDescription("Builds all projects in the solution.")
                .DependsOn(dependTargets)
                .AddCoreTask(x => x.Build().InformationalVersion(BuildVersion.VersionWithSuffix()));
        }

        /// <summary>
        /// 运行测试
        /// </summary>
        private ITarget Test(ITaskContext context, params ITarget[] dependTargets)
        {
            var unitTest = UnitTest(context, dependTargets);
            var integrationTest = IntegrationTest(context, dependTargets);
            return context.CreateTarget("test")
                .SetDescription("Run all tests.")
                .DependsOn(unitTest, integrationTest);
        }

        /// <summary>
        /// 运行单元测试
        /// </summary>
        private ITarget UnitTest(ITaskContext context, params ITarget[] dependTargets)
        {
            return context.CreateTarget("unit.test")
                .SetDescription("Runs unit tests.")
                .DependsOn(dependTargets)
                .ForEach(UnitTestProjectFiles, (project, target) =>
                {
                    target.AddCoreTask(x => x.Test().Project(project).NoBuild());
                });
        }

        /// <summary>
        /// 运行集成测试
        /// </summary>
        private ITarget IntegrationTest(ITaskContext context, params ITarget[] dependTargets)
        {
            return context.CreateTarget("integration.test")
                .SetDescription("Runs integration tests.")
                .DependsOn(dependTargets)
                .ForEach(GetIntegrationTestProjects(), (project, target) =>
                {
                    target.AddCoreTask(x => x.Test().Project(project).NoBuild());
                });
        }

        /// <summary>
        /// 创建nuget包
        /// </summary>
        private ITarget Pack(ITaskContext context, params ITarget[] dependTargets)
        {
            return context.CreateTarget("pack")
                .SetDescription("Create nuget packages for Bing.")
                .DependsOn(dependTargets)
                .ForEach(GetPackProjects(), (project, target) =>
                {
                    target.AddCoreTask(x => x.Pack()
                        .NoBuild()
                        .Project(project)
                        .IncludeSymbols()
                        .VersionSuffix(BuildVersion.Suffix)
                        .OutputDirectory(OutputDir));
                });
        }

        /// <summary>
        /// 发布nuget包
        /// </summary>
        private void PublishNugetPackage(ITaskContext context, params ITarget[] dependTargets)
        {
            context.CreateTarget("nuget.publish")
                .SetDescription("Publishes the nuget packages to the nuget server.")
                .DependsOn(dependTargets)
                .Do(x =>
                {
                    var packages = Directory.GetFiles(OutputDir, "*.nupkg");
                    foreach (var package in packages)
                    {
                        if (package.EndsWith("symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                            continue;
                        context.CoreTasks().NugetPush(package)
                            .DoNotFailOnError(ex =>
                            {
                                context.LogError($"Failed to publish {package}. Exception: {ex.Message}");
                            })
                            .ServerUrl(NugetUrl)
                            .ApiKey(NugetApiKey)
                            .SkipDuplicate()
                            .Execute(context);
                    }
                });
        }
    }
}