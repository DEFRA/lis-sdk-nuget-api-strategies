// <copyright file="GetListRepoStrategyTests.cs" company="Defra">
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

public class GetListRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IRepoListable<TestEntity> repository = Substitute.For<IRepoListable<TestEntity>>();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;
    private readonly GetListRepoStrategy<TestService, TestEntity> strategy = new();
    private readonly Expression<Func<TestEntity, bool>> filter = e => e.Name == "Test";

    public GetListRepoStrategyTests()
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
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
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
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
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
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.ListableRepositoryRequired);
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
            .WithActionDescription("List");

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.EntityFilterRequired);
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
            .WithActionDescription("List")
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
            .WithActionDescription("List")
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
        var validationFailures = new List<RequestValidationFailure> { new("Filter", "Invalid filter") };
        var validationResult = new RequestValidationResult(validationFailures);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("List")
            .WithEntityFilter(filter)
            .WithRequestValidation(() => Task.FromResult(validationResult));

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(() => strategy.Execute());
        exception.Errors.ShouldBe(validationFailures);
        await repository.DidNotReceive().GetList(Arg.Any<Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>());
        logger.ShouldHaveReceived(LogLevel.Warning, "Execute list [testentity] failed validation");
    }

    [Fact]
    public async Task Execute_FullSuccessfulFlow_ExecutesLifecycleInOrderAndReturnsList()
    {
        // Arrange
        var executionOrder = new List<string>();
        var items = new List<TestEntity>
        {
            new() { Id = "1", Name = "Item 1" },
            new() { Id = "2", Name = "Item 2" },
        };
        repository.GetList(filter, cancellationToken).Returns(Task.FromResult(items));

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
            .WithActionDescription("List")
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
        var result = await strategy.Execute();

        // Assert
        result.ShouldBe(items);
        executionOrder.ShouldBe([
            "BeforeExecute",
            "AfterExecute",
        ]);
        await repository.Received(1).GetList(filter, cancellationToken);
        logger.ShouldHaveReceived(LogLevel.Information, "Executing list [testentity] by operator user123");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed list [testentity] by operator user123");
    }

    [Fact]
    public async Task ExecuteAndMap_WhenCalled_MapsEntitiesAndReturnsList()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new() { Id = "1", Name = "Item 1" },
            new() { Id = "2", Name = "Item 2" },
        };
        repository.GetList(filter, cancellationToken).Returns(Task.FromResult(items));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("List")
            .WithEntityFilter(filter);

        // Act
        var result = await strategy.ExecuteAndMap(e => new TestResult { MappedName = $"Mapped_{e.Name}" });

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].MappedName.ShouldBe("Mapped_Item 1");
        result[1].MappedName.ShouldBe("Mapped_Item 2");
        logger.ShouldHaveReceived(LogLevel.Information, "Executing list [testentity] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed list [testentity] by operator ");
    }
}
