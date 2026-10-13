using Bing.Reflection;
using Bing.Conversions;
using BingTypeConverter = Bing.Conversions.TypeConverter;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReflectionTrimProbe;

public static class Program
{
    public static void Main()
    {
        var probeAssembly = typeof(Program).Assembly;
        if (string.IsNullOrWhiteSpace(AssemblyVisit.GetFileVersion(probeAssembly))
            || string.IsNullOrWhiteSpace(AssemblyVisit.GetProductVersion(probeAssembly)))
            throw new InvalidOperationException("Assembly version metadata was unavailable in the trim/AOT probe.");

        VerifyRootedCrossAssemblyModel();
        VerifySecondRootedCrossAssemblyModel();
        VerifyTypePredicateEntrypoints();
        VerifyConstructorAndAsyncEntrypoints();
        VerifyMemberClassificationEntrypoints();
        VerifyByRefMethodSignatureEntrypoints();
        VerifyRefReturnSignatureEntrypoints();
        VerifyTypeReflectionsMemberPredicates();
        VerifyTypeRelationshipEntrypoints();
        VerifyGenericVarianceEntrypoints();

        var instance = TypeVisit.CreateInstance<TrimSample>(7);
        if (instance.Value != 7)
            throw new InvalidOperationException("Trimmed constructor binding returned an unexpected value.");

        var property = typeof(TrimSample).GetProperty(nameof(TrimSample.Value))
            ?? throw new MissingMemberException(typeof(TrimSample).FullName, nameof(TrimSample.Value));
        var visitProperties = TypeVisit.GetProperties(typeof(TrimSample), PropertyAccessOptions.Both).ToArray();
        if (!visitProperties.Any(candidate => candidate.Name == nameof(TrimSample.Value)))
            throw new InvalidOperationException("Trimmed TypeVisit property scan returned an unexpected result.");
        var visitFields = TypeVisit.GetFields(typeof(TrimFieldSample)).ToArray();
        if (!visitFields.Any(candidate => candidate.Name == nameof(TrimFieldSample.PublicField)))
            throw new InvalidOperationException("Trimmed TypeVisit field scan returned an unexpected result.");
        var publicProperties = TypeReflections.GetPublicInstanceProperties(typeof(TrimSample));
        var genericPublicProperties = TypeReflections.GetPublicInstanceProperties<TrimSample>();
        if (!publicProperties.Any(candidate => candidate.Name == nameof(TrimSample.Value))
            || !genericPublicProperties.Any(candidate => candidate.Name == nameof(TrimSample.Value)))
            throw new InvalidOperationException("Trimmed TypeReflections property cache returned an unexpected result.");
        var readableProperties = TypeReflections.GetPublicReadableInstanceProperties(typeof(TrimSample));
        var writableProperties = TypeReflections.GetPublicWritableInstanceProperties(typeof(TrimSample));
        var reflectedProperties = Reflections.GetProperties(typeof(TrimSample));
        var reflectedInstanceProperties = Reflections.GetProperties(typeof(TrimSample), BindingFlags.Public | BindingFlags.Instance);
        var genericReflectedProperties = Reflections.GetProperties<TrimSample>();
        var reflectedMethods = Reflections.GetMethods<TrimSample>();
        var reflectedInstanceMethods = Reflections.GetMethods<TrimSample>(BindingFlags.Public | BindingFlags.Instance);
        var propertyOrField = Reflections.GetPropertyOrField<TrimSample>(nameof(TrimSample.Value));
        var publicPropertyItems = Reflections.GetPublicProperties(instance);
        Reflections.SetPropertyValueByPath(instance, typeof(TrimSample), nameof(TrimSample.Value), 9);
        var pathPropertyValue = Reflections.GetPropertyValueByPath(
            instance, typeof(TrimSample), nameof(TrimSample.Value));
        if (!Equals(pathPropertyValue, 9))
            throw new InvalidOperationException("Trimmed property path wrappers returned an unexpected value.");
        Reflections.SetPropertyValueByPath(instance, typeof(TrimSample), nameof(TrimSample.Value), 7);
        if (!readableProperties.Any(candidate => candidate.Name == nameof(TrimSample.Value))
            || !writableProperties.Any(candidate => candidate.Name == nameof(TrimSample.Value))
            || !reflectedProperties.Any(candidate => candidate.Name == nameof(TrimSample.Value))
            || !reflectedInstanceProperties.Any(candidate => candidate.Name == nameof(TrimSample.Value))
            || !genericReflectedProperties.Any(candidate => candidate.Name == nameof(TrimSample.Value))
            || !reflectedMethods.Any(candidate => candidate.Name == nameof(TrimSample.Execute))
            || !reflectedInstanceMethods.Any(candidate => candidate.Name == nameof(TrimSample.Execute))
            || propertyOrField is not PropertyInfo propertyOrFieldInfo
            || propertyOrFieldInfo.Name != nameof(TrimSample.Value)
            || publicPropertyItems.Count != 1
            || publicPropertyItems[0].Text != nameof(TrimSample.Value)
            || !Equals(publicPropertyItems[0].Value, 7))
            throw new InvalidOperationException("Trimmed reflection collection wrappers returned an unexpected result.");
        var typeDescription = Reflections.GetDescription(typeof(TrimSample));
        if (typeDescription != "Trim sample")
            throw new InvalidOperationException("Trimmed type attribute lookup returned an unexpected description.");
        var propertyDescription = Reflections.GetDescription(typeof(TrimSample), nameof(TrimSample.Value));
        if (propertyDescription != "Trim value")
            throw new InvalidOperationException("Trimmed member attribute lookup returned an unexpected description.");
        var descriptions = Reflections.GetAttributes<DescriptionAttribute>(typeof(TrimSample)).ToArray();
        if (descriptions.Length != 1 || descriptions[0].Description != "Trim sample")
            throw new InvalidOperationException("Trimmed generic attribute lookup returned an unexpected result.");
        var typeReflectionsDescription = TypeReflections.GetDescription(typeof(TrimSample));
        if (typeReflectionsDescription != "Trim sample"
            || !TypeReflections.IsDescriptionDefined(typeof(TrimSample)))
            throw new InvalidOperationException("Trimmed TypeReflections type description lookup returned an unexpected result.");
        var typeReflectionsAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(typeof(TrimSample));
        if (typeReflectionsAttribute?.Description != "Trim sample")
            throw new InvalidOperationException("Trimmed TypeReflections type attribute lookup returned an unexpected result.");
        var propertyReflectionsDescription = TypeReflections.GetDescription(property);
        if (propertyReflectionsDescription != "Trim value"
            || !TypeReflections.IsDescriptionDefined(property))
            throw new InvalidOperationException("Trimmed TypeReflections member description lookup returned an unexpected result.");
        var propertyReflectionsAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(property);
        if (propertyReflectionsAttribute?.Description != "Trim value")
            throw new InvalidOperationException("Trimmed TypeReflections member attribute lookup returned an unexpected result.");
        var typeReflectionsAttributes = TypeReflections.GetAttributes<DescriptionAttribute>(typeof(TrimSample)).ToArray();
        if (typeReflectionsAttributes.Length != 1 || typeReflectionsAttributes[0].Description != "Trim sample")
            throw new InvalidOperationException("Trimmed TypeReflections generic attribute lookup returned an unexpected result.");
        var method = TypeVisit.FindMethod(typeof(TrimSample), nameof(TrimSample.Execute), Type.EmptyTypes);
        if (method.ReturnType != typeof(void))
            throw new InvalidOperationException("Trimmed method lookup returned an unexpected signature.");

        var generic = TypeVisit.CreateInstance<TrimGenericSample<int>>(9);
        var genericProperty = typeof(TrimGenericSample<int>).GetProperty(nameof(TrimGenericSample<int>.Value))
            ?? throw new MissingMemberException(typeof(TrimGenericSample<int>).FullName, nameof(TrimGenericSample<int>.Value));
        var genericMethod = TypeVisit.FindMethod(typeof(TrimGenericSample<int>), nameof(TrimGenericSample<int>.Execute), Type.EmptyTypes);
        if (!Equals(genericProperty.GetValue(generic), 9) || genericMethod.ReturnType != typeof(void))
            throw new InvalidOperationException("Trimmed generic model metadata returned an unexpected result.");

        var container = new TrimContainer(new TrimSample(11));
        if (!Reflections.TryGetMemberValueByPath(container, "Child.Value", out var nestedValue)
            || !Equals(nestedValue, 11))
            throw new InvalidOperationException("Trimmed nested member path returned an unexpected value.");
        if (!Reflections.TrySetMemberValueByPath(container, "Child.Value", 12)
            || container.Child.Value != 12)
            throw new InvalidOperationException("Trimmed nested member path setter returned an unexpected value.");

        var fieldSample = new TrimFieldSample(21);
        if (!Equals(Reflections.GetFieldValue(fieldSample, nameof(TrimFieldSample.PublicField)), 21))
            throw new InvalidOperationException("Trimmed public field lookup returned an unexpected value.");
        Reflections.SetFieldValue(fieldSample, nameof(TrimFieldSample.PublicField), 22);
        if (fieldSample.PublicField != 22)
            throw new InvalidOperationException("Trimmed public field setter did not update the value.");

        var privateField = Reflections.GetField<TrimFieldSample>(
            "PrivateField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (!Equals(privateField.GetValue(fieldSample), 23))
            throw new InvalidOperationException("Trimmed non-public field lookup returned an unexpected value.");
        privateField.SetValue(fieldSample, 24);
        if (fieldSample.ReadPrivateField() != 24)
            throw new InvalidOperationException("Trimmed non-public field setter did not update the value.");

        var privateContainer = new TrimPrivateContainer(new TrimPrivateNode(31));
        if (Reflections.TryGetMemberValueByPath(privateContainer, "Child.Value", out _))
            throw new InvalidOperationException("Non-public member path unexpectedly succeeded without explicit opt-in.");
        var nonPublicOptions = new MemberPathOptions
        {
            IncludeFields = false,
            IncludeNonPublic = true
        };
        if (!Reflections.TryGetMemberValueByPath(privateContainer, "Child.Value", out var privateValue,
                nonPublicOptions) || !Equals(privateValue, 31))
            throw new InvalidOperationException("Trimmed non-public member path getter returned an unexpected value.");
        if (!Reflections.TrySetMemberValueByPath(privateContainer, "Child.Value", 32, nonPublicOptions)
            || privateContainer.Child.ReadValue() != 32)
            throw new InvalidOperationException("Trimmed non-public member path setter did not update the value.");

        var valueProperty = typeof(TrimValueSample).GetProperty(nameof(TrimValueSample.Value))
            ?? throw new MissingMemberException(typeof(TrimValueSample).FullName, nameof(TrimValueSample.Value));

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            // NativeAOT/无动态代码运行时必须使用公开特性 API，不能触发第三方 Reflection.Emit 反射器。
            var fallbackTypeAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(typeof(TrimSample));
            var fallbackRuntimeAttribute = Reflections.GetAttribute(
                typeof(TrimSample), typeof(DescriptionAttribute));
            var fallbackAttributes = Reflections.GetAttributes<DescriptionAttribute>(typeof(TrimSample)).ToArray();
            var fallbackMethod = typeof(TrimMemberSample).GetMethod(
                nameof(TrimMemberSample.Execute), new[] { typeof(int) })
                ?? throw new MissingMethodException(typeof(TrimMemberSample).FullName, nameof(TrimMemberSample.Execute));
            var fallbackParameter = fallbackMethod.GetParameters().Single();
            var fallbackMethodAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(fallbackMethod);
            var fallbackMethodRuntimeAttribute = Reflections.GetAttribute(
                fallbackMethod, typeof(DescriptionAttribute));
            var fallbackMethodAttributes = Reflections.GetAttributes<DescriptionAttribute>(fallbackMethod).ToArray();
            var fallbackParameterAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(fallbackParameter);
            var fallbackParameterAttributes = TypeReflections.GetAttributes<DescriptionAttribute>(fallbackParameter).ToArray();
            if (fallbackTypeAttribute?.Description != "Trim sample"
                || fallbackRuntimeAttribute is not DescriptionAttribute runtimeAttribute
                || runtimeAttribute.Description != "Trim sample"
                || fallbackAttributes.Length != 1
                || fallbackAttributes[0].Description != "Trim sample"
                || !TypeReflections.IsAttributeDefined<DescriptionAttribute>(typeof(TrimSample))
                || !TypeReflections.IsAttributeDefined(typeof(TrimSample), typeof(DescriptionAttribute))
                || fallbackMethodAttribute?.Description != "Trim member execute"
                || fallbackMethodRuntimeAttribute is not DescriptionAttribute methodRuntimeAttribute
                || methodRuntimeAttribute.Description != "Trim member execute"
                || fallbackMethodAttributes.Length != 1
                || fallbackMethodAttributes[0].Description != "Trim member execute"
                || !TypeReflections.IsAttributeDefined<DescriptionAttribute>(fallbackMethod)
                || !TypeReflections.IsAttributeDefined(fallbackMethod, typeof(DescriptionAttribute))
                || fallbackParameterAttribute?.Description != "Trim member value"
                || fallbackParameterAttributes.Length != 1
                || fallbackParameterAttributes[0].Description != "Trim member value"
                || !TypeReflections.IsAttributeDefined<DescriptionAttribute>(fallbackParameter)
                || !TypeReflections.IsAttributeDefined(fallbackParameter, typeof(DescriptionAttribute)))
                throw new InvalidOperationException("NativeAOT attribute fallback returned an unexpected result.");

            // NativeAOT 路径只验证保留元数据的直接访问，不调用 Expression.Compile。
            if (!Equals(property.GetValue(instance), 7))
                throw new InvalidOperationException("AOT property reflection getter returned an unexpected value.");
            property.SetValue(instance, 8);
            if (instance.Value != 8)
                throw new InvalidOperationException("AOT property reflection setter did not update the value.");
            var boxedValueSample = (object)new TrimValueSample(3);
            valueProperty.SetValue(boxedValueSample, 4);
            if (((TrimValueSample)boxedValueSample).Value != 4)
                throw new InvalidOperationException("AOT value-type property reflection setter did not update the boxed value.");

            try
            {
                TypeVisit.CreateDynamicType(new Dictionary<string, Type> { [nameof(TrimSample.Value)] = typeof(int) });
                throw new InvalidOperationException("Dynamic type generation unexpectedly succeeded without dynamic code support.");
            }
            catch (PlatformNotSupportedException)
            {
                // 动态 Emit 在 NativeAOT 中必须明确拒绝。
            }

#pragma warning disable IL2026, IL3050
            try
            {
                TypeVisit.CreateInstance(new Dictionary<string, object>
                {
                    [nameof(TrimSample.Value)] = 1
                });
                throw new InvalidOperationException("Dynamic object creation unexpectedly succeeded without dynamic code support.");
            }
            catch (PlatformNotSupportedException)
            {
                // 动态对象包装同样依赖 Reflection.Emit，必须明确拒绝。
            }
#pragma warning restore IL2026, IL3050

#pragma warning disable IL2026, IL3050
            var batchInstances = TypeVisit.CreateInstances<TrimSample>(new[] { new object[] { 1 } });
            if (batchInstances.Count != 1 || batchInstances[0].Value != 1)
                throw new InvalidOperationException("AOT batch constructor creation returned an unexpected result.");
#pragma warning restore IL2026, IL3050

            Console.WriteLine("reflection-aot-attribute-fallback-ok");
            Console.WriteLine("reflection-aot-metadata-ok");
            return;
        }

        var valueSample = new TrimValueSample(3);
        var refSetter = valueProperty.GetValueSetterByRef<TrimValueSample>();
        refSetter(ref valueSample, 4);
        if (valueSample.Value != 4)
            throw new InvalidOperationException("Value-type property ref setter did not update the original value.");

        var reflectionsInstance = Reflections.CreateInstance<TrimSample>(typeof(TrimSample), 8);
        if (reflectionsInstance.Value != 8)
            throw new InvalidOperationException("Reflections constructor wrapper returned an unexpected value.");

        var directPropertyValue = Reflections.GetPropertyValue(instance, nameof(TrimSample.Value));
        if (!Equals(directPropertyValue, 7))
            throw new InvalidOperationException("Trimmed direct property getter returned an unexpected value.");
        Reflections.SetPropertyValue(instance, nameof(TrimSample.Value), 8);
        if (instance.Value != 8)
            throw new InvalidOperationException("Trimmed direct property setter did not update the value.");
        Reflections.SetPropertyValue(instance, nameof(TrimSample.Value), 7);

        var getter = property.GetValueGetter<TrimSample>();
        var setter = property.GetValueSetter<TrimSample>();
        if (!Equals(getter(instance), 7))
            throw new InvalidOperationException("Trimmed property getter returned an unexpected value.");
        setter(instance, 8);
        if (instance.Value != 8)
            throw new InvalidOperationException("Trimmed property setter did not update the value.");

        Console.WriteLine("reflection-trim-ok");
    }

