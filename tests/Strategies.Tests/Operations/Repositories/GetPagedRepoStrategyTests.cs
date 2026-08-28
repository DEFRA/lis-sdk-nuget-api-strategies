// <copyright file="GetPagedRepoStrategyTests.cs" company="Defra">
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
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests.Pagination;
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

public class GetPagedRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IRepoPageable<TestEntity> repository = Substitute.For<IRepoPageable<TestEntity>>();
    private readonly MockRepoContext<TestEntity> repoContext;
    private readonly GetPagedRepoStrategy<TestService, TestEntity> strategy = new();
    private readonly PagedQuery query = new() { PageNumber = 1, PageSize = 10, OrderByDescending = false };
    private readonly Expression<Func<TestEntity, bool>> filter = e => e.Name == "Test";

    public GetPagedRepoStrategyTests()
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
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute(e => e.Id));

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
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute(e => e.Id));

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
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute(e => e.Id));

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.PageableRepositoryRequired);
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
            .WithActionDescription("Paged")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute(e => e.Id));

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
            .WithActionDescription("Paged")
            .WithRequest(query);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute(e => e.Id));

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
            .WithActionDescription("Paged")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(query)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute(e => e.Id));

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
            .WithActionDescription("Paged")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(query)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() => strategy.Execute(e => e.Id));

        exception.Message.ShouldBe(StrategyConstants.Errors.OperatorContextAuthenticatedOperatorRequired);
    }

    [Fact]
    public async Task Execute_WhenValidationFails_ThrowsRequestValidationException()
    {
        // Arrange
        var validationFailures = new List<RequestValidationFailure> { new("PageSize", "Invalid page size") };
        var validationResult = new RequestValidationResult(validationFailures);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Paged")
            .WithRequestValidation(() => Task.FromResult(validationResult))
            .WithRequest(query)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(() => strategy.Execute(e => e.Id));

        exception.Errors.ShouldBe(validationFailures);

        repoContext.Calls.GetPagedCallCount.ShouldBe(0);

        logger.ShouldHaveReceived(LogLevel.Warning, "Execute paged [testentity] failed validation");
    }

    [Fact]
    public async Task Execute_FullSuccessfulFlow_ExecutesLifecycleInOrderAndReturnsPagedEntities()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        var operatorUser = new Operator("user123", true);

        var matching1 = new TestEntity { Id = "1", Name = "Test" };
        var nonMatching1 = new TestEntity { Id = "2", Name = "Other1" };
        var matching2 = new TestEntity { Id = "3", Name = "Test" };
        var nonMatching2 = new TestEntity { Id = "4", Name = "Other2" };
        var matching3 = new TestEntity { Id = "5", Name = "Test" };

        var executionOrder = new List<string>();

        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(true);
        operatorContext.Operator.Returns(operatorUser);

        repoContext.WithData([matching1, nonMatching1, matching2, nonMatching2, matching3]);

        strategy
            .WithLogger(logger)
            .WithOperatorContext(operatorContext)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Paged")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(query)
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
        var result = await strategy.Execute(e => e.Id);

        // Assert
        result.ShouldNotBeNull();

        result.ShouldSatisfyAllConditions(
            x => x.TotalCount.ShouldBe(3),
            x => x.Items.ShouldBe([matching1, matching2, matching3]));

        executionOrder.ShouldBe([
            "BeforeExecute",
            "AfterExecute",
        ]);

        repoContext.Calls.GetPagedCallCount.ShouldBe(1);
        repoContext.Calls.LastGetPagedResult.ShouldNotBeNull();

        repoContext.Calls.LastGetPagedResult.ShouldSatisfyAllConditions(
            x => x.TotalCount.ShouldBe(3),
            x => x.Items.ShouldBe([matching1, matching2, matching3]));

        logger.ShouldHaveReceived(LogLevel.Information, "Executing paged [testentity] by operator user123");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed paged [testentity] by operator user123");
    }

    [Fact]
    public async Task ExecuteAndMap_WhenCalled_MapsEntitiesAndReturnsPagedResults()
    {
        // Arrange
        var matching1 = new TestEntity { Id = "1", Name = "Test" };
        var nonMatching1 = new TestEntity { Id = "2", Name = "Other1" };
        var matching2 = new TestEntity { Id = "3", Name = "Test" };
        var nonMatching2 = new TestEntity { Id = "4", Name = "Other2" };
        var matching3 = new TestEntity { Id = "5", Name = "Test" };

        repoContext.WithData([matching1, nonMatching1, matching2, nonMatching2, matching3]);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter);

        // Act
        var result =
            await strategy.ExecuteAndMap(e => new TestResult { MappedName = $"Mapped_{e.Id}_{e.Name}" }, e => e.Id);

        // Assert
        result.ShouldNotBeNull();

        result.ShouldSatisfyAllConditions(
            x => x.PageNumber.ShouldBe(1),
            x => x.PageSize.ShouldBe(10),
            x => x.TotalCount.ShouldBe(3),
            x => x.TotalPages.ShouldBe(1),
            x => x.Items.Count().ShouldBe(3));

        var itemList = result.Items.ToList();

        itemList[0].MappedName.ShouldBe("Mapped_1_Test");
        itemList[1].MappedName.ShouldBe("Mapped_3_Test");
        itemList[2].MappedName.ShouldBe("Mapped_5_Test");

        repoContext.Calls.GetPagedCallCount.ShouldBe(1);
        repoContext.Calls.LastGetPagedResult.ShouldNotBeNull();

        logger.ShouldHaveReceived(LogLevel.Information, "Executing paged [testentity] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed paged [testentity] by operator ");
    }
}
