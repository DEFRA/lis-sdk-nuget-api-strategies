// <copyright file="CircuitBreakerStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Constants;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;

public sealed partial class CircuitBreakerStrategy<TService>
    : StrategyBase<TService, ICircuitBreakerStrategy<TService>>,
        ICircuitBreakerStrategy<TService>
    where TService : class
{
    public CircuitBreakerStrategy(ResiliencePipelineRegistry<string> pipelineRegistry)
    {
        PipelineRegistry = pipelineRegistry;
        SetParentBuilder(this);
        TargetDescription = "Circuit Breaker";
    }

    private ResiliencePipelineRegistry<string> PipelineRegistry { get; set; }

    private List<Func<Exception, bool>> HandledExceptions { get; } = [];

    private List<Delegate> HandledResultPredicates { get; } = [];

    private Delegate? ConfiguredAction { get; set; }

    private Delegate? ConfiguredFallback { get; set; }

    private string? CircuitKey { get; set; }

    private int? ConsecutiveFailureThreshold { get; set; }

    private double? FailureRatio { get; set; }

    private int? SamplingDurationMs { get; set; }

    private int? MinimumThroughput { get; set; }

    private int? DurationOfBreakMs { get; set; }

    private Action<Exception, TimeSpan>? OnBreakAction { get; set; }

    private Action? OnResetAction { get; set; }

    private Action? OnHalfOpenAction { get; set; }

    public ICircuitBreakerStrategy<TService> WithCircuitKey(string circuitKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitKey);

        CircuitKey = circuitKey;
        TargetDescription = circuitKey;

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithAction(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        ConfiguredAction = new Func<CancellationToken, Task<object?>>(async ct =>
        {
            await action(ct);
            return null;
        });

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithAction<TResult>(Func<CancellationToken, Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        ConfiguredAction = action;

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithConsecutiveFailureThreshold(int consecutiveFailureThreshold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(consecutiveFailureThreshold, 1);

        ConsecutiveFailureThreshold = consecutiveFailureThreshold;

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithFailureRatio(double failureRatio)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(failureRatio, 0.001);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(failureRatio, 1.0);

        FailureRatio = failureRatio;

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithSamplingDuration(int samplingDurationMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(samplingDurationMs, 1);

        SamplingDurationMs = samplingDurationMs;

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithMinimumThroughput(int minimumThroughput)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumThroughput, 1);

        MinimumThroughput = minimumThroughput;

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithDurationOfBreak(int durationOfBreakMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(durationOfBreakMs, 1);

        DurationOfBreakMs = durationOfBreakMs;

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithHandledException<TException>()
        where TException : Exception
    {
        HandledExceptions.Add(ex => ex is TException);

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithHandledException(Func<Exception, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        HandledExceptions.Add(predicate);

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithHandledResult<TResult>(Func<TResult, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        HandledResultPredicates.Add(predicate);

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithFallback(Func<CancellationToken, Task> fallbackAction)
    {
        ArgumentNullException.ThrowIfNull(fallbackAction);

        ConfiguredFallback = new Func<CancellationToken, Task<object?>>(async ct =>
        {
            await fallbackAction(ct);
            return null;
        });

        return this;
    }

    public ICircuitBreakerStrategy<TService> WithFallback<TResult>(
        Func<CancellationToken, Task<TResult>> fallbackAction)
    {
        ArgumentNullException.ThrowIfNull(fallbackAction);

        ConfiguredFallback = fallbackAction;

        return this;
    }

    public ICircuitBreakerStrategy<TService> OnBreak(Action<Exception, TimeSpan> onBreakAction)
    {
        ArgumentNullException.ThrowIfNull(onBreakAction);

        OnBreakAction = onBreakAction;

        return this;
    }

    public ICircuitBreakerStrategy<TService> OnReset(Action onResetAction)
    {
        ArgumentNullException.ThrowIfNull(onResetAction);

        OnResetAction = onResetAction;

        return this;
    }

    public ICircuitBreakerStrategy<TService> OnHalfOpen(Action onHalfOpenAction)
    {
        ArgumentNullException.ThrowIfNull(onHalfOpenAction);

        OnHalfOpenAction = onHalfOpenAction;

        return this;
    }

    public async Task ExecuteAsync()
    {
        await ExecuteAsync<object?>();
    }

    public async Task<TResult> ExecuteAsync<TResult>()
    {
        ValidateConfiguration();

        EnsureOperatorHasRequiredPermissions();

        LogExecutingAction();

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        var pipeline = GetOrCreatePipeline<TResult>(CircuitKey!);

        var ct = CancellationToken!.Value;

        try
        {
            var result = await pipeline.ExecuteAsync(
                async token => await ExecuteConfiguredActionAsync<TResult>(token),
                ct);

            await InvokeAfterExecuteAction();

            LogSuccessfullyExecutedAction();

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            LogCircuitBreakerOpen(CircuitKey!, ex);

            if (ConfiguredFallback != null)
            {
                return await ExecuteFallbackWithLoggingAsync<TResult>(ct);
            }

            throw new CircuitBreakerOpenException($"Circuit '{CircuitKey!}' is OPEN. Requests are blocked.", ex);
        }
        catch (Exception ex) when (IsHandledException(ex))
        {
            LogCircuitBreakerOperationEncounteredHandledFailure(ex);

            if (ConfiguredFallback != null)
            {
                return await ExecuteFallbackWithLoggingAsync<TResult>(ct);
            }

            throw;
        }
    }

    private void ValidateConfiguration()
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.LoggerRequired);
        }

        if (CancellationToken == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.CancellationTokenRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (CircuitKey == null)
        {
            throw new InvalidOperationException(CircuitBreakerStrategyConstants.Errors.CircuitKeyRequired);
        }

        if (TargetDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.TargetDescriptionRequired);
        }

        if (ConfiguredAction is null)
        {
            throw new InvalidOperationException("Action to execute has not been configured.");
        }

        if (ConsecutiveFailureThreshold == null && FailureRatio == null)
        {
            throw new InvalidOperationException(
                "Circuit breaker must be configured with at least one threshold: call WithConsecutiveFailureThreshold(...) or WithFailureRatio(...) (or configure factory defaults).");
        }
    }

    private async Task<TResult> ExecuteConfiguredActionAsync<TResult>(CancellationToken token)
    {
        switch (ConfiguredAction)
        {
            case Func<CancellationToken, Task<TResult>> typedAction:
                return await typedAction(token);
            case Func<CancellationToken, Task<object?>> voidAction:
            {
                var voidRes = await voidAction(token);
                return (TResult)voidRes!;
            }

            default:
                throw new InvalidOperationException(
                    $"Configured action return type does not match '{typeof(TResult).Name}'.");
        }
    }

    private async Task<TResult> ExecuteFallbackWithLoggingAsync<TResult>(CancellationToken ct)
    {
        LogExecutingFallbackAction();

        var fallbackResult = await ExecuteFallbackAsync<TResult>(ct);

        await InvokeAfterExecuteAction();

        return fallbackResult;
    }

    private async Task<TResult> ExecuteFallbackAsync<TResult>(CancellationToken ct)
    {
        switch (ConfiguredFallback)
        {
            case Func<CancellationToken, Task<TResult>> typedFallback:
                return await typedFallback(ct);
            case Func<CancellationToken, Task<object?>> voidFallback:
            {
                var voidRes = await voidFallback(ct);
                return (TResult)voidRes!;
            }

            default:
                throw new InvalidOperationException(
                    $"Configured fallback return type does not match '{typeof(TResult).Name}'.");
        }
    }

    private ResiliencePipeline<TResult> GetOrCreatePipeline<TResult>(string key)
    {
        return PipelineRegistry.GetOrAddPipeline<TResult>(key, builder =>
        {
            var shouldHandle = BuildPredicate<TResult>();
            var breakDuration = DurationOfBreakMs.HasValue
                ? TimeSpan.FromMilliseconds(DurationOfBreakMs.Value)
                : TimeSpan.FromSeconds(30);

            AddConsecutiveBreaker(builder, key, breakDuration, shouldHandle);
            AddRatioBreaker(builder, key, breakDuration, shouldHandle);
        });
    }

    private PredicateBuilder<TResult> BuildPredicate<TResult>()
    {
        return new PredicateBuilder<TResult>()
            .Handle<Exception>(ex => HandledExceptions.Count == 0 || HandledExceptions.Exists(p => p(ex)))
            .HandleResult(IsHandledResult);
    }

    private bool IsHandledResult<TResult>(TResult result)
    {
        return HandledResultPredicates.OfType<Func<TResult, bool>>().Any(predicate => predicate(result));
    }

    private void AddConsecutiveBreaker<TResult>(
        ResiliencePipelineBuilder<TResult> builder,
        string key,
        TimeSpan breakDuration,
        PredicateBuilder<TResult> shouldHandle)
    {
        if (!ConsecutiveFailureThreshold.HasValue)
        {
            return;
        }

        var threshold = ConsecutiveFailureThreshold.Value;
        var minThroughput = Math.Max(2, threshold);
        var ratio = threshold < 2 ? 0.5 : 1.0;

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

    private void AddRatioBreaker<TResult>(
        ResiliencePipelineBuilder<TResult> builder,
        string key,
        TimeSpan breakDuration,
        PredicateBuilder<TResult> shouldHandle)
    {
        if (!FailureRatio.HasValue)
        {
            return;
        }

        var samplingDuration = SamplingDurationMs.HasValue
            ? TimeSpan.FromMilliseconds(SamplingDurationMs.Value)
            : TimeSpan.FromSeconds(30);

        var minThroughput = MinimumThroughput.HasValue
            ? Math.Max(2, MinimumThroughput.Value)
            : 2;

        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
        {
            BreakDuration = breakDuration,
            SamplingDuration = samplingDuration,
            FailureRatio = FailureRatio.Value,
            MinimumThroughput = minThroughput,
            ShouldHandle = shouldHandle,
            OnOpened = CreateOnOpenedHandler<TResult>($"{key}:Ratio"),
            OnClosed = CreateOnClosedHandler<TResult>($"{key}:Ratio"),
            OnHalfOpened = CreateOnHalfOpenedHandler($"{key}:Ratio"),
        });
    }

    private Func<OnCircuitOpenedArguments<TResult>, ValueTask> CreateOnOpenedHandler<TResult>(string tag) => args =>
    {
        LogCircuitOpened(tag, args.BreakDuration.TotalSeconds, args.Outcome.Exception?.Message);

        var ex = args.Outcome.Exception ??
                 new InvalidOperationException($"Circuit opened due to handled result outcome in '{tag}'.");

        OnBreakAction?.Invoke(ex, args.BreakDuration);

        return ValueTask.CompletedTask;
    };

    private Func<OnCircuitClosedArguments<TResult>, ValueTask> CreateOnClosedHandler<TResult>(string tag) => _ =>
    {
        LogCircuitClosed(tag);

        OnResetAction?.Invoke();

        return ValueTask.CompletedTask;
    };

    private Func<OnCircuitHalfOpenedArguments, ValueTask> CreateOnHalfOpenedHandler(string tag) => _ =>
    {
        LogCircuitHalfOpened(tag);

        OnHalfOpenAction?.Invoke();

        return ValueTask.CompletedTask;
    };

    private bool IsHandledException(Exception ex)
    {
        return HandledExceptions.Count == 0 || HandledExceptions.Any(predicate => predicate(ex));
    }
}
