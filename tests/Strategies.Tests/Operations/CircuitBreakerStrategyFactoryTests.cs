// <copyright file="CircuitBreakerStrategyFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations;

using System;
using System.Collections.Generic;
using System.Reflection;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker.Policies;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly.Registry;
using Shouldly;
using Xunit;

public class CircuitBreakerStrategyFactoryTests
{
    private readonly IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ResiliencePipelineRegistry<string> pipelineRegistry = new();
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();

    public CircuitBreakerStrategyFactoryTests()
    {
        serviceProvider.GetService(typeof(ResiliencePipelineRegistry<string>)).Returns(pipelineRegistry);
    }

    [Fact]
    public void FluentSetters_ShouldSetDefaultsAndReturnFactory()
    {
        // Arrange
        var factory = new CircuitBreakerStrategyFactory<TestService>(serviceProvider);
        var policy = Substitute.For<ICircuitBreakerPolicy>();

        // Act & Assert
        factory.WithDefaultCircuitKey("test-key").ShouldBe(factory);
        factory.WithDefaultPolicy(policy).ShouldBe(factory);
        factory.WithDefaultConsecutiveFailureThreshold(3).ShouldBe(factory);
        factory.WithDefaultFailureRatio(0.4).ShouldBe(factory);
        factory.WithDefaultSamplingDuration(10000).ShouldBe(factory);
        factory.WithDefaultMinimumThroughput(10).ShouldBe(factory);
        factory.WithDefaultDurationOfBreak(15000).ShouldBe(factory);
        factory.WithDefaultHandledException<HttpRequestException>().ShouldBe(factory);
        factory.WithDefaultHandledException(ex => ex is TimeoutException).ShouldBe(factory);
        factory.WithDefaultHandledException((ex, ct) => ex is TimeoutException && !ct.IsCancellationRequested)
            .ShouldBe(factory);
        factory.WithDefaultExcludedException<ArgumentException>().ShouldBe(factory);
        factory.WithDefaultExcludedException(ex => ex is InvalidOperationException).ShouldBe(factory);
        factory.WithDefaultExcludedException((ex, ct) =>
            ex is InvalidOperationException && !ct.IsCancellationRequested).ShouldBe(factory);
        factory.WithDefaultOnBreak((_, _) => { }).ShouldBe(factory);
        factory.WithDefaultOnReset(() => { }).ShouldBe(factory);
        factory.WithDefaultOnHalfOpen(() => { }).ShouldBe(factory);
    }

