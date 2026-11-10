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
    public CircuitBreakerStrategyFactory(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;

        SetParentFactory(this);
    }

    private IServiceProvider ServiceProvider { get; }

    private List<Func<Exception, bool>> DefaultHandledExceptions { get; } = [];

    private string? DefaultCircuitKey { get; set; }

    private int? DefaultConsecutiveFailureThreshold { get; set; }

    private double? DefaultFailureRatio { get; set; }

    private int? DefaultSamplingDurationMs { get; set; }

    private int? DefaultMinimumThroughput { get; set; }

    private int? DefaultDurationOfBreakMs { get; set; }

    private Action<Exception, TimeSpan>? DefaultOnBreakAction { get; set; }

    private Action? DefaultOnResetAction { get; set; }

    private Action? DefaultOnHalfOpenAction { get; set; }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultCircuitKey(string circuitKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitKey);

        DefaultCircuitKey = circuitKey;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultConsecutiveFailureThreshold(
        int consecutiveFailureThreshold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(consecutiveFailureThreshold, 1);

        DefaultConsecutiveFailureThreshold = consecutiveFailureThreshold;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultFailureRatio(double failureRatio)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(failureRatio, 0.001);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(failureRatio, 1.0);

        DefaultFailureRatio = failureRatio;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultSamplingDuration(int samplingDurationMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(samplingDurationMs, 1);

        DefaultSamplingDurationMs = samplingDurationMs;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultMinimumThroughput(int minimumThroughput)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumThroughput, 1);

        DefaultMinimumThroughput = minimumThroughput;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultDurationOfBreak(int durationOfBreakMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(durationOfBreakMs, 1);

        DefaultDurationOfBreakMs = durationOfBreakMs;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultHandledException<TException>()
        where TException : Exception
    {
        DefaultHandledExceptions.Add(ex => ex is TException);

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultHandledException(Func<Exception, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        DefaultHandledExceptions.Add(predicate);

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultOnBreak(Action<Exception, TimeSpan> onBreakAction)
    {
        ArgumentNullException.ThrowIfNull(onBreakAction);

        DefaultOnBreakAction = onBreakAction;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultOnReset(Action onResetAction)
    {
        ArgumentNullException.ThrowIfNull(onResetAction);

        DefaultOnResetAction = onResetAction;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategyFactory<TService> WithDefaultOnHalfOpen(Action onHalfOpenAction)
    {
        ArgumentNullException.ThrowIfNull(onHalfOpenAction);

        DefaultOnHalfOpenAction = onHalfOpenAction;

        return GetParentFactory();
    }

    public ICircuitBreakerStrategy<TService> BuildCircuitBreakerStrategy()
    {
        var pipelineRegistry = ServiceProvider.GetRequiredService<ResiliencePipelineRegistry<string>>();
        var circuitBreakerStrategy = new CircuitBreakerStrategy<TService>(pipelineRegistry);

        AttachDefaults(circuitBreakerStrategy);

        return circuitBreakerStrategy;
    }

    private void AttachDefaults(CircuitBreakerStrategy<TService> strategyBuilder)
    {
        AttachDefaultsToBuilder(strategyBuilder);

        if (DefaultCircuitKey != null)
        {
            strategyBuilder.WithCircuitKey(DefaultCircuitKey);
        }

        if (DefaultConsecutiveFailureThreshold.HasValue)
        {
            strategyBuilder.WithConsecutiveFailureThreshold(DefaultConsecutiveFailureThreshold.Value);
        }

        if (DefaultFailureRatio.HasValue)
        {
            strategyBuilder.WithFailureRatio(DefaultFailureRatio.Value);
        }

        if (DefaultSamplingDurationMs.HasValue)
        {
            strategyBuilder.WithSamplingDuration(DefaultSamplingDurationMs.Value);
        }

        if (DefaultMinimumThroughput.HasValue)
        {
            strategyBuilder.WithMinimumThroughput(DefaultMinimumThroughput.Value);
        }

        if (DefaultDurationOfBreakMs.HasValue)
        {
            strategyBuilder.WithDurationOfBreak(DefaultDurationOfBreakMs.Value);
        }

        foreach (var handledException in DefaultHandledExceptions)
        {
            strategyBuilder.WithHandledException(handledException);
        }

        if (DefaultOnBreakAction != null)
        {
            strategyBuilder.OnBreak(DefaultOnBreakAction);
        }

        if (DefaultOnResetAction != null)
        {
            strategyBuilder.OnReset(DefaultOnResetAction);
        }

        if (DefaultOnHalfOpenAction != null)
        {
            strategyBuilder.OnHalfOpen(DefaultOnHalfOpenAction);
        }
    }
}
