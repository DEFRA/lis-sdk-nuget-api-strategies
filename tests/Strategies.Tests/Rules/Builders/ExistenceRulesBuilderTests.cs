// <copyright file="ExistenceRulesBuilderTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Rules.Builders;

using System;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class ExistenceRulesBuilderTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly ExistenceRulesBuilder<TestService, TestEntity> builder = new();
    private readonly TestRequest request = new() { Id = "test-123" };
    private readonly TestEntity entity = new() { Id = "test-123", Name = "Active" };

    public ExistenceRulesBuilderTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void Validate_WhenExpressionRulePasses_CompletesSuccessfully()
    {
        // Arrange
        var result = builder.Add(e => e.Id == "test-123", "Entity must have matching ID");

        // Act & Assert
        result.ShouldBe(builder);
        Should.NotThrow(() => builder.Validate(request, entity, "TestEntity", logger));
    }

    [Fact]
    public void Validate_WhenExpressionRuleFails_ThrowsExistenceRuleExceptionAndLogsWarning()
    {
        // Arrange
        builder.Add(e => e.Name == "Inactive", "Entity must be inactive");

        // Act & Assert
        var exception = Should.Throw<ExistenceRuleException>(() =>
            builder.Validate(request, entity, "TestEntity", logger));

        exception.Message.ShouldBe("TestEntity not found");
        logger.ShouldHaveReceived(LogLevel.Warning, "TestEntity with id test-123 not found");
    }

    [Fact]
    public void Validate_WhenPredicateRulePasses_CompletesSuccessfully()
    {
        // Arrange
        var rule = Substitute.For<IPredicateRule<TestEntity>>();
        rule.Description.Returns("Entity must be active");
        rule.Predicate.Returns(new Func<TestEntity, bool>(_ => true));

        var result = builder.Add(rule);

        // Act & Assert
        result.ShouldBe(builder);
        Should.NotThrow(() => builder.Validate(request, entity, "TestEntity", logger));
    }

    [Fact]
    public void Validate_WhenPredicateRuleFails_ThrowsExistenceRuleExceptionAndLogsWarning()
    {
        // Arrange
        var rule = Substitute.For<IPredicateRule<TestEntity>>();
        rule.Description.Returns("Entity must be active");
        rule.Predicate.Returns(new Func<TestEntity, bool>(_ => false));

        builder.Add(rule);

        // Act & Assert
        var exception = Should.Throw<ExistenceRuleException>(() =>
            builder.Validate(request, entity, "TestEntity", logger));

        exception.Message.ShouldBe("TestEntity not found");
        logger.ShouldHaveReceived(LogLevel.Warning, "TestEntity with id test-123 not found");
    }
}
