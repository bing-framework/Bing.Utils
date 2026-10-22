using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

// 类型反射 - 缓存
public static partial class TypeReflections
{
    /// <summary>
    /// 类型缓存管理器
    /// </summary>
    public static class TypeCacheManager
    {
        internal const int FlaggedCacheCapacity = 128;

        private sealed class FlaggedPropertyLookup
        {
            internal readonly ConcurrentDictionary<BindingFlags, Lazy<PropertyInfo[]>> Entries = new();
            internal readonly object AdmissionGate = new();
        }

        private sealed class FlaggedFieldLookup
        {
            internal readonly ConcurrentDictionary<BindingFlags, Lazy<FieldInfo[]>> Entries = new();
            internal readonly object AdmissionGate = new();
        }

        private sealed class FlaggedMethodLookup
        {
            internal readonly ConcurrentDictionary<BindingFlags, Lazy<MethodInfo[]>> Entries = new();
            internal readonly object AdmissionGate = new();
        }

        /// <summary>
        /// 类型 - 属性 缓存
        /// </summary>
        private static readonly ConditionalWeakTable<Type, Lazy<PropertyInfo[]>> TypePropertyCache = new();

        /// <summary>
        /// 类型 - 字段 缓存
        /// </summary>
        private static readonly ConditionalWeakTable<Type, Lazy<FieldInfo[]>> TypeFieldCache = new();

        /// <summary>
        /// 类型 - 方法 缓存
        /// </summary>
        private static readonly ConditionalWeakTable<Type, Lazy<MethodInfo[]>> TypeMethodCache = new();

        private static readonly ConditionalWeakTable<Type, FlaggedPropertyLookup> FlaggedPropertyCache = new();
        private static readonly ConditionalWeakTable<Type, FlaggedFieldLookup> FlaggedFieldCache = new();
        private static readonly ConditionalWeakTable<Type, FlaggedMethodLookup> FlaggedMethodCache = new();
        private static readonly ConditionalWeakTable<object, object> SafeMemberSnapshots = new();
        private static readonly object SafeMemberSnapshotMarker = new();

        private static bool ContainsCollectible(PropertyInfo[] members)
        {
            if (members == null || SafeMemberSnapshots.TryGetValue(members, out _))
                return false;
            if (members.Any(member => ReflectionCacheScope.IsCollectible((MemberInfo)member)))
                return true;
            SafeMemberSnapshots.GetValue(members, _ => SafeMemberSnapshotMarker);
            return false;
        }

        private static bool ContainsCollectible(FieldInfo[] members)
        {
            if (members == null || SafeMemberSnapshots.TryGetValue(members, out _))
                return false;
            if (members.Any(member => ReflectionCacheScope.IsCollectible((MemberInfo)member)))
                return true;
            SafeMemberSnapshots.GetValue(members, _ => SafeMemberSnapshotMarker);
            return false;
        }

        private static bool ContainsCollectible(MethodInfo[] members)
        {
            if (members == null || SafeMemberSnapshots.TryGetValue(members, out _))
                return false;
            if (members.Any(member => ReflectionCacheScope.IsCollectible((MemberInfo)member)))
                return true;
            SafeMemberSnapshots.GetValue(members, _ => SafeMemberSnapshotMarker);
            return false;
        }

        /// <summary>返回指定绑定标志的独立属性快照。</summary>
        public static PropertyInfo[] GetTypeProperties(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
            Type type, BindingFlags flags)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            flags &= ~BindingFlags.IgnoreCase;
            if (ReflectionCacheScope.IsCollectible(type))
                return type.GetProperties(flags).ToArray();
            var lookup = FlaggedPropertyCache.GetValue(type, _ => new FlaggedPropertyLookup());
            if (lookup.Entries.TryGetValue(flags, out var existing))
            {
                var cached = existing.Value;
                if (ContainsCollectible(cached))
                {
                    ((ICollection<KeyValuePair<BindingFlags, Lazy<PropertyInfo[]>>>)lookup.Entries)
                        .Remove(new KeyValuePair<BindingFlags, Lazy<PropertyInfo[]>>(flags, existing));
                    CoreCacheMetrics.RecordBypass();
                }
                return cached.ToArray();
            }

