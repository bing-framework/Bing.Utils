namespace Bing.Collections;

/// <summary>
/// 数组 操作
/// </summary>
public static partial class Arrays
{
    /// <summary>
    /// 空数组 - 获取指定类型的空数组
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <returns>指定类型的空数组实例，该实例可安全使用且不会被修改</returns>
    /// <remarks>
    /// 此方法返回一个缓存的空数组实例，适用于需要返回空数组而非 null 的场景。
    /// 使用空数组比 null 更安全，可以避免空引用异常。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 获取空字符串数组
    /// string[] emptyStrings = Arrays.Empty&lt;string&gt;();
    /// 
    /// // 在方法返回中使用
    /// public string[] GetNames() {
    ///     // 当没有数据时返回空数组而非 null
    ///     return hasData ? actualData : Arrays.Empty&lt;string&gt;();
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Empty<T>() => InternalArray.ForEmpty<T>();

    /// <summary>
    /// 安全地转换为数组 - 从可枚举集合中获取指定数量的元素
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <param name="src">源数据集合</param>
    /// <param name="count">要获取的元素数量</param>
    /// <returns>
    /// 包含指定数量元素的新数组。如果 <paramref name="count"/> 小于或等于 0，则返回空数组；
    /// 如果 <paramref name="src"/> 为 null，则返回包含 count 个默认值的数组
    /// </returns>
    /// <remarks>
    /// 此方法会创建一个新数组并从源集合中复制最多 count 个元素。
    /// 如果源集合中的元素数量少于 count，则数组中剩余的位置将保持元素类型的默认值。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 从列表获取前3个元素
    /// List&lt;int&gt; numbers = new List&lt;int&gt; { 1, 2, 3, 4, 5 };
    /// int[] first3 = Arrays.ToArraySafety(numbers, 3); // 结果: [1, 2, 3]
    /// 
    /// // 请求的元素数大于源集合
    /// List&lt;string&gt; twoNames = new List&lt;string&gt; { "Alice", "Bob" };
    /// string[] names = Arrays.ToArraySafety(twoNames, 5); // 结果: ["Alice", "Bob", null, null, null]
    /// 
    /// // 源集合为 null
    /// int[] array = Arrays.ToArraySafety((IEnumerable&lt;int&gt;)null, 3); // 结果: [0, 0, 0]
    /// </code>
    /// </example>
    public static TElement[] ToArraySafety<TElement>(IEnumerable<TElement> src, int count)
    {
        if (count <= 0)
            return Empty<TElement>();
        var elements = new TElement[count];
        if (src is null)
            return elements;
        var index = 0;
        foreach (var item in src)
        {
            if (index == count)
                break;
            elements[index++] = item;
        }
        return elements;
    }

    /// <summary>
    /// 安全地转换为数组 - 从可枚举集合创建数组
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <param name="src">源数据集合</param>
    /// <returns>
    /// 包含源集合所有元素的新数组。如果 <paramref name="src"/> 为 null，则返回空数组；
    /// 如果 <paramref name="src"/> 已经是所需类型的数组，则直接返回该数组
    /// </returns>
    /// <remarks>
    /// 此方法是对 <see cref="Enumerable.ToArray{TSource}"/> 的安全封装，它会处理 null 输入的情况。
    /// 为了提高性能，如果输入已经是目标类型的数组，则直接返回该数组而不创建新实例。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 从列表创建数组
    /// List&lt;int&gt; numbers = new List&lt;int&gt; { 1, 2, 3 };
    /// int[] array1 = Arrays.ToArraySafety(numbers); // 结果: [1, 2, 3]
    /// 
    /// // 源集合为 null
    /// string[] array2 = Arrays.ToArraySafety((IEnumerable&lt;string&gt;)null); // 结果: []
    /// 
    /// // 源已经是数组，避免重复创建
    /// int[] original = new int[] { 1, 2, 3 };
    /// int[] array3 = Arrays.ToArraySafety(original); // 直接返回 original 引用
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TElement[] ToArraySafety<TElement>(IEnumerable<TElement> src)
    {
        if (src is null)
            return Empty<TElement>();
        return src as TElement[] ?? src.ToArray();
    }

