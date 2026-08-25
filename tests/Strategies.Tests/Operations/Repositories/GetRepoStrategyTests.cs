// <copyright file="GetRepoStrategyTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Repositories;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestUtilities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using TestResult = Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData.TestResult;

public class GetRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly ITestRepository repository = Substitute.For<ITestRepository>();
    private readonly MockRepoContext<TestEntity> repoContext;
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;
    private readonly GetRepoStrategy<TestService, TestEntity> strategy = new();
    private readonly TestRequest request = new() { Id = "test-123" };
    private readonly Expression<Func<TestEntity, bool>> filter = e => e.Id == "test-123";

    public GetRepoStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        repoContext = MockRepoContext<TestEntity>.CreateFor(repository);
    }

    public interface ITestRepository : IRepoGettable<TestEntity>, IRepoUpdatable<TestEntity>
    {
    }

    [Fact]
    public async Task Execute_WhenLoggerIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
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
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task Execute_WhenRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithEntityDescription("TestEntity")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.GettableRepositoryRequired);
    }

    [Fact]
    public async Task Execute_WhenTargetDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WhenRequestIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
    }

    [Fact]
    public async Task Execute_WhenEntityFilterIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequest(request);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
    }

    [Fact]
    public async Task Execute_WhenRequiresAuthenticatedOperatorAndOperatorContextIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(StrategyConstants.Errors.OperatorContextRequired);
    }

    [Fact]
    public async Task Execute_WhenRequiresAuthenticatedOperatorAndOperatorIsNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        operatorContext.HasAuthenticatedOperator.Returns(false);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() => strategy.Execute());
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
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithRequestValidation(() => Task.FromResult(validationResult));

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(() => strategy.Execute());
        exception.Errors.ShouldBe(validationFailures);
        repoContext.Calls.GetCallCount.ShouldBe(0);
        await repository.DidNotReceive().GetSingle(Arg.Any<Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>());
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
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<EntityNotFoundException>(() => strategy.Execute());
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
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithExistenceRules(rules => rules.Add(e => e.Name == "Active", "Must be active"));

        // Act & Assert
        var exception = await Should.ThrowAsync<ExistenceRuleException>(() => strategy.Execute());
        exception.Message.ShouldBe("TestEntity not found");
        repoContext.Calls.GetCallCount.ShouldBe(1);
        repoContext.Calls.LastGetResult.ShouldBe(existingEntity);
        logger.ShouldHaveReceived(LogLevel.Warning, "TestEntity with id test-123 not found");
    }

    [Fact]
    public async Task Execute_FullSuccessfulFlow_ExecutesLifecycleInOrderAndLogsInformation()
    {
        // Arrange
        var executionOrder = new List<string>();
        var nonMatching1 = new TestEntity { Id = "other-111", Name = "Other" };
        var existingEntity = new TestEntity { Id = "test-123", Name = "Active" };
        var nonMatching2 = new TestEntity { Id = "other-222", Name = "Other" };
        repoContext.WithData([nonMatching1, existingEntity, nonMatching2]);

        var operatorContext = Substitute.For<IOperatorContext>();
        var operatorUser = new Operator("user123", true);
        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(true);
        operatorContext.Operator.Returns(operatorUser);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Get")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator()
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
        await repository.Received(1).GetSingle(filter, cancellationToken);
        logger.ShouldHaveReceived(LogLevel.Information, "Executing get [testentity] with id test-123 by operator user123");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed get [testentity] with id test-123 by operator user123");
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
            .WithCancellationToken(cancellationToken)
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
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed get [testentity] with id test-123 by operator ");
    }
}
