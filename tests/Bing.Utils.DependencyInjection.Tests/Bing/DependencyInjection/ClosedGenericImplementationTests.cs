using System;
using System.Collections.Generic;
using System.Linq;
using Bing.DependencyInjection;
using Bing.Utils.DependencyInjection.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Bing.Utils.DependencyInjection.Tests.Bing.DependencyInjection;

public sealed class ClosedGenericImplementationTests
{
    [Fact]
    public void AddClosedGenericImplementations_SupportsReorderedParameters()
    {
        var services = new ServiceCollection();

        services.AddClosedGenericImplementations(
            new[] { typeof(IPair<string, int>) },
            new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient);

        services.Single().ImplementationType.ShouldBe(typeof(ReorderedPair<int, string>));
        using (var provider = services.BuildServiceProvider())
        {
            provider.GetRequiredService<IPair<string, int>>()
                .ShouldBeOfType<ReorderedPair<int, string>>();
        }
    }

    [Fact]
    public void AddClosedGenericImplementations_SupportsNestedArguments()
    {
        var services = new ServiceCollection();

        services.AddClosedGenericImplementations(
            new[] { typeof(IEnvelope<IReadOnlyList<string>>) },
            new[] { typeof(NestedEnvelope<>) }, ServiceLifetime.Transient);

        services.Single().ImplementationType.ShouldBe(typeof(NestedEnvelope<string>));
    }

    [Fact]
    public void AddClosedGenericImplementations_SupportsFixedArguments()
    {
        var services = new ServiceCollection();

        services.AddClosedGenericImplementations(
            new[] { typeof(IMarkedPair<string, FixedMarker>) },
            new[] { typeof(FixedMarkedPair<>) }, ServiceLifetime.Transient);

        services.Single().ImplementationType.ShouldBe(typeof(FixedMarkedPair<string>));
    }

    [Fact]
    public void AddClosedGenericImplementations_SupportsArrayArguments()
    {
        var services = new ServiceCollection();

        services.AddClosedGenericImplementations(
            new[] { typeof(IEnvelope<string[]>) },
            new[] { typeof(ArrayEnvelope<>) }, ServiceLifetime.Transient);

        services.Single().ImplementationType.ShouldBe(typeof(ArrayEnvelope<string>));
    }

    [Fact]
    public void AddClosedGenericImplementations_FollowsInheritedInterfaceProjection()
    {
        var services = new ServiceCollection();

        services.AddClosedGenericImplementations(
            new[] { typeof(IBaseContract<int>) },
            new[] { typeof(InheritedContract<>) }, ServiceLifetime.Transient);

        services.Single().ImplementationType.ShouldBe(typeof(InheritedContract<int>));
    }

    [Theory]
    [InlineData(typeof(IPair<string, int>), typeof(RepeatedPair<>))]
    [InlineData(typeof(IEnvelope<FixedMarker>), typeof(UnboundEnvelope<>))]
    [InlineData(typeof(IEnvelope<int>), typeof(ReferenceEnvelope<>))]
    [InlineData(typeof(IPair<string, int>), typeof(AmbiguousPair<>))]
    public void AddClosedGenericImplementations_RejectsUnresolvableMappings(Type serviceType, Type implementationType)
    {
        var services = new ServiceCollection();
        services.AddSingleton<KeepExisting>();
        var existing = services.ToArray();

        Should.Throw<InvalidOperationException>(() => services.AddClosedGenericImplementations(
            new[] { serviceType }, new[] { implementationType }, ServiceLifetime.Transient));

        services.Count.ShouldBe(existing.Length);
        services[0].ShouldBeSameAs(existing[0]);
    }

    [Fact]
    public void AddClosedGenericImplementations_FailsAtomicallyWhenOneFiniteRequestHasNoImplementation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<KeepExisting>();
        var existing = services.ToArray();

        Should.Throw<InvalidOperationException>(() => services.AddClosedGenericImplementations(
            new Type[] { typeof(IPair<string, int>), typeof(IEnvelope<FixedMarker>) },
            new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient));

