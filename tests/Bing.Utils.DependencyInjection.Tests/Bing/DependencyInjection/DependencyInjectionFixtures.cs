using System;
using System.Collections.Generic;

namespace Bing.Utils.DependencyInjection.Tests.Fixtures;

public interface IProbe
{
}

public abstract class DisposableProbeBase : IProbe, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public sealed class LifecycleProbeA : DisposableProbeBase
{
}

public sealed class LifecycleProbeB : DisposableProbeBase
{
}

public interface IScanProbe
{
}

public sealed class ScanAlpha : IScanProbe
{
}

public sealed class ScanBeta : IScanProbe
{
}

public abstract class ScanAbstract : IScanProbe
{
}

internal sealed class ScanInternal : IScanProbe
{
}

public sealed class ScanUnrelated
{
}

public interface IGenericBox<T>
{
}

public sealed class GenericBox<T> : IGenericBox<T>
{
}

public interface IReferenceBox<T>
{
}

public sealed class ReferenceBox<T> : IReferenceBox<T> where T : class
{
}

public interface IPair<TLeft, TRight>
{
}

public sealed class ReorderedPair<TLeft, TRight> : IPair<TRight, TLeft>
{
}

public sealed class RepeatedPair<T> : IPair<T, T>
{
}

public interface IEnvelope<T>
{
}

public sealed class NestedEnvelope<T> : IEnvelope<IReadOnlyList<T>>
{
}

public sealed class ArrayEnvelope<T> : IEnvelope<T[]>
{
}

public sealed class UnboundEnvelope<T> : IEnvelope<FixedMarker>
{
}

public sealed class ReferenceEnvelope<T> : IEnvelope<T> where T : class
{
}

public sealed class FixedMarker
{
}

public interface IMarkedPair<TValue, TMarker>
{
}

public sealed class FixedMarkedPair<TValue> : IMarkedPair<TValue, FixedMarker>
{
}

public interface IBaseContract<T>
{
}

public abstract class BaseContract<T> : IBaseContract<T>
{
}

public sealed class InheritedContract<T> : BaseContract<T>
{
}

public sealed class KeepExisting
{
}

public interface IConflict
{
}

public sealed class ConflictImplementation : IConflict
{
}

public interface IThrowingProbe
{
}

public sealed class ThrowingCtorProbe : IThrowingProbe
{
    public static int ConstructorCalls { get; private set; }

    public ThrowingCtorProbe()
    {
        ConstructorCalls++;
        throw new InvalidOperationException("构造函数不应在注册阶段执行。");
    }
}

public sealed class ClosedPair : IPair<string, int>
{
}

public sealed class AmbiguousPair<T> : IPair<T, int>, IPair<string, T>
{
}
