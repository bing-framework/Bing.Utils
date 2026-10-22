using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Bing.Dynamic;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

#if !NETSTANDARD

/// <summary>类型工厂</summary>
internal static partial class TypeFactory
{
    private const string AssemblyName = "Bing.Reflection.NRefDynamic";
    internal const int MaxGeneratedTypes = 4096;
    internal const int MaxProperties = 255;
    private const int MaxPropertyNameLength = 256;
    private static readonly SchemaCache _sharedCache = new(MaxGeneratedTypes);
    private static readonly ConditionalWeakTable<IDictionary<string, Type>, SchemaFastLookup> _schemaFastCache = new();
    private static readonly ConditionalWeakTable<Type, SchemaTypeIdentity> _schemaTypeIdentities = new();
    private static SchemaFastEntry _recentSchema;
    private static long _buildCount;
    internal static long BuildCount => System.Threading.Interlocked.Read(ref _buildCount);
    private static readonly ConstructorInfo _baseConstructor = typeof(DynamicBase)
        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!;

    internal sealed class SchemaTypeIdentity
    {
        internal readonly string AssemblyQualifiedName;
        internal readonly long RuntimeHandle;
        private readonly WeakReference<Type> _type;

        internal SchemaTypeIdentity(Type type)
        {
            AssemblyQualifiedName = type.AssemblyQualifiedName!;
            RuntimeHandle = type.TypeHandle.Value.ToInt64();
            _type = new WeakReference<Type>(type);
        }

        internal bool Matches(Type type)
        {
            if (type == null)
                return false;
            if (_type.TryGetTarget(out var cached) && ReferenceEquals(cached, type))
                return true;
            return RuntimeHandle == type.TypeHandle.Value.ToInt64()
                   && StringComparer.Ordinal.Equals(AssemblyQualifiedName, type.AssemblyQualifiedName);
        }

        internal bool Matches(SchemaTypeIdentity other) => other != null
            && RuntimeHandle == other.RuntimeHandle
            && StringComparer.Ordinal.Equals(AssemblyQualifiedName, other.AssemblyQualifiedName);
    }

    internal readonly struct SchemaSegment
    {
        internal readonly string Name;
        internal readonly SchemaTypeIdentity Type;

        internal SchemaSegment(string name, SchemaTypeIdentity type)
        {
            Name = name;
            Type = type;
        }
    }

    internal sealed class SchemaKey : IEquatable<SchemaKey>
    {
        private readonly SchemaSegment[] _segments;
        private readonly int _hashCode;

        internal SchemaKey(SchemaSegment[] segments)
        {
            _segments = segments;
            var hash = 17;
            foreach (var segment in segments)
            {
                hash = unchecked(hash * 31 + StringComparer.Ordinal.GetHashCode(segment.Name));
                hash = unchecked(hash * 31 + StringComparer.Ordinal.GetHashCode(segment.Type.AssemblyQualifiedName));
                hash = unchecked(hash * 31 + segment.Type.RuntimeHandle.GetHashCode());
            }
            _hashCode = hash;
        }

        public bool Equals(SchemaKey other)
        {
            if (other == null || _segments.Length != other._segments.Length)
                return false;
            for (var index = 0; index < _segments.Length; index++)
            {
                var left = _segments[index];
                var right = other._segments[index];
                if (!StringComparer.Ordinal.Equals(left.Name, right.Name)
                    || left.Type.RuntimeHandle != right.Type.RuntimeHandle
                    || !StringComparer.Ordinal.Equals(left.Type.AssemblyQualifiedName, right.Type.AssemblyQualifiedName))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as SchemaKey);
        public override int GetHashCode() => _hashCode;
    }