            Lazy<PropertyInfo[]> lazy = null;
            var bypass = false;
            lock (lookup.AdmissionGate)
            {
                if (lookup.Entries.TryGetValue(flags, out existing))
                    lazy = existing;
                else if (lookup.Entries.Count >= FlaggedCacheCapacity)
                {
                    CoreCacheMetrics.RecordFlaggedCacheBypass();
                    bypass = true;
                }
                else
                {
                    lazy = new Lazy<PropertyInfo[]>(() => type.GetProperties(flags),
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    lookup.Entries.TryAdd(flags, lazy);
                    CoreCacheMetrics.RecordFlaggedCacheAdmission();
                }
            }

            var properties = bypass ? type.GetProperties(flags) : lazy.Value;
            if (!bypass && ContainsCollectible(properties))
            {
                ((ICollection<KeyValuePair<BindingFlags, Lazy<PropertyInfo[]>>>)lookup.Entries)
                    .Remove(new KeyValuePair<BindingFlags, Lazy<PropertyInfo[]>>(flags, lazy));
                CoreCacheMetrics.RecordBypass();
            }
            return properties.ToArray();
        }

        /// <summary>返回指定绑定标志的独立字段快照。</summary>
        public static FieldInfo[] GetTypeFields(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
#endif
            Type type, BindingFlags flags)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            flags &= ~BindingFlags.IgnoreCase;
            if (ReflectionCacheScope.IsCollectible(type))
                return type.GetFields(flags).ToArray();
            var lookup = FlaggedFieldCache.GetValue(type, _ => new FlaggedFieldLookup());
            if (lookup.Entries.TryGetValue(flags, out var existing))
            {
                var cached = existing.Value;
                if (ContainsCollectible(cached))
                {
                    ((ICollection<KeyValuePair<BindingFlags, Lazy<FieldInfo[]>>>)lookup.Entries)
                        .Remove(new KeyValuePair<BindingFlags, Lazy<FieldInfo[]>>(flags, existing));
                    CoreCacheMetrics.RecordBypass();
                }
                return cached.ToArray();
            }

            Lazy<FieldInfo[]> lazy = null;
            var bypass = false;
            lock (lookup.AdmissionGate)
            {
                if (lookup.Entries.TryGetValue(flags, out existing))
                    lazy = existing;
                else if (lookup.Entries.Count >= FlaggedCacheCapacity)
                {
                    CoreCacheMetrics.RecordFlaggedCacheBypass();
                    bypass = true;
                }
                else
                {
                    lazy = new Lazy<FieldInfo[]>(() => type.GetFields(flags),
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    lookup.Entries.TryAdd(flags, lazy);
                    CoreCacheMetrics.RecordFlaggedCacheAdmission();
                }
            }

            var fields = bypass ? type.GetFields(flags) : lazy.Value;
            if (!bypass && ContainsCollectible(fields))
            {
                ((ICollection<KeyValuePair<BindingFlags, Lazy<FieldInfo[]>>>)lookup.Entries)
                    .Remove(new KeyValuePair<BindingFlags, Lazy<FieldInfo[]>>(flags, lazy));
                CoreCacheMetrics.RecordBypass();
            }
            return fields.ToArray();
        }

        /// <summary>返回指定绑定标志的独立方法快照。</summary>
        public static MethodInfo[] GetTypeMethods(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
            Type type, BindingFlags flags)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            flags &= ~BindingFlags.IgnoreCase;
            if (ReflectionCacheScope.IsCollectible(type))
                return type.GetMethods(flags).ToArray();
            var lookup = FlaggedMethodCache.GetValue(type, _ => new FlaggedMethodLookup());
            if (lookup.Entries.TryGetValue(flags, out var existing))
            {
                var cached = existing.Value;
                if (ContainsCollectible(cached))
                {
                    ((ICollection<KeyValuePair<BindingFlags, Lazy<MethodInfo[]>>>)lookup.Entries)
                        .Remove(new KeyValuePair<BindingFlags, Lazy<MethodInfo[]>>(flags, existing));
                    CoreCacheMetrics.RecordBypass();
                }
                return cached.ToArray();
            }

