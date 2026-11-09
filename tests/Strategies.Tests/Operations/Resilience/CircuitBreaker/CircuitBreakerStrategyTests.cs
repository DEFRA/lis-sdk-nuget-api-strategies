// <copyright file="CircuitBreakerStrategyTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Resilience.CircuitBreaker;

using System;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly.Registry;
using Shouldly;
using Xunit;

public class CircuitBreakerStrategyTests
{
    private readonly ResiliencePipelineRegistry<string> pipelineRegistry = new();
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();

    public CircuitBreakerStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void FluentSetters_WithInvalidArguments_ShouldThrow()
    {
        var strategy = CreateStrategy();

        Should.Throw<ArgumentException>(() => strategy.WithCircuitKey(""));
        Should.Throw<ArgumentNullException>(() => strategy.WithAction((Func<CancellationToken, Task>)null!));
        Should.Throw<ArgumentNullException>(() => strategy.WithAction((Func<CancellationToken, Task<string>>)null!));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithConsecutiveFailureThreshold(0));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithFailureRatio(0));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithFailureRatio(1.1));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithSamplingDuration(0));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithMinimumThroughput(0));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithDurationOfBreak(0));
        Should.Throw<ArgumentNullException>(() => strategy.WithHandledException(null!));
        Should.Throw<ArgumentNullException>(() => strategy.WithHandledResult<int>(null!));
        Should.Throw<ArgumentNullException>(() => strategy.WithFallback((Func<CancellationToken, Task>)null!));
        Should.Throw<ArgumentNullException>(() => strategy.WithFallback((Func<CancellationToken, Task<string>>)null!));
        Should.Throw<ArgumentNullException>(() => strategy.OnBreak(null!));
        Should.Throw<ArgumentNullException>(() => strategy.OnReset(null!));
        Should.Throw<ArgumentNullException>(() => strategy.OnHalfOpen(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoggerNull_ThrowsInvalidOperationException()
    {
        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithActionDescription("Test")
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ => Task.CompletedTask);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync());
        ex.Message.ShouldBe(StrategyConstants.Errors.LoggerRequired);
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionDescriptionNull_ThrowsInvalidOperationException()
    {
        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ => Task.CompletedTask);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync());
        ex.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoActionConfigured_ShouldThrowInvalidOperationException()
    {
        var strategy = CreateStrategy()
            .WithConsecutiveFailureThreshold(3);

        await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoThresholdConfigured_ShouldThrowInvalidOperationException()
    {
        var strategy = CreateStrategy()
            .WithAction(_ => Task.CompletedTask);

        await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_VoidAction_SuccessfulExecution()
    {
        var actionExecuted = false;
        var strategy = CreateStrategy()
            .WithCircuitKey("void-success-key")
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ =>
            {
                actionExecuted = true;
                return Task.CompletedTask;
            });

        await strategy.ExecuteAsync();

        actionExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_GenericAction_SuccessfulExecution()
    {
        var strategy = CreateStrategy()
            .WithCircuitKey("generic-success-key")
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ => Task.FromResult("result-value"));

        var result = await strategy.ExecuteAsync<string>();

        result.ShouldBe("result-value");
    }

    [Fact]
    public async Task ExecuteAsync_ConsecutiveFailures_OpensCircuitAndThrowsCircuitBreakerOpenException()
    {
        var onBreakCalled = false;
        var strategy = CreateStrategy()
            .WithCircuitKey("consecutive-fail-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(5000)
            .WithHandledException<InvalidOperationException>()
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new InvalidOperationException("boom"));

        // Call 1: fails
        await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync());

        // Call 2: fails and trips circuit
        await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync());
        onBreakCalled.ShouldBeTrue();

        // Call 3: circuit is now OPEN
        var ex = await Should.ThrowAsync<CircuitBreakerOpenException>(() => strategy.ExecuteAsync());
        ex.Message.ShouldContain("is OPEN");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitOpen_ExecutesFallback()
    {
        var fallbackCalled = false;
        var strategy = CreateStrategy()
            .WithCircuitKey("fallback-key")
            .WithConsecutiveFailureThreshold(1)
            .WithDurationOfBreak(5000)
            .WithHandledException<InvalidOperationException>()
            .WithAction<string>(_ => throw new InvalidOperationException("backend error"))
            .WithFallback<string>(_ =>
            {
                fallbackCalled = true;
                return Task.FromResult("fallback-data");
            });

        // Call 1: fails and executes fallback
        var result1 = await strategy.ExecuteAsync<string>();
        result1.ShouldBe("fallback-data");
        fallbackCalled.ShouldBeTrue();

        // Call 2: circuit is OPEN, should execute fallback
        var result2 = await strategy.ExecuteAsync<string>();
        result2.ShouldBe("fallback-data");
    }

    [Fact]
    public async Task ExecuteAsync_VoidAction_WhenCircuitOpen_ExecutesFallback()
    {
        var fallbackCalled = false;
        var strategy = CreateStrategy()
            .WithCircuitKey("void-fallback-key")
            .WithConsecutiveFailureThreshold(1)
            .WithDurationOfBreak(5000)
            .WithHandledException<InvalidOperationException>()
            .WithAction(_ => throw new InvalidOperationException("backend error"))
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            });

        await strategy.ExecuteAsync();

        fallbackCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnhandledExceptionThrown_DoesNotExecuteFallbackAndRethrows()
    {
        var fallbackCalled = false;
        var strategy = CreateStrategy()
            .WithCircuitKey("unhandled-ex-key")
            .WithConsecutiveFailureThreshold(2)
            .WithHandledException<ArgumentException>()
            .WithAction<string>(_ => throw new InvalidOperationException("unhandled"))
            .WithFallback<string>(_ =>
            {
                fallbackCalled = true;
                return Task.FromResult("fallback");
            });

        await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync<string>());
        fallbackCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandledResultPredicateMatches_TriggersCircuitFailure()
    {
        var onBreakCalled = false;
        var strategy = CreateStrategy()
            .WithCircuitKey("handled-result-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(5000)
            .WithHandledResult<int>(res => res == 500)
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => Task.FromResult(500));

        var res1 = await strategy.ExecuteAsync<int>();
        res1.ShouldBe(500);

        var res2 = await strategy.ExecuteAsync<int>();
        res2.ShouldBe(500);

        onBreakCalled.ShouldBeTrue();

        // Next call should be blocked by circuit breaker
        var strategy2 = CreateStrategy()
            .WithCircuitKey("handled-result-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(5000)
            .WithAction(_ => Task.FromResult(200));

        await Should.ThrowAsync<CircuitBreakerOpenException>(() => strategy2.ExecuteAsync<int>());
    }

    [Fact]
    public async Task ExecuteAsync_RatioBased_OpensCircuitWhenRatioExceeded()
    {
        var onBreakCalled = false;
        var strategy = CreateStrategy()
            .WithCircuitKey("ratio-circuit-key")
            .WithFailureRatio(0.5)
            .WithSamplingDuration(10000)
            .WithMinimumThroughput(2)
            .WithDurationOfBreak(5000)
            .WithHandledException<InvalidOperationException>()
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new InvalidOperationException("failed"));

        await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync());
        await Should.ThrowAsync<InvalidOperationException>(() => strategy.ExecuteAsync());

        onBreakCalled.ShouldBeTrue();

        await Should.ThrowAsync<CircuitBreakerOpenException>(() => strategy.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_LifecycleHooksAndValidation_ShouldBeExecuted()
    {
        var beforeCalled = false;
        var afterCalled = false;
        var validateCalled = false;

        var strategy = CreateStrategy()
            .WithCircuitKey("hooks-key")
            .WithConsecutiveFailureThreshold(3)
            .WithBeforeExecute(() =>
            {
                beforeCalled = true;
                return Task.CompletedTask;
            })
            .WithAfterExecute(() =>
            {
                afterCalled = true;
                return Task.CompletedTask;
            })
            .WithRequestValidation(() =>
            {
                validateCalled = true;
                return Task.FromResult(new RequestValidationResult(Array.Empty<RequestValidationFailure>()));
            })
            .WithAction(_ => Task.FromResult(42));

        var result = await strategy.ExecuteAsync<int>();

        result.ShouldBe(42);
        beforeCalled.ShouldBeTrue();
        afterCalled.ShouldBeTrue();
        validateCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_RequiresAuthenticatedOperator_WhenNotAuthenticated_Throws()
    {
        var operatorContext = Substitute.For<IOperatorContext>();
        operatorContext.HasAuthenticatedOperator.Returns(false);

        var strategy = CreateStrategy()
            .WithCircuitKey("auth-key")
            .WithConsecutiveFailureThreshold(3)
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator()
            .WithAction(_ => Task.CompletedTask);

        await Should.ThrowAsync<UnauthorizedAccessException>(() => strategy.ExecuteAsync());
    }

    private CircuitBreakerStrategy<TestService> CreateStrategy()
    {
        var s = new CircuitBreakerStrategy<TestService>(pipelineRegistry);
        s.WithLogger(logger);
        s.WithActionDescription("TestAction");
        return s;
    }
}