    private static void VerifyRootedCrossAssemblyModel()
    {
        var assembly = Assembly.Load("ReflectionTrimProbe.Models");
        var type = assembly.GetType("ReflectionTrimProbe.Models.UnrootedModel", throwOnError: true)
                   ?? throw new TypeLoadException("Rooted cross-assembly model type was not found.");
        var instance = Activator.CreateInstance(type)
                       ?? throw new MissingMethodException(type.FullName, ".ctor()");
        var options = new MemberPathOptions { IncludeFields = false, IncludeNonPublic = true };
        var property = type.GetProperty("ReflectedValue", BindingFlags.Instance | BindingFlags.NonPublic);
        var pathFound = Reflections.TryGetMemberValueByPath(instance, "ReflectedValue", out var value, options);
        if (property == null || !pathFound || !Equals(value, 41))
            throw new InvalidOperationException("Rooted cross-assembly reflection metadata was not available.");
    }

    private static void VerifyTypePredicateEntrypoints()
    {
        if (!Types.IsInterfaceDefined<TrimImplementation, ITrimContract>()
            || !TypeReflections.IsInterfaceDefined(typeof(TrimImplementation), typeof(ITrimContract))
            || !Types.IsInterfaceDefined(typeof(TrimCollection), typeof(IEnumerable<>), InterfaceOptions.IgnoreGenericArgs)
            || !TypeReflections.IsInterfaceDefined(
                typeof(TrimCollection), typeof(IEnumerable<string>), InterfaceOptions.IgnoreGenericArgs)
            || Types.IsInterfaceDefined(typeof(TrimCollection), typeof(IEnumerable<string>))
            || !Types.IsInterfaceDefined<TrimCollection, IEnumerable<int>>()
            || !Types.IsInterfaceDefined<TrimCollection, IEnumerable<string>>(InterfaceOptions.IgnoreGenericArgs))
            throw new InvalidOperationException("Trimmed interface predicate returned an unexpected result.");

        if (!Types.IsCollectionType<TrimCollection>())
            throw new InvalidOperationException("Trimmed collection predicate returned an unexpected result.");

        if (!Types.IsTupleType<(int, string)>()
            || !Types.IsValueTupleType<(int, string)>())
            throw new InvalidOperationException("Trimmed tuple predicate returned an unexpected result.");

        var nullablePredicate = Types.IsNullableType<int?>();
        var enumPredicate = Types.IsEnumType<TrimStatus>();
        var nullTypeInfoIsStruct = Types.IsStructType((TypeInfo)null!);
        var dateTypeInfoIsStruct = Types.IsStructType(typeof(DateTime).GetTypeInfo());
        var enumUnderlyingType = TypeReflections.GetEnumUnderlyingType<TrimStatus>();
        var enumUnderlyingName = enumUnderlyingType?.FullName ?? "<null>";
        if (!nullablePredicate || !enumPredicate || nullTypeInfoIsStruct || !dateTypeInfoIsStruct
            || enumUnderlyingType != typeof(byte))
            throw new InvalidOperationException(
                $"Trimmed nullable/enum predicate returned an unexpected result: nullable={nullablePredicate}, "
                + $"enum={enumPredicate}, nullTypeInfo={nullTypeInfoIsStruct}, dateTypeInfo={dateTypeInfoIsStruct}, "
                + $"underlying={enumUnderlyingName}.");

        if (!TypeReflections.IsImplementationOfGenericType(
                typeof(TrimCollection), typeof(IEnumerable<>), out var genericType, out var genericArguments)
            || genericType != typeof(IEnumerable<int>)
            || genericArguments.Length != 1
            || genericArguments[0] != typeof(int))
            throw new InvalidOperationException("Trimmed generic implementation lookup returned an unexpected result.");
    }

