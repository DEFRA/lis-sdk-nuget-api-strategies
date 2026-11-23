// <copyright file="CircuitBreakerStrategy.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker;

using System;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logging operations for the circuit breaker strategy builder class.
/// </summary>
/// <typeparam name="TService">The type of the service class that is consuming the derived builder.</typeparam>
public sealed partial class CircuitBreakerStrategy<TService>
{
    [LoggerMessage(LogLevel.Warning, "Circuit breaker '{CircuitKey}' is open for action '{ActionDescription}'")]
    static partial void LogCircuitBreakerOpen(ILogger<TService> logger, string circuitKey, string actionDescription,
        Exception ex);

    [LoggerMessage(LogLevel.Information, "Circuit breaker '{CircuitKey}' Executing fallback action")]
    static partial void LogExecutingFallbackAction(ILogger<TService> logger, string circuitKey);

    [LoggerMessage(LogLevel.Warning, "Circuit breaker '{CircuitKey}' encountered handled failure")]
    static partial void LogCircuitBreakerOperationEncounteredHandledFailure(ILogger<TService> logger,
        string circuitKey, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Circuit breaker '{Tag}' opened for {Duration}s due to error: {Reason}")]
    static partial void LogCircuitOpened(ILogger<TService> logger, string tag, double duration, string? reason);

    [LoggerMessage(LogLevel.Information, "Circuit breaker '{Tag}' reset to closed")]
    static partial void LogCircuitClosed(ILogger<TService> logger, string tag);

    [LoggerMessage(LogLevel.Information, "Circuit breaker '{Tag}' entered half-open state")]
    static partial void LogCircuitHalfOpened(ILogger<TService> logger, string tag);

    private void LogCircuitBreakerOpen(string circuitKey, Exception ex)
    {
        if (Logger != null && ActionDescription != null)
        {
            LogCircuitBreakerOpen(Logger, circuitKey, ActionDescription.ToLowerInvariant(), ex);
        }
    }

    private void LogExecutingFallbackAction(string circuitKey)
    {
        if (Logger != null)
        {
            LogExecutingFallbackAction(Logger, circuitKey);
        }
    }

    private void LogCircuitBreakerOperationEncounteredHandledFailure(string circuitKey, Exception ex)
    {
        if (Logger != null)
        {
            LogCircuitBreakerOperationEncounteredHandledFailure(Logger, circuitKey, ex);
        }
    }

    private void LogCircuitOpened(string tag, double duration, string? reason)
    {
        if (Logger != null)
        {
            LogCircuitOpened(Logger, tag, duration, reason);
        }
    }

    private void LogCircuitClosed(string tag)
    {
        if (Logger != null)
        {
            LogCircuitClosed(Logger, tag);
        }
    }

    private void LogCircuitHalfOpened(string tag)
    {
        if (Logger != null)
        {
            LogCircuitHalfOpened(Logger, tag);
        }
    }
}
