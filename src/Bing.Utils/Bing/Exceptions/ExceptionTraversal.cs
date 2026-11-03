namespace Bing.Exceptions;

/// <summary>
/// 按原始分支顺序迭代异常节点，避免递归及宽聚合的预先物化。
/// </summary>
internal static class ExceptionTraversal
{
    /// <summary>
    /// 根优先遍历异常，重复实例只产生标记而不再次展开。
    /// </summary>
    /// <param name="root">非空根异常，由公开入口校验。</param>
    /// <param name="includeAggregate">是否枚举聚合异常的全部分支。</param>
    /// <param name="maxDepth">允许展开的最大深度，根节点为 1。</param>
    /// <param name="maxExceptionCount">允许接纳的不同异常实例数；重复引用不消耗预算。</param>
    /// <returns>按需读取的节点序列，超过不同节点预算时只产生边界标记。</returns>
    internal static IEnumerable<Node> Enumerate(Exception root, bool includeAggregate, int maxDepth = int.MaxValue, int maxExceptionCount = int.MaxValue)
    {
        var seen = new HashSet<Exception>(IdentityComparer.Instance);
        var frames = new Stack<Frame>();
        var current = root;
        var depth = 1;
        while (current != null)
        {
            // 先核对身份，再决定是否接纳新节点，避免去重集合及属性访问超过预算。
            if (seen.Count >= maxExceptionCount && !seen.Contains(current))
            {
                yield return new Node(current, depth, false, false, true);
                yield break;
            }
            var repeated = !seen.Add(current);
            var hasChildren = includeAggregate && current is AggregateException aggregate
                ? aggregate.InnerExceptions.Count > 0
                : current.InnerException != null;
            var depthLimited = !repeated && hasChildren && depth >= maxDepth;
            yield return new Node(current, depth, repeated, depthLimited);
            if (!repeated && hasChildren && !depthLimited)
                frames.Push(new Frame(current, depth + 1, includeAggregate));
            current = null;
            while (frames.Count != 0)
            {
                var frame = frames.Peek();
                if (frame.TryRead(out current))
                {
                    depth = frame.Depth;
                    break;
                }
                frames.Pop();
            }
        }
    }

    /// <summary>
    /// 表示一次遍历产生的节点及其边界状态。
    /// </summary>
    internal readonly struct Node
    {
        /// <summary>
        /// 初始化当前异常节点及遍历状态。
        /// </summary>
        /// <param name="exception">当前原异常实例。</param>
        /// <param name="depth">根节点起算为 1 的深度。</param>
        /// <param name="isRepeated">是否已访问该实例。</param>
        /// <param name="depthLimited">是否因深度预算而停止展开子节点。</param>
        /// <param name="countLimited">是否仅为不同节点预算耗尽的标记，未读取该异常的诊断属性。</param>
        internal Node(Exception exception, int depth, bool isRepeated, bool depthLimited, bool countLimited = false)
        {
            Exception = exception;
            Depth = depth;
            IsRepeated = isRepeated;
            DepthLimited = depthLimited;
            CountLimited = countLimited;
        }

        /// <summary>
        /// 获取原异常实例。
        /// </summary>
        internal Exception Exception { get; }

        /// <summary>
        /// 获取从 1 开始的深度。
        /// </summary>
        internal int Depth { get; }

        /// <summary>
        /// 获取该实例是否已访问。
        /// </summary>
        internal bool IsRepeated { get; }

        /// <summary>
        /// 获取是否因深度限制而不再展开子节点。
        /// </summary>
        internal bool DepthLimited { get; }

        /// <summary>
        /// 获取是否仅标记不同节点预算耗尽，不再读取或展开该异常。
        /// </summary>
        internal bool CountLimited { get; }
    }

    /// <summary>
    /// 保存一个父节点的子项位置，每次只读取一个子项。
    /// </summary>
    private sealed class Frame
    {
        /// <summary>
        /// 当前父异常，生命周期限于遍历过程。
        /// </summary>
        private readonly Exception _parent;

        /// <summary>
        /// 是否展开聚合分支。
        /// </summary>
        private readonly bool _includeAggregate;

        /// <summary>
        /// 下一个需要访问的子项索引。
        /// </summary>
        private int _index;

        /// <summary>
        /// 初始化子节点遍历帧。
        /// </summary>
        /// <param name="parent">需要逐项读取子节点的父异常。</param>
        /// <param name="depth">子节点的深度。</param>
        /// <param name="includeAggregate">是否读取聚合异常的全部分支。</param>
        internal Frame(Exception parent, int depth, bool includeAggregate)
        {
            _parent = parent;
            Depth = depth;
            _includeAggregate = includeAggregate;
        }

        /// <summary>
        /// 获取子节点的深度。
        /// </summary>
        internal int Depth { get; }

        /// <summary>
        /// 尝试读取下一个子节点。
        /// </summary>
        /// <param name="exception">读取到的子节点，结束时为 null。</param>
        /// <returns>有子节点时为 true，否则为 false。</returns>
        internal bool TryRead(out Exception exception)
        {
            if (_includeAggregate && _parent is AggregateException aggregate)
            {
                if (_index < aggregate.InnerExceptions.Count)
                {
                    exception = aggregate.InnerExceptions[_index++];
                    return true;
                }
            }
            else if (_index++ == 0 && _parent.InnerException != null)
            {
                exception = _parent.InnerException;
                return true;
            }
            exception = null;
            return false;
        }
    }

    /// <summary>
    /// 仅按引用身份去重，不调用用户异常重写的相等方法。
    /// </summary>
    private sealed class IdentityComparer : IEqualityComparer<Exception>
    {
        /// <summary>
        /// 无状态引用比较器实例，不缓存异常。
        /// </summary>
        internal static readonly IdentityComparer Instance = new IdentityComparer();

        /// <inheritdoc />
        public bool Equals(Exception x, Exception y) => ReferenceEquals(x, y);

        /// <inheritdoc />
        public int GetHashCode(Exception obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