    /// <summary>
    /// 安全地转换为数组 - 从非泛型数组中获取指定数量的元素并转换为指定类型
    /// </summary>
    /// <typeparam name="TElement">目标元素类型</typeparam>
    /// <param name="src">源数组</param>
    /// <param name="count">要获取的元素数量</param>
    /// <returns>
    /// 包含指定数量元素的新类型化数组。如果 <paramref name="count"/> 小于或等于 0，则返回空数组；
    /// 如果 <paramref name="src"/> 为 null，则返回包含 count 个默认值的数组
    /// </returns>
    /// <remarks>
    /// 此方法会创建一个新的目标类型数组，并从源数组中复制最多 count 个元素，同时执行类型转换。
    /// 如果类型转换失败，将抛出异常。请确保源数组中的元素可以转换为目标类型。
    /// </remarks>
    /// <exception cref="InvalidCastException">当源数组中的元素无法转换为目标类型时</exception>
    /// <example>
    /// <code>
    /// // 从 object 数组转换为 int 数组
    /// object[] objects = new object[] { 1, 2, 3, 4, 5 };
    /// int[] ints = Arrays.ToArraySafety&lt;int&gt;(objects, 3); // 结果: [1, 2, 3]
    /// 
    /// // 指定的数量大于源数组长度
    /// object[] twoItems = new object[] { "A", "B" };
    /// string[] strings = Arrays.ToArraySafety&lt;string&gt;(twoItems, 5); // 结果: ["A", "B", null, null, null]
    /// </code>
    /// </example>
    public static TElement[] ToArraySafety<TElement>(Array src, int count)
    {
        if (count <= 0)
            return Empty<TElement>();
        var elements = new TElement[count];
        if (src is null)
            return elements;
        var index = 0;
        foreach (var item in src)
        {
            if (index == count)
                break;
            elements[index++] = (TElement)item;
        }
        return elements;
    }

    /// <summary>
    /// 安全地转换为数组 - 将非泛型数组转换为指定类型的数组
    /// </summary>
    /// <typeparam name="TElement">目标元素类型</typeparam>
    /// <param name="src">源数组</param>
    /// <returns>
    /// 包含源数组所有元素的新类型化数组。如果 <paramref name="src"/> 为 null，则返回空数组
    /// </returns>
    /// <remarks>
    /// 此方法会创建一个新的目标类型数组，并从源数组中复制所有元素，同时执行类型转换。
    /// 如果类型转换失败，将抛出异常。请确保源数组中的元素可以转换为目标类型。
    /// </remarks>
    /// <exception cref="InvalidCastException">当源数组中的元素无法转换为目标类型时</exception>
    /// <example>
    /// <code>
    /// // 从 object 数组转换为 string 数组
    /// object[] objects = new object[] { "A", "B", "C" };
    /// string[] strings = Arrays.ToArraySafety&lt;string&gt;(objects); // 结果: ["A", "B", "C"]
    /// 
    /// // 源数组为 null
    /// int[] array = Arrays.ToArraySafety&lt;int&gt;(null); // 结果: []
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TElement[] ToArraySafety<TElement>(Array src)
    {
        if (src is null)
            return Empty<TElement>();
        var elements = new TElement[src.Length];
        var index = 0;
        foreach (var item in src)
            elements[index++] = (TElement)item;
        return elements;
    }

    /// <summary>
    /// 获取数组长度 - 安全获取数组的元素数量
    /// </summary>
    /// <param name="array">要检查的数组</param>
    /// <returns>数组长度，若数组为 null，则会引发 <see cref="NullReferenceException"/> 异常</returns>
    /// <exception cref="NullReferenceException">当 <paramref name="array"/> 为 null 时</exception>
    /// <remarks>
    /// 此方法是对 <see cref="Array.Length"/> 属性的封装，提供一致的 API 体验。
    /// </remarks>
    /// <example>
    /// <code>
    /// int[] numbers = { 1, 2, 3, 4, 5 };
    /// int length = Arrays.GetLength(numbers); // 返回 5
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetLength(Array array) => array?.Length ?? throw new ArgumentNullException(nameof(array));

