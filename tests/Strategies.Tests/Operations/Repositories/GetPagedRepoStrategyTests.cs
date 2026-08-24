// <copyright file="GetPagedRepoStrategyTests.cs" company="Defra">
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
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Responses.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using TestResult = Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData.TestResult;

public class GetPagedRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IRepoPageable<TestEntity> repository = Substitute.For<IRepoPageable<TestEntity>>();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;
    private readonly GetPagedRepoStrategy<TestService, TestEntity> strategy = new();
    private readonly PagedQuery query = new() { PageNumber = 1, PageSize = 10, OrderByDescending = false };
    private readonly Expression<Func<TestEntity, bool>> filter = e => e.Name == "Test";

    public GetPagedRepoStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
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
            .WithCancellationToken(cancellationToken)
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
            .WithCancellationToken(cancellationToken)
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
            .WithCancellationToken(cancellationToken)
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
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Paged")
            .WithRequest(query);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute(e => e.Id));
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
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter)
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute(e => e.Id));
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
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter)
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator();

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
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter)
            .WithRequestValidation(() => Task.FromResult(validationResult));

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(() => strategy.Execute(e => e.Id));
        exception.Errors.ShouldBe(validationFailures);
        await repository.DidNotReceive().GetPaged(
            Arg.Any<Expression<Func<TestEntity, bool>>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<Expression<Func<TestEntity, string>>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        logger.ShouldHaveReceived(LogLevel.Warning, "Execute paged [testentity] failed validation");
    }

    [Fact]
    public async Task Execute_FullSuccessfulFlow_ExecutesLifecycleInOrderAndReturnsPagedEntities()
    {
        // Arrange
        var executionOrder = new List<string>();
        var items = new List<TestEntity>
        {
            new() { Id = "1", Name = "Item 1" },
            new() { Id = "2", Name = "Item 2" },
        };
        var pagedEntities = new PagedEntities<TestEntity>(items, 2, 1, 1, 10);
        repository.GetPaged(filter, 1, 10, Arg.Any<Expression<Func<TestEntity, string>>>(), false, cancellationToken)
            .Returns(Task.FromResult(pagedEntities));

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
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter)
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator()
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
        result.ShouldBe(pagedEntities);
        executionOrder.ShouldBe([
            "BeforeExecute",
            "AfterExecute",
        ]);
        logger.ShouldHaveReceived(LogLevel.Information, "Executing paged [testentity] by operator user123");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed paged [testentity] by operator user123");
    }

    [Fact]
    public async Task ExecuteAndMap_WhenCalled_MapsEntitiesAndReturnsPagedResults()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new() { Id = "1", Name = "Item 1" },
            new() { Id = "2", Name = "Item 2" },
        };
        var pagedEntities = new PagedEntities<TestEntity>(items, 2, 1, 1, 10);

        repository.GetPaged(filter, 1, 10, Arg.Any<Expression<Func<TestEntity, string>>>(), false, cancellationToken)
            .Returns(Task.FromResult(pagedEntities));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Paged")
            .WithRequest(query)
            .WithEntityFilter(filter);

        // Act
        var result = await strategy.ExecuteAndMap(e => new TestResult { MappedName = $"Mapped_{e.Name}" }, e => e.Id);

        // Assert
        result.ShouldNotBeNull();
        result.PageNumber.ShouldBe(1);
        result.PageSize.ShouldBe(10);
        result.TotalCount.ShouldBe(2);
        result.TotalPages.ShouldBe(1);
        result.Items.Count().ShouldBe(2);
        var itemList = result.Items.ToList();
        itemList[0].MappedName.ShouldBe("Mapped_Item 1");
        itemList[1].MappedName.ShouldBe("Mapped_Item 2");
        logger.ShouldHaveReceived(LogLevel.Information, "Executing paged [testentity] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed paged [testentity] by operator ");
    }
}
