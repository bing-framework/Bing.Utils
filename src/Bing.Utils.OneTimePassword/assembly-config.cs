// 允许核心测试独立验证极罕见的 HOTP 双码歧义，公开服务仍使用真实 HMAC。
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Bing.Utils.OneTimePassword.Tests")]
