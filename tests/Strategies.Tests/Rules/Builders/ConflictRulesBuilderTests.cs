// <copyright file="ConflictRulesBuilderTests.cs" company="Defra">
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

public class ConflictRulesBuilderTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly ConflictRulesBuilder<TestService, TestEntity> builder = new();
    private readonly TestRequest request = new() { Id = "test-123" };
    private readonly TestEntity entity = new() { Id = "test-123", Name = "Active" };

    public ConflictRulesBuilderTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void Validate_WhenExpressionRulePasses_CompletesSuccessfully()
    {
        // Arrange
        var result = builder.Add(e => e.Id == "test-123", "Entity must not be modified elsewhere");

        // Act & Assert
        result.ShouldBe(builder);
        Should.NotThrow(() => builder.Validate(request, entity, "Update", "TestEntity", logger));
    }

    [Fact]
    public void Validate_WhenExpressionRuleFails_ThrowsConflictRuleExceptionAndLogsWarning()
    {
        // Arrange
        builder.Add(e => e.Name == "Locked", "Entity is locked");

        // Act & Assert
        var exception = Should.Throw<ConflictRuleException>(() =>
            builder.Validate(request, entity, "Update", "TestEntity", logger));

        exception.Message.ShouldBe("Entity is locked");
        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Execute update [testentity] with id test-123 failed conflict rule 'Entity is locked'");
    }

    [Fact]
    public void Validate_WhenPredicateRulePasses_CompletesSuccessfully()
    {
        // Arrange
        var rule = Substitute.For<IPredicateRule<TestEntity>>();
        rule.Description.Returns("Version must match");
        rule.Predicate.Returns(new Func<TestEntity, bool>(_ => true));

        var result = builder.Add(rule);

        // Act & Assert
        result.ShouldBe(builder);
        Should.NotThrow(() => builder.Validate(request, entity, "Update", "TestEntity", logger));
    }

    [Fact]
    public void Validate_WhenPredicateRuleFails_ThrowsConflictRuleExceptionAndLogsWarning()
    {
        // Arrange
        var rule = Substitute.For<IPredicateRule<TestEntity>>();
        rule.Description.Returns("Version must match");
        rule.Predicate.Returns(new Func<TestEntity, bool>(_ => false));

        builder.Add(rule);

        // Act & Assert
        var exception = Should.Throw<ConflictRuleException>(() =>
            builder.Validate(request, entity, "Update", "TestEntity", logger));

        exception.Message.ShouldBe("Version must match");
        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Execute update [testentity] with id test-123 failed conflict rule 'Version must match'");
    }
}
