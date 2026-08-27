// <copyright file="StrategyFactoryBaseTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Base;

using System;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class StrategyFactoryBaseTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();
    private readonly TestStrategyFactory factory = new();

    [Fact]
    public void WithDefaultLogger_SetsDefaultLoggerAndReturnsParentFactory()
    {
        // Act
        var result = factory.WithDefaultLogger(logger);

        // Assert
        result.ShouldBe(factory);
    }

    [Fact]
    public void WithDefaultOperatorContext_SetsDefaultOperatorContextAndReturnsParentFactory()
    {
        // Act
        var result = factory.WithDefaultOperatorContext(operatorContext);

        // Assert
        result.ShouldBe(factory);
    }

    [Fact]
    public void GetParentFactory_WhenParentNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedFactory = new UninitializedStrategyFactory();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(uninitializedFactory.CallGetParentFactory);

        exception.Message.ShouldBe("The parent factory has not been set.");
    }

    [Fact]
    public void AttachDefaultsToBuilder_WhenDefaultsConfigured_AttachesToStrategyBuilder()
    {
        // Arrange
        var strategyBuilder = Substitute.For<IStrategy<TestService, object>>();

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext);

        // Act
        factory.CallAttachDefaultsToBuilder(strategyBuilder);

        // Assert
        strategyBuilder.Received(1).WithLogger(logger);
        strategyBuilder.Received(1).WithOperatorContext(operatorContext);
    }

    [Fact]
    public void AttachDefaultsToBuilder_WhenDefaultsNotConfigured_DoesNotAttachToStrategyBuilder()
    {
        // Arrange
        var strategyBuilder = Substitute.For<IStrategy<TestService, object>>();

        // Act
        factory.CallAttachDefaultsToBuilder(strategyBuilder);

        // Assert
        strategyBuilder.DidNotReceive().WithLogger(Arg.Any<ILogger<TestService>>());
        strategyBuilder.DidNotReceive().WithOperatorContext(Arg.Any<IOperatorContext>());
    }

    private sealed class UninitializedStrategyFactory : StrategyFactoryBase<TestService, UninitializedStrategyFactory>
    {
        public UninitializedStrategyFactory CallGetParentFactory() => GetParentFactory();
    }

    private sealed class TestStrategyFactory : StrategyFactoryBase<TestService, TestStrategyFactory>
    {
        public TestStrategyFactory()
        {
            SetParentFactory(this);
        }

        public void CallAttachDefaultsToBuilder<TParentBuilder>(IStrategy<TestService, TParentBuilder> strategyBuilder)
            where TParentBuilder : class
        {
            AttachDefaultsToBuilder(strategyBuilder);
        }
    }
}
