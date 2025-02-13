# Bing.Utils
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://mit-license.org/)

Bing.Utils 是一个基于`.net core`平台下的工具库，旨在提升小型团队的开发输出能力，由常用公共操作类（工具类、帮助类）、分层架构基类，第三方组件封装，第三方业务接口封装等组成。

## Nuget Packages

|包名称|Nuget版本|下载数|
|---|---|---|
|Bing.Utils|[![Bing.Utils](https://img.shields.io/nuget/v/Bing.Utils.svg)](https://www.nuget.org/packages/Bing.Utils/)|[![Bing.Utils](https://img.shields.io/nuget/dt/Bing.Utils.svg)](https://www.nuget.org/packages/Bing.Utils/)|
|Bing.Utils.Text|[![Bing.Utils.Drawing](https://img.shields.io/nuget/v/Bing.Utils.Text.svg)](https://www.nuget.org/packages/Bing.Utils.Text/)|[![Bing.Utils.Text](https://img.shields.io/nuget/dt/Bing.Utils.Text.svg)](https://www.nuget.org/packages/Bing.Utils.Text/)|
|Bing.Utils.Collections|[![Bing.Utils.Collections](https://img.shields.io/nuget/v/Bing.Utils.Collections.svg)](https://www.nuget.org/packages/Bing.Utils.Collections/)|[![Bing.Utils.Collections](https://img.shields.io/nuget/dt/Bing.Utils.Collections.svg)](https://www.nuget.org/packages/Bing.Utils.Collections/)|
|Bing.Utils.Reflection|[![Bing.Utils.Reflection](https://img.shields.io/nuget/v/Bing.Utils.Reflection.svg)](https://www.nuget.org/packages/Bing.Utils.Reflection/)|[![Bing.Utils.Reflectionn](https://img.shields.io/nuget/dt/Bing.Utils.Reflection.svg)](https://www.nuget.org/packages/Bing.Utils.Reflection/)|
|Bing.Utils.IdUtils|[![Bing.Utils.IdUtils](https://img.shields.io/nuget/v/Bing.Utils.IdUtils.svg)](https://www.nuget.org/packages/Bing.Utils.IdUtils/)|[![Bing.Utils.IdUtils](https://img.shields.io/nuget/dt/Bing.Utils.IdUtils.svg)](https://www.nuget.org/packages/Bing.Utils.IdUtils/)|
|Bing.Utils.DateTime|[![Bing.Utils.DateTime](https://img.shields.io/nuget/v/Bing.Utils.DateTime.svg)](https://www.nuget.org/packages/Bing.Utils.DateTime/)|[![Bing.Utils.DateTime](https://img.shields.io/nuget/dt/Bing.Utils.DateTime.svg)](https://www.nuget.org/packages/Bing.Utils.DateTime/)|
|Bing.Utils.Drawing|[![Bing.Utils.Drawing](https://img.shields.io/nuget/v/Bing.Utils.Drawing.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing/)|[![Bing.Utils.Drawing](https://img.shields.io/nuget/dt/Bing.Utils.Drawing.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing/)|
|Bing.Utils.Drawing.ImageSharp|[![Bing.Utils.Drawing.ImageSharp](https://img.shields.io/nuget/v/Bing.Utils.Drawing.ImageSharp.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing.ImageSharp/)|[![Bing.Utils.Drawing.ImageSharp](https://img.shields.io/nuget/dt/Bing.Utils.Drawing.ImageSharp.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing.ImageSharp/)|
|Bing.Utils.Drawing.SkiaSharp|[![Bing.Utils.Drawing.SkiaSharp](https://img.shields.io/nuget/v/Bing.Utils.Drawing.SkiaSharp.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing.SkiaSharp/)|[![Bing.Utils.Drawing.SkiaSharp](https://img.shields.io/nuget/dt/Bing.Utils.Drawing.SkiaSharp.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing.SkiaSharp/)|
|Bing.Utils.Http|[![Bing.Utils.Http](https://img.shields.io/nuget/v/Bing.Utils.Http.svg)](https://www.nuget.org/packages/Bing.Utils.Http/)|[![Bing.Utils.Http](https://img.shields.io/nuget/dt/Bing.Utils.Http.svg)](https://www.nuget.org/packages/Bing.Utils.Http/)|

## 开发环境与依赖

在项目开发和部署过程中，我们使用了以下工具和组件：

- **开发工具**
  - [Visual Studio 2022](https://visualstudio.microsoft.com/zh-hans/vs/)
  - [Resharper Ultimate](https://www.jetbrains.com/resharper/)

> 如果没有标注版本号，则采用最新版本。

## 框架开发流程

我们的开发流程包括以下步骤：

0. 搜集：收集常用的工具和组件。
1. 整理：对收集的资源进行分类和整理。
2. 集成：将整理后的资源集成到框架中。
3. 封装：对集成的功能进行封装，提供简洁的接口。

## 作者

简玄冰

## 贡献与反馈
如果你在阅读或使用Bing中任意一个代码片断时发现Bug，或有更佳实现方式，请通知我们。
- **功能完善**：目前，许多功能仅建立了基本结构，细节特性尚未完全实现。如果某个类无法满足您的需求，请告诉我们。
- **提交方式**：您可以通过 Github 的 Issue 或 Pull Reuqest 向我们提交问题和代码。如果您更喜欢使用 QQ 交流，请加入我们的交流群。
- **代码风格**：对于您提交的代码，如果我们决定采纳，可能会进行相应的重构，以统一代码风格。
- **贡献者名单**：对于热心的贡献者，我们会将您的名字列入贡献者名单。

## 免责声明
- **Bug 风险**：尽管我们对代码进行了严格审查，并在自己的项目中使用，但仍可能存在未知的 Bug。如果您的生产系统因此受到影响，Bing 团队不承担责任。
- **API 兼容性**：出于成本考虑，我们不保证已发布的 API 保持兼容。每次更新代码时，请注意可能的变更。

## 开源地址
[https://github.com/bing-framework/Bing.Utils](https://github.com/bing-framework/Bing.Utils)

## License

**MIT**

> 这意味着你可以在任意场景下使用 Bing 应用框架而不会有人找你要钱。

> Bing 会尽量引入开源免费的第三方技术框架，如有意外，还请自行了解。
