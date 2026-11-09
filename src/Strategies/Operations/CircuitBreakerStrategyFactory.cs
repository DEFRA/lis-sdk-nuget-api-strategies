// <copyright file="CircuitBreakerStrategyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations;

using System;
using System.Collections.Generic;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;

public sealed class CircuitBreakerStrategyFactory<TService>
    : StrategyFactoryBase<TService, ICircuitBreakerStrategyFactory<TService>>,
      ICircuitBreakerStrategyFactory<TService>
    where TService : class
{
    private readonly IServiceProvider serviceProvider;
    private readonly List<Func<Exception, bool>> defaultHandledExceptions = new();

    private string? defaultCircuitKey;
    private int? defaultConsecutiveFailureThreshold;
    private double? defaultFailureRatio;
    private int? defaultSamplingDurationMs;
    private int? defaultMinimumThroughput;
    private int? defaultDurationOfBreakMs;

    private Action<Exception, TimeSpan>? defaultOnBreakAction;
    private Action? defaultOnResetAction;
    private Action? defaultOnHalfOpenAction;

    public CircuitBreakerStrategyFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        SetParentFactory(this);
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultCircuitKey(string circuitKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitKey);
        defaultCircuitKey = circuitKey;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultConsecutiveFailureThreshold(int consecutiveFailureThreshold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(consecutiveFailureThreshold, 1);
        defaultConsecutiveFailureThreshold = consecutiveFailureThreshold;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultFailureRatio(double failureRatio)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(failureRatio, 0.001);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(failureRatio, 1.0);
        defaultFailureRatio = failureRatio;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultSamplingDuration(int samplingDurationMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(samplingDurationMs, 1);
        defaultSamplingDurationMs = samplingDurationMs;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultMinimumThroughput(int minimumThroughput)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumThroughput, 1);
        defaultMinimumThroughput = minimumThroughput;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultDurationOfBreak(int durationOfBreakMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(durationOfBreakMs, 1);
        defaultDurationOfBreakMs = durationOfBreakMs;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultHandledException<TException>()
        where TException : Exception
    {
        defaultHandledExceptions.Add(ex => ex is TException);
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultHandledException(Func<Exception, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        defaultHandledExceptions.Add(predicate);
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultOnBreak(Action<Exception, TimeSpan> onBreakAction)
    {
        ArgumentNullException.ThrowIfNull(onBreakAction);
        defaultOnBreakAction = onBreakAction;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultOnReset(Action onResetAction)
    {
        ArgumentNullException.ThrowIfNull(onResetAction);
        defaultOnResetAction = onResetAction;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultOnHalfOpen(Action onHalfOpenAction)
    {
        ArgumentNullException.ThrowIfNull(onHalfOpenAction);
        defaultOnHalfOpenAction = onHalfOpenAction;
        return GetParentFactory();
    }

    public ICircuitBreakerStrategy<TService> BuildCircuitBreakerStrategy()
    {
        var pipelineRegistry = serviceProvider.GetRequiredService<ResiliencePipelineRegistry<string>>();
        var circuitBreakerStrategy = new CircuitBreakerStrategy<TService>(pipelineRegistry);

        AttachDefaults(circuitBreakerStrategy);

        return circuitBreakerStrategy;
    }

    private void AttachDefaults(CircuitBreakerStrategy<TService> strategyBuilder)
    {
        AttachDefaultsToBuilder(strategyBuilder);

        if (defaultCircuitKey != null)
        {
            strategyBuilder.WithCircuitKey(defaultCircuitKey);
        }

        if (defaultConsecutiveFailureThreshold.HasValue)
        {
            strategyBuilder.WithConsecutiveFailureThreshold(defaultConsecutiveFailureThreshold.Value);
        }

        if (defaultFailureRatio.HasValue)
        {
            strategyBuilder.WithFailureRatio(defaultFailureRatio.Value);
        }

        if (defaultSamplingDurationMs.HasValue)
        {
            strategyBuilder.WithSamplingDuration(defaultSamplingDurationMs.Value);
        }

        if (defaultMinimumThroughput.HasValue)
        {
            strategyBuilder.WithMinimumThroughput(defaultMinimumThroughput.Value);
        }

        if (defaultDurationOfBreakMs.HasValue)
        {
            strategyBuilder.WithDurationOfBreak(defaultDurationOfBreakMs.Value);
        }

        foreach (var handledException in defaultHandledExceptions)
        {
            strategyBuilder.WithHandledException(handledException);
        }

        if (defaultOnBreakAction != null)
        {
            strategyBuilder.OnBreak(defaultOnBreakAction);
        }

        if (defaultOnResetAction != null)
        {
            strategyBuilder.OnReset(defaultOnResetAction);
        }

        if (defaultOnHalfOpenAction != null)
        {
            strategyBuilder.OnHalfOpen(defaultOnHalfOpenAction);
        }
    }
}
