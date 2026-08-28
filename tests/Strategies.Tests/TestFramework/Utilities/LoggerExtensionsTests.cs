// <copyright file="LoggerExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Utilities;

using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

public class LoggerExtensionsTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();

    public LoggerExtensionsTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void ShouldHaveReceived_WhenNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger? nullLogger = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => nullLogger!.ShouldHaveReceived(LogLevel.Information, "Test"));
        Should.Throw<ArgumentNullException>(() => nullLogger!.ShouldHaveReceived("Test"));
    }

    [Fact]
    public void ShouldNotHaveReceived_WhenNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger? nullLogger = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => nullLogger!.ShouldNotHaveReceived(LogLevel.Information, "Test"));
        Should.Throw<ArgumentNullException>(() => nullLogger!.ShouldNotHaveReceived("Test"));
    }

    [Fact]
    public void ShouldHaveReceived_WhenMessageWasLogged_Passes()
    {
        // Arrange
        logger.LogInformation("Information message");

        // Act & Assert
        Should.NotThrow(() => logger.ShouldHaveReceived(LogLevel.Information, "Information message"));
        Should.NotThrow(() => logger.ShouldHaveReceived("Information message"));
    }

    [Fact]
    public void ShouldNotHaveReceived_WhenMessageWasNotLogged_Passes()
    {
        // Act & Assert
        Should.NotThrow(() => logger.ShouldNotHaveReceived(LogLevel.Warning, "Unlogged warning"));
        Should.NotThrow(() => logger.ShouldNotHaveReceived("Unlogged message"));
    }

    [Fact]
    public void ShouldNotHaveReceivedAny_WhenNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger? nullLogger = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => nullLogger!.ShouldNotHaveReceivedAny());
    }

    [Fact]
    public void ShouldNotHaveReceivedAny_WhenNoMessageWasLogged_Passes()
    {
        // Act & Assert
        Should.NotThrow(() => logger.ShouldNotHaveReceivedAny());
    }
}
