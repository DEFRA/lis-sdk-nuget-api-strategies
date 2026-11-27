// <copyright file="StrategyFactoryRegistrationContextTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests;

using Defra.Livestock.Sdk.Api.Strategies;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class StrategyFactoryRegistrationContextTests
{
    [Fact]
    public void Constructor_WhenParametersProvided_ShouldInitializeProperties()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var operatorContext = Substitute.For<IOperatorContext>();
        var logger = Substitute.For<ILogger<ITestService>>();

        // Act
        var context = new StrategyFactoryRegistrationContext<ITestService>(
            serviceProvider,
            operatorContext,
            logger);

        // Assert
        context.ServiceProvider.ShouldBeSameAs(serviceProvider);
        context.OperatorContext.ShouldBeSameAs(operatorContext);
        context.Logger.ShouldBeSameAs(logger);
    }

    [Fact]
    public void Constructor_WhenOptionalDependenciesAreNull_ShouldAllowNullProperties()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var context = new StrategyFactoryRegistrationContext<ITestService>(
            serviceProvider,
            null!,
            null!);

        // Assert
        context.ServiceProvider.ShouldBeSameAs(serviceProvider);
        context.OperatorContext.ShouldBeNull();
        context.Logger.ShouldBeNull();
    }
}
