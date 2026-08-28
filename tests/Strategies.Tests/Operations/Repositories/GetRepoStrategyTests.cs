// <copyright file="GetRepoStrategyTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Repositories;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Utilities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using TestResult = Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.TestResult;

public class GetRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IRepoGettable<TestEntity> repository = Substitute.For<IRepoGettable<TestEntity>>();
    private readonly MockRepoContext<TestEntity> repoContext;
    private readonly GetRepoStrategy<TestService, TestEntity> strategy = new();
    private readonly TestRequest request = new() { Id = "test-123" };
    private readonly Expression<Func<TestEntity, bool>> filter = e => e.Id == "test-123";

    public GetRepoStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        repoContext = MockRepoContext<TestEntity>.CreateFor(repository);
    }

    [Fact]
    public async Task Execute_WhenLoggerIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(StrategyConstants.Errors.LoggerRequired);
    }

    [Fact]
    public async Task Execute_WhenCancellationTokenIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task Execute_WhenRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithEntityDescription("TestEntity")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.GettableRepositoryRequired);
    }

    [Fact]
    public async Task Execute_WhenTargetDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WhenRequestIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
    }

    [Fact]
    public async Task Execute_WhenEntityFilterIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequest(request);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
    }

    [Fact]
    public async Task
        Execute_WhenRequiresAuthenticatedOperatorAndOperatorContextIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(StrategyConstants.Errors.OperatorContextRequired);
    }

    [Fact]
    public async Task
        Execute_WhenRequiresAuthenticatedOperatorAndOperatorIsNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();

        operatorContext.HasAuthenticatedOperator.Returns(false);

        strategy
            .WithLogger(logger)
            .WithOperatorContext(operatorContext)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(strategy.Execute);

        exception.Message.ShouldBe(StrategyConstants.Errors.OperatorContextAuthenticatedOperatorRequired);
    }

    [Fact]
    public async Task Execute_WhenValidationFails_ThrowsRequestValidationException()
    {
        // Arrange
        var validationFailures = new List<RequestValidationFailure> { new("Id", "Id is required") };
        var validationResult = new RequestValidationResult(validationFailures);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequestValidation(() => Task.FromResult(validationResult))
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(strategy.Execute);

        exception.Errors.ShouldBe(validationFailures);

        repoContext.Calls.GetCallCount.ShouldBe(0);

        logger.ShouldHaveReceived(LogLevel.Warning, "Execute get [testentity] failed validation");
    }

    [Fact]
    public async Task Execute_WhenEntityNotFound_LogsWarningAndThrowsEntityNotFoundException()
    {
        // Arrange
        var nonMatching1 = new TestEntity { Id = "other-456", Name = "Other1" };
        var nonMatching2 = new TestEntity { Id = "other-789", Name = "Other2" };

        repoContext.WithData([nonMatching1, nonMatching2]);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<EntityNotFoundException>(strategy.Execute);

        exception.Message.ShouldBe("TestEntity not found");

        repoContext.Calls.GetCallCount.ShouldBe(1);
        repoContext.Calls.LastGetResult.ShouldBeNull();

        logger.ShouldHaveReceived(LogLevel.Warning, "TestEntity with id test-123 not found");
    }

    [Fact]
    public async Task Execute_WhenExistenceRuleFails_ThrowsExistenceRuleException()
    {
        // Arrange
        var nonMatching1 = new TestEntity { Id = "other-111", Name = "Other" };
        var existingEntity = new TestEntity { Id = "test-123", Name = "Inactive" };
        var nonMatching2 = new TestEntity { Id = "other-222", Name = "Other" };

        repoContext.WithData([nonMatching1, existingEntity, nonMatching2]);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithExistenceRules(rules => rules.Add(e => e.Name == "Active", "Must be active"));

        // Act & Assert
        var exception = await Should.ThrowAsync<ExistenceRuleException>(strategy.Execute);

        exception.Message.ShouldBe("TestEntity not found");

        repoContext.Calls.GetCallCount.ShouldBe(1);
        repoContext.Calls.LastGetResult.ShouldBe(existingEntity);

        logger.ShouldHaveReceived(LogLevel.Warning, "TestEntity with id test-123 not found");
    }

    [Fact]
    public async Task Execute_FullSuccessfulFlow_ExecutesLifecycleInOrderAndLogsInformation()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        var operatorUser = new Operator("user123", true);

        var nonMatching1 = new TestEntity { Id = "other-111", Name = "Other" };
        var existingEntity = new TestEntity { Id = "test-123", Name = "Active" };
        var nonMatching2 = new TestEntity { Id = "other-222", Name = "Other" };

        var executionOrder = new List<string>();

        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(true);
        operatorContext.Operator.Returns(operatorUser);

        repoContext.WithData([nonMatching1, existingEntity, nonMatching2]);

        strategy
            .WithLogger(logger)
            .WithOperatorContext(operatorContext)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithExistenceRules(rules => rules.Add(e => e.Name == "Active", "Must be active"))
            .WithBeforeExecute(() =>
            {
                executionOrder.Add("BeforeExecute");
                return Task.CompletedTask;
            })
            .WithAfterExecute(() =>
            {
                executionOrder.Add("AfterExecute");
                return Task.CompletedTask;
            });

        // Act
        var result = await strategy.Execute();

        // Assert
        result.ShouldBe(existingEntity);

        executionOrder.ShouldBe([
            "BeforeExecute",
            "AfterExecute",
        ]);

        repoContext.Calls.GetCallCount.ShouldBe(1);
        repoContext.Calls.LastGetResult.ShouldBe(existingEntity);

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Executing get [testentity] with id test-123 by operator user123");

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Successfully executed get [testentity] with id test-123 by operator user123");
    }

    [Fact]
    public async Task ExecuteAndMap_WhenCalled_MapsEntityAndLogs()
    {
        // Arrange
        var nonMatching1 = new TestEntity { Id = "other-111", Name = "Other" };
        var existingEntity = new TestEntity { Id = "test-123", Name = "MappedName" };
        var nonMatching2 = new TestEntity { Id = "other-222", Name = "Other" };

        repoContext.WithData([nonMatching1, existingEntity, nonMatching2]);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act
        var result = await strategy.ExecuteAndMap(e => new TestResult { MappedName = $"Mapped_{e.Name}" });

        // Assert
        result.ShouldNotBeNull();

        result.MappedName.ShouldBe("Mapped_MappedName");

        repoContext.Calls.GetCallCount.ShouldBe(1);
        repoContext.Calls.LastGetResult.ShouldBe(existingEntity);

        logger.ShouldHaveReceived(LogLevel.Information, "Executing get [testentity] with id test-123 by operator ");

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Successfully executed get [testentity] with id test-123 by operator ");
    }
}
