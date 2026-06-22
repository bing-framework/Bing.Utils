global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Reflection;
global using Shouldly;
global using Xunit;
global using Xunit.Abstractions;
// 注：Bing.Text 和 Bing.Validation 通过 extern alias guard 在各测试文件中引入，避免与 Bing.Utils 程序集同名冲突
