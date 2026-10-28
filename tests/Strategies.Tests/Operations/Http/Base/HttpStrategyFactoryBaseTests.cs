// <copyright file="HttpStrategyFactoryBaseTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Base;

using System.Text;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Factories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class HttpStrategyFactoryBaseTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();

    [Fact]
    public void FluentSetters_ShouldSetPropertiesAndReturnParent()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        Action<string, string?> verboseAction = (_, _) => { };

        // Act & Assert
        factory.WithDefaultApiDescription("API Description").ShouldBe(factory);
        factory.WithDefaultBaseUrl("https://example.com/api").ShouldBe(factory);
        factory.WithDefaultMediaType("application/json").ShouldBe(factory);
        factory.WithDefaultBasicAuth("admin", "pass").ShouldBe(factory);
        factory.WithDefaultApiKey("api-key").ShouldBe(factory);
        factory.WithDefaultApiKey(() => true, "conditional-api-key").ShouldBe(factory);
        factory.WithDefaultTimeout(3000).ShouldBe(factory);
        factory.WithDefaultVerboseOutput(verboseAction).ShouldBe(factory);

        factory.GetDefaultVerboseOutputAction().ShouldBe(verboseAction);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5000)]
    [InlineData(30001)]
    [InlineData(50000)]
    public void WithDefaultTimeout_WithInvalidTimeout_ShouldThrowArgumentOutOfRangeException(int invalidTimeout)
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => factory.WithDefaultTimeout(invalidTimeout));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    [InlineData(30000)]
    public void WithDefaultTimeout_WithValidTimeout_ShouldSetDefaultTimeoutAndReturnParent(int timeoutMs)
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        // Act
        var result = factory.WithDefaultTimeout(timeoutMs);
        factory.CallAttachHttpDefaultsToBuilder(strategy);

        // Assert
        result.ShouldBe(factory);
        strategy.GetTimeoutMs().ShouldBe(timeoutMs);
    }

    [Theory]
    [InlineData(null, "pass")]
    [InlineData("", "pass")]
    [InlineData(" ", "pass")]
    [InlineData("user", null)]
    [InlineData("user", "")]
    [InlineData("user", " ")]
    public void WithDefaultBasicAuth_WithInvalidInputs_ShouldThrowArgumentException(string? username, string? password)
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();

        // Act & Assert
        Should.Throw<ArgumentException>(() => factory.WithDefaultBasicAuth(username!, password!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithDefaultApiKey_WithInvalidInputs_ShouldThrowArgumentException(string? apiKey)
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();

        // Act & Assert
        Should.Throw<ArgumentException>(() => factory.WithDefaultApiKey(apiKey!));
    }

    [Fact]
    public void WithDefaultApiKey_WithConditionTrue_ShouldSetDefaultApiKeyAndReturnParent()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        // Act
        var result = factory.WithDefaultApiKey(() => true, "conditional-api-key");
        factory.CallAttachHttpDefaultsToBuilder(strategy);

        // Assert
        result.ShouldBe(factory);
        strategy.GetHeaders()["x-api-key"].ShouldBe("conditional-api-key");
    }

    [Fact]
    public void WithDefaultApiKey_WithConditionFalse_ShouldNotSetDefaultApiKeyAndReturnParent()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        // Act
        var result = factory.WithDefaultApiKey(() => false, "conditional-api-key");
        factory.CallAttachHttpDefaultsToBuilder(strategy);

        // Assert
        result.ShouldBe(factory);
        strategy.GetHeaders().ContainsKey("x-api-key").ShouldBeFalse();
    }

    [Fact]
    public void WithDefaultApiKey_WithDynamicCondition_ShouldPassStoredExpressionToBuilder()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var shouldIncludeApiKey = false;
        factory.WithDefaultApiKey(() => shouldIncludeApiKey, "conditional-api-key");

        var firstStrategy = new TestHttpStrategy();
        var secondStrategy = new TestHttpStrategy();

        // Act - First attach when condition is false
        factory.CallAttachHttpDefaultsToBuilder(firstStrategy);

        // Change condition to true
        shouldIncludeApiKey = true;

        // Act - Second attach when condition is true
        factory.CallAttachHttpDefaultsToBuilder(secondStrategy);

        // Assert
        firstStrategy.GetHeaders().ContainsKey("x-api-key").ShouldBeFalse();
        secondStrategy.GetHeaders()["x-api-key"].ShouldBe("conditional-api-key");
    }

    [Fact]
    public void AttachHttpDefaultsToBuilder_WithConditionalApiKey_ShouldCallWithApiKeyWithStoredExpression()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        Func<bool> condition = () => true;
        factory.WithDefaultApiKey(condition, "conditional-api-key");
        var mockStrategy = Substitute.For<IHttpStrategy<TestService, object>>();

        // Act
        factory.CallAttachHttpDefaultsToBuilder(mockStrategy);

        // Assert
        mockStrategy.Received(1).WithApiKey(condition, "conditional-api-key");
    }

    [Fact]
    public void WithDefaultApiKey_WithNullExpression_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => factory.WithDefaultApiKey(null!, "api-key"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithDefaultApiKey_WithConditionTrueAndInvalidKey_WhenDefaultsAttached_ShouldThrowArgumentException(string? apiKey)
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        // Act
        factory.WithDefaultApiKey(() => true, apiKey!);

        // Assert
        Should.Throw<ArgumentException>(() => factory.CallAttachHttpDefaultsToBuilder(strategy));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithDefaultApiKey_WithConditionFalseAndInvalidKey_WhenDefaultsAttached_ShouldNotThrowAndNotSetApiKeyHeader(string? apiKey)
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        // Act
        var result = factory.WithDefaultApiKey(() => false, apiKey!);
        factory.CallAttachHttpDefaultsToBuilder(strategy);

        // Assert
        result.ShouldBe(factory);
        strategy.GetHeaders().ContainsKey("x-api-key").ShouldBeFalse();
    }

    [Fact]
    public void AttachHttpDefaultsToBuilder_WithAllDefaults_ShouldAttachAllDefaultsToBuilder()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultApiDescription("Default Api")
            .WithDefaultBaseUrl("https://example.com")
            .WithDefaultMediaType("application/json")
            .WithDefaultBasicAuth("admin", "secret123")
            .WithDefaultApiKey("api-key-value")
            .WithDefaultTimeout(4500);

        // Act
        factory.CallAttachHttpDefaultsToBuilder(strategy);

        // Assert
        strategy.GetTargetDescription().ShouldBe("Default Api");
        strategy.GetBaseUrl().ShouldBe("https://example.com");
        strategy.GetMediaType().ShouldBe("application/json");

        var expectedAuth = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:secret123"))}";
        strategy.GetHeaders()["Authorization"].ShouldBe(expectedAuth);
        strategy.GetHeaders()["x-api-key"].ShouldBe("api-key-value");
        strategy.GetTimeoutMs().ShouldBe(4500);
    }

    [Fact]
    public void AttachHttpDefaultsToBuilder_WhenNoDefaultsConfigured_ShouldNotOverwriteBuilderProperties()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        // Act
        factory.CallAttachHttpDefaultsToBuilder(strategy);

        // Assert
        strategy.GetTargetDescription().ShouldBeNull();
        strategy.GetBaseUrl().ShouldBeNull();
        strategy.GetMediaType().ShouldBeNull();
        strategy.GetHeaders().ShouldBeEmpty();
        strategy.GetTimeoutMs().ShouldBeNull();
    }
}