    /// <summary>
    /// 判断两个数组是否相等 - 比较两个数组的长度和元素
    /// </summary>
    /// <param name="array1">第一个数组</param>
    /// <param name="array2">第二个数组</param>
    /// <returns>
    /// 如果两个数组相等则返回 <c>true</c>，否则返回 <c>false</c>。
    /// 满足以下所有条件时，两个数组才被认为是相等的：
    /// - 两个数组都为 null；或
    /// - 两个数组长度相同且每个位置的元素都相等
    /// </returns>
    /// <remarks>
    /// 此方法使用 <see cref="object.Equals(object, object)"/> 比较每个元素，这意味着它比较的是内容相等性
    /// 而非引用同一性。对于自定义类型，需确保正确实现了 <see cref="object.Equals(object)"/> 方法。
    /// 
    /// 比较从最后一个元素开始向前进行，这在某些情况下可能会提高性能（如数组末尾更可能不同）。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 相同值的不同数组实例
    /// int[] arr1 = { 1, 2, 3 };
    /// int[] arr2 = { 1, 2, 3 };
    /// bool result1 = Arrays.AreEqual(arr1, arr2); // 返回 true
    /// 
    /// // 不同长度的数组
    /// int[] arr3 = { 1, 2, 3, 4 };
    /// bool result2 = Arrays.AreEqual(arr1, arr3); // 返回 false
    /// 
    /// // 两个 null 数组
    /// bool result3 = Arrays.AreEqual(null, null); // 返回 true
    /// 
    /// // 一个数组为 null
    /// bool result4 = Arrays.AreEqual(arr1, null); // 返回 false
    /// 
    /// // 自定义对象数组
    /// Person[] people1 = { new Person("Alice"), new Person("Bob") };
    /// Person[] people2 = { new Person("Alice"), new Person("Bob") };
    /// bool result5 = Arrays.AreEqual(people1, people2); // 取决于 Person.Equals 的实现
    /// </code>
    /// </example>
    public static bool AreEqual(Array array1, Array array2)
    {
        if (array1 == null && array2 == null)
            return true;
        if (array1 == null || array2 == null)
            return false;
        if (array1.Length != array2.Length)
            return false;
        for (var i = array1.Length - 1; i >= 0; i--)
        {
            if (!Equals(array1.GetValue(i), array2.GetValue(i)))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 获取数组的泛型枚举器 - 用于遍历数组的泛型枚举器
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="array">要枚举的数组</param>
    /// <returns>数组的泛型枚举器，如果数组为 <c>null</c>，则返回空枚举器</returns>
    /// <remarks>
    /// 此方法是对 <see cref="Enumerate{T}"/> 方法的封装，它返回一个能够用于 foreach 语句的枚举器。
    /// 与直接使用数组的枚举器相比，此方法能够安全处理 null 数组，避免出现 NullReferenceException。
    /// </remarks>
    /// <example>
    /// <code>
    /// string[] names = { "Alice", "Bob", "Charlie" };
    /// 
    /// // 使用 GetEnumerator 获取枚举器并手动枚举
    /// var enumerator = Arrays.GetEnumerator(names);
    /// while (enumerator.MoveNext())
    /// {
    ///     Console.WriteLine(enumerator.Current);
    /// }
    /// 
    /// // 对于 null 数组，也能安全使用
    /// string[] nullArray = null;
    /// var safeEnumerator = Arrays.GetEnumerator(nullArray);
    /// while (safeEnumerator.MoveNext()) // 不会执行循环体
    /// {
    ///     // 永远不会到达这里
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Enumerate{T}"/>
    /// <seealso cref="ReverseEnumerate{T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerator<T> GetEnumerator<T>(T[] array) => Enumerate(array).GetEnumerator();

    /// <summary>
    /// 提供数组的泛型可枚举表示 - 按顺序遍历数组的元素
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="array">要枚举的数组</param>
    /// <returns>数组的泛型可枚举表示，如果数组为 <c>null</c>，则返回空枚举</returns>
    /// <remarks>
    /// 此方法使用迭代器模式生成数组的可枚举表示，可用于 LINQ 查询和 foreach 循环。
    /// 它会按照数组索引的自然顺序（从低到高）枚举元素，并且安全处理 null 数组的情况。
    /// 
    /// 使用此方法而非直接枚举数组的主要优势是它对 null 数组返回空枚举而不是抛出异常。
    /// </remarks>
    /// <example>
    /// <code>
    /// string[] names = { "Alice", "Bob", "Charlie" };
    /// 
    /// // 使用 LINQ 查询
    /// var filteredNames = Arrays.Enumerate(names)
    ///                          .Where(n => n.StartsWith("A"))
    ///                          .ToList(); // 结果: ["Alice"]
    /// 
    /// // 使用 foreach 循环
    /// foreach (var name in Arrays.Enumerate(names))
    /// {
    ///     Console.WriteLine(name); // 输出: Alice, Bob, Charlie
    /// }
    /// 
    /// // 安全处理 null 数组
    /// string[] nullArray = null;
    /// int count = Arrays.Enumerate(nullArray).Count(); // 结果: 0
    /// </code>
    /// </example>
    /// <seealso cref="GetEnumerator{T}"/>
    /// <seealso cref="ReverseEnumerate{T}"/>
    public static IEnumerable<T> Enumerate<T>(T[] array)
    {
        if (array != null)
        {
            var length = array.LongLength;
            for (var i = 0; i < length; i++)
                yield return array[i];
        }
    }

    /// <summary>
    /// 提供数组的逆序泛型可枚举表示 - 按逆序遍历数组的元素
    /// </summary>
    /// <typeparam name="T">数组元素类型</typeparam>
    /// <param name="array">要枚举的数组</param>
    /// <returns>数组的逆序泛型可枚举表示，如果数组为 <c>null</c>，则返回空枚举</returns>
    /// <remarks>
    /// 此方法使用迭代器模式生成数组的逆序可枚举表示，可用于 LINQ 查询和 foreach 循环。
    /// 它会按照数组索引的反向顺序（从高到低）枚举元素，适用于需要反向遍历数组的场景。
    /// 
    /// 与 <see cref="Array.Reverse(Array)"/> 不同，此方法不会修改原始数组，而是提供一个逆序的视图。
    /// 与 LINQ 的 <see cref="Enumerable.Reverse{TSource}"/> 相比，此方法针对数组进行了优化，避免了额外的集合创建。
    /// </remarks>
    /// <example>
    /// <code>
    /// string[] names = { "Alice", "Bob", "Charlie" };
    /// 
    /// // 使用 foreach 循环逆序遍历数组
    /// foreach (var name in Arrays.ReverseEnumerate(names))
    /// {
    ///     Console.WriteLine(name); // 输出顺序：Charlie, Bob, Alice
    /// }
    /// 
    /// // 使用 LINQ 查询
    /// var reversedNames = Arrays.ReverseEnumerate(names)
    ///                          .Where(n => n.Length > 3)
    ///                          .ToList(); // 结果: ["Charlie", "Alice"]
    /// 
    /// // 安全处理 null 数组
    /// string[] nullArray = null;
    /// bool anyItems = Arrays.ReverseEnumerate(nullArray).Any(); // 结果: false
    /// </code>
    /// </example>
    /// <seealso cref="Enumerate{T}"/>
    /// <seealso cref="Array.Reverse(Array)"/>
    /// <seealso cref="Enumerable.Reverse{TSource}(IEnumerable{TSource})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ReverseEnumerate<T>(T[] array)
    {
        if (array != null)
        {
            var length = array.LongLength;
            for (var i = length - 1; i >= 0; i--)
                yield return array[i];
        }
    }
}