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
    [LoggerMessage(LogLevel.Warning, "Circuit breaker '{CircuitKey}' is OPEN for action '{ActionDescription}'")]
    static partial void LogCircuitBreakerOpen(ILogger<TService> logger, string circuitKey, string actionDescription, Exception ex);

    [LoggerMessage(LogLevel.Information, "Executing fallback action for '{ActionDescription}'")]
    static partial void LogExecutingFallbackAction(ILogger<TService> logger, string actionDescription);

    [LoggerMessage(LogLevel.Warning, "Circuit breaker operation '{ActionDescription}' encountered handled failure")]
    static partial void LogCircuitBreakerOperationEncounteredHandledFailure(ILogger<TService> logger, string actionDescription, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Circuit '{Tag}' opened for {Duration}s due to error: {Reason}")]
    static partial void LogCircuitOpened(ILogger<TService> logger, string tag, double duration, string? reason);

    [LoggerMessage(LogLevel.Information, "Circuit '{Tag}' reset to CLOSED")]
    static partial void LogCircuitClosed(ILogger<TService> logger, string tag);

    [LoggerMessage(LogLevel.Information, "Circuit '{Tag}' entered HALF-OPEN state")]
    static partial void LogCircuitHalfOpened(ILogger<TService> logger, string tag);

    private void LogCircuitBreakerOpen(string circuitKey, Exception ex)
    {
        if (Logger != null && ActionDescription != null)
        {
            LogCircuitBreakerOpen(Logger, circuitKey, ActionDescription, ex);
        }
    }

    private void LogExecutingFallbackAction()
    {
        if (Logger != null && ActionDescription != null)
        {
            LogExecutingFallbackAction(Logger, ActionDescription);
        }
    }

    private void LogCircuitBreakerOperationEncounteredHandledFailure(Exception ex)
    {
        if (Logger != null && ActionDescription != null)
        {
            LogCircuitBreakerOperationEncounteredHandledFailure(Logger, ActionDescription, ex);
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
