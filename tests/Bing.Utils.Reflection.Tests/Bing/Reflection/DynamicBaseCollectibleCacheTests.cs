using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Bing.Dynamic;

namespace Bing.Reflection;

/// <summary>验证可回收动态类型的实例缓存不串值、不影响并发读取和程序集回收。</summary>
public class DynamicBaseCollectibleCacheTests
{
#if NET6_0_OR_GREATER
    [Fact]
    public void DynamicProperties_RepeatedConcurrentReads_AreIsolatedPerInstance()
    {
        var first = (DynamicBase)TypeVisit.CreateInstance(new Dictionary<string, object>
        {
            ["Value"] = 7, ["Other"] = 8
        });
        var second = (DynamicBase)TypeVisit.CreateInstance(new Dictionary<string, object>
        {
            ["Value"] = 9, ["Other"] = 10
        });
        first.GetType().ShouldBe(second.GetType());
        Parallel.For(0, 32, _ =>
        {
            first.GetPropertyValue("Value").ShouldBe(7);
            first.GetPropertyValue("Other").ShouldBe(8);
            second.GetPropertyValue("Value").ShouldBe(9);
            second.GetPropertyValue("Other").ShouldBe(10);
        });
        first.SetPropertyValue("Value", 11);
        first.SetPropertyValue("Other", 12);
        second.GetPropertyValue("Value").ShouldBe(9);
        second.GetPropertyValue("Other").ShouldBe(10);
        first.GetPropertyValue("Value").ShouldBe(11);
        first.GetPropertyValue("Other").ShouldBe(12);
    }

    [Fact]
    public void DynamicProperties_ConcurrentReadsAcrossSmallAndDictionaryCaches_KeepCorrectMetadata()
    {
        var schema = Enumerable.Range(0, 12).ToDictionary(index => $"Property{index}", index => (object)index);
        var template = (DynamicBase)TypeVisit.CreateInstance(schema);
        var instance = (DynamicBase)Activator.CreateInstance(template.GetType())!;
        var names = schema.Keys.ToArray();
        Parallel.For(0, 192, index => instance.GetPropertyValue(names[index % names.Length]).ShouldBe(0));
        for (var i = 0; i < names.Length; i++)
            instance.SetPropertyValue(names[i], i + 1);
        for (var i = 0; i < names.Length; i++)
            instance.GetPropertyValue(names[i]).ShouldBe(i + 1);
        Should.Throw<MissingMemberException>(() => instance.GetPropertyValue("Missing"));
        Should.Throw<MissingMemberException>(() => instance.GetPropertyValue("Missing"));
    }

    [Fact]
    public void DynamicProperties_ConcurrentDistinctMisses_RespectCombinedCacheCapacity()
    {
        var template = (DynamicBase)TypeVisit.CreateInstance(new Dictionary<string, object> { ["Seed"] = 0 });
        var instance = (DynamicBase)Activator.CreateInstance(template.GetType())!;
        var names = Enumerable.Range(0, 1100).Select(index => $"Missing{index}").ToArray();

        Parallel.ForEach(names, name =>
            Should.Throw<MissingMemberException>(() => instance.GetPropertyValue(name)));

        GetCollectibleCacheEntryCount(instance).ShouldBe(DynamicBase.PropertyCacheCapacity);
        Should.Throw<MissingMemberException>(() => instance.GetPropertyValue("BeyondCapacity"));
        GetCollectibleCacheEntryCount(instance).ShouldBe(DynamicBase.PropertyCacheCapacity);
    }

    [Fact]
    public void DynamicProperties_ConcurrentRepeatedMisses_DoNotDuplicateCacheEntries()
    {
        var template = (DynamicBase)TypeVisit.CreateInstance(new Dictionary<string, object> { ["Seed"] = 0 });
        var instance = (DynamicBase)Activator.CreateInstance(template.GetType())!;
        var names = Enumerable.Range(0, 6).Select(index => $"RepeatedMissing{index}").ToArray();

        Parallel.For(0, 4096, index =>
            Should.Throw<MissingMemberException>(() => instance.GetPropertyValue(names[index % names.Length])));

        GetCollectibleCacheEntryCount(instance).ShouldBe(names.Length);
    }

    [Fact]
    public void DynamicProperties_CachedAccess_DoesNotPreventCollection()
    {
        var weak = CreateAndRelease();
        for (var attempt = 0; attempt < 20 && weak.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        weak.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndRelease()
    {
        var schema = Enumerable.Range(0, 12)
            .ToDictionary(index => $"CacheLifecycleValue{index}", index => (object)index);
        var instance = (DynamicBase)TypeVisit.CreateInstance(schema);
        for (var i = 0; i < 20; i++)
        {
            foreach (var name in schema.Keys)
            {
                instance.SetPropertyValue(name, i);
                instance.GetPropertyValue(name).ShouldBe(i);
            }
        }
        return new WeakReference(instance.GetType().Assembly);
    }

    private static int GetCollectibleCacheEntryCount(DynamicBase instance)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var cacheField = typeof(DynamicBase).GetField("_instancePropertyCache", flags)!;
        var lookup = cacheField.GetValue(instance)!;
        var lookupType = lookup.GetType();
        var firstName = lookupType.GetField("FirstName", flags)!.GetValue(lookup);
        var count = firstName == null ? 0 : 1;

        var entry = lookupType.GetField("AdditionalHead", flags)!.GetValue(lookup);
        while (entry != null)
        {
            count++;
            entry = entry.GetType().GetField("Next", flags)!.GetValue(entry);
        }

        var additionalProperties = lookupType.GetField("AdditionalProperties", flags)!.GetValue(lookup);
        if (additionalProperties == null)
            return count;

        var properties = additionalProperties.GetType().GetField("Properties", flags)!.GetValue(additionalProperties)!;
        return count + (int)properties.GetType().GetProperty("Count")!.GetValue(properties)!;
    }
#endif
}
