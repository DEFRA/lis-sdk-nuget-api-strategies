// <copyright file="StrategyFactoryBaseTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Base;

using System;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Factories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
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
        var uninitializedFactory = new TestStrategyFactoryUninitialized();

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

    [Fact]
    public void When_WithBooleanTrue_ShouldExecuteConfigureActionAndReturnParent()
    {
        // Arrange
        var configured = false;
        var elseConfigured = false;

        // Act
        var result = factory.When(
            true,
            f =>
            {
                configured = true;
                f.WithDefaultLogger(logger);
            },
            _ => elseConfigured = true);

        // Assert
        result.ShouldBe(factory);
        configured.ShouldBeTrue();
        elseConfigured.ShouldBeFalse();
    }

    [Fact]
    public void When_WithBooleanFalseAndConfigureElse_ShouldExecuteConfigureElseActionAndReturnParent()
    {
        // Arrange
        var configured = false;
        var elseConfigured = false;

        // Act
        var result = factory.When(
            false,
            _ => configured = true,
            f =>
            {
                elseConfigured = true;
                f.WithDefaultLogger(logger);
            });

        // Assert
        result.ShouldBe(factory);
        configured.ShouldBeFalse();
        elseConfigured.ShouldBeTrue();
    }

    [Fact]
    public void When_WithBooleanFalseAndNoConfigureElse_ShouldNotExecuteConfigureActionAndReturnParent()
    {
        // Arrange
        var configured = false;

        // Act
        var result = factory.When(
            false,
            _ => configured = true);

        // Assert
        result.ShouldBe(factory);
        configured.ShouldBeFalse();
    }

    [Fact]
    public void When_WithBooleanAndNullConfigure_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => factory.When(true, null!));
    }

    [Fact]
    public void When_WithPredicateTrue_ShouldExecuteConfigureActionAndReturnParent()
    {
        // Arrange
        var configured = false;
        var elseConfigured = false;

        // Act
        var result = factory.When(
            () => true,
            f =>
            {
                configured = true;
                f.WithDefaultLogger(logger);
            },
            _ => elseConfigured = true);

        // Assert
        result.ShouldBe(factory);
        configured.ShouldBeTrue();
        elseConfigured.ShouldBeFalse();
    }

    [Fact]
    public void When_WithPredicateFalseAndConfigureElse_ShouldExecuteConfigureElseActionAndReturnParent()
    {
        // Arrange
        var configured = false;
        var elseConfigured = false;

        // Act
        var result = factory.When(
            () => false,
            _ => configured = true,
            f =>
            {
                elseConfigured = true;
                f.WithDefaultLogger(logger);
            });

        // Assert
        result.ShouldBe(factory);
        configured.ShouldBeFalse();
        elseConfigured.ShouldBeTrue();
    }

    [Fact]
    public void When_WithPredicateFalseAndNoConfigureElse_ShouldNotExecuteConfigureActionAndReturnParent()
    {
        // Arrange
        var configured = false;

        // Act
        var result = factory.When(
            () => false,
            _ => configured = true);

        // Assert
        result.ShouldBe(factory);
        configured.ShouldBeFalse();
    }

    [Fact]
    public void When_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => factory.When(null!, _ => { }));
    }

    [Fact]
    public void When_WithPredicateAndNullConfigure_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => factory.When(() => true, null!));
    }
}