    private sealed class SchemaEntry
    {
        private Lazy<Type> _pendingBuild;
        private WeakReference<Type> _type;

#if NET7_0_OR_GREATER
        [RequiresDynamicCode("动态类型使用 Reflection.Emit 创建，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("动态类型创建依赖运行时生成的属性元数据。")]
#endif
        internal SchemaEntry(KeyValuePair<string, Type>[] schema)
        {
            _pendingBuild = new Lazy<Type>(() => BuildType(schema), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        internal bool TryGetType(out Type type)
        {
            var cached = System.Threading.Volatile.Read(ref _type);
            if (cached != null && cached.TryGetTarget(out type))
                return true;
            lock (this)
            {
                if (_pendingBuild != null)
                {
                    type = _pendingBuild.Value;
                    System.Threading.Volatile.Write(ref _type, new WeakReference<Type>(type));
                    System.Threading.Volatile.Write(ref _pendingBuild, null);
                    return true;
                }
                if (_type != null)
                    return _type.TryGetTarget(out type);
                type = null;
                return false;
            }
        }

        internal bool IsLive()
        {
            if (System.Threading.Volatile.Read(ref _pendingBuild) != null)
                return true;
            var cached = System.Threading.Volatile.Read(ref _type);
            return cached != null && cached.TryGetTarget(out _);
        }

        internal bool IsReady()
        {
            var cached = System.Threading.Volatile.Read(ref _type);
            return cached != null && cached.TryGetTarget(out _);
        }
    }

    private sealed class SchemaFastLookup
    {
        private SchemaFastEntry _entry;
        private int _observations;

        internal bool TryGet(IDictionary<string, Type> properties, out Type type)
        {
            var entry = System.Threading.Volatile.Read(ref _entry);
            if (entry != null && entry.Matches(properties) && entry.Type.TryGetTarget(out type))
                return true;
            type = null;
            return false;
        }

        internal void SetOnReuse(KeyValuePair<string, Type>[] schema, Type type)
        {
            if (System.Threading.Interlocked.Increment(ref _observations) < 2)
                return;
            System.Threading.Volatile.Write(ref _entry, new SchemaFastEntry(schema, type));
        }
    }

    private sealed class SchemaFastEntry
    {
        // 快路径只保存稳定身份，避免通过长期存活的 schema 快照强引用属性类型。
        private readonly Dictionary<string, SchemaTypeIdentity> _properties;
        internal readonly WeakReference<Type> Type;

        internal SchemaFastEntry(KeyValuePair<string, Type>[] schema, Type type)
        {
            _properties = schema.ToDictionary(
                item => item.Key,
                item => _schemaTypeIdentities.GetValue(item.Value, value => new SchemaTypeIdentity(value)),
                StringComparer.Ordinal);
            Type = new WeakReference<Type>(type);
        }

        internal bool Matches(IDictionary<string, Type> properties)
        {
            if (properties.Count != _properties.Count)
                return false;
            foreach (var item in properties)
            {
                var valueType = item.Value ?? typeof(object);
                if (!_properties.TryGetValue(item.Key, out var identity))
                    return false;
                if (!identity.Matches(valueType))
                    return false;
            }
            return true;
        }
    }

    internal sealed class SchemaCache
    {
        private readonly int _capacity;
        private readonly Dictionary<SchemaKey, SchemaEntry> _generatedTypes = new();
        private readonly object _generationLock = new();
        private int _buildCount;

        internal SchemaCache(int capacity) => _capacity = capacity;
        internal int BuildCount => System.Threading.Volatile.Read(ref _buildCount);

#if NET7_0_OR_GREATER
        [RequiresDynamicCode("动态类型使用 Reflection.Emit 创建，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("动态类型创建依赖运行时生成的属性元数据。")]
#endif
        internal Type GetOrCreate(SchemaKey id, KeyValuePair<string, Type>[] schema)
        {
            while (true)
            {
                SchemaEntry entry;
                var created = false;
                lock (_generationLock)
                {
                    if (_generatedTypes.TryGetValue(id, out entry) && !entry.IsLive())
                    {
                        _generatedTypes.Remove(id);
                        entry = null;
                    }
                    if (entry == null)
                    {
                        if (_generatedTypes.Count >= _capacity)
                        {
                            foreach (var key in _generatedTypes.Keys.ToArray())
                            {
                                if (!_generatedTypes[key].IsLive())
                                    _generatedTypes.Remove(key);
                            }
                            if (_generatedTypes.Count >= _capacity)
                                throw new InvalidOperationException($"动态类型模式已达到 {_capacity} 个的存活容量上限。");
                        }
                        entry = new SchemaEntry(schema);
                        _generatedTypes[id] = entry;
                        created = true;
                    }
                    if (ReflectionCacheMetrics.Enabled)
                    {
                        if (created)
                            ReflectionCacheMetrics.RecordMiss();
                        else if (entry.IsReady())
                            ReflectionCacheMetrics.RecordHit();
                        else
                            ReflectionCacheMetrics.RecordPending();
                    }
                }

                try
                {
                    if (entry.TryGetType(out var type))
                    {
                        if (created)
                            System.Threading.Interlocked.Increment(ref _buildCount);
                        return type;
                    }
                }
                catch
                {
                    lock (_generationLock)
                    {
                        if (_generatedTypes.TryGetValue(id, out var current) && ReferenceEquals(current, entry))
                            _generatedTypes.Remove(id);
                    }
                    throw;
                }
                lock (_generationLock)
                {
                    if (_generatedTypes.TryGetValue(id, out var current) && ReferenceEquals(current, entry))
                        _generatedTypes.Remove(id);
                }
            }
        }
    }

    /// <summary>根据属性模式创建或复用动态类型。</summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("动态类型使用 Reflection.Emit 创建，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("动态类型创建依赖运行时生成的属性元数据。")]
#endif
    public static Type CreateType(IDictionary<string, Type> properties)
    {
        if (properties == null)
            throw new ArgumentNullException(nameof(properties));

        // 只有标准 Dictionary 的枚举和 Count/CopyTo 契约可信，才能安全使用无分配快路径。
        var supportsFastLookup = properties.GetType() == typeof(Dictionary<string, Type>);
        if (supportsFastLookup)
        {
            var recent = System.Threading.Volatile.Read(ref _recentSchema);
            if (recent != null && recent.Matches(properties) && recent.Type.TryGetTarget(out var recentType))
            {
                ReflectionCacheMetrics.RecordHit();
                return recentType;
            }

            var lookup = _schemaFastCache.GetValue(properties, _ => new SchemaFastLookup());
            if (lookup.TryGet(properties, out var cachedType))
            {
                ReflectionCacheMetrics.RecordHit();
                return cachedType;
            }
        }

        var type = CreateType(properties, _sharedCache, out var schema);
        if (supportsFastLookup && !schema.Any(item => ReflectionPackageCacheScope.IsCollectible(item.Value)))
        {
            var lookup = _schemaFastCache.GetValue(properties, _ => new SchemaFastLookup());
            lookup.SetOnReuse(schema, type);
            System.Threading.Volatile.Write(ref _recentSchema, new SchemaFastEntry(schema, type));
        }
        return type;
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("动态类型使用 Reflection.Emit 创建，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("动态类型创建依赖运行时生成的属性元数据。")]
#endif
    internal static Type CreateType(IDictionary<string, Type> properties, SchemaCache cache) =>
        CreateType(properties, cache, out _);

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("动态类型使用 Reflection.Emit 创建，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("动态类型创建依赖运行时生成的属性元数据。")]
#endif
    private static Type CreateType(IDictionary<string, Type> properties, SchemaCache cache,
        out KeyValuePair<string, Type>[] schema)
    {
        if (properties is null)
            throw new ArgumentNullException(nameof(properties));
        if (cache is null)
            throw new ArgumentNullException(nameof(cache));

        if (properties.Count > MaxProperties)
            throw new ArgumentException($"动态类型最多支持 {MaxProperties} 个属性。", nameof(properties));
        if (properties.GetType() == typeof(Dictionary<string, Type>))
        {
            schema = properties.ToArray();
        }
        else
        {
            var items = new List<KeyValuePair<string, Type>>();
            foreach (var item in properties)
            {
                if (items.Count >= MaxProperties)
                    throw new ArgumentException($"动态类型最多支持 {MaxProperties} 个属性。", nameof(properties));
                items.Add(item);
            }
            schema = items.ToArray();
        }
        if (schema.Length > MaxProperties)
            throw new ArgumentException($"动态类型最多支持 {MaxProperties} 个属性。", nameof(properties));
        Array.Sort(schema, static (left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));

        var segments = new SchemaSegment[schema.Length];
        for (var index = 0; index < schema.Length; index++)
        {
            var item = schema[index];
            if (string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > MaxPropertyNameLength || !IsValidPropertyName(item.Key))
                throw new ArgumentException($"无效的动态属性名称：'{item.Key}'。", nameof(properties));
            if (index > 0 && StringComparer.Ordinal.Equals(schema[index - 1].Key, item.Key))
                throw new ArgumentException($"动态属性名称重复：'{item.Key}'。", nameof(properties));

            // 空值没有可用的运行时类型，按 object 归一化，使生成属性仍可安全读写 null。
            if (item.Value is null)
            {
                item = new KeyValuePair<string, Type>(item.Key, typeof(object));
                schema[index] = item;
            }

            if (!IsSupportedPropertyType(item.Value))
                throw new ArgumentException($"动态属性 '{item.Key}' 的类型不可用于公开属性。", nameof(properties));

            var identity = _schemaTypeIdentities.GetValue(item.Value, type => new SchemaTypeIdentity(type));
            segments[index] = new SchemaSegment(item.Key, identity);
        }

        return cache.GetOrCreate(new SchemaKey(segments), schema);
    }

    private static bool IsSupportedPropertyType(Type type)
    {
        if (type == null)
            return false;
#if NET8_0_OR_GREATER
        if (type.IsFunctionPointer)
            return false;
#endif
        return type != typeof(void)
               && !type.IsByRef
               && !type.IsPointer
               && !type.IsByRefLike
               && !type.ContainsGenericParameters
               && type.IsVisible
               && type.AssemblyQualifiedName != null;
    }

    private static bool IsValidPropertyName(string name)
    {
        if (!char.IsLetter(name[0]) && name[0] != '_')
            return false;
        for (var i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                return false;
        }
        return true;
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("动态类型使用 Reflection.Emit 创建，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("动态类型创建依赖运行时生成的属性元数据。")]
    [UnconditionalSuppressMessage("Trimming", "IL2110", Justification = "动态基类的属性需求由 Reflection.Emit 在运行时生成，静态裁剪器无法解析该元数据闭包。")]
    [UnconditionalSuppressMessage("Trimming", "IL2118", Justification = "动态类型属性访问闭包由 Reflection.Emit 生成，只有在动态代码支持时执行。")]
#endif
    private static Type BuildType(KeyValuePair<string, Type>[] schema)
    {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new PlatformNotSupportedException("动态类型生成需要运行时动态代码支持。");
            var assemblyName = $"{AssemblyName}.{Guid.NewGuid():N}";
            var moduleBuilder = AssemblyBuilder
                .DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect)
                .DefineDynamicModule(assemblyName);
            var typeBuilder = moduleBuilder.DefineType(
                $"{AssemblyName}.Dynamic_{Guid.NewGuid():N}",
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
                typeof(DynamicBase));

            var fields = new FieldBuilder[schema.Length];
            for (var i = 0; i < schema.Length; i++)
            {
                var item = schema[i];
                var field = typeBuilder.DefineField($"field_{item.Key}", item.Value, FieldAttributes.Private);
                fields[i] = field;
                var property = typeBuilder.DefineProperty(item.Key, PropertyAttributes.None, item.Value, null);

                var getter = typeBuilder.DefineMethod($"get_{item.Key}",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    item.Value, Type.EmptyTypes);
                var getterIl = getter.GetILGenerator();
                getterIl.Emit(OpCodes.Ldarg_0);
                getterIl.Emit(OpCodes.Ldfld, field);
                getterIl.Emit(OpCodes.Ret);
                property.SetGetMethod(getter);

                var setter = typeBuilder.DefineMethod($"set_{item.Key}",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    typeof(void), new[] { item.Value });
                setter.DefineParameter(1, ParameterAttributes.None, "value");
                var setterIl = setter.GetILGenerator();
                setterIl.Emit(OpCodes.Ldarg_0);
                setterIl.Emit(OpCodes.Ldarg_1);
                setterIl.Emit(OpCodes.Stfld, field);
                setterIl.Emit(OpCodes.Ret);
                property.SetSetMethod(setter);
            }

            var defaultConstructor = typeBuilder.DefineConstructor(MethodAttributes.Public,
                CallingConventions.HasThis, Type.EmptyTypes);
            var defaultIl = defaultConstructor.GetILGenerator();
            defaultIl.Emit(OpCodes.Ldarg_0);
            defaultIl.Emit(OpCodes.Call, _baseConstructor);
            defaultIl.Emit(OpCodes.Ret);

            if (schema.Length > 0)
            {
                var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public,
                    CallingConventions.HasThis, schema.Select(item => item.Value).ToArray());
                var constructorIl = constructor.GetILGenerator();
                constructorIl.Emit(OpCodes.Ldarg_0);
                constructorIl.Emit(OpCodes.Call, _baseConstructor);
                for (var i = 0; i < schema.Length; i++)
                {
                    constructor.DefineParameter(i + 1, ParameterAttributes.None, schema[i].Key);
                    constructorIl.Emit(OpCodes.Ldarg_0);
                    constructorIl.Emit(OpCodes.Ldarg, unchecked((short)(i + 1)));
                    constructorIl.Emit(OpCodes.Stfld, fields[i]);
                }
                constructorIl.Emit(OpCodes.Ret);
            }

            var generatedType = typeBuilder.CreateTypeInfo()!.AsType();
            foreach (var item in schema)
            {
                if (generatedType.GetProperty(item.Key)!.PropertyType != item.Value)
                    throw new ArgumentException($"动态属性 '{item.Key}' 的类型无法保持原有运行时身份。", "properties");
            }
            System.Threading.Interlocked.Increment(ref _buildCount);
            return generatedType;
    }
}

#endif
