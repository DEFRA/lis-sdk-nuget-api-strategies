// <copyright file="CircuitBreakerStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;

public sealed partial class CircuitBreakerStrategy<TService>
    : StrategyBase<TService, ICircuitBreakerStrategy<TService>>,
      ICircuitBreakerStrategy<TService>
    where TService : class
{
    private readonly ResiliencePipelineRegistry<string> pipelineRegistry;
    private readonly List<Func<Exception, bool>> handledExceptions = new();
    private readonly List<Delegate> handledResultPredicates = new();

    private Delegate? configuredAction;
    private Delegate? configuredFallback;
    private string? circuitKey;

    private int? consecutiveFailureThreshold;
    private double? failureRatio;
    private int? samplingDurationMs;
    private int? minimumThroughput;
    private int? durationOfBreakMs;

    private Action<Exception, TimeSpan>? onBreakAction;
    private Action? onResetAction;
    private Action? onHalfOpenAction;

    public CircuitBreakerStrategy(ResiliencePipelineRegistry<string> pipelineRegistry)
    {
        this.pipelineRegistry = pipelineRegistry;
        SetParentBuilder(this);
        TargetDescription = "Circuit Breaker";
    }

    public ICircuitBreakerStrategy<TService> WithCircuitKey(string circuitKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitKey);
        this.circuitKey = circuitKey;
        TargetDescription = circuitKey;
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithAction(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        configuredAction = new Func<CancellationToken, Task<object?>>(async ct =>
        {
            await action(ct);
            return null;
        });
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithAction<TResult>(Func<CancellationToken, Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        configuredAction = action;
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithConsecutiveFailureThreshold(int consecutiveFailureThreshold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(consecutiveFailureThreshold, 1);
        this.consecutiveFailureThreshold = consecutiveFailureThreshold;
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithFailureRatio(double failureRatio)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(failureRatio, 0.001);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(failureRatio, 1.0);
        this.failureRatio = failureRatio;
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithSamplingDuration(int samplingDurationMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(samplingDurationMs, 1);
        this.samplingDurationMs = samplingDurationMs;
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithMinimumThroughput(int minimumThroughput)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumThroughput, 1);
        this.minimumThroughput = minimumThroughput;
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithDurationOfBreak(int durationOfBreakMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(durationOfBreakMs, 1);
        this.durationOfBreakMs = durationOfBreakMs;
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithHandledException<TException>()
        where TException : Exception
    {
        handledExceptions.Add(ex => ex is TException);
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithHandledException(Func<Exception, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        handledExceptions.Add(predicate);
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithHandledResult<TResult>(Func<TResult, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        handledResultPredicates.Add(predicate);
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithFallback(Func<CancellationToken, Task> fallbackAction)
    {
        ArgumentNullException.ThrowIfNull(fallbackAction);
        configuredFallback = new Func<CancellationToken, Task<object?>>(async ct =>
        {
            await fallbackAction(ct);
            return null;
        });
        return this;
    }

    public ICircuitBreakerStrategy<TService> WithFallback<TResult>(Func<CancellationToken, Task<TResult>> fallbackAction)
    {
        ArgumentNullException.ThrowIfNull(fallbackAction);
        configuredFallback = fallbackAction;
        return this;
    }

    public ICircuitBreakerStrategy<TService> OnBreak(Action<Exception, TimeSpan> onBreakAction)
    {
        ArgumentNullException.ThrowIfNull(onBreakAction);
        this.onBreakAction = onBreakAction;
        return this;
    }

    public ICircuitBreakerStrategy<TService> OnReset(Action onResetAction)
    {
        ArgumentNullException.ThrowIfNull(onResetAction);
        this.onResetAction = onResetAction;
        return this;
    }

    public ICircuitBreakerStrategy<TService> OnHalfOpen(Action onHalfOpenAction)
    {
        ArgumentNullException.ThrowIfNull(onHalfOpenAction);
        this.onHalfOpenAction = onHalfOpenAction;
        return this;
    }

    public async Task ExecuteAsync()
    {
        await ExecuteAsync<object?>();
    }

    public async Task<TResult> ExecuteAsync<TResult>()
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.LoggerRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (TargetDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.TargetDescriptionRequired);
        }

        if (configuredAction is null)
        {
            throw new InvalidOperationException("Action to execute has not been configured.");
        }

        if (consecutiveFailureThreshold == null && failureRatio == null)
        {
            throw new InvalidOperationException(
                "Circuit breaker must be configured with at least one threshold: call WithConsecutiveFailureThreshold(...) or WithFailureRatio(...) (or configure factory defaults).");
        }

        EnsureOperatorHasRequiredPermissions();
        LogExecutingAction();
        await InvokeBeforeExecuteAction();
        await ExecuteRequestValidation();

        var resolvedKey = circuitKey ?? $"{typeof(TService).FullName}:{ActionDescription}";
        var pipeline = GetOrCreatePipeline<TResult>(resolvedKey);
        var ct = CancellationToken ?? System.Threading.CancellationToken.None;

        try
        {
            var result = await pipeline.ExecuteAsync(
                async token =>
                {
                    if (configuredAction is Func<CancellationToken, Task<TResult>> typedAction)
                    {
                        return await typedAction(token);
                    }

                    if (configuredAction is Func<CancellationToken, Task<object?>> voidAction)
                    {
                        var voidRes = await voidAction(token);
                        return (TResult)voidRes!;
                    }

                    throw new InvalidOperationException($"Configured action return type does not match '{typeof(TResult).Name}'.");
                },
                ct);

            await InvokeAfterExecuteAction();
            LogSuccessfullyExecutedAction();
            return result;
        }
        catch (BrokenCircuitException ex)
        {
            LogCircuitBreakerOpen(resolvedKey, ex);

            if (configuredFallback != null)
            {
                LogExecutingFallbackAction();
                var fallbackResult = await ExecuteFallbackAsync<TResult>(ct);
                await InvokeAfterExecuteAction();
                return fallbackResult;
            }

            throw new CircuitBreakerOpenException($"Circuit '{resolvedKey}' is OPEN. Requests are blocked.", ex);
        }
        catch (Exception ex) when (IsHandledException(ex))
        {
            LogCircuitBreakerOperationEncounteredHandledFailure(ex);

            if (configuredFallback != null)
            {
                LogExecutingFallbackAction();
                var fallbackResult = await ExecuteFallbackAsync<TResult>(ct);
                await InvokeAfterExecuteAction();
                return fallbackResult;
            }

            throw;
        }
    }

    private async Task<TResult> ExecuteFallbackAsync<TResult>(CancellationToken ct)
    {
        if (configuredFallback is Func<CancellationToken, Task<TResult>> typedFallback)
        {
            return await typedFallback(ct);
        }

        if (configuredFallback is Func<CancellationToken, Task<object?>> voidFallback)
        {
            var voidRes = await voidFallback(ct);
            return (TResult)voidRes!;
        }

        throw new InvalidOperationException($"Configured fallback return type does not match '{typeof(TResult).Name}'.");
    }

    private ResiliencePipeline<TResult> GetOrCreatePipeline<TResult>(string key)
    {
        return pipelineRegistry.GetOrAddPipeline<TResult>(key, builder =>
        {
            var shouldHandle = new PredicateBuilder<TResult>()
                .Handle<Exception>(ex => handledExceptions.Count == 0 || handledExceptions.Exists(p => p(ex)))
                .HandleResult(res =>
                {
                    foreach (var del in handledResultPredicates)
                    {
                        if (del is Func<TResult, bool> pred && pred(res))
                        {
                            return true;
                        }
                    }

                    return false;
                });

            var breakDuration = durationOfBreakMs.HasValue
                ? TimeSpan.FromMilliseconds(durationOfBreakMs.Value)
                : TimeSpan.FromSeconds(30);

            if (consecutiveFailureThreshold.HasValue)
            {
                var minThroughput = Math.Max(2, consecutiveFailureThreshold.Value);
                var ratio = consecutiveFailureThreshold.Value < 2 ? 0.5 : 1.0;

                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
                {
                    BreakDuration = breakDuration,
                    SamplingDuration = TimeSpan.FromHours(24),
                    FailureRatio = ratio,
                    MinimumThroughput = minThroughput,
                    ShouldHandle = shouldHandle,
                    OnOpened = CreateOnOpenedHandler<TResult>($"{key}:Consecutive"),
                    OnClosed = CreateOnClosedHandler<TResult>($"{key}:Consecutive"),
                    OnHalfOpened = CreateOnHalfOpenedHandler($"{key}:Consecutive"),
                });
            }

            if (failureRatio.HasValue)
            {
                var samplingDuration = samplingDurationMs.HasValue
                    ? TimeSpan.FromMilliseconds(samplingDurationMs.Value)
                    : TimeSpan.FromSeconds(30);

                var minThroughput = minimumThroughput.HasValue
                    ? Math.Max(2, minimumThroughput.Value)
                    : 2;

                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
                {
                    BreakDuration = breakDuration,
                    SamplingDuration = samplingDuration,
                    FailureRatio = failureRatio.Value,
                    MinimumThroughput = minThroughput,
                    ShouldHandle = shouldHandle,
                    OnOpened = CreateOnOpenedHandler<TResult>($"{key}:Ratio"),
                    OnClosed = CreateOnClosedHandler<TResult>($"{key}:Ratio"),
                    OnHalfOpened = CreateOnHalfOpenedHandler($"{key}:Ratio"),
                });
            }
        });
    }

    private Func<OnCircuitOpenedArguments<TResult>, ValueTask> CreateOnOpenedHandler<TResult>(string tag) => args =>
    {
        LogCircuitOpened(tag, args.BreakDuration.TotalSeconds, args.Outcome.Exception?.Message);
        var ex = args.Outcome.Exception ?? new InvalidOperationException($"Circuit opened due to handled result outcome in '{tag}'.");
        onBreakAction?.Invoke(ex, args.BreakDuration);
        return ValueTask.CompletedTask;
    };

    private Func<OnCircuitClosedArguments<TResult>, ValueTask> CreateOnClosedHandler<TResult>(string tag) => _ =>
    {
        LogCircuitClosed(tag);
        onResetAction?.Invoke();
        return ValueTask.CompletedTask;
    };

    private Func<OnCircuitHalfOpenedArguments, ValueTask> CreateOnHalfOpenedHandler(string tag) => _ =>
    {
        LogCircuitHalfOpened(tag);
        onHalfOpenAction?.Invoke();
        return ValueTask.CompletedTask;
    };

    private bool IsHandledException(Exception ex)
    {
        if (handledExceptions.Count == 0)
        {
            return true;
        }

        return handledExceptions.Any(predicate => predicate(ex));
    }
}
