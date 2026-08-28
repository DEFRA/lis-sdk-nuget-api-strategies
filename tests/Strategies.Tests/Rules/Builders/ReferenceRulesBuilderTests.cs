// <copyright file="ReferenceRulesBuilderTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Rules.Builders;

using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class ReferenceRulesBuilderTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;
    private readonly ReferenceRulesBuilder<TestService> builder = new();

    public ReferenceRulesBuilderTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public async Task Validate_WhenRulePasses_CompletesSuccessfully()
    {
        // Arrange
        var rule = Substitute.For<IReferenceRule>();

        rule.Description.Returns("Entity must exist");
        rule.Validator.Returns(_ => Task.FromResult(true));

        builder.Add(rule);

        // Act & Assert
        await Should.NotThrowAsync(() => builder.Validate("Create", "Item", logger, cancellationToken));
    }

    [Fact]
    public async Task Validate_WhenRuleFails_ThrowsReferenceRuleException()
    {
        // Arrange
        var rule = Substitute.For<IReferenceRule>();

        rule.Description.Returns("Entity must exist");
        rule.Validator.Returns(_ => Task.FromResult(false));

        builder.Add(rule);

        // Act & Assert
        var exception = await Should.ThrowAsync<ReferenceRuleException>(() =>
            builder.Validate("Create", "Item", logger, cancellationToken));

        exception.Message.ShouldBe("Entity must exist");

        logger.ShouldHaveReceived(LogLevel.Warning, "Execute create [item] failed reference rule 'Entity must exist'");
    }

    [Fact]
    public async Task Add_WithRepositoryAndPredicate_AddsRuleAndValidatesAgainstRepository()
    {
        // Arrange
        var gettableRepo = Substitute.For<IRepoGettable<TestEntity>>();
        var entity = new TestEntity { Id = "abc" };

        gettableRepo.GetSingle(Arg.Any<Expression<Func<TestEntity, bool>>>(), cancellationToken)
            .Returns(Task.FromResult<TestEntity?>(entity));

        // Act
        var result = builder.Add(gettableRepo, e => e.Id == "abc", "Entity must exist");

        // Assert
        result.ShouldBe(builder);

        await Should.NotThrowAsync(() => builder.Validate("Create", "Item", logger, cancellationToken));
    }

    [Fact]
    public async Task Validate_WhenRuleFailsAndLoggingDisabled_ThrowsReferenceRuleException()
    {
        // Arrange
        var disabledLogger = Substitute.For<ILogger<TestService>>();

        disabledLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(false);

        var rule = Substitute.For<IReferenceRule>();

        rule.Description.Returns("Entity must exist");
        rule.Validator.Returns(_ => Task.FromResult(false));

        builder.Add(rule);

        // Act & Assert
        var exception = await Should.ThrowAsync<ReferenceRuleException>(() =>
            builder.Validate("Create", "Item", disabledLogger, cancellationToken));

        exception.Message.ShouldBe("Entity must exist");

        disabledLogger.ShouldNotHaveReceivedAny();
    }
}
