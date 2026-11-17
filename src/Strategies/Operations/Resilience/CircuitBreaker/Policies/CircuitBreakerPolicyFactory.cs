// <copyright file="CircuitBreakerPolicyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Policies;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker.Policies;

public class CircuitBreakerPolicyFactory : ICircuitBreakerPolicyFactory
{
    public ICircuitBreakerPolicy CreateHttpPolicy()
    {
        return new CircuitBreakerHttpPolicy();
    }
}