    private static void VerifyConstructorAndAsyncEntrypoints()
    {
        var parameterlessType = typeof(TrimParameterlessSample);
        if (!TypeVisit.HasParameterlessConstructor(parameterlessType)
            || TypeVisit.GetParameterlessConstructor(parameterlessType).GetParameters().Length != 0
            || !parameterlessType.HasParameterlessConstructor()
            || parameterlessType.GetParameterlessConstructor().GetParameters().Length != 0)
            throw new InvalidOperationException("Trimmed parameterless constructor metadata returned an unexpected result.");

        if (TypeVisit.HasParameterlessConstructor(typeof(ITrimContract))
            || TypeVisit.HasParameterlessConstructor(typeof(TrimBaseSample))
            || typeof(ITrimContract).HasParameterlessConstructor()
            || typeof(TrimBaseSample).HasParameterlessConstructor())
            throw new InvalidOperationException("Non-creatable constructor probes returned an unexpected result.");

        var taskInfo = typeof(Task).GetTypeInfo();
        var taskResultInfo = typeof(Task<int>).GetTypeInfo();
        var voidTaskResultType = Type.GetType("System.Threading.Tasks.VoidTaskResult", throwOnError: true)!;
        var voidTaskResultInfo = typeof(Task<>).MakeGenericType(voidTaskResultType).GetTypeInfo();
        var valueTaskInfo = typeof(ValueTask).GetTypeInfo();
        var valueTaskResultInfo = typeof(ValueTask<int>).GetTypeInfo();
        if (!TypeReflections.IsTask(taskInfo)
            || !TypeReflections.IsTaskWithResult(taskResultInfo)
            || !TypeReflections.IsTaskWithVoidTaskResult(voidTaskResultInfo)
            || !TypeReflections.IsValueTask(valueTaskInfo)
            || !TypeReflections.IsValueTaskWithResult(valueTaskResultInfo)
            || taskInfo.IsTask() is false
            || taskResultInfo.IsTaskWithResult() is false
            || voidTaskResultInfo.IsTaskWithVoidTaskResult() is false
            || valueTaskInfo.IsValueTask() is false
            || valueTaskResultInfo.IsValueTaskWithResult() is false)
            throw new InvalidOperationException("Trimmed task type predicates returned an unexpected result.");

#pragma warning disable IL2026, IL3050
        var batchInstances = TypeVisit.CreateInstances<TrimSample>(
            new[] { new object[] { 1 }, new object[] { 2 } });
        var runtimeBatchInstances = TypeVisit.CreateInstances(
            new[] { typeof(TrimSample), typeof(TrimSample) }, 3);
        var typedRuntimeBatchInstances = TypeVisit.CreateInstances<TrimSample>(
            new[] { typeof(TrimSample) }, 4);
        var genericInstance = TypeVisit.CreateGenericInstance<TrimGenericSample<int>>(
            typeof(TrimGenericSample<>), 5);
        var genericInstanceWithArguments = TypeVisit.CreateGenericInstance<TrimGenericSample<int>>(
            typeof(TrimGenericSample<>), new[] { typeof(int) }, 6);
#pragma warning restore IL2026, IL3050

        if (batchInstances.Count != 2
            || batchInstances[0].Value != 1
            || batchInstances[1].Value != 2
            || runtimeBatchInstances.Count != 2
            || runtimeBatchInstances.Any(item => item.GetType() != typeof(TrimSample)
                || ((TrimSample)item).Value != 3)
            || typedRuntimeBatchInstances.Count != 1
            || typedRuntimeBatchInstances[0].Value != 4
            || genericInstance.Value != 5
            || genericInstanceWithArguments.Value != 6)
            throw new InvalidOperationException("Trimmed batch and generic constructor entrypoints returned an unexpected result.");
    }

