// <copyright file="ExceptionExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Extensions;

using System;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Shouldly;
using Xunit;

public class ExceptionExtensionsTests
{
    [Fact]
    public void IsCircuitBreakerOpen_WhenExceptionIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Exception exception = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => exception.IsCircuitBreakerOpen());
    }

    [Fact]
    public void IsCircuitBreakerOpen_WhenExceptionIsCircuitBreakerOpenException_ReturnsTrue()
    {
        // Arrange
        var exception = new CircuitBreakerOpenException("Circuit is open.");

        // Act
        var result = exception.IsCircuitBreakerOpen();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsCircuitBreakerOpen_WhenExceptionIsNotCircuitBreakerOpenExceptionAndHasNoInnerException_ReturnsFalse()
    {
        // Arrange
        var exception = new InvalidOperationException("Some other error.");

        // Act
        var result = exception.IsCircuitBreakerOpen();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCircuitBreakerOpen_WhenDirectInnerExceptionIsCircuitBreakerOpenException_ReturnsTrue()
    {
        // Arrange
        var circuitBreakerException = new CircuitBreakerOpenException("Circuit is open.");
        var exception = new InvalidOperationException("Wrapper error.", circuitBreakerException);

        // Act
        var result = exception.IsCircuitBreakerOpen();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsCircuitBreakerOpen_WhenDeeplyNestedInnerExceptionIsCircuitBreakerOpenException_ReturnsTrue()
    {
        // Arrange
        var circuitBreakerException = new CircuitBreakerOpenException("Circuit is open.");
        var innerLevel2 = new Exception("Level 2", circuitBreakerException);
        var innerLevel1 = new InvalidOperationException("Level 1", innerLevel2);
        var rootException = new ApplicationException("Root error", innerLevel1);

        // Act
        var result = rootException.IsCircuitBreakerOpen();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsCircuitBreakerOpen_WhenNestedInnerExceptionsDoNotContainCircuitBreakerOpenException_ReturnsFalse()
    {
        // Arrange
#pragma warning disable S3928
        // ReSharper disable once NotResolvedInText
        var innerLevel2 = new ArgumentNullException("param");
#pragma warning restore S3928
        var innerLevel1 = new InvalidOperationException("Level 1", innerLevel2);
        var rootException = new ApplicationException("Root error", innerLevel1);

        // Act
        var result = rootException.IsCircuitBreakerOpen();

        // Assert
        result.ShouldBeFalse();
    }
}