        services.Count.ShouldBe(existing.Length);
        services[0].ShouldBeSameAs(existing[0]);
    }

    [Fact]
    public void AddClosedGenericImplementations_RegistersEveryFiniteRequest()
    {
        var services = new ServiceCollection();

        services.AddClosedGenericImplementations(
            new Type[] { typeof(IPair<string, int>), typeof(IPair<int, string>) },
            new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Singleton);

        services.Select(descriptor => descriptor.ImplementationType).ShouldBe(new[]
        {
            typeof(ReorderedPair<int, string>),
            typeof(ReorderedPair<string, int>)
        });
    }

    [Fact]
    public void AddClosedGenericImplementations_SkipsUnmatchedCandidate()
    {
        var services = new ServiceCollection();

        services.AddClosedGenericImplementations(
            new[] { typeof(IPair<string, int>) },
            new Type[] { typeof(ReorderedPair<,>), typeof(NestedEnvelope<>) },
            ServiceLifetime.Transient);

        services.Count.ShouldBe(1);
        services.Single().ImplementationType.ShouldBe(typeof(ReorderedPair<int, string>));
    }

    [Fact]
    public void AddClosedGenericImplementations_AcceptsClosedImplementationType()
    {
        var services = new ServiceCollection();

        services.AddClosedGenericImplementations(
            new[] { typeof(IPair<string, int>) },
            new[] { typeof(ClosedPair) }, ServiceLifetime.Transient);

        services.Single().ImplementationType.ShouldBe(typeof(ClosedPair));
    }

    [Fact]
    public void AddClosedGenericImplementations_DoesNotRegisterUndeclaredClosedService()
    {
        var services = new ServiceCollection();
        services.AddClosedGenericImplementations(
            new[] { typeof(IPair<string, int>) },
            new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient);

        using (var provider = services.BuildServiceProvider())
        {
            provider.GetRequiredService<IPair<string, int>>().ShouldBeOfType<ReorderedPair<int, string>>();
            provider.GetService<IPair<int, int>>().ShouldBeNull();
        }
    }

    [Fact]
    public void AddClosedGenericImplementations_AppliesConflictStrategiesToClosedDescriptors()
    {
        var skip = new ServiceCollection();
        skip.AddClosedGenericImplementations(
            new Type[] { typeof(IPair<string, int>), typeof(IPair<string, int>) },
            new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient);
        skip.Count.ShouldBe(1);

        var append = new ServiceCollection();
        append.AddClosedGenericImplementations(
            new Type[] { typeof(IPair<string, int>), typeof(IPair<string, int>) },
            new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient,
            ServiceRegistrationStrategy.Append);
        append.Count.ShouldBe(2);

        var throwOnDuplicate = new ServiceCollection();
        var thrown = Should.Throw<InvalidOperationException>(() => throwOnDuplicate.AddClosedGenericImplementations(
            new Type[] { typeof(IPair<string, int>), typeof(IPair<string, int>) },
            new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient,
            ServiceRegistrationStrategy.ThrowOnDuplicate));
        thrown.Message.ShouldContain("重复注册");
        throwOnDuplicate.ShouldBeEmpty();
    }

    [Fact]
    public void AddClosedGenericImplementations_RejectsOpenServiceAndInvalidImplementationArguments()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() => services.AddClosedGenericImplementations(
            new[] { typeof(IPair<,>) }, new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient));
        Should.Throw<ArgumentException>(() => services.AddClosedGenericImplementations(
            new[] { typeof(IPair<string, int>) }, new[] { typeof(IScanProbe) }, ServiceLifetime.Transient));
        Should.Throw<InvalidOperationException>(() => services.AddClosedGenericImplementations(
            new[] { typeof(IPair<string, int>) }, new[] { typeof(ScanUnrelated) }, ServiceLifetime.Transient));
        services.ShouldBeEmpty();
    }
}
