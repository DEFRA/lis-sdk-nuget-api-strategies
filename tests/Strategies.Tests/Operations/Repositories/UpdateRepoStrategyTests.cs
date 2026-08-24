// <copyright file="UpdateRepoStrategyTests.cs" company="Defra">
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
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using TestResult = Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData.TestResult;

public class UpdateRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly ITestRepository repository = Substitute.For<ITestRepository>();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;
    private readonly UpdateRepoStrategy<TestService, TestEntity> strategy = new();
    private readonly TestRequest request = new() { Id = "test-123" };
    private readonly Expression<Func<TestEntity, bool>> filter = e => e.Id == "test-123";

    public UpdateRepoStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
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
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

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
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task Execute_WhenGettableRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.GettableRepositoryRequired);
    }

    [Fact]
    public async Task Execute_WhenUpdatableRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var gettableRepo = Substitute.For<IRepoGettable<TestEntity>>();
        var prop = typeof(UpdateRepoStrategy<TestService, TestEntity>)
            .GetProperty("GettableRepository", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        prop?.SetValue(strategy, gettableRepo);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.UpdatableRepositoryRequired);
    }

    [Fact]
    public async Task Execute_WhenTargetDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

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
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
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
            .WithActionDescription("Update")
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

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
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
    }

    [Fact]
    public async Task Execute_WhenUpdateActionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        exception.Message.ShouldBe(RepoStrategyConstants.Errors.UpdateActionRequired);
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
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { })
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
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { })
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
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { })
            .WithRequestValidation(() => Task.FromResult(validationResult));

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(() => strategy.Execute());
        exception.Errors.ShouldBe(validationFailures);
        await repository.DidNotReceive().GetSingle(Arg.Any<Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>());
        logger.ShouldHaveReceived(LogLevel.Warning, "Execute update [testentity] failed validation");
    }

    [Fact]
    public async Task Execute_WhenReferenceRuleFails_ThrowsReferenceRuleException()
    {
        // Arrange
        var referenceRule = Substitute.For<IReferenceRule>();
        referenceRule.Description.Returns("Related item missing");
        referenceRule.Validator.Returns(_ => Task.FromResult(false));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { })
            .WithReferenceRules(rules => rules.Add(referenceRule));

        // Act & Assert
        var exception = await Should.ThrowAsync<ReferenceRuleException>(() => strategy.Execute());
        exception.Message.ShouldBe("Related item missing");
        await repository.DidNotReceive().GetSingle(Arg.Any<Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>());
        logger.ShouldHaveReceived(LogLevel.Warning, "Execute update [testentity] failed reference rule 'Related item missing'");
    }

    [Fact]
    public async Task Execute_WhenEntityNotFound_LogsWarningAndThrowsEntityNotFoundException()
    {
        // Arrange
        repository.GetSingle(filter, cancellationToken).Returns(Task.FromResult<TestEntity?>(null));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<EntityNotFoundException>(() => strategy.Execute());
        exception.Message.ShouldBe("TestEntity not found");
        logger.ShouldHaveReceived(LogLevel.Warning, "TestEntity with id test-123 not found");
    }

    [Fact]
    public async Task Execute_WhenExistenceRuleFails_ThrowsExistenceRuleException()
    {
        // Arrange
        var existingEntity = new TestEntity { Id = "test-123", Name = "Inactive" };
        repository.GetSingle(filter, cancellationToken).Returns(Task.FromResult<TestEntity?>(existingEntity));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { })
            .WithExistenceRules(rules => rules.Add(e => e.Name == "Active", "Must be active"));

        // Act & Assert
        var exception = await Should.ThrowAsync<ExistenceRuleException>(() => strategy.Execute());
        exception.Message.ShouldBe("TestEntity not found");
        logger.ShouldHaveReceived(LogLevel.Warning, "TestEntity with id test-123 not found");
    }

    [Fact]
    public async Task Execute_WhenConflictRuleFails_ThrowsConflictRuleException()
    {
        // Arrange
        var existingEntity = new TestEntity { Id = "test-123", Name = "Locked" };
        repository.GetSingle(filter, cancellationToken).Returns(Task.FromResult<TestEntity?>(existingEntity));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { })
            .WithConflictRules(rules => rules.Add(e => e.Name != "Locked", "Cannot update locked entity"));

        // Act & Assert
        var exception = await Should.ThrowAsync<ConflictRuleException>(() => strategy.Execute());
        exception.Message.ShouldBe("Cannot update locked entity");
        logger.ShouldHaveReceived(LogLevel.Warning, "Execute update [testentity] with id test-123 failed conflict rule 'Cannot update locked entity'");
    }

    [Fact]
    public async Task Execute_WhenBusinessRuleFails_ThrowsBusinessRuleException()
    {
        // Arrange
        var existingEntity = new TestEntity { Id = "test-123", Name = "Archived" };
        repository.GetSingle(filter, cancellationToken).Returns(Task.FromResult<TestEntity?>(existingEntity));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { })
            .WithBusinessRules(rules => rules.Add(e => e.Name != "Archived", "Cannot update archived entity"));

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(() => strategy.Execute());
        exception.Message.ShouldBe("Cannot update archived entity");
        logger.ShouldHaveReceived(LogLevel.Warning, "Execute update [testentity] with id test-123 failed business rule 'Cannot update archived entity'");
    }

    [Fact]
    public async Task Execute_FullSuccessfulFlow_ExecutesLifecycleInOrderAndReturnsUpdatedEntity()
    {
        // Arrange
        var executionOrder = new List<string>();
        var existingEntity = new TestEntity { Id = "test-123", Name = "Original" };
        var updatedEntity = new TestEntity { Id = "test-123", Name = "Updated" };

        repository.GetSingle(filter, cancellationToken).Returns(Task.FromResult<TestEntity?>(existingEntity));
        repository.Update(existingEntity, cancellationToken).Returns(Task.FromResult(updatedEntity));

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
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
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
            .WithExistenceRules(rules => rules.Add(
                e =>
                {
                    executionOrder.Add("ExistenceRule");
                    return true;
                },
                "Must exist"))
            .WithConflictRules(rules => rules.Add(
                e =>
                {
                    executionOrder.Add("ConflictRule");
                    return true;
                },
                "No conflict"))
            .WithBusinessRules(rules => rules.Add(
                e =>
                {
                    executionOrder.Add("BusinessRule");
                    return true;
                },
                "Valid business"))
            .WithBeforeUpdate(e =>
            {
                executionOrder.Add("BeforeUpdate");
                return Task.CompletedTask;
            })
            .WithUpdate(e =>
            {
                executionOrder.Add("UpdateAction");
                e.Name = "Updated";
            })
            .WithAfterUpdate(e =>
            {
                executionOrder.Add($"AfterUpdate:{e.Name}");
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
        result.ShouldBe(updatedEntity);
        executionOrder.ShouldBe([
            "BeforeExecute",
            "Validation",
            "ReferenceRule",
            "ExistenceRule",
            "ConflictRule",
            "BusinessRule",
            "BeforeUpdate",
            "UpdateAction",
            "AfterUpdate:Updated",
            "AfterExecute",
        ]);
        await repository.Received(1).Update(existingEntity, cancellationToken);
        logger.ShouldHaveReceived(LogLevel.Information, "Executing update [testentity] with id test-123 by operator user123");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed update [testentity] with id test-123 by operator user123");
    }

    [Fact]
    public async Task ExecuteAndMap_WhenCalled_MapsUpdatedEntityToResult()
    {
        // Arrange
        var existingEntity = new TestEntity { Id = "test-123", Name = "Original" };
        var updatedEntity = new TestEntity { Id = "test-123", Name = "Updated" };

        repository.GetSingle(filter, cancellationToken).Returns(Task.FromResult<TestEntity?>(existingEntity));
        repository.Update(existingEntity, cancellationToken).Returns(Task.FromResult(updatedEntity));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Update")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(e => e.Name = "Updated");

        // Act
        var result = await strategy.ExecuteAndMap(e => new TestResult { MappedName = $"Mapped_{e.Name}" });

        // Assert
        result.ShouldNotBeNull();
        result.MappedName.ShouldBe("Mapped_Updated");
        logger.ShouldHaveReceived(LogLevel.Information, "Executing update [testentity] with id test-123 by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed update [testentity] with id test-123 by operator ");
    }
}
