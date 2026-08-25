// <copyright file="CreateRepoStrategyTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Repositories;

using System;
using System.Collections.Generic;
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

public class CreateRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IRepoCreatable<TestEntity> repository = Substitute.For<IRepoCreatable<TestEntity>>();
    private readonly MockRepoContext<TestEntity> repoContext;
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;
    private readonly CreateRepoStrategy<TestService, TestEntity> strategy = new();

    public CreateRepoStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        repoContext = MockRepoContext<TestEntity>.CreateFor(repository);
    }

    [Fact]
    public async Task Execute_WhenLoggerIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity());

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
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity());

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task Execute_WhenCreatableRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity());

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.CreatableRepositoryRequired);
    }

    [Fact]
    public async Task Execute_WhenTargetDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity());

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WhenActionDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithCreate(() => new TestEntity());

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WhenCreateActionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create");

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.CreateActionRequired);
    }

    [Fact]
    public async Task
        Execute_WhenRequiresAuthenticatedOperatorAndOperatorContextIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity())
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
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
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity())
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
        var validationFailures = new List<RequestValidationFailure> { new("Name", "Name is required") };
        var validationResult = new RequestValidationResult(validationFailures);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity())
            .WithRequestValidation(() => Task.FromResult(validationResult));

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(() => strategy.Execute());
        exception.Errors.ShouldBe(validationFailures);
        repoContext.Calls.CreateCallCount.ShouldBe(0);
        await repository.DidNotReceive().Create(Arg.Any<TestEntity>(), Arg.Any<CancellationToken>());
        logger.ShouldHaveReceived(LogLevel.Warning, "Execute create [testentity] failed validation");
    }

    [Fact]
    public async Task Execute_WhenReferenceRuleFails_ThrowsReferenceRuleException()
    {
        // Arrange
        var referenceRule = Substitute.For<IReferenceRule>();
        referenceRule.Description.Returns("Parent entity does not exist");
        referenceRule.Validator.Returns(_ => Task.FromResult(false));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity())
            .WithReferenceRules(rules => rules.Add(referenceRule));

        // Act & Assert
        var exception = await Should.ThrowAsync<ReferenceRuleException>(() => strategy.Execute());
        exception.Message.ShouldBe("Parent entity does not exist");
        repoContext.Calls.CreateCallCount.ShouldBe(0);
        await repository.DidNotReceive().Create(Arg.Any<TestEntity>(), Arg.Any<CancellationToken>());
        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Execute create [testentity] failed reference rule 'Parent entity does not exist'");
    }

    [Fact]
    public async Task Execute_WhenAllPreconditionsMet_ExecutesLifecycleInOrderAndReturnsCreatedEntity()
    {
        // Arrange
        var entityToCreate = new TestEntity { Id = "1", Name = "Original" };
        var createdEntity = new TestEntity { Id = "1", Name = "Created" };
        var executionOrder = new List<string>();

        repoContext.WithCreateResult(_ => createdEntity);

        var operatorContext = Substitute.For<IOperatorContext>();
        var operatorUser = new Operator("user123", true);
        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(true);
        operatorContext.Operator.Returns(operatorUser);

        var referenceRule = Substitute.For<IReferenceRule>();
        referenceRule.Description.Returns("Valid rule");
        referenceRule.Validator.Returns(_ =>
        {
            executionOrder.Add("ReferenceRule");
            return Task.FromResult(true);
        });

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create")
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator()
            .WithBeforeExecute(() =>
            {
                executionOrder.Add("BeforeExecute");
                return Task.CompletedTask;
            })
            .WithRequestValidation(() =>
            {
                executionOrder.Add("Validation");
                return Task.FromResult(new RequestValidationResult(Array.Empty<RequestValidationFailure>()));
            })
            .WithReferenceRules(rules => rules.Add(referenceRule))
            .WithCreate(() =>
            {
                executionOrder.Add("CreateAction");
                return entityToCreate;
            })
            .WithAfterCreate(entity =>
            {
                executionOrder.Add($"AfterCreate:{entity.Name}");
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
        result.ShouldBe(createdEntity);
        executionOrder.ShouldBe([
            "BeforeExecute",
            "Validation",
            "ReferenceRule",
            "CreateAction",
            "AfterCreate:Created",
            "AfterExecute",
        ]);
        repoContext.Calls.CreateCallCount.ShouldBe(1);
        repoContext.Calls.LastCreateResult.ShouldBe(createdEntity);
        await repository.Received(1).Create(entityToCreate, cancellationToken);
        logger.ShouldHaveReceived(LogLevel.Information, "Executing create [testentity] by operator user123");
        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Successfully executed create [testentity] by operator user123");
    }

    [Fact]
    public async Task ExecuteAndMap_WhenCalled_MapsCreatedEntityToResultType()
    {
        // Arrange
        var entityToCreate = new TestEntity { Id = "123", Name = "Item" };

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Create")
            .WithCreate(() => entityToCreate);

        // Act
        var result = await strategy.ExecuteAndMap(entity => new TestResult { MappedName = $"Mapped_{entity.Name}" });

        // Assert
        result.ShouldNotBeNull();
        result.MappedName.ShouldBe("Mapped_Item");
        repoContext.Calls.CreateCallCount.ShouldBe(1);
        repoContext.Calls.LastCreateResult.ShouldBe(entityToCreate);
        logger.ShouldHaveReceived(LogLevel.Information, "Executing create [testentity] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed create [testentity] by operator ");
    }
}
