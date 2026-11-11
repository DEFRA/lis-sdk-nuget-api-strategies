// <copyright file="CircuitBreakerStrategyTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Resilience.CircuitBreaker;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Constants;
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
    public void WithCircuitKey_SetsCircuitKeyAndTargetDescription()
    {
        // Arrange
        var strategy = CreateStrategy();

        // Act
        strategy.WithCircuitKey("my-circuit-key");

        // Assert
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        typeof(CircuitBreakerStrategy<TestService>).GetProperty("CircuitKey", flags)?.GetValue(strategy)
            .ShouldBe("my-circuit-key");
        typeof(CircuitBreakerStrategy<TestService>).GetProperty("TargetDescription", flags)?.GetValue(strategy)
            .ShouldBe("circuit-breaker-my-circuit-key");
    }

    [Fact]
    public void FluentSetters_WithInvalidArguments_ShouldThrow()
    {
        // Arrange
        var strategy = CreateStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithCircuitKey(string.Empty));
        Should.Throw<ArgumentNullException>(() => strategy.WithAction(null!));
        Should.Throw<ArgumentNullException>(() => strategy.WithAction<string>(null!));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithConsecutiveFailureThreshold(0));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithFailureRatio(0));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithFailureRatio(1.1));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithSamplingDuration(0));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithMinimumThroughput(0));
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithDurationOfBreak(0));
        Should.Throw<ArgumentNullException>(() => strategy.WithHandledException(null!));
        Should.Throw<ArgumentNullException>(() => strategy.WithHandledResult<int>(null!));
        Should.Throw<ArgumentNullException>(() => strategy.WithFallback(null!));
        Should.Throw<ArgumentNullException>(() => strategy.WithFallback<string>(null!));
        Should.Throw<ArgumentNullException>(() => strategy.OnBreak(null!));
        Should.Throw<ArgumentNullException>(() => strategy.OnReset(null!));
        Should.Throw<ArgumentNullException>(() => strategy.OnHalfOpen(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoggerNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithActionDescription("Test")
            .WithCancellationToken(CancellationToken.None)
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ => Task.CompletedTask);

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        // Assert
        ex.Message.ShouldBe(StrategyConstants.Errors.LoggerRequired);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationTokenNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithActionDescription("Test")
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ => Task.CompletedTask);

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        // Assert
        ex.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionDescriptionNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithCircuitKey("TestKey")
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ => Task.CompletedTask);

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        // Assert
        ex.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitKeyNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithActionDescription("Test")
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ => Task.CompletedTask);

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        // Assert
        ex.Message.ShouldBe(CircuitBreakerStrategyConstants.Errors.CircuitKeyRequired);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoActionConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = CreateStrategy()
            .WithCircuitKey("no-action-key")
            .WithConsecutiveFailureThreshold(3);

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        // Assert
        ex.Message.ShouldBe("Action to execute has not been configured.");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoThresholdConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = CreateStrategy()
            .WithCircuitKey("no-threshold-key")
            .WithAction(_ => Task.CompletedTask);

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        // Assert
        ex.Message.ShouldBe("Circuit breaker must be configured with at least one threshold.");
    }

    [Fact]
    public async Task ExecuteAsync_VoidAction_SuccessfulExecution()
    {
        // Arrange
        var actionExecuted = false;

        var strategy = CreateStrategy()
            .WithCircuitKey("void-success-key")
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ =>
            {
                actionExecuted = true;
                return Task.CompletedTask;
            });

        // Act
        await strategy.ExecuteAsync();

        // Assert
        actionExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_GenericAction_SuccessfulExecution()
    {
        // Arrange
        var strategy = CreateStrategy()
            .WithCircuitKey("generic-success-key")
            .WithConsecutiveFailureThreshold(3)
            .WithAction(_ => Task.FromResult("result-value"));

        // Act
        var result = await strategy.ExecuteAsync<string>();

        // Assert
        result.ShouldBe("result-value");
    }

    [Fact]
    public async Task ExecuteAsync_ConsecutiveFailures_OpensCircuitAndThrowsCircuitBreakerOpenException()
    {
        // Arrange
        var onBreakCalled = false;

        var strategy = CreateStrategy()
            .WithCircuitKey("consecutive-fail-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(5000)
            .WithHandledException<InvalidOperationException>()
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new InvalidOperationException("boom"));

        // Act & Assert
        // Call 1: fails
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Circuit breaker operation 'TestAction' encountered handled failure");

        // Call 2: fails and trips circuit
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        onBreakCalled.ShouldBeTrue();

        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Circuit 'consecutive-fail-key:consecutive' opened for 5s due to error: boom");

        // Call 3: circuit is now OPEN
        var ex = await Should.ThrowAsync<CircuitBreakerOpenException>(strategy.ExecuteAsync);

        ex.Message.ShouldContain("is OPEN");

        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Circuit breaker 'consecutive-fail-key' is OPEN for action 'TestAction'");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitOpen_ExecutesFallback()
    {
        // Arrange
        var fallbackCount = 0;

        var strategy = CreateStrategy()
            .WithCircuitKey("fallback-key")
            .WithConsecutiveFailureThreshold(1)
            .WithDurationOfBreak(5000)
            .WithHandledException<InvalidOperationException>()
            .WithAction<string>(_ => throw new InvalidOperationException("backend error"))
            .WithFallback<string>(_ =>
            {
                fallbackCount++;
                return Task.FromResult("fallback-data");
            });

        // Act
        // Calls 1 and 2 fail and trip the circuit
        var result1 = await strategy.ExecuteAsync<string>();
        var result2 = await strategy.ExecuteAsync<string>();

        // Call 3: circuit is OPEN (Polly throws BrokenCircuitException on execution), executes fallback
        var result3 = await strategy.ExecuteAsync<string>();

        // Assert
        result1.ShouldBe("fallback-data");
        result2.ShouldBe("fallback-data");
        result3.ShouldBe("fallback-data");

        fallbackCount.ShouldBe(3);

        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Circuit breaker operation 'TestAction' encountered handled failure",
            count: 2);

        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Circuit breaker 'fallback-key' is OPEN for action 'TestAction'",
            count: 1);

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Executing fallback action for 'TestAction'",
            count: 3);
    }

    [Fact]
    public async Task ExecuteAsync_VoidAction_WhenCircuitOpen_ExecutesFallback()
    {
        // Arrange
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

        // Act
        await strategy.ExecuteAsync();

        // Assert
        fallbackCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnhandledExceptionThrown_DoesNotExecuteFallbackAndRethrows()
    {
        // Arrange
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

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync<string>);

        fallbackCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandledResultPredicateMatches_TriggersCircuitFailure()
    {
        // Arrange
        var onBreakCalled = false;

        var strategy = CreateStrategy()
            .WithCircuitKey("handled-result-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(5000)
            .WithHandledResult<int>(res => res == 500)
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => Task.FromResult(500));

        // Act
        var res1 = await strategy.ExecuteAsync<int>();
        var res2 = await strategy.ExecuteAsync<int>();

        // Assert
        res1.ShouldBe(500);
        res2.ShouldBe(500);

        onBreakCalled.ShouldBeTrue();

        // Next call should be blocked by circuit breaker
        var strategy2 = CreateStrategy()
            .WithCircuitKey("handled-result-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(5000)
            .WithAction(_ => Task.FromResult(200));

        await Should.ThrowAsync<CircuitBreakerOpenException>(strategy2.ExecuteAsync<int>);
    }

    [Fact]
    public async Task ExecuteAsync_RatioBased_OpensCircuitWhenRatioExceeded()
    {
        // Arrange
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

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        onBreakCalled.ShouldBeTrue();

        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Circuit 'ratio-circuit-key:ratio' opened for 5s due to error: failed");

        await Should.ThrowAsync<CircuitBreakerOpenException>(strategy.ExecuteAsync);
    }

    [Fact]
    public async Task ExecuteAsync_LifecycleHooksAndValidation_ShouldBeExecuted()
    {
        // Arrange
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

        // Act
        var result = await strategy.ExecuteAsync<int>();

        // Assert
        result.ShouldBe(42);
        beforeCalled.ShouldBeTrue();
        afterCalled.ShouldBeTrue();
        validateCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_RequiresAuthenticatedOperator_WhenNotAuthenticated_Throws()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();

        operatorContext.HasAuthenticatedOperator.Returns(false);

        var strategy = CreateStrategy()
            .WithCircuitKey("auth-key")
            .WithConsecutiveFailureThreshold(3)
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator()
            .WithAction(_ => Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(strategy.ExecuteAsync);
    }

    [Fact]
    public async Task ExecuteAsync_CircuitRecovery_TriggersOnHalfOpenAndOnResetAndLogs()
    {
        // Arrange
        var onHalfOpenCalled = false;
        var onResetCalled = false;
        var fail = true;

        var strategy = CreateStrategy()
            .WithCircuitKey("recovery-circuit-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(500) // 500ms break (Polly minimum)
            .WithHandledException<InvalidOperationException>()
            .OnHalfOpen(() => onHalfOpenCalled = true)
            .OnReset(() => onResetCalled = true)
            .WithAction(_ => fail
                ? throw new InvalidOperationException("temporary failure")
                : Task.FromResult("recovered"));

        // Act & Assert
        // Calls 1 and 2: fail and trip circuit
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync<string>);
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync<string>);

        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Circuit 'recovery-circuit-key:consecutive' opened for 0.5s due to error: temporary failure");

        // Wait for break duration to elapse so circuit enters Half-Open on next call
        await Task.Delay(600, TestContext.Current.CancellationToken);

        // Call 3: probe succeeds -> transitions from Half-Open to Closed
        fail = false;

        var result = await strategy.ExecuteAsync<string>();

        result.ShouldBe("recovered");

        onHalfOpenCalled.ShouldBeTrue();
        onResetCalled.ShouldBeTrue();

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Circuit 'recovery-circuit-key:consecutive' entered HALF-OPEN state");

        logger.ShouldHaveReceived(LogLevel.Information, "Circuit 'recovery-circuit-key:consecutive' reset to CLOSED");
    }

    [Fact]
    public async Task ExecuteAsync_WithHandledExceptionPredicate_HandlesMatchingAndIgnoresNonMatching()
    {
        // Arrange
        var strategy = CreateStrategy()
            .WithCircuitKey("predicate-ex-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(5000)
            .WithHandledException(ex => ex is InvalidOperationException ioe && ioe.Message.Contains("handled"))
            .WithAction<string>(_ => throw new InvalidOperationException("not-a-match"))
            .WithFallback<string>(_ => Task.FromResult("fallback-executed"));

        // Act & Assert
        // When exception does not match predicate, fallback should not be executed and exception is rethrown
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync<string>);

        // Arrange matching strategy
        var matchingStrategy = CreateStrategy()
            .WithCircuitKey("predicate-ex-matching-key")
            .WithConsecutiveFailureThreshold(2)
            .WithDurationOfBreak(5000)
            .WithHandledException(ex => ex is InvalidOperationException ioe && ioe.Message.Contains("handled"))
            .WithAction<string>(_ => throw new InvalidOperationException("handled error"))
            .WithFallback<string>(_ => Task.FromResult("fallback-executed"));

        // Act
        var result = await matchingStrategy.ExecuteAsync<string>();

        // Assert
        result.ShouldBe("fallback-executed");
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionTypeMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        // Configured as void action (Task), but executed with generic ExecuteAsync<int>()
        // In this case, void action returns null, casting null to int throws or configuredAction is Func<CancellationToken, Task<object?>>
        // Let's test configuredAction is Func<CancellationToken, Task<string>> but ExecuteAsync<int>() is called
        var strategy = CreateStrategy()
            .WithCircuitKey("mismatch-key")
            .WithConsecutiveFailureThreshold(2)
            .WithAction(_ => Task.FromResult("string-result"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync<int>);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFallbackTypeMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategy = CreateStrategy()
            .WithCircuitKey("fallback-mismatch-key")
            .WithConsecutiveFailureThreshold(1)
            .WithHandledException<InvalidOperationException>()
            .WithAction<int>(_ => throw new InvalidOperationException("error"))
            .WithFallback<string>(_ => Task.FromResult("string-fallback"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync<int>);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultTimingOptions_ExecutesSuccessfully()
    {
        // Arrange
        // When durationOfBreakMs, samplingDurationMs, and minimumThroughput are not explicitly set
        var strategy = CreateStrategy()
            .WithCircuitKey("defaults-timing-key")
            .WithFailureRatio(0.5)
            .WithAction(_ => Task.FromResult("ok"));

        // Act
        var result = await strategy.ExecuteAsync<string>();

        // Assert
        result.ShouldBe("ok");
    }

    [Fact]
    public async Task ExecuteAsync_WhenConsecutiveThresholdIsOne_ConfiguresMinimumThroughputCorrectly()
    {
        // Arrange
        var onBreakCalled = false;
        var strategy = CreateStrategy()
            .WithCircuitKey("threshold-one-key")
            .WithConsecutiveFailureThreshold(1)
            .WithDurationOfBreak(5000)
            .WithHandledException<InvalidOperationException>()
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new InvalidOperationException("fail immediately"));

        // Act & Assert
        // Call 1
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        // Call 2: with minThroughput=2, ratio=0.5, 2 failures out of 2 requests trips the circuit
        await Should.ThrowAsync<InvalidOperationException>(strategy.ExecuteAsync);

        onBreakCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithHandledResultNonMatching_ReturnsResultWithoutBreaking()
    {
        // Arrange
        var strategy = CreateStrategy()
            .WithCircuitKey("handled-result-no-match-key")
            .WithConsecutiveFailureThreshold(2)
            .WithHandledResult<int>(res => res == 500)
            .WithAction(_ => Task.FromResult(200));

        // Act
        var result = await strategy.ExecuteAsync<int>();

        // Assert
        result.ShouldBe(200);
    }

    [Fact]
    public async Task ExecuteAsync_WithExplicitCircuitKey_ExecutesSuccessfully()
    {
        // Arrange
        var strategy = CreateStrategy()
            .WithCircuitKey("explicit-key")
            .WithConsecutiveFailureThreshold(2)
            .WithAction(_ => Task.FromResult("explicit-key-result"));

        // Act
        var result = await strategy.ExecuteAsync<string>();

        // Assert
        result.ShouldBe("explicit-key-result");
    }

    private CircuitBreakerStrategy<TestService> CreateStrategy()
    {
        var s = new CircuitBreakerStrategy<TestService>(pipelineRegistry);
        s.WithLogger(logger);
        s.WithCancellationToken(CancellationToken.None);
        s.WithActionDescription("TestAction");
        return s;
    }
}
