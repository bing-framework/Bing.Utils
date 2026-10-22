using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bing.DependencyInjection;
using Bing.Utils.DependencyInjection.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace Bing.Utils.DependencyInjection.Tests.Bing.DependencyInjection;

public sealed class BingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddImplementations_ReturnsSameCollectionAndRegistersAllImplementations()
    {
        var services = new ServiceCollection();

        var result = services.AddImplementations<IProbe>(
            new[] { typeof(LifecycleProbeA), typeof(LifecycleProbeB) }, ServiceLifetime.Singleton);

        result.ShouldBeSameAs(services);
        services.Count(descriptor => descriptor.ServiceType == typeof(IProbe)).ShouldBe(2);
        services.Select(descriptor => descriptor.ImplementationType)
            .ShouldBe(new[] { typeof(LifecycleProbeA), typeof(LifecycleProbeB) });
    }

    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public void AddImplementations_HonorsLifetimeAndDisposesResolvedInstances(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        services.AddImplementations<IProbe>(
            new[] { typeof(LifecycleProbeA), typeof(LifecycleProbeB) }, lifetime);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        IProbe[] first;
        IProbe[] second;

        if (lifetime == ServiceLifetime.Scoped)
        {
            using (var scope = provider.CreateScope())
            {
                first = scope.ServiceProvider.GetServices<IProbe>().ToArray();
                second = scope.ServiceProvider.GetServices<IProbe>().ToArray();
                first.Length.ShouldBe(2);
                first[0].ShouldBeSameAs(second[0]);
                first[1].ShouldBeSameAs(second[1]);
            }

            first.Cast<DisposableProbeBase>().ShouldAllBe(probe => probe.IsDisposed);

            IProbe[] otherScope;
            using (var scope = provider.CreateScope())
            {
                otherScope = scope.ServiceProvider.GetServices<IProbe>().ToArray();
                ReferenceEquals(first[0], otherScope[0]).ShouldBeFalse();
                ReferenceEquals(first[1], otherScope[1]).ShouldBeFalse();
            }

            otherScope.Cast<DisposableProbeBase>().ShouldAllBe(probe => probe.IsDisposed);
        }
        else
        {
            first = provider.GetServices<IProbe>().ToArray();
            second = provider.GetServices<IProbe>().ToArray();
            first.Length.ShouldBe(2);
            second.Length.ShouldBe(2);

            if (lifetime == ServiceLifetime.Singleton)
            {
                first[0].ShouldBeSameAs(second[0]);
                first[1].ShouldBeSameAs(second[1]);
            }
            else
            {
                ReferenceEquals(first[0], second[0]).ShouldBeFalse();
                ReferenceEquals(first[1], second[1]).ShouldBeFalse();
            }
        }

        provider.Dispose();
        first.Cast<DisposableProbeBase>().ShouldAllBe(probe => probe.IsDisposed);
        second.Cast<DisposableProbeBase>().ShouldAllBe(probe => probe.IsDisposed);
    }

    [Fact]
    public void AddImplementations_SkippingExistingTypePreservesItsLifetime()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProbe, LifecycleProbeA>();

        services.AddImplementations<IProbe>(
            new[] { typeof(LifecycleProbeA) }, ServiceLifetime.Scoped);

        services.Count(descriptor => descriptor.ServiceType == typeof(IProbe)).ShouldBe(1);
        services.Single(descriptor => descriptor.ServiceType == typeof(IProbe))
            .Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddImplementations_DoesNotExecuteConstructors()
    {
        ThrowingCtorProbe.ConstructorCalls.ShouldBe(0);
        var services = new ServiceCollection();
        services.AddImplementations<IThrowingProbe>(
            new[] { typeof(ThrowingCtorProbe) }, ServiceLifetime.Singleton);

        using (var provider = services.BuildServiceProvider())
        {
            ThrowingCtorProbe.ConstructorCalls.ShouldBe(0);
        }

        ThrowingCtorProbe.ConstructorCalls.ShouldBe(0);
    }

    [Fact]
    public void AddImplementations_DefaultStrategySkipsDuplicateImplementation()
    {
        var services = new ServiceCollection();

        services.AddImplementations<IProbe>(
            new[] { typeof(LifecycleProbeA), typeof(LifecycleProbeA) }, ServiceLifetime.Transient);

        services.Count(descriptor => descriptor.ServiceType == typeof(IProbe)).ShouldBe(1);
    }

    [Fact]
    public void AddImplementations_AppendStrategyKeepsDuplicateImplementation()
    {
        var services = new ServiceCollection();

        services.AddImplementations<IProbe>(
            new[] { typeof(LifecycleProbeA), typeof(LifecycleProbeA) },
            ServiceLifetime.Transient, ServiceRegistrationStrategy.Append);

        services.Count(descriptor => descriptor.ServiceType == typeof(IProbe)).ShouldBe(2);
    }

    [Fact]
    public void AddImplementations_ThrowOnDuplicateLeavesCollectionUnchanged()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProbe, LifecycleProbeA>();
        var existing = services.ToArray();

        Should.Throw<InvalidOperationException>(() => services.AddImplementations<IProbe>(
            new[] { typeof(LifecycleProbeA), typeof(LifecycleProbeB) },
            ServiceLifetime.Transient, ServiceRegistrationStrategy.ThrowOnDuplicate));

        services.Count.ShouldBe(existing.Length);
        services[0].ShouldBeSameAs(existing[0]);
    }

    [Fact]
    public void AddImplementations_DoesNotInstantiateExistingFactoryAndPreservesNativeDescriptorSemantics()
    {
        var factoryCalls = 0;
        IServiceCollection factoryServices = new ServiceCollection();
        factoryServices.AddSingleton<IConflict>(_ =>
        {
            factoryCalls++;
            return new ConflictImplementation();
        });

        factoryServices.AddImplementations<IConflict>(
            new[] { typeof(ConflictImplementation) }, ServiceLifetime.Singleton);

        factoryCalls.ShouldBe(0);

        IServiceCollection instanceServices = new ServiceCollection();
        instanceServices.AddSingleton<IConflict>(new ConflictImplementation());
        var instanceCount = instanceServices.Count;
        instanceServices.AddImplementations<IConflict>(
            new[] { typeof(ConflictImplementation) }, ServiceLifetime.Singleton);
        instanceServices.Count.ShouldBe(instanceCount);

        IServiceCollection keyedServices = new ServiceCollection();
        keyedServices.AddKeyedSingleton<IConflict>("key", new ConflictImplementation());
        var keyedCount = keyedServices.Count;
        keyedServices.AddImplementations<IConflict>(
            new[] { typeof(ConflictImplementation) }, ServiceLifetime.Singleton);
        keyedServices.Count.ShouldBe(keyedCount + 1);
    }

    [Fact]
    public void AddImplementations_RejectsInvalidArgumentsBeforeMutation()
    {
        var services = new ServiceCollection();
        var serviceException = Should.Throw<ArgumentException>(() => services.AddImplementations(
            typeof(ScanUnrelated), new[] { typeof(ConflictImplementation) }, ServiceLifetime.Transient));
        serviceException.ParamName.ShouldBe("serviceType");

        var implementationException = Should.Throw<ArgumentException>(() => services.AddImplementations(
            typeof(IConflict), new Type[] { null }, ServiceLifetime.Transient));
        implementationException.ParamName.ShouldBe("implementationTypes");

        Should.Throw<ArgumentException>(() => services.AddImplementations(
            typeof(IConflict), new[] { typeof(ScanUnrelated) }, ServiceLifetime.Transient));
        services.ShouldBeEmpty();
    }

    [Fact]
    public void AllRegistrationEntryPoints_ValidateNullArgumentsAndEnums()
    {
        var services = new ServiceCollection();
        var closedService = new[] { typeof(IPair<string, int>) };
        var assembly = new[] { typeof(ScanAlpha).Assembly };

        Should.Throw<ArgumentNullException>(() =>
            BingServiceCollectionExtensions.AddImplementations<IProbe>(
                null, new[] { typeof(LifecycleProbeA) }, ServiceLifetime.Transient));
        Should.Throw<ArgumentNullException>(() => services.AddImplementations<IProbe>(
            null, ServiceLifetime.Transient));
        Should.Throw<ArgumentNullException>(() => services.AddImplementations(
            typeof(IProbe), null, ServiceLifetime.Transient));
        Should.Throw<ArgumentNullException>(() => services.ScanImplementations<IScanProbe>(
            null, ServiceLifetime.Transient));
        Should.Throw<ArgumentNullException>(() => services.ScanImplementations(
            typeof(IScanProbe), null, ServiceLifetime.Transient));
        Should.Throw<ArgumentNullException>(() => services.AddClosedGenericImplementations(
            null, new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient));
        Should.Throw<ArgumentNullException>(() => services.AddClosedGenericImplementations(
            closedService, null, ServiceLifetime.Transient));
        Should.Throw<ArgumentNullException>(() => services.ScanClosedGenericImplementations(
            null, assembly, ServiceLifetime.Transient));
        Should.Throw<ArgumentNullException>(() => services.ScanClosedGenericImplementations(
            closedService, null, ServiceLifetime.Transient));

        Should.Throw<ArgumentOutOfRangeException>(() => services.AddImplementations<IProbe>(
            Array.Empty<Type>(), (ServiceLifetime)99));
        Should.Throw<ArgumentOutOfRangeException>(() => services.AddImplementations<IProbe>(
            Array.Empty<Type>(), ServiceLifetime.Transient, (ServiceRegistrationStrategy)99));
    }

    [Fact]
    public void AddImplementations_EmptyImplementationBatchIsNoOp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<KeepExisting>();
        var existing = services.ToArray();

        services.AddImplementations<IProbe>(Array.Empty<Type>(), ServiceLifetime.Transient)
            .ShouldBeSameAs(services);

        services.Count.ShouldBe(existing.Length);
        services[0].ShouldBeSameAs(existing[0]);
    }

    [Fact]
    public void AddImplementations_ComplexOpenMappingRequiresClosedRegistration()
    {
        var services = new ServiceCollection();

        var exception = Should.Throw<ArgumentException>(() => services.AddImplementations(
            typeof(IPair<,>), new[] { typeof(ReorderedPair<,>) }, ServiceLifetime.Transient));

        exception.Message.ShouldContain("AddClosedGenericImplementations");
        services.ShouldBeEmpty();
    }

    [Fact]
    public void ScanImplementations_DeduplicatesAssembliesFiltersVisibilityAndSortsByFullName()
    {
        var services = new ServiceCollection();
        var assembly = typeof(ScanAlpha).Assembly;

        var result = services.ScanImplementations<IScanProbe>(
            new[] { assembly, assembly }, ServiceLifetime.Transient,
            predicate: type => typeof(IScanProbe).IsAssignableFrom(type));

        result.ShouldBeSameAs(services);
        services.Select(descriptor => descriptor.ImplementationType)
            .ShouldBe(new[] { typeof(ScanAlpha), typeof(ScanBeta) });
    }

    [Fact]
    public void ScanImplementations_WithNoCandidatesIsNoOp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<KeepExisting>();
        var existing = services.ToArray();

        services.ScanImplementations<IScanProbe>(
            new[] { typeof(ScanAlpha).Assembly }, ServiceLifetime.Transient,
            predicate: type => type == typeof(ScanUnrelated)).ShouldBeSameAs(services);

        services.Count.ShouldBe(existing.Length);
        services[0].ShouldBeSameAs(existing[0]);
    }

    [Fact]
    public void ScanImplementations_PredicateFailureLeavesCollectionUnchanged()
    {
        var services = new ServiceCollection();
        services.AddSingleton<KeepExisting>();
        var existing = services.ToArray();
        var failure = new InvalidOperationException("predicate failure");

        var thrown = Should.Throw<InvalidOperationException>(() => services.ScanImplementations<IScanProbe>(
            new[] { typeof(ScanAlpha).Assembly }, ServiceLifetime.Transient,
            predicate: type => throw failure));

        thrown.ShouldBeSameAs(failure);
        services.Count.ShouldBe(existing.Length);
        services[0].ShouldBeSameAs(existing[0]);
    }

    [Fact]
    public void ScanImplementations_TypeLoadingFailureLeavesCollectionUnchanged()
    {
        var services = new ServiceCollection();
        services.AddSingleton<KeepExisting>();
        var existing = services.ToArray();
        var failure = new ReflectionTypeLoadException(
            Array.Empty<Type>(), new[] { new InvalidOperationException("type loading failure") });
        var assembly = new Mock<Assembly>();
        assembly.Setup(value => value.GetTypes()).Throws(failure);

        var thrown = Should.Throw<ReflectionTypeLoadException>(() => services.ScanImplementations<IScanProbe>(
            new[] { assembly.Object }, ServiceLifetime.Transient));

        thrown.ShouldBeSameAs(failure);
        services.Count.ShouldBe(existing.Length);
        services[0].ShouldBeSameAs(existing[0]);
    }

    [Fact]
    public void AddImplementations_StandardOpenGenericResolvesNativeClosedService()
    {
        var services = new ServiceCollection();
        services.AddImplementations(
            typeof(IGenericBox<>), new[] { typeof(GenericBox<>) }, ServiceLifetime.Transient);

        using (var provider = services.BuildServiceProvider())
        {
            provider.GetRequiredService<IGenericBox<string>>().ShouldBeOfType<GenericBox<string>>();
        }
    }

    [Fact]
    public void ScanImplementations_StandardOpenGenericResolvesNativeClosedService()
    {
        var services = new ServiceCollection();
        services.ScanImplementations(
            typeof(IGenericBox<>), new[] { typeof(GenericBox<>).Assembly }, ServiceLifetime.Transient,
            predicate: type => type == typeof(GenericBox<>));

        using (var provider = services.BuildServiceProvider())
        {
            provider.GetRequiredService<IGenericBox<int>>().ShouldBeOfType<GenericBox<int>>();
        }
    }

    [Fact]
    public void AddImplementations_OpenGenericConstraintIsCheckedAtResolution()
    {
        var services = new ServiceCollection();
        services.AddImplementations(
            typeof(IReferenceBox<>), new[] { typeof(ReferenceBox<>) }, ServiceLifetime.Transient);

        using (var provider = services.BuildServiceProvider())
        {
            provider.GetRequiredService<IReferenceBox<string>>().ShouldBeOfType<ReferenceBox<string>>();
            Should.Throw<ArgumentException>(() => provider.GetRequiredService<IReferenceBox<int>>());
        }
    }

    [Fact]
    public void ScanClosedGenericImplementations_UsesFiniteRequestsAndFilteredCandidates()
    {
        var services = new ServiceCollection();
        var result = services.ScanClosedGenericImplementations(
            new Type[] { typeof(IPair<string, int>), typeof(IEnvelope<IReadOnlyList<string>>) },
            new[] { typeof(ReorderedPair<,>).Assembly, typeof(ReorderedPair<,>).Assembly },
            ServiceLifetime.Singleton,
            predicate: type => type == typeof(ReorderedPair<,>) || type == typeof(NestedEnvelope<>));

        result.ShouldBeSameAs(services);
        services.Select(descriptor => descriptor.ImplementationType).ShouldBe(new[]
        {
            typeof(ReorderedPair<int, string>),
            typeof(NestedEnvelope<string>)
        });
    }
}
