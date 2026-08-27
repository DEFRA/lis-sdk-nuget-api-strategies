// <copyright file="BusinessRulesBuilderTests.cs" company="Defra">
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

public class BusinessRulesBuilderTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly BusinessRulesBuilder<TestService, TestEntity> builder = new();
    private readonly TestRequest request = new() { Id = "test-123" };
    private readonly TestEntity entity = new() { Id = "test-123", Name = "Active" };

    public BusinessRulesBuilderTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void Validate_WhenExpressionRulePasses_CompletesSuccessfully()
    {
        // Arrange
        var result = builder.Add(e => e.Id == "test-123", "Entity must have valid ID");

        // Act & Assert
        result.ShouldBe(builder);
        Should.NotThrow(() => builder.Validate(request, entity, "Update", "TestEntity", logger));
    }

    [Fact]
    public void Validate_WhenExpressionRuleFails_ThrowsBusinessRuleExceptionAndLogsWarning()
    {
        // Arrange
        builder.Add(e => e.Name == "Archived", "Entity must be archived");

        // Act & Assert
        var exception = Should.Throw<BusinessRuleException>(() =>
            builder.Validate(request, entity, "Update", "TestEntity", logger));

        exception.Message.ShouldBe("Entity must be archived");
        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Execute update [testentity] with id test-123 failed business rule 'Entity must be archived'");
    }

    [Fact]
    public void Validate_WhenPredicateRulePasses_CompletesSuccessfully()
    {
        // Arrange
        var rule = Substitute.For<IPredicateRule<TestEntity>>();
        rule.Description.Returns("Status must allow update");
        rule.Predicate.Returns(new Func<TestEntity, bool>(_ => true));

        var result = builder.Add(rule);

        // Act & Assert
        result.ShouldBe(builder);
        Should.NotThrow(() => builder.Validate(request, entity, "Update", "TestEntity", logger));
    }

    [Fact]
    public void Validate_WhenPredicateRuleFails_ThrowsBusinessRuleExceptionAndLogsWarning()
    {
        // Arrange
        var rule = Substitute.For<IPredicateRule<TestEntity>>();
        rule.Description.Returns("Status must allow update");
        rule.Predicate.Returns(new Func<TestEntity, bool>(_ => false));

        builder.Add(rule);

        // Act & Assert
        var exception = Should.Throw<BusinessRuleException>(() =>
            builder.Validate(request, entity, "Update", "TestEntity", logger));

        exception.Message.ShouldBe("Status must allow update");
        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Execute update [testentity] with id test-123 failed business rule 'Status must allow update'");
    }
}
