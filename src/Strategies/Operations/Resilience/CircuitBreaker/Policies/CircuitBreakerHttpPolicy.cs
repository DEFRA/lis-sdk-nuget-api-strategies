// <copyright file="CircuitBreakerHttpPolicy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Policies;

using System;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions.Base;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker.Policies;
using Polly.Timeout;

public class CircuitBreakerHttpPolicy : ICircuitBreakerPolicy
{
    public void Execute<TService>(ICircuitBreakerStrategy<TService> circuitBreakerStrategy)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(circuitBreakerStrategy);

        circuitBreakerStrategy.WithHandledException((ex, ct) =>
            ex is HttpClientException httpClientException &&
            (httpClientException.IsNetworkError() ||
             httpClientException.IsTooManyRequests() ||
             httpClientException.Is500RangeError() ||
             httpClientException.InnerException is TimeoutException ||
             httpClientException.InnerException is OperationCanceledException { InnerException: TimeoutException } ||
             (httpClientException.InnerException is TaskCanceledException && !ct.IsCancellationRequested)));

        circuitBreakerStrategy.WithHandledException<TimeoutRejectedException>();
    }
}
