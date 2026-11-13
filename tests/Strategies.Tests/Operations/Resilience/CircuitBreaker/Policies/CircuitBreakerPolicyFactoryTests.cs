// <copyright file="CircuitBreakerPolicyFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Resilience.CircuitBreaker.Policies;

using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Policies;
using Shouldly;
using Xunit;

public class CircuitBreakerPolicyFactoryTests
{
    [Fact]
    public void CreateHttpPolicy_ShouldReturnCircuitBreakerHttpPolicy()
    {
        // Arrange
        var factory = new CircuitBreakerPolicyFactory();

        // Act
        var policy = factory.CreateHttpPolicy();

        // Assert
        policy.ShouldNotBeNull();
        policy.ShouldBeOfType<CircuitBreakerHttpPolicy>();
    }
}
