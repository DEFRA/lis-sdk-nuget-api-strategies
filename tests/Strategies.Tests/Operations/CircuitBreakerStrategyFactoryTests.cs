// <copyright file="CircuitBreakerStrategyFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations;

using System;
using System.Reflection;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
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

        // Act & Assert
        factory.WithDefaultCircuitKey("test-key").ShouldBe(factory);
        factory.WithDefaultConsecutiveFailureThreshold(3).ShouldBe(factory);
        factory.WithDefaultFailureRatio(0.4).ShouldBe(factory);
        factory.WithDefaultSamplingDuration(10000).ShouldBe(factory);
        factory.WithDefaultMinimumThroughput(10).ShouldBe(factory);
        factory.WithDefaultDurationOfBreak(15000).ShouldBe(factory);
        factory.WithDefaultHandledException<HttpRequestException>().ShouldBe(factory);
        factory.WithDefaultHandledException(ex => ex is TimeoutException).ShouldBe(factory);
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
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultConsecutiveFailureThreshold(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultFailureRatio(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultFailureRatio(1.1));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultSamplingDuration(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultMinimumThroughput(0));
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultDurationOfBreak(0));
        Should.Throw<ArgumentNullException>(() => factory.WithDefaultHandledException(null!));
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

        var factory = new CircuitBreakerStrategyFactory<TestService>(serviceProvider);

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultCircuitKey("my-shared-circuit")
            .WithDefaultConsecutiveFailureThreshold(4)
            .WithDefaultFailureRatio(0.3)
            .WithDefaultSamplingDuration(20000)
            .WithDefaultMinimumThroughput(8)
            .WithDefaultDurationOfBreak(5000)
            .WithDefaultHandledException<InvalidOperationException>()
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

        typeof(CircuitBreakerStrategy<TestService>).GetProperty("Logger", flags)?.GetValue(cbStrategy).ShouldBe(logger);
        typeof(CircuitBreakerStrategy<TestService>).GetField("circuitKey", flags)?.GetValue(cbStrategy).ShouldBe("my-shared-circuit");
        typeof(CircuitBreakerStrategy<TestService>).GetField("consecutiveFailureThreshold", flags)?.GetValue(cbStrategy).ShouldBe(4);
        typeof(CircuitBreakerStrategy<TestService>).GetField("failureRatio", flags)?.GetValue(cbStrategy).ShouldBe(0.3);
        typeof(CircuitBreakerStrategy<TestService>).GetField("samplingDurationMs", flags)?.GetValue(cbStrategy).ShouldBe(20000);
        typeof(CircuitBreakerStrategy<TestService>).GetField("minimumThroughput", flags)?.GetValue(cbStrategy).ShouldBe(8);
        typeof(CircuitBreakerStrategy<TestService>).GetField("durationOfBreakMs", flags)?.GetValue(cbStrategy).ShouldBe(5000);

        var onBreakAction = (Action<Exception, TimeSpan>?)typeof(CircuitBreakerStrategy<TestService>)
            .GetField("onBreakAction", flags)?.GetValue(cbStrategy);
        onBreakAction.ShouldNotBeNull();
        onBreakAction?.Invoke(new Exception("test"), TimeSpan.FromSeconds(5));
        onBreakCalled.ShouldBeTrue();

        var onResetAction = (Action?)typeof(CircuitBreakerStrategy<TestService>)
            .GetField("onResetAction", flags)?.GetValue(cbStrategy);
        onResetAction.ShouldNotBeNull();
        onResetAction?.Invoke();
        onResetCalled.ShouldBeTrue();

        var onHalfOpenAction = (Action?)typeof(CircuitBreakerStrategy<TestService>)
            .GetField("onHalfOpenAction", flags)?.GetValue(cbStrategy);
        onHalfOpenAction.ShouldNotBeNull();
        onHalfOpenAction?.Invoke();
        onHalfOpenCalled.ShouldBeTrue();
    }
}