            Lazy<MethodInfo[]> lazy = null;
            var bypass = false;
            lock (lookup.AdmissionGate)
            {
                if (lookup.Entries.TryGetValue(flags, out existing))
                    lazy = existing;
                else if (lookup.Entries.Count >= FlaggedCacheCapacity)
                {
                    CoreCacheMetrics.RecordFlaggedCacheBypass();
                    bypass = true;
                }
                else
                {
                    lazy = new Lazy<MethodInfo[]>(() => type.GetMethods(flags),
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    lookup.Entries.TryAdd(flags, lazy);
                    CoreCacheMetrics.RecordFlaggedCacheAdmission();
                }
            }

            var methods = bypass ? type.GetMethods(flags) : lazy.Value;
            if (!bypass && ContainsCollectible(methods))
            {
                ((ICollection<KeyValuePair<BindingFlags, Lazy<MethodInfo[]>>>)lookup.Entries)
                    .Remove(new KeyValuePair<BindingFlags, Lazy<MethodInfo[]>>(flags, lazy));
                CoreCacheMetrics.RecordBypass();
            }
            return methods.ToArray();
        }

        /// <summary>返回公共方法的独立快照。</summary>
        public static MethodInfo[] GetTypeMethods(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
            Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (ReflectionCacheScope.IsCollectible(type))
                return type.GetMethods().ToArray();
            var lazy = TypeMethodCache.GetValue(type, _ => new Lazy<MethodInfo[]>(
                () => type.GetMethods(), LazyThreadSafetyMode.ExecutionAndPublication));
            var methods = lazy.Value;
            if (ContainsCollectible(methods))
            {
                TypeMethodCache.Remove(type);
                CoreCacheMetrics.RecordBypass();
            }
            return methods.ToArray();
        }

        /// <summary>
        /// 属性 - 值 获取器 缓存
        /// </summary>
        internal static readonly ConditionalWeakTable<PropertyInfo, Lazy<Func<object, object>>> PropertyValueGetters = new();

        /// <summary>
        /// 属性 - 值 设置器 缓存
        /// </summary>
        internal static readonly ConditionalWeakTable<PropertyInfo, Lazy<Action<object, object>>> PropertyValueSetters = new();

        /// <summary>
        /// 获取指定类型的所有公共属性，并缓存结果以提高后续访问的性能。
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>表示指定类型所有公共属性的<see cref="PropertyInfo"/>数组。</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static PropertyInfo[] GetTypeProperties(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            Type type)
        {
            if (null == type)
                throw new ArgumentNullException(nameof(type));
            if (ReflectionCacheScope.IsCollectible(type))
                return type.GetProperties().ToArray();
            var lazy = TypePropertyCache.GetValue(type, _ => new Lazy<PropertyInfo[]>(
                () => type.GetProperties(), LazyThreadSafetyMode.ExecutionAndPublication));
            var properties = lazy.Value;
            if (ContainsCollectible(properties))
            {
                TypePropertyCache.Remove(type);
                CoreCacheMetrics.RecordBypass();
            }
            return properties.ToArray();
        }

        /// <summary>
        /// 获取指定类型的所有公共字段，并缓存结果以提高后续访问的性能。
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>表示指定类型所有公共字段的<see cref="FieldInfo"/>数组。</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static FieldInfo[] GetTypeFields(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type type)
        {
            if (null == type)
                throw new ArgumentNullException(nameof(type));
            if (ReflectionCacheScope.IsCollectible(type))
                return type.GetFields().ToArray();
            var lazy = TypeFieldCache.GetValue(type, _ => new Lazy<FieldInfo[]>(
                () => type.GetFields(), LazyThreadSafetyMode.ExecutionAndPublication));
            var fields = lazy.Value;
            if (ContainsCollectible(fields))
            {
                TypeFieldCache.Remove(type);
                CoreCacheMetrics.RecordBypass();
            }
            return fields.ToArray();
        }
    }

    /// <summary>
    /// 类型缓存管理器
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    internal static class TypeCacheManager<T>
    {
        /// <summary>
        /// 属性 - 值 获取器 缓存
        /// </summary>
        public static readonly ConditionalWeakTable<PropertyInfo, Lazy<Func<T, object>>> PropertyValueGetters = new();

        /// <summary>
        /// 属性 - 值 设置器 缓存
        /// </summary>
        public static readonly ConditionalWeakTable<PropertyInfo, Lazy<Action<T, object>>> PropertyValueSetters = new();
    }
}
