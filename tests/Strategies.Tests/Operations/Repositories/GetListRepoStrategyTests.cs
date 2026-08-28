// <copyright file="GetListRepoStrategyTests.cs" company="Defra">
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
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Utilities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using TestResult = Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.TestResult;

public class GetListRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IRepoListable<TestEntity> repository = Substitute.For<IRepoListable<TestEntity>>();
    private readonly MockRepoContext<TestEntity> repoContext;
    private readonly GetListRepoStrategy<TestService, TestEntity> strategy = new();
    private readonly Expression<Func<TestEntity, bool>> filter = e => e.Name == "Test";

    public GetListRepoStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        repoContext = MockRepoContext<TestEntity>.CreateFor(repository);
    }

    [Fact]
    public async Task Execute_WhenCancellationTokenIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task Execute_WhenTargetDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WhenRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.ListableRepositoryRequired);
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
            .WithActionDescription("List");

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.EntityFilterRequired);
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
            .WithActionDescription("List")
            .WithRequiresAuthenticatedOperator()
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
            .WithActionDescription("List")
            .WithRequiresAuthenticatedOperator()
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(strategy.Execute);

        exception.Message.ShouldBe(StrategyConstants.Errors.OperatorContextAuthenticatedOperatorRequired);
    }

    [Fact]
    public async Task Execute_WhenValidationFails_ThrowsRequestValidationException()
    {
        // Arrange
        var validationFailures = new List<RequestValidationFailure> { new("Filter", "Invalid filter") };
        var validationResult = new RequestValidationResult(validationFailures);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("List")
            .WithRequestValidation(() => Task.FromResult(validationResult))
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(strategy.Execute);

        exception.Errors.ShouldBe(validationFailures);

        repoContext.Calls.GetListCallCount.ShouldBe(0);

        logger.ShouldHaveReceived(LogLevel.Warning, "Execute list [testentity] failed validation");
    }

    [Fact]
    public async Task Execute_FullSuccessfulFlow_ExecutesLifecycleInOrderAndReturnsList()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        var operatorUser = new Operator("user123", true);

        var matchingItem1 = new TestEntity { Id = "1", Name = "Test" };
        var nonMatchingItem1 = new TestEntity { Id = "2", Name = "Other1" };
        var matchingItem2 = new TestEntity { Id = "3", Name = "Test" };
        var nonMatchingItem2 = new TestEntity { Id = "4", Name = "Other2" };
        var matchingItem3 = new TestEntity { Id = "5", Name = "Test" };

        var executionOrder = new List<string>();

        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(true);
        operatorContext.Operator.Returns(operatorUser);

        repoContext.WithData([matchingItem1, nonMatchingItem1, matchingItem2, nonMatchingItem2, matchingItem3]);

        strategy
            .WithLogger(logger)
            .WithOperatorContext(operatorContext)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("List")
            .WithRequiresAuthenticatedOperator()
            .WithEntityFilter(filter)
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
        result.ShouldBe([matchingItem1, matchingItem2, matchingItem3]);

        executionOrder.ShouldBe([
            "BeforeExecute",
            "AfterExecute",
        ]);

        repoContext.Calls.GetListCallCount.ShouldBe(1);
        repoContext.Calls.LastGetListResult.ShouldBe([matchingItem1, matchingItem2, matchingItem3]);

        logger.ShouldHaveReceived(LogLevel.Information, "Executing list [testentity] by operator user123");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed list [testentity] by operator user123");
    }

    [Fact]
    public async Task Execute_WhenNoEntitiesMatchFilter_ReturnsEmptyList()
    {
        // Arrange
        var nonMatchingItem1 = new TestEntity { Id = "1", Name = "Other1" };
        var nonMatchingItem2 = new TestEntity { Id = "2", Name = "Other2" };
        var nonMatchingItem3 = new TestEntity { Id = "3", Name = "Other3" };

        repoContext.WithData([nonMatchingItem1, nonMatchingItem2, nonMatchingItem3]);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act
        var result = await strategy.Execute();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();

        repoContext.Calls.GetListCallCount.ShouldBe(1);
        repoContext.Calls.LastGetListResult.ShouldNotBeNull();
        repoContext.Calls.LastGetListResult.ShouldBeEmpty();

        logger.ShouldHaveReceived(LogLevel.Information, "Executing list [testentity] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed list [testentity] by operator ");
    }

    [Fact]
    public async Task ExecuteAndMap_WhenCalled_MapsEntitiesAndReturnsList()
    {
        // Arrange
        var matchingItem1 = new TestEntity { Id = "1", Name = "Test" };
        var nonMatchingItem1 = new TestEntity { Id = "2", Name = "Other1" };
        var matchingItem2 = new TestEntity { Id = "3", Name = "Test" };
        var nonMatchingItem2 = new TestEntity { Id = "4", Name = "Other2" };
        var matchingItem3 = new TestEntity { Id = "5", Name = "Test" };

        repoContext.WithData([matchingItem1, nonMatchingItem1, matchingItem2, nonMatchingItem2, matchingItem3]);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act
        var result = await strategy.ExecuteAndMap(e => new TestResult { MappedName = $"Mapped_{e.Id}_{e.Name}" });

        // Assert
        result.ShouldNotBeNull();

        result.Count.ShouldBe(3);

        result[0].MappedName.ShouldBe("Mapped_1_Test");
        result[1].MappedName.ShouldBe("Mapped_3_Test");
        result[2].MappedName.ShouldBe("Mapped_5_Test");

        repoContext.Calls.GetListCallCount.ShouldBe(1);
        repoContext.Calls.LastGetListResult.ShouldBe([matchingItem1, matchingItem2, matchingItem3]);

        logger.ShouldHaveReceived(LogLevel.Information, "Executing list [testentity] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed list [testentity] by operator ");
    }
}
