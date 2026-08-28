// <copyright file="RepoStrategyBaseTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Repositories.Base;

using System;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class RepoStrategyBaseTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly TestRepoStrategy strategy = new();

    public RepoStrategyBaseTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void WithEntityDescription_SetsTargetDescriptionAndReturnsParentBuilder()
    {
        // Act
        var result = strategy.WithEntityDescription("TestDescription");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetTargetDescription().ShouldBe("TestDescription");
    }

    [Fact]
    public void LogEntityWithIdNotFound_WhenLoggerIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Find");

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => strategy.InvokeLogEntityWithIdNotFound("123"));

        exception.Message.ShouldBe(StrategyConstants.Errors.LoggerRequired);
    }

    [Fact]
    public void LogEntityWithIdNotFound_WhenActionDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithEntityDescription("TestEntity");

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => strategy.InvokeLogEntityWithIdNotFound("123"));

        exception.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
    }

    [Fact]
    public void LogEntityWithIdNotFound_WhenPrerequisitesMet_LogsWarningMessage()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Find");

        // Act
        strategy.InvokeLogEntityWithIdNotFound("123");

        // Assert
        logger.ShouldHaveReceived(LogLevel.Warning, "TestEntity with id 123 not found");
    }

    [Fact]
    public void LogEntityWithIdNotFound_WhenTargetDescriptionIsNull_DoesNotLog()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithActionDescription("Find");

        // Act
        strategy.InvokeLogEntityWithIdNotFound("123");

        // Assert
        logger.ShouldNotHaveReceivedAny();
    }
}