    private static void VerifyMemberClassificationEntrypoints()
    {
        var type = typeof(TrimMemberSample);
        var numericProperty = type.GetProperty(nameof(TrimMemberSample.Numeric))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.Numeric));
        var tupleProperty = type.GetProperty(nameof(TrimMemberSample.Tuple))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.Tuple));
        var structProperty = type.GetProperty(nameof(TrimMemberSample.StructValue))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.StructValue));
        var numericField = type.GetField(nameof(TrimMemberSample.NumericField))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.NumericField));
        var tupleField = type.GetField(nameof(TrimMemberSample.TupleField))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.TupleField));
        var method = type.GetMethod(nameof(TrimMemberSample.Execute), new[] { typeof(int) })
            ?? throw new MissingMethodException(type.FullName, nameof(TrimMemberSample.Execute));
        var parameter = method.GetParameters().Single();
        var expressionField = TypeVisit.GetField<TrimMemberSample, int>(item => item.NumericField);
        var expressionProperty = TypeVisit.GetProperty<TrimMemberSample, int>(item => item.Numeric);
        var asyncMethod = type.GetMethod(nameof(TrimMemberSample.ExecuteAsync))
            ?? throw new MissingMethodException(type.FullName, nameof(TrimMemberSample.ExecuteAsync));
        var overrideMethod = typeof(TrimMemberDerived).GetMethod(nameof(TrimMemberDerived.ExecuteVirtual))
            ?? throw new MissingMethodException(typeof(TrimMemberDerived).FullName, nameof(TrimMemberDerived.ExecuteVirtual));
        var baseMethod = TypeVisit.GetBaseMethod(overrideMethod);
        var signatureMethod = TypeVisit.GetMethodBySignature(typeof(TrimMemberBase), overrideMethod);

        if (expressionField.Name != nameof(TrimMemberSample.NumericField)
            || expressionProperty.Name != nameof(TrimMemberSample.Numeric)
            || numericProperty.IsDescriptionDefined()
            || numericProperty.GetDescriptionOr("missing-description") != "missing-description"
            || !numericProperty.IsNumeric()
            || !numericField.IsNumeric()
            || !parameter.IsNumeric()
            || !tupleProperty.IsTupleType()
            || !tupleField.IsTupleType()
            || !structProperty.IsStructType()
            || !asyncMethod.IsAsyncMethod()
            || !overrideMethod.IsOverridden()
            || !overrideMethod.IsVisibleAndVirtual()
            || !TypeVisit.IsVisible(overrideMethod)
            || !TypeVisit.IsVisibleAndVirtual(overrideMethod)
            || baseMethod is null
            || signatureMethod.DeclaringType != typeof(TrimMemberBase)
            || !TypeVisit.IsSignatureCompatible(baseMethod, overrideMethod)
            || !numericProperty.IsAttributeNotDefined<DescriptionAttribute>()
            || !numericProperty.IsAttributeNotDefined(typeof(DescriptionAttribute)))
            throw new InvalidOperationException("Trimmed member classification returned an unexpected result.");

        var virtualProperty = typeof(TrimMemberDerived).GetProperty(nameof(TrimMemberDerived.VirtualValue))
            ?? throw new MissingMemberException(typeof(TrimMemberDerived).FullName, nameof(TrimMemberDerived.VirtualValue));
        if (!virtualProperty.IsVirtual()
            || !virtualProperty.IsVisibleAndVirtual()
            || virtualProperty.IsAbstract())
            throw new InvalidOperationException("Trimmed property virtual classification returned an unexpected result.");

        var selected = TypeVisit.GetProperties<TrimMemberSample>(
            PropertyAccessOptions.Both,
            item => item.Numeric,
            item => item.StructValue).ToArray();
        if (selected.Length != 2
            || !selected.Any(item => item.Name == nameof(TrimMemberSample.Numeric))
            || !selected.Any(item => item.Name == nameof(TrimMemberSample.StructValue)))
            throw new InvalidOperationException("Trimmed expression property selection returned an unexpected result.");

        var excluded = TypeVisit.Exclude<TrimMemberSample>(
            TypeVisit.GetProperties<TrimMemberSample>(PropertyAccessOptions.Both),
            item => item.Numeric).ToArray();
        if (excluded.Any(item => item.Name == nameof(TrimMemberSample.Numeric)))
            throw new InvalidOperationException("Trimmed expression property exclusion returned an unexpected result.");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针按显式 linker root 验证 ref/in/out 方法签名。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针按显式 linker root 验证 ref/in/out 方法元数据。")]
    private static void VerifyByRefMethodSignatureEntrypoints()
    {
        var type = typeof(TrimMemberSample);
        var refMethod = TypeVisit.FindMethod(
            type,
            nameof(TrimMemberSample.RefSignature),
            0,
            new MethodParameterSignature(typeof(int), ParameterPassingKind.Ref));
        var inMethod = TypeVisit.FindMethod(
            type,
            nameof(TrimMemberSample.InSignature),
            0,
            new MethodParameterSignature(typeof(int), ParameterPassingKind.In));
        var outMethod = TypeVisit.FindMethod(
            type,
            nameof(TrimMemberSample.OutSignature),
            0,
            new MethodParameterSignature(typeof(int), ParameterPassingKind.Out));

        var refParameter = refMethod.GetParameters().Single();
        var inParameter = inMethod.GetParameters().Single();
        var outParameter = outMethod.GetParameters().Single();
        if (refMethod.ReturnType != typeof(void)
            || inMethod.ReturnType != typeof(void)
            || outMethod.ReturnType != typeof(void)
            || !IsPassingKind(refParameter, ParameterPassingKind.Ref)
            || !IsPassingKind(inParameter, ParameterPassingKind.In)
            || !IsPassingKind(outParameter, ParameterPassingKind.Out))
            throw new InvalidOperationException("Trimmed by-ref method signatures returned an unexpected metadata shape.");

        AssertMissingPassingKind(nameof(TrimMemberSample.RefSignature), ParameterPassingKind.In);
        AssertMissingPassingKind(nameof(TrimMemberSample.RefSignature), ParameterPassingKind.Out);
        AssertMissingPassingKind(nameof(TrimMemberSample.InSignature), ParameterPassingKind.Ref);
        AssertMissingPassingKind(nameof(TrimMemberSample.InSignature), ParameterPassingKind.Out);
        AssertMissingPassingKind(nameof(TrimMemberSample.OutSignature), ParameterPassingKind.Ref);
        AssertMissingPassingKind(nameof(TrimMemberSample.OutSignature), ParameterPassingKind.In);

        Console.WriteLine("reflection-trim-byref-signature-ok");

        static bool IsPassingKind(ParameterInfo parameter, ParameterPassingKind expected)
        {
            if (parameter.ParameterType != typeof(int).MakeByRefType())
                return false;
            var actual = parameter.IsOut
                ? ParameterPassingKind.Out
                : parameter.IsIn || parameter.GetRequiredCustomModifiers().Any(modifier =>
                    string.Equals(modifier.FullName,
                        "System.Runtime.CompilerServices.IsReadOnlyAttribute",
                        StringComparison.Ordinal))
                    ? ParameterPassingKind.In
                    : ParameterPassingKind.Ref;
            return actual == expected;
        }

        static void AssertMissingPassingKind(string methodName, ParameterPassingKind passingKind)
        {
            try
            {
                TypeVisit.FindMethod(
                    typeof(TrimMemberSample),
                    methodName,
                    0,
                    new MethodParameterSignature(typeof(int), passingKind));
            }
            catch (MissingMethodException)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Trimmed by-ref method {methodName} incorrectly matched {passingKind}.");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针按显式 linker root 验证 ref 返回值签名。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针按显式 linker root 验证 ref 返回值元数据。")]
    private static void VerifyRefReturnSignatureEntrypoints()
    {
        var type = typeof(TrimMemberSample);
        var refReturnType = typeof(int).MakeByRefType();
        var method = TypeVisit.FindMethodByReturnType(
            type,
            nameof(TrimMemberSample.RefReturnSignature),
            refReturnType);

        if (!method.ReturnType.IsByRef
            || method.ReturnType.GetElementType() != typeof(int)
            || method.GetParameters().Length != 0)
            throw new InvalidOperationException("Trimmed ref return signature returned an unexpected metadata shape.");

        try
        {
            TypeVisit.FindMethodByReturnType(
                type,
                nameof(TrimMemberSample.RefReturnSignature),
                typeof(int));
        }
        catch (MissingMethodException)
        {
            Console.WriteLine("reflection-trim-ref-return-ok");
            return;
        }

        throw new InvalidOperationException("A ref return method incorrectly matched a by-value return type.");
    }

    private static void VerifyTypeReflectionsMemberPredicates()
    {
        var type = typeof(TrimMemberSample);
        var booleanProperty = type.GetProperty(nameof(TrimMemberSample.BooleanValue))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.BooleanValue));
        var dateTimeProperty = type.GetProperty(nameof(TrimMemberSample.DateTimeValue))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.DateTimeValue));
        var nullableDateTimeProperty = type.GetProperty(nameof(TrimMemberSample.NullableDateTime))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.NullableDateTime));
        var nullableNumericProperty = type.GetProperty(nameof(TrimMemberSample.NullableNumeric))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.NullableNumeric));
        var statusProperty = type.GetProperty(nameof(TrimMemberSample.Status))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.Status));
        var nullableStatusProperty = type.GetProperty(nameof(TrimMemberSample.NullableStatus))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.NullableStatus));
        var collectionProperty = type.GetProperty(nameof(TrimMemberSample.Collection))
            ?? throw new MissingMemberException(type.FullName, nameof(TrimMemberSample.Collection));

        if (!TypeReflections.IsBoolean(booleanProperty)
            || !TypeReflections.IsDateTime(dateTimeProperty)
            || TypeReflections.IsDateTime(nullableDateTimeProperty)
            || !TypeReflections.IsDateTime(nullableDateTimeProperty, TypeIsOptions.IgnoreNullable)
            || TypeReflections.IsNumeric(nullableNumericProperty)
            || !TypeReflections.IsNumeric(nullableNumericProperty, TypeIsOptions.IgnoreNullable)
            || !TypeReflections.IsEnum(statusProperty)
            || TypeReflections.IsEnum(nullableStatusProperty)
            || !TypeReflections.IsEnum(nullableStatusProperty, TypeIsOptions.IgnoreNullable)
            || !TypeReflections.IsStruct(dateTimeProperty)
            || !TypeReflections.IsStruct(nullableDateTimeProperty, TypeIsOptions.IgnoreNullable)
            || !TypeReflections.IsPrimitive(nullableNumericProperty, TypeIsOptions.IgnoreNullable)
            || !TypeReflections.IsValueType(statusProperty)
            || !TypeReflections.IsCollection(collectionProperty))
            throw new InvalidOperationException("Trimmed TypeReflections member predicates returned an unexpected result.");
    }

    private static void VerifyTypeRelationshipEntrypoints()
    {
        var ownerTypes = Types.Of<int?, string>(TypeOfOptions.Owner).ToArray();
        var underlyingTypes = Types.Of<int?, string>(TypeOfOptions.Underlying).ToArray();
        var objectTypes = Types.Of(new object[] { 1, "text", null! });
        var nullObject = (object)null!;
        var genericBaseType = typeof(TrimGenericBase<>);

        if (ownerTypes.Length != 2
            || ownerTypes[0] != typeof(int?)
            || ownerTypes[1] != typeof(string)
            || underlyingTypes.Length != 2
            || underlyingTypes[0] != typeof(int)
            || underlyingTypes[1] != typeof(string)
            || objectTypes.Length != 2
            || objectTypes[0] != typeof(int)
            || objectTypes[1] != typeof(string)
            || Types.DefaultValue<int>() != 0
            || !Equals(Types.DefaultValue(typeof(int)), 0)
            || Types.DefaultValue(typeof(string)) is not null
            || !Types.IsDefaultValue(0)
            || Types.IsDefaultValue(1)
            || !Types.IsDefaultValue(nullObject))
            throw new InvalidOperationException("Trimmed type value helpers returned an unexpected result.");

        if (!Types.IsGenericImplementation(typeof(TrimCollection), typeof(IEnumerable<>))
            || !TypeReflections.IsTypeBasedOn(typeof(TrimDerivedSample), typeof(TrimBaseSample))
            || !TypeReflections.IsTypeBasedOn<TrimDerivedSample, TrimBaseSample>()
            || !TypeReflections.IsTypeDerivedFrom(typeof(TrimDerivedSample), typeof(TrimBaseSample))
            || !TypeReflections.IsTypeDerivedFrom<TrimDerivedSample, TrimBaseSample>()
            || !TypeReflections.IsObjectDerivedFrom(new TrimDerivedSample(), typeof(TrimBaseSample))
            || !Reflections.IsDeriveClassFrom<TrimBaseSample>(typeof(TrimDerivedSample))
            || !Reflections.IsBaseOn<TrimBaseSample>(typeof(TrimDerivedSample))
            || !Reflections.IsAssignableTo<TrimBaseSample>(typeof(TrimDerivedSample))
            || !Reflections.IsGenericAssignableFrom(typeof(IEnumerable<>), typeof(TrimCollection))
            || !Reflections.IsGenericAssignableFrom(typeof(IEnumerable<object>), typeof(List<string>))
            || Reflections.IsGenericAssignableFrom(typeof(IEnumerable<string>), typeof(List<object>))
            || !Reflections.IsGenericAssignableFrom(typeof(IComparer<string>), typeof(IComparer<object>))
            || Reflections.IsGenericAssignableFrom(typeof(IComparer<object>), typeof(IComparer<string>))
            || !TypeReflections.IsTypeBasedOn(typeof(List<string>), typeof(IEnumerable<object>)))
            throw new InvalidOperationException("Trimmed type relationship helpers returned an unexpected result.");

        var baseClassesWithoutObject = Reflections.GetBaseClasses(typeof(TrimDerivedSample), includeObject: false);
        var baseClassesWithObject = Reflections.GetBaseClasses(typeof(TrimDerivedSample), includeObject: true);
        var stoppedBaseClasses = Reflections.GetBaseClasses(
            typeof(TrimDerivedSample), typeof(TrimBaseSample), includeObject: true);
        var qualifiedName = Reflections.GetFullNameWithAssemblyName(typeof(TrimDerivedSample));
        if (!Reflections.IsCollection(typeof(int[]))
            || !Reflections.IsCollection(typeof(List<int>))
            || Reflections.IsCollection(typeof(string))
            || !Reflections.IsGenericCollection(typeof(IEnumerable<int>))
            || Reflections.IsGenericCollection(typeof(int[]))
            || Reflections.GetTopBaseType<TrimDerivedSample>() != typeof(TrimBaseSample)
            || Reflections.GetTopBaseType(typeof(ITrimContract)) != typeof(ITrimContract)
            || !Reflections.IsAssignableTo(typeof(List<string>), typeof(IEnumerable<object>))
            || Reflections.IsAssignableTo(typeof(List<object>), typeof(IEnumerable<string>))
            || baseClassesWithoutObject.Length != 1
            || baseClassesWithoutObject[0] != typeof(TrimBaseSample)
            || baseClassesWithObject.Length != 2
            || baseClassesWithObject[0] != typeof(object)
            || baseClassesWithObject[1] != typeof(TrimBaseSample)
            || stoppedBaseClasses.Length != 0
            || !qualifiedName.StartsWith(typeof(TrimDerivedSample).FullName + ", ", StringComparison.Ordinal))
            throw new InvalidOperationException("Trimmed base type and collection helpers returned an unexpected result.");

        var rawGenericType = TypeReflections.GetRawTypeFromGenericType(typeof(TrimGenericDerived), genericBaseType);
        var rawGenericArgs = TypeReflections.GetRawTypeArgsFromGenericType(typeof(TrimGenericDerived), genericBaseType);
        if (rawGenericType != typeof(TrimGenericBase<int>)
            || rawGenericArgs.Count != 1
            || rawGenericArgs[0] != typeof(int)
            || Types.GetRawTypeFromGenericClass(typeof(TrimGenericDerived), genericBaseType) != typeof(int)
            || Types.GetRawTypeFromGenericClass<TrimGenericDerived, TrimGenericBase<int>>() != typeof(int)
            || Reflections.GetRawTypeFromGenericClass(typeof(TrimGenericDerived), genericBaseType) != typeof(int)
            || Reflections.GetRawTypeFromGenericClass(typeof(TrimGenericDerived), typeof(TrimGenericBase<int>)) != typeof(int)
            || Types.IsGenericImplementation<TrimGenericDerived, TrimGenericBase<string>>()
            || Types.GetRawTypeFromGenericClass(typeof(TrimGenericDerived), typeof(TrimGenericBase<string>)) is not null)
            throw new InvalidOperationException("Trimmed generic raw type helpers returned an unexpected result.");

        var anonymousType = new { Value = 1 }.GetType();
        if (!Types.IsNumericType(typeof(int))
            || Types.IsNumericType(typeof(int?))
            || !Types.IsNumericType(typeof(int?), TypeIsOptions.IgnoreNullable)
            || !Types.IsPrimitiveType(typeof(int))
            || !Types.IsStaticType(typeof(Types))
            || !Types.IsAnonymousType(anonymousType)
            || Types.IsAnonymousType(typeof(TrimMemberSample)))
            throw new InvalidOperationException("Trimmed direct type predicate helpers returned an unexpected result.");

        var intTypeInfo = typeof(int).GetTypeInfo();
        var nullableIntTypeInfo = typeof(int?).GetTypeInfo();
        var enumTypeInfo = typeof(TrimStatus).GetTypeInfo();
        var nullableEnumTypeInfo = typeof(TrimStatus?).GetTypeInfo();
        if (!TypeJudge.IsNumericType<int>()
            || !TypeJudge.IsNumericType(intTypeInfo)
            || !TypeJudge.IsNullableType(nullableIntTypeInfo)
            || TypeJudge.IsNullableType(intTypeInfo)
            || !TypeJudge.IsEnumType(enumTypeInfo)
            || !TypeJudge.IsEnumType(nullableEnumTypeInfo, mayNullable: true)
            || !TypeJudge.IsGenericImplementation(typeof(TrimCollection), typeof(IEnumerable<>)))
            throw new InvalidOperationException("Trimmed TypeJudge overloads returned an unexpected result.");

        if (BingTypeConverter.ToNonNullableTypeInfo(intTypeInfo) is not null
            || BingTypeConverter.ToNonNullableTypeInfo(nullableIntTypeInfo) != intTypeInfo
            || BingTypeConverter.ToSafeNonNullableTypeInfo(intTypeInfo) != intTypeInfo
            || BingTypeConverter.ToSafeNonNullableTypeInfo(nullableIntTypeInfo) != intTypeInfo)
            throw new InvalidOperationException("Trimmed TypeConverter TypeInfo overloads returned an unexpected result.");

        if (Reflections.GetUnderlyingType(typeof(int?)) != typeof(int)
            || Reflections.GetUnderlyingType(typeof(int?).GetTypeInfo()) != typeof(int)
            || Reflections.GetElementType(typeof(int[])) != typeof(int)
            || Reflections.GetElementType(typeof(List<int>)) != typeof(int))
            throw new InvalidOperationException("Trimmed underlying and element type helpers returned an unexpected result.");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针按显式 linker root 验证自定义泛型方差和方法裁剪矩阵。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针按显式 linker root 验证自定义泛型方差元数据和方法裁剪矩阵。")]
    private static void VerifyGenericVarianceEntrypoints()
    {
        var implementationType = typeof(TrimVarianceSample);
        var covariantDefinition = typeof(ITrimCovariant<>);
        var contravariantDefinition = typeof(ITrimContravariant<>);
        var covariantParameter = covariantDefinition.GetGenericArguments().Single();
        var contravariantParameter = contravariantDefinition.GetGenericArguments().Single();
        var varianceMask = GenericParameterAttributes.VarianceMask;

        if ((covariantParameter.GenericParameterAttributes & varianceMask)
                != GenericParameterAttributes.Covariant
            || (contravariantParameter.GenericParameterAttributes & varianceMask)
                != GenericParameterAttributes.Contravariant)
            throw new InvalidOperationException("Custom generic variance parameter metadata was not preserved.");

        var covariantObject = typeof(ITrimCovariant<object>);
        var covariantString = typeof(ITrimCovariant<string>);
        var contravariantObject = typeof(ITrimContravariant<object>);
        var contravariantString = typeof(ITrimContravariant<string>);

        if (!covariantObject.IsAssignableFrom(covariantString)
            || covariantString.IsAssignableFrom(covariantObject)
            || !covariantObject.IsAssignableFrom(implementationType)
            || !contravariantString.IsAssignableFrom(contravariantObject)
            || contravariantObject.IsAssignableFrom(contravariantString)
            || !contravariantString.IsAssignableFrom(implementationType))
            throw new InvalidOperationException("CLR generic variance assignability returned an unexpected result.");

        if (!Reflections.IsGenericAssignableFrom(covariantObject, covariantString)
            || Reflections.IsGenericAssignableFrom(covariantString, covariantObject)
            || !Reflections.IsGenericAssignableFrom(covariantObject, implementationType)
            || !Reflections.IsGenericAssignableFrom(contravariantString, contravariantObject)
            || Reflections.IsGenericAssignableFrom(contravariantObject, contravariantString)
            || !Reflections.IsGenericAssignableFrom(contravariantString, implementationType))
            throw new InvalidOperationException("Reflections generic variance assignability returned an unexpected result.");

        if (!TypeReflections.IsInterfaceDefined(implementationType, covariantString)
            || TypeReflections.IsInterfaceDefined(implementationType, covariantObject)
            || !TypeReflections.IsInterfaceDefined(
                implementationType, covariantObject, InterfaceOptions.IgnoreGenericArgs)
            || !TypeReflections.IsInterfaceDefined(implementationType, contravariantObject)
            || TypeReflections.IsInterfaceDefined(implementationType, contravariantString)
            || !TypeReflections.IsInterfaceDefined(
                implementationType, contravariantString, InterfaceOptions.IgnoreGenericArgs))
            throw new InvalidOperationException("TypeReflections interface variance matching returned an unexpected result.");

        var rootedMethod = TypeVisit.FindMethod(
            implementationType, nameof(TrimVarianceSample.Produce), Type.EmptyTypes);
        if (rootedMethod.ReturnType != typeof(string))
            throw new InvalidOperationException("Rooted generic variance method returned an unexpected signature.");

        if (string.Equals(
                Environment.GetEnvironmentVariable("REFLECTION_TRIM_EXPECT_UNROOTED_METHOD"),
                "1",
                StringComparison.Ordinal))
        {
            try
            {
                TypeVisit.FindMethod(
                    implementationType,
                    "UnrootedMethod",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    0);
            }
            catch (MissingMethodException)
            {
                Console.WriteLine("reflection-trim-generic-variance-unrooted-method-trimmed");
                Console.WriteLine("reflection-trim-generic-variance-ok");
                return;
            }

            throw new InvalidOperationException("Unrooted generic variance method was unexpectedly preserved.");
        }

        Console.WriteLine("reflection-trim-generic-variance-ok");
    }

    private static void VerifySecondRootedCrossAssemblyModel()
    {
        var assembly = Assembly.Load("ReflectionTrimProbe.PluginModels");
        var type = assembly.GetType("ReflectionTrimProbe.PluginModels.PluginModel", throwOnError: true)
                   ?? throw new TypeLoadException("Second rooted cross-assembly model type was not found.");
        var instance = Activator.CreateInstance(type, 51)
                       ?? throw new MissingMethodException(type.FullName, ".ctor(int)");
        var options = new MemberPathOptions { IncludeFields = false, IncludeNonPublic = true };
        var pathFound = Reflections.TryGetMemberValueByPath(instance, "Child.Secret", out var value, options);
        if (!pathFound || !Equals(value, 51))
            throw new InvalidOperationException("Second rooted cross-assembly member path was not available.");
        if (!Reflections.TrySetMemberValueByPath(instance, "Child.Secret", 52, options)
            || !Equals(Reflections.GetMemberValueByPath(instance, "Child.Secret", options), 52))
            throw new InvalidOperationException("Second rooted cross-assembly member path setter failed.");
    }
}

