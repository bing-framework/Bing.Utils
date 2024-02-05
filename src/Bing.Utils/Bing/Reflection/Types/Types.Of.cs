// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 定义类型操作的选项。
/// </summary>
public enum TypeOfOptions
{
    /// <summary>
    /// 拥有者：指代类型本身的操作或者上下文。
    /// 在类型相关的操作中，这通常意味着直接对当前类型进行操作，而不考虑是否存在基础类型或封装的类型。
    /// </summary>
    Owner = 0,

    /// <summary>
    /// 基础类型：指代类型的基础类型操作，如果类型是封装或可空类型。
    /// 在类型相关的操作中，选择此选项意味着如果当前类型是一个可空类型或具有基础类型的复杂类型，操作将应用于该基础类型。
    /// 例如，对于一个可空的整数类型`int?`，选择此选项将会关注`int`类型本身。
    /// </summary>
    Underlying = 1
}

/// <summary>
/// 类型 操作
/// </summary>
public static partial class Types
{
    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static Type Of<T>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        return options switch
        {
            TypeOfOptions.Owner => typeof(T),
            TypeOfOptions.Underlying => TypeConv.GetNonNullableType<T>(),
            _ => typeof(T)
        };
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[2];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[3];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[4];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[5];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[6];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[7];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[8];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <typeparam name="T9">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8, T9>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[9];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        types[8] = Of<T9>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <typeparam name="T9">类型</typeparam>
    /// <typeparam name="T10">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[10];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        types[8] = Of<T9>(options);
        types[9] = Of<T10>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <typeparam name="T9">类型</typeparam>
    /// <typeparam name="T10">类型</typeparam>
    /// <typeparam name="T11">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[11];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        types[8] = Of<T9>(options);
        types[9] = Of<T10>(options);
        types[10] = Of<T11>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <typeparam name="T9">类型</typeparam>
    /// <typeparam name="T10">类型</typeparam>
    /// <typeparam name="T11">类型</typeparam>
    /// <typeparam name="T12">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[12];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        types[8] = Of<T9>(options);
        types[9] = Of<T10>(options);
        types[10] = Of<T11>(options);
        types[11] = Of<T12>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <typeparam name="T9">类型</typeparam>
    /// <typeparam name="T10">类型</typeparam>
    /// <typeparam name="T11">类型</typeparam>
    /// <typeparam name="T12">类型</typeparam>
    /// <typeparam name="T13">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[13];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        types[8] = Of<T9>(options);
        types[9] = Of<T10>(options);
        types[10] = Of<T11>(options);
        types[11] = Of<T12>(options);
        types[12] = Of<T13>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <typeparam name="T9">类型</typeparam>
    /// <typeparam name="T10">类型</typeparam>
    /// <typeparam name="T11">类型</typeparam>
    /// <typeparam name="T12">类型</typeparam>
    /// <typeparam name="T13">类型</typeparam>
    /// <typeparam name="T14">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[14];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        types[8] = Of<T9>(options);
        types[9] = Of<T10>(options);
        types[10] = Of<T11>(options);
        types[11] = Of<T12>(options);
        types[12] = Of<T13>(options);
        types[13] = Of<T14>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <typeparam name="T9">类型</typeparam>
    /// <typeparam name="T10">类型</typeparam>
    /// <typeparam name="T11">类型</typeparam>
    /// <typeparam name="T12">类型</typeparam>
    /// <typeparam name="T13">类型</typeparam>
    /// <typeparam name="T14">类型</typeparam>
    /// <typeparam name="T15">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[15];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        types[8] = Of<T9>(options);
        types[9] = Of<T10>(options);
        types[10] = Of<T11>(options);
        types[11] = Of<T12>(options);
        types[12] = Of<T13>(options);
        types[13] = Of<T14>(options);
        types[14] = Of<T15>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的通用参数返回相应的类型
    /// </summary>
    /// <typeparam name="T1">类型</typeparam>
    /// <typeparam name="T2">类型</typeparam>
    /// <typeparam name="T3">类型</typeparam>
    /// <typeparam name="T4">类型</typeparam>
    /// <typeparam name="T5">类型</typeparam>
    /// <typeparam name="T6">类型</typeparam>
    /// <typeparam name="T7">类型</typeparam>
    /// <typeparam name="T8">类型</typeparam>
    /// <typeparam name="T9">类型</typeparam>
    /// <typeparam name="T10">类型</typeparam>
    /// <typeparam name="T11">类型</typeparam>
    /// <typeparam name="T12">类型</typeparam>
    /// <typeparam name="T13">类型</typeparam>
    /// <typeparam name="T14">类型</typeparam>
    /// <typeparam name="T15">类型</typeparam>
    /// <typeparam name="T16">类型</typeparam>
    /// <param name="options">类型选项</param>
    public static IEnumerable<Type> Of<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(TypeOfOptions options = TypeOfOptions.Owner)
    {
        var types = new Type[16];
        types[0] = Of<T1>(options);
        types[1] = Of<T2>(options);
        types[2] = Of<T3>(options);
        types[3] = Of<T4>(options);
        types[4] = Of<T5>(options);
        types[5] = Of<T6>(options);
        types[6] = Of<T7>(options);
        types[7] = Of<T8>(options);
        types[8] = Of<T9>(options);
        types[9] = Of<T10>(options);
        types[10] = Of<T11>(options);
        types[11] = Of<T12>(options);
        types[12] = Of<T13>(options);
        types[13] = Of<T14>(options);
        types[14] = Of<T15>(options);
        types[15] = Of<T16>(options);
        return types;
    }

    /// <summary>
    /// 根据给定的对象，返回其对应的类型
    /// </summary>
    /// <param name="objects">对象数组</param>
    /// <param name="options">类型选项</param>
    public static Type[] Of(object[] objects, TypeOfOptions options = TypeOfOptions.Owner)
    {
        if (objects is null)
            return null;

        var nativeTypes = GetNativeTypeArray(objects);

        return options switch
        {
            TypeOfOptions.Owner => nativeTypes,
            TypeOfOptions.Underlying => FilterAndConvert(nativeTypes.Select(TypeConv.GetNonNullableType)),
            _ => nativeTypes
        };

        // 获取导航类型数组
        Type[] GetNativeTypeArray(object[] objectCollection)
        {
            // After the int? type is boxed, and then use GetType(), Nullable<int> will not be obtained.
            // More info
            //    https://github.com/dotnet/runtime/pull/42837

            //return objectCollection.Select(obj => obj?.GetType());
            if (objectCollection.Contains(null))
                return FilterAndConvert(objectCollection.Select(obj => obj?.GetType()));
            return Type.GetTypeArray(objectCollection);
        }

        Type[] FilterAndConvert(IEnumerable<Type> ntVal)
        {
            return ntVal.Where(x => x != null).Select(x => x!).ToArray();
        }
    }
}