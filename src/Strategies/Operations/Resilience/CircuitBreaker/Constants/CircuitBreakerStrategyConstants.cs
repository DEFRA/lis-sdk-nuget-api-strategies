// <copyright file="CircuitBreakerStrategyConstants.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Constants;

internal static class CircuitBreakerStrategyConstants
{
    public static class Errors
    {
        public const string CircuitKeyRequired = "Circuit key must be provided for this operation";
        public const string ActionRequired = "Circuit breaker action to execute is required for this operation";
        public const string ThresholdsRequired = "Circuit breaker must be configured with at least one threshold";
    }
}