[Description("Trim sample")]
public sealed class TrimSample
{
    public TrimSample(int value) => Value = value;

    [Description("Trim value")]
    public int Value { get; set; }

    public int SetterOnly
    {
        set { }
    }

    public static int StaticValue => 11;

    public int this[int index] => index;

    public void Execute()
    {
    }
}

public sealed class TrimGenericSample<T>
{
    public TrimGenericSample(T value) => Value = value;

    public T Value { get; set; }

    public void Execute()
    {
    }
}

public sealed class TrimParameterlessSample
{
    public TrimParameterlessSample()
    {
    }
}

public struct TrimValueSample
{
    public TrimValueSample(int value) => Value = value;

    public int Value { get; set; }
}

public sealed class TrimContainer
{
    public TrimContainer(TrimSample child) => Child = child;

    public TrimSample Child { get; }
}

public sealed class TrimFieldSample
{
    public TrimFieldSample(int value)
    {
        PublicField = value;
        PrivateField = value + 2;
    }

    public int PublicField;

    private int PrivateField;

    public int ReadPrivateField() => PrivateField;
}

public sealed class TrimPrivateContainer
{
    public TrimPrivateContainer(TrimPrivateNode child) => Child = child;

    public TrimPrivateNode Child { get; }
}

public sealed class TrimPrivateNode
{
    public TrimPrivateNode(int value) => Value = value;

