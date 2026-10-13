using System;
using System.Reflection;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>提供跨目标框架的反射运行时能力探测。</summary>
internal static class ReflectionRuntimeFeatures
{
    /// <summary>当前运行时是否支持动态代码。</summary>
#if NET5_0_OR_GREATER
    internal static bool IsDynamicCodeSupported => RuntimeFeature.IsDynamicCodeSupported;
#else
    private static readonly Lazy<bool> DynamicCodeSupported = new(DetectDynamicCodeSupport);

    internal static bool IsDynamicCodeSupported => DynamicCodeSupported.Value;

    private static bool DetectDynamicCodeSupport()
    {
        try
        {
            var runtimeFeatureType = typeof(object).Assembly
                .GetType("System.Runtime.CompilerServices.RuntimeFeature");
            var property = runtimeFeatureType?.GetProperty(
                "IsDynamicCodeSupported", BindingFlags.Public | BindingFlags.Static);
            var value = property?.GetValue(null);
            return value is bool supported ? supported : true;
        }
        catch (Exception)
        {
            // 无法确认时保持兼容旧运行时的行为，由实际动态 API 调用报告失败。
            return true;
        }
    }
#endif
}