    [Fact]
    public void FluentSetters_WithInvalidArguments_ShouldThrow()
    {
        // Arrange
        var factory = new CircuitBreakerStrategyFactory<TestService>(serviceProvider);

        // Act & Assert
        Should.Throw<ArgumentException>(() => factory.WithDefaultCircuitKey(string.Empty));
        Should.Throw<ArgumentNullException>(() => factory.WithDefaultPolicy(null!));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultConsecutiveFailureThreshold(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultFailureRatio(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultFailureRatio(1.1));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultSamplingDuration(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultMinimumThroughput(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultDurationOfBreak(0));
        Should.Throw<ArgumentNullException>(() =>
            factory.WithDefaultHandledException((Func<Exception, bool>)null!));
        Should.Throw<ArgumentNullException>(() =>
            factory.WithDefaultHandledException((Func<Exception, CancellationToken, bool>)null!));
        Should.Throw<ArgumentNullException>(() =>
            factory.WithDefaultExcludedException((Func<Exception, bool>)null!));
        Should.Throw<ArgumentNullException>(() =>
            factory.WithDefaultExcludedException((Func<Exception, CancellationToken, bool>)null!));
        Should.Throw<ArgumentNullException>(() => factory.WithDefaultOnBreak(null!));
        Should.Throw<ArgumentNullException>(() => factory.WithDefaultOnReset(null!));
        Should.Throw<ArgumentNullException>(() => factory.WithDefaultOnHalfOpen(null!));
    }

    [Fact]
    public void BuildCircuitBreakerStrategy_ShouldCreateStrategyAndAttachAllDefaults()
    {
        // Arrange
        var onBreakCalled = false;
        var onResetCalled = false;
        var onHalfOpenCalled = false;
        var policy = Substitute.For<ICircuitBreakerPolicy>();

        var factory = new CircuitBreakerStrategyFactory<TestService>(serviceProvider);

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultCircuitKey("my-shared-circuit")
            .WithDefaultPolicy(policy)
            .WithDefaultConsecutiveFailureThreshold(4)
            .WithDefaultFailureRatio(0.3)
            .WithDefaultSamplingDuration(20000)
            .WithDefaultMinimumThroughput(8)
            .WithDefaultDurationOfBreak(5000)
            .WithDefaultHandledException<InvalidOperationException>()
            .WithDefaultExcludedException<ArgumentException>()
            .WithDefaultOnBreak((_, _) => onBreakCalled = true)
            .WithDefaultOnReset(() => onResetCalled = true)
            .WithDefaultOnHalfOpen(() => onHalfOpenCalled = true);

        // Act
        var strategy = factory.BuildCircuitBreakerStrategy();

        // Assert
        strategy.ShouldNotBeNull();
        strategy.ShouldBeOfType<CircuitBreakerStrategy<TestService>>();

        var cbStrategy = (CircuitBreakerStrategy<TestService>)strategy;
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        var defaultOnBreak = typeof(CircuitBreakerStrategyFactory<TestService>)
            .GetProperty("DefaultOnBreakAction", flags)?.GetValue(factory);

        defaultOnBreak.ShouldNotBeNull();

        var defaultPolicies = (List<ICircuitBreakerPolicy>?)typeof(CircuitBreakerStrategyFactory<TestService>)
            .GetProperty("DefaultPolicies", flags)?.GetValue(factory);

        defaultPolicies.ShouldNotBeNull();
        defaultPolicies.Count.ShouldBe(1);
        defaultPolicies[0].ShouldBe(policy);

        policy.Received(1).Execute(cbStrategy);

        typeof(CircuitBreakerStrategy<TestService>).GetProperty("Logger", flags)?.GetValue(cbStrategy).ShouldBe(logger);
        typeof(CircuitBreakerStrategy<TestService>).GetProperty("CircuitKey", flags)?.GetValue(cbStrategy)
            .ShouldBe("my-shared-circuit");
        typeof(CircuitBreakerStrategy<TestService>).GetProperty("TargetDescription", flags)?.GetValue(cbStrategy)
            .ShouldBe("circuit-breaker-my-shared-circuit");
        typeof(CircuitBreakerStrategy<TestService>).GetProperty("ConsecutiveFailureThreshold", flags)
            ?.GetValue(cbStrategy).ShouldBe(4);
        typeof(CircuitBreakerStrategy<TestService>).GetProperty("FailureRatio", flags)?.GetValue(cbStrategy)
            .ShouldBe(0.3);
        typeof(CircuitBreakerStrategy<TestService>).GetProperty("SamplingDurationMs", flags)?.GetValue(cbStrategy)
            .ShouldBe(20000);
        typeof(CircuitBreakerStrategy<TestService>).GetProperty("MinimumThroughput", flags)?.GetValue(cbStrategy)
            .ShouldBe(8);
        typeof(CircuitBreakerStrategy<TestService>).GetProperty("DurationOfBreakMs", flags)?.GetValue(cbStrategy)
            .ShouldBe(5000);

        var onBreakProp = typeof(CircuitBreakerStrategy<TestService>).GetProperty("OnBreakAction", flags);

        onBreakProp.ShouldNotBeNull();

        var onBreakAction = (Action<Exception, TimeSpan>?)onBreakProp.GetValue(cbStrategy);

        onBreakAction.ShouldNotBeNull();

        onBreakAction?.Invoke(new Exception("test"), TimeSpan.FromSeconds(5));

        onBreakCalled.ShouldBeTrue();

        var onResetAction = (Action?)typeof(CircuitBreakerStrategy<TestService>)
            .GetProperty("OnResetAction", flags)?.GetValue(cbStrategy);

        onResetAction.ShouldNotBeNull();

        onResetAction?.Invoke();

        onResetCalled.ShouldBeTrue();

        var onHalfOpenAction = (Action?)typeof(CircuitBreakerStrategy<TestService>)
            .GetProperty("OnHalfOpenAction", flags)?.GetValue(cbStrategy);

        onHalfOpenAction.ShouldNotBeNull();

        onHalfOpenAction?.Invoke();

        onHalfOpenCalled.ShouldBeTrue();
    }

    [Fact]
    public void BuildCircuitBreakerStrategy_ShouldAttachCancellationTokenAwareExceptionDefaults()
    {
        // Arrange
        var factory = new CircuitBreakerStrategyFactory<TestService>(serviceProvider);

        factory
            .WithDefaultCircuitKey("ct-defaults-circuit")
            .WithDefaultConsecutiveFailureThreshold(2)
            .WithDefaultHandledException((ex, ct) => ex is TimeoutException && !ct.IsCancellationRequested)
            .WithDefaultExcludedException((ex, ct) => ex is NotFiniteNumberException && !ct.IsCancellationRequested);

        // Act
        var strategy = factory.BuildCircuitBreakerStrategy();

        // Assert
        strategy.ShouldNotBeNull();
        strategy.ShouldBeOfType<CircuitBreakerStrategy<TestService>>();

        var cbStrategy = (CircuitBreakerStrategy<TestService>)strategy;
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        var handledExceptions =
            (List<Func<Exception, CancellationToken, bool>>?)typeof(CircuitBreakerStrategy<TestService>)
                .GetProperty("HandledExceptions", flags)?.GetValue(cbStrategy);

        handledExceptions.ShouldNotBeNull();
        handledExceptions.Count.ShouldBe(1);
        handledExceptions[0](new TimeoutException(), CancellationToken.None).ShouldBeTrue();
        handledExceptions[0](new TimeoutException(), new CancellationToken(true)).ShouldBeFalse();

        var excludedExceptions =
            (List<Func<Exception, CancellationToken, bool>>?)typeof(CircuitBreakerStrategy<TestService>)
                .GetProperty("ExcludedExceptions", flags)?.GetValue(cbStrategy);

        excludedExceptions.ShouldNotBeNull();

        // default 7 exclusions + 1 custom
        excludedExceptions.Count.ShouldBe(8);
        excludedExceptions[7](new NotFiniteNumberException(), CancellationToken.None).ShouldBeTrue();
        excludedExceptions[7](new NotFiniteNumberException(), new CancellationToken(true)).ShouldBeFalse();
    }
}
