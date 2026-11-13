// <copyright file="CircuitBreakerHttpPolicyTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Resilience.CircuitBreaker.Policies;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions.Base;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Policies;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly.Registry;
using Polly.Timeout;
using Shouldly;
using Xunit;

public class CircuitBreakerHttpPolicyTests
{
    private readonly ResiliencePipelineRegistry<string> pipelineRegistry = new();
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();

    [Fact]
    public void Execute_WhenCircuitBreakerStrategyIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => policy.Execute<TestService>(null!));
    }

    [Fact]
    public void Execute_RegistersHandledExceptionPredicatesOnStrategy()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        strategy.WithHandledException<TimeoutRejectedException>()
            .Returns(strategy);

        // Act
        policy.Execute(strategy);

        // Assert
        strategy.Received(1).WithHandledException(Arg.Any<Func<Exception, CancellationToken, bool>>());
        strategy.Received(1).WithHandledException<TimeoutRejectedException>();
        strategy.DidNotReceive().WithHandledException(Arg.Any<Func<Exception, bool>>());
        strategy.DidNotReceive().WithHandledException<TimeoutException>();
        capturedCtPredicates.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.OK, false)]
    public void Predicate_WhenHttpClientExceptionWithStatusCode_ReturnsExpected(
        HttpStatusCode statusCode,
        bool expected)
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];

        var exception = new TestHttpClientException(
            "HTTP error",
            new HttpRequestException("inner", null, statusCode));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public void Predicate_WhenRestRequestExceptionWithStatusCode_ReturnsExpected(
        HttpStatusCode statusCode,
        bool expected)
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];

        var exception = new RestRequestException(
            "REST error",
            new HttpRequestException("inner", null, statusCode));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public void Predicate_WhenSoapRequestExceptionWithStatusCode_ReturnsExpected(
        HttpStatusCode statusCode,
        bool expected)
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];

        var exception = new SoapRequestException(
            "SOAP error",
            new HttpRequestException("inner", null, statusCode));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Predicate_WhenHttpClientExceptionWithNetworkErrorNullStatusCode_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];

        var exception = new TestHttpClientException(
            "Network error",
            new HttpRequestException("Connection refused"));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Predicate_WhenHttpClientExceptionWithNetworkErrorZeroStatusCode_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>> { Capacity = 0, };

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];

        var exception = new TestHttpClientException(
            "Network error",
            new HttpRequestException("Zero status", null, 0));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Predicate_WhenHttpClientExceptionWithInnerTimeoutException_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];
        var exception = new TestHttpClientException("Timeout", new TimeoutException("Direct inner timeout"));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Predicate_WhenHttpClientExceptionWithInnerOperationCanceledExceptionHavingTimeoutInner_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];
        var innerOpCanceled = new OperationCanceledException("Timed out", new TimeoutException("Nested timeout"));
        var exception = new TestHttpClientException("Timeout", innerOpCanceled);

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Predicate_WhenHttpClientExceptionWithInnerTaskCanceledExceptionAndCancellationNotRequested_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];
        var exception = new TestHttpClientException("Task canceled", new TaskCanceledException("Http client timeout"));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Predicate_WhenHttpClientExceptionWithInnerTaskCanceledExceptionAndCancellationRequested_ReturnsFalse()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        cts.Cancel();

        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);
        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];
        var exception = new TestHttpClientException("Task canceled", new TaskCanceledException("User canceled"));

        // Act
        var result = httpPredicate(exception, cts.Token);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Predicate_WhenRestRequestExceptionWithInnerTimeoutException_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];
        var exception = new RestRequestException("Timeout", new TimeoutException("Inner timeout"));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Predicate_WhenSoapRequestExceptionWithInnerTimeoutException_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];
        var exception = new SoapRequestException("Timeout", new TimeoutException("Inner timeout"));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Predicate_WhenHttpClientExceptionWithUnrelatedInnerException_ReturnsFalse()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];
        var exception = new TestHttpClientException("Other", new InvalidOperationException("Other inner"));

        // Act
        var result = httpPredicate(exception, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Predicate_WhenExceptionIsNotHttpClientException_ReturnsFalse()
    {
        // Arrange
        var policy = new CircuitBreakerHttpPolicy();
        var strategy = Substitute.For<ICircuitBreakerStrategy<TestService>>();
        var capturedCtPredicates = new List<Func<Exception, CancellationToken, bool>>();

        strategy.WithHandledException(
                Arg.Do<Func<Exception, CancellationToken, bool>>(capturedCtPredicates.Add))
            .Returns(strategy);

        policy.Execute(strategy);

        capturedCtPredicates.Count.ShouldBe(1);

        var httpPredicate = capturedCtPredicates[0];

        var exceptions = new[]
        {
            new InvalidOperationException("invalid"), new HttpRequestException("direct http request exception"),
            new Exception("general"), new TimeoutException("timeout"), new OperationCanceledException("canceled"),
            new TaskCanceledException("task canceled"),
        };

        foreach (var ex in exceptions)
        {
            // Act
            var result = httpPredicate(ex, CancellationToken.None);

            // Assert
            result.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task StrategyExecution_WithHttpPolicy_TripsCircuitOn500AndExecutesFallback()
    {
        // Arrange
        var fallbackCalled = false;
        var onBreakCalled = false;

        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithActionDescription("TestHttpAction")
            .WithCircuitKey("http-policy-500-key")
            .WithConsecutiveFailureThreshold(2)
            .WithPolicy(new CircuitBreakerHttpPolicy())
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            })
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new RestRequestException(
                "500 Internal Server Error",
                new HttpRequestException("500", null, HttpStatusCode.InternalServerError)));

        // Act
        await strategy.ExecuteAsync();
        await strategy.ExecuteAsync();

        // Assert
        fallbackCalled.ShouldBeTrue();
        onBreakCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task StrategyExecution_WithHttpPolicy_TripsCircuitOn429TooManyRequestsAndExecutesFallback()
    {
        // Arrange
        var fallbackCalled = false;
        var onBreakCalled = false;

        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithActionDescription("Test429Action")
            .WithCircuitKey("http-policy-429-key")
            .WithConsecutiveFailureThreshold(2)
            .WithPolicy(new CircuitBreakerHttpPolicy())
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            })
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new RestRequestException(
                "429 Too Many Requests",
                new HttpRequestException("429", null, HttpStatusCode.TooManyRequests)));

        // Act
        await strategy.ExecuteAsync();
        await strategy.ExecuteAsync();

        // Assert
        fallbackCalled.ShouldBeTrue();
        onBreakCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task StrategyExecution_WithHttpPolicy_TripsCircuitOnNetworkErrorAndExecutesFallback()
    {
        // Arrange
        var fallbackCalled = false;
        var onBreakCalled = false;

        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithActionDescription("TestNetworkErrorAction")
            .WithCircuitKey("http-policy-network-error-key")
            .WithConsecutiveFailureThreshold(2)
            .WithPolicy(new CircuitBreakerHttpPolicy())
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            })
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ =>
                throw new RestRequestException("Network Failure", new HttpRequestException("Connection refused")));

        // Act
        await strategy.ExecuteAsync();
        await strategy.ExecuteAsync();

        // Assert
        fallbackCalled.ShouldBeTrue();
        onBreakCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task StrategyExecution_WithHttpPolicy_TripsCircuitOnWrappedTimeoutExceptionAndExecutesFallback()
    {
        // Arrange
        var fallbackCalled = false;
        var onBreakCalled = false;

        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithActionDescription("TestWrappedTimeoutAction")
            .WithCircuitKey("http-policy-wrapped-timeout-key")
            .WithConsecutiveFailureThreshold(2)
            .WithPolicy(new CircuitBreakerHttpPolicy())
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            })
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new RestRequestException("Timeout", new TimeoutException("Request timed out")));

        // Act
        await strategy.ExecuteAsync();
        await strategy.ExecuteAsync();

        // Assert
        fallbackCalled.ShouldBeTrue();
        onBreakCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task
        StrategyExecution_WithHttpPolicy_TripsCircuitOnWrappedOperationCanceledWithTimeoutInnerAndExecutesFallback()
    {
        // Arrange
        var fallbackCalled = false;
        var onBreakCalled = false;

        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithActionDescription("TestWrappedOpCanceledTimeoutAction")
            .WithCircuitKey("http-policy-wrapped-opcanceled-timeout-key")
            .WithConsecutiveFailureThreshold(2)
            .WithPolicy(new CircuitBreakerHttpPolicy())
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            })
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new RestRequestException(
                "Timeout",
                new OperationCanceledException("Timeout", new TimeoutException("Inner timeout"))));

        // Act
        await strategy.ExecuteAsync();
        await strategy.ExecuteAsync();

        // Assert
        fallbackCalled.ShouldBeTrue();
        onBreakCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task
        StrategyExecution_WithHttpPolicy_TripsCircuitOnWrappedTaskCanceledWhenCancellationNotRequestedAndExecutesFallback()
    {
        // Arrange
        var fallbackCalled = false;
        var onBreakCalled = false;

        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithActionDescription("TestWrappedTaskCanceledAction")
            .WithCircuitKey("http-policy-wrapped-taskcanceled-key")
            .WithConsecutiveFailureThreshold(2)
            .WithPolicy(new CircuitBreakerHttpPolicy())
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            })
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ =>
                throw new RestRequestException("Task canceled", new TaskCanceledException("Internal timeout")));

        // Act
        await strategy.ExecuteAsync();
        await strategy.ExecuteAsync();

        // Assert
        fallbackCalled.ShouldBeTrue();
        onBreakCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task
        StrategyExecution_WithHttpPolicy_DoesNotTripCircuitOnWrappedTaskCanceledWhenCancellationRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        var onBreakCalled = false;
        var fallbackCalled = false;

        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(cts.Token)
            .WithActionDescription("TestUserCanceledWrappedAction")
            .WithCircuitKey("http-policy-user-canceled-wrapped-key")
            .WithConsecutiveFailureThreshold(2)
            .WithPolicy(new CircuitBreakerHttpPolicy())
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            })
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ =>
                throw new RestRequestException("User canceled", new TaskCanceledException("User canceled")));

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(strategy.ExecuteAsync);
        await Should.ThrowAsync<OperationCanceledException>(strategy.ExecuteAsync);

        onBreakCalled.ShouldBeFalse();
        fallbackCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task StrategyExecution_WithHttpPolicy_TripsCircuitOnTimeoutRejectedExceptionAndExecutesFallback()
    {
        // Arrange
        var fallbackCalled = false;
        var onBreakCalled = false;

        var strategy = new CircuitBreakerStrategy<TestService>(pipelineRegistry)
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithActionDescription("TestTimeoutRejectedAction")
            .WithCircuitKey("http-policy-timeout-rejected-key")
            .WithConsecutiveFailureThreshold(2)
            .WithPolicy(new CircuitBreakerHttpPolicy())
            .WithFallback(_ =>
            {
                fallbackCalled = true;
                return Task.CompletedTask;
            })
            .OnBreak((_, _) => onBreakCalled = true)
            .WithAction(_ => throw new TimeoutRejectedException("Timeout rejected"));

        // Act
        await strategy.ExecuteAsync();
        await strategy.ExecuteAsync();

        // Assert
        fallbackCalled.ShouldBeTrue();
        onBreakCalled.ShouldBeTrue();
    }

    private sealed class TestHttpClientException : HttpClientException
    {
        public TestHttpClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