    private int Value { get; set; }

    public int ReadValue() => Value;
}

public interface ITrimContract
{
}

public interface ITrimCovariant<out T>
{
    T Produce();
}

public interface ITrimContravariant<in T>
{
    void Consume(T value);
}

public sealed class TrimVarianceSample : ITrimCovariant<string>, ITrimContravariant<object>
{
    public string Produce() => string.Empty;

    public void Consume(object value)
    {
    }

    private void UnrootedMethod()
    {
    }
}

public sealed class TrimImplementation : ITrimContract
{
}

public sealed class TrimCollection : List<int>
{
}

public abstract class TrimBaseSample
{
}

public sealed class TrimDerivedSample : TrimBaseSample
{
}

public class TrimGenericBase<T>
{
}

public sealed class TrimGenericDerived : TrimGenericBase<int>
{
}

public enum TrimStatus : byte
{
    Ready = 1
}

public class TrimMemberBase
{
    public virtual int VirtualValue { get; set; }

    public virtual void ExecuteVirtual()
    {
    }
}

public sealed class TrimMemberDerived : TrimMemberBase
{
    public override int VirtualValue { get; set; }

    public override void ExecuteVirtual()
    {
    }
}

public sealed class TrimMemberSample
{
    public int Numeric { get; set; }

    public bool BooleanValue { get; set; }

    public DateTime DateTimeValue { get; set; }

    public DateTime? NullableDateTime { get; set; }

    public int? NullableNumeric { get; set; }

    public TrimStatus Status { get; set; }

    public TrimStatus? NullableStatus { get; set; }

    public List<int> Collection { get; set; } = new();

    public (int, string) Tuple { get; set; }

    public DateTime StructValue { get; set; }

    public int NumericField;

    public (int, string) TupleField;

    [Description("Trim member execute")]
    public void Execute([Description("Trim member value")] int value)
    {
    }

    public void Execute(string value)
    {
    }

    public void RefSignature(ref int value)
    {
    }

    public void InSignature(in int value)
    {
    }

    public void OutSignature(out int value) => value = 0;

    public ref int RefReturnSignature() => ref NumericField;

    public Task ExecuteAsync() => Task.CompletedTask;
}
