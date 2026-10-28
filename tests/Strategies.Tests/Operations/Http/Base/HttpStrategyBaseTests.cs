// <copyright file="HttpStrategyBaseTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Base;

using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;
using Shouldly;
using Xunit;

public class HttpStrategyBaseTests
{
    [Fact]
    public void WithApiDescription_ShouldSetTargetDescriptionAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithApiDescription("Test API");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetTargetDescription().ShouldBe("Test API");
    }

    [Fact]
    public void WithBaseUrl_WithValidUrl_ShouldSetBaseUrlAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithBaseUrl("https://example.com/api");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetBaseUrl().ShouldBe("https://example.com/api");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithBaseUrl_WithInvalidUrl_ShouldThrowArgumentException(string? invalidUrl)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithBaseUrl(invalidUrl!));
    }

    [Fact]
    public void WithMediaType_WithValidMediaType_ShouldSetMediaTypeAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithMediaType("application/xml");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetMediaType().ShouldBe("application/xml");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithMediaType_WithInvalidMediaType_ShouldThrowArgumentException(string? invalidMediaType)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithMediaType(invalidMediaType!));
    }

    [Fact]
    public void WithHeader_WithValidValues_ShouldAddHeaderAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithHeader("X-Api-Key", "Secret");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetHeaders()["X-Api-Key"].ShouldBe("Secret");
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData(" ", "value")]
    [InlineData("header", null)]
    [InlineData("header", "")]
    [InlineData("header", " ")]
    public void WithHeader_WithInvalidValues_ShouldThrowArgumentException(string? name, string? value)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithHeader(name!, value!));
    }

    [Fact]
    public void WithBasicAuth_WithValidCredentials_ShouldSetAuthorizationHeaderAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithBasicAuth("admin", "secret123");

        // Assert
        result.ShouldBe(strategy);

        var expectedAuth = $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("admin:secret123"))}";
        strategy.GetHeaders()["Authorization"].ShouldBe(expectedAuth);
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData(" ", "password")]
    [InlineData("username", null)]
    [InlineData("username", "")]
    [InlineData("username", " ")]
    public void WithBasicAuth_WithInvalidCredentials_ShouldThrowArgumentException(string? username, string? password)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithBasicAuth(username!, password!));
    }

    [Fact]
    public void WithApiKey_WithValidKey_ShouldSetApiKeyHeaderAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithApiKey("my-secret-key");

        // Assert
        result.ShouldBe(strategy);
        strategy.GetHeaders()["x-api-key"].ShouldBe("my-secret-key");
    }

    [Fact]
    public void WithApiKey_WithConditionTrue_ShouldSetApiKeyHeaderAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithApiKey(() => true, "my-secret-key");

        // Assert
        result.ShouldBe(strategy);
        strategy.GetHeaders()["x-api-key"].ShouldBe("my-secret-key");
    }

    [Fact]
    public void WithApiKey_WithConditionFalse_ShouldNotSetApiKeyHeaderAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithApiKey(() => false, "my-secret-key");

        // Assert
        result.ShouldBe(strategy);
        strategy.GetHeaders().ContainsKey("x-api-key").ShouldBeFalse();
    }

    [Fact]
    public void WithApiKey_WithNullExpression_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => strategy.WithApiKey(null!, "my-secret-key"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithApiKey_WithConditionTrueAndInvalidKey_ShouldThrowArgumentException(string? apiKey)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithApiKey(() => true, apiKey!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithApiKey_WithConditionFalseAndInvalidKey_ShouldNotThrowAndNotSetApiKeyHeader(string? apiKey)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithApiKey(() => false, apiKey!);

        // Assert
        result.ShouldBe(strategy);
        strategy.GetHeaders().ContainsKey("x-api-key").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithApiKey_WithInvalidKey_ShouldThrowArgumentException(string? apiKey)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithApiKey(apiKey!));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    [InlineData(30000)]
    public void WithTimeout_WithValidTimeout_ShouldSetTimeoutMsAndReturnParent(int timeoutMs)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithTimeout(timeoutMs);

        // Assert
        result.ShouldBe(strategy);
        strategy.GetTimeoutMs().ShouldBe(timeoutMs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5000)]
    [InlineData(30001)]
    [InlineData(50000)]
    public void WithTimeout_WithInvalidTimeout_ShouldThrowArgumentOutOfRangeException(int invalidTimeout)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => strategy.WithTimeout(invalidTimeout));
    }
}
