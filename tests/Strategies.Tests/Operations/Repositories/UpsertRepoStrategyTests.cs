// <copyright file="UpsertRepoStrategyTests.cs" company="Defra">
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
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;
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

public class UpsertRepoStrategyTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly ITestUpsertRepository repository = Substitute.For<ITestUpsertRepository>();
    private readonly MockRepoContext<TestEntity> repoContext;
    private readonly UpsertRepoStrategy<TestService, TestEntity> strategy = new();
    private readonly TestRequest request = new() { Id = "test-123" };
    private readonly Expression<Func<TestEntity, bool>> filter = e => e.Id == "test-123";

    public UpsertRepoStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        repoContext = MockRepoContext<TestEntity>.CreateFor(repository);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public interface ITestUpsertRepository : IRepoGettable<TestEntity>, IRepoCreatable<TestEntity>,
        IRepoUpdatable<TestEntity>;

    [Fact]
    public async Task Execute_WhenLoggerIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

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
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task Execute_WhenGettableRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.GettableRepositoryRequired);
    }

    [Fact]
    public async Task Execute_WhenCreatableRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var gettableRepo = Substitute.For<IRepoGettable<TestEntity>>();

        var prop = typeof(UpsertRepoStrategy<TestService, TestEntity>)
            .GetProperty(
                "GettableRepository",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        prop?.SetValue(strategy, gettableRepo);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.CreatableRepositoryRequired);
    }

    [Fact]
    public async Task Execute_WhenUpdatableRepositoryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var gettableRepo = Substitute.For<IRepoGettable<TestEntity>>();
        var creatableRepo = Substitute.For<IRepoCreatable<TestEntity>>();

        var gettableProp = typeof(UpsertRepoStrategy<TestService, TestEntity>)
            .GetProperty(
                "GettableRepository",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var creatableProp = typeof(UpsertRepoStrategy<TestService, TestEntity>)
            .GetProperty(
                "CreatableRepository",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        gettableProp?.SetValue(strategy, gettableRepo);
        creatableProp?.SetValue(strategy, creatableRepo);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.UpdatableRepositoryRequired);
    }

    [Fact]
    public async Task Execute_WhenTargetDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WhenActionDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
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
            .WithActionDescription("Upsert")
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

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
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
    }

    [Fact]
    public async Task Execute_WhenCreateActionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.CreateActionRequired);
    }

    [Fact]
    public async Task Execute_WhenUpdateActionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity());

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(strategy.Execute);

        exception.Message.ShouldBe(RepoStrategyConstants.Errors.UpdateActionRequired);
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
            .WithActionDescription("Upsert")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

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
            .WithActionDescription("Upsert")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(strategy.Execute);

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
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequestValidation(() => Task.FromResult(validationResult))
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<RequestValidationException>(strategy.Execute);

        exception.Errors.ShouldBe(validationFailures);

        repoContext.Calls.GetCallCount.ShouldBe(0);
        repoContext.Calls.CreateCallCount.ShouldBe(0);
        repoContext.Calls.UpdateCallCount.ShouldBe(0);

        logger.ShouldHaveReceived(LogLevel.Warning, "Execute upsert [testentity] failed validation");
    }

    [Fact]
    public async Task Execute_WhenReferenceRuleFails_ThrowsReferenceRuleException()
    {
        // Arrange
        var referenceRule = Substitute.For<IReferenceRule>();

        referenceRule.Description.Returns("Missing reference");
        referenceRule.Validator.Returns(_ => Task.FromResult(false));

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithReferenceRules(rules => rules.Add(referenceRule))
            .WithCreate(() => new TestEntity())
            .WithUpdate(_ => { });

        // Act & Assert
        var exception = await Should.ThrowAsync<ReferenceRuleException>(strategy.Execute);

        exception.Message.ShouldBe("Missing reference");

        repoContext.Calls.GetCallCount.ShouldBe(0);
        repoContext.Calls.CreateCallCount.ShouldBe(0);
        repoContext.Calls.UpdateCallCount.ShouldBe(0);

        logger.ShouldHaveReceived(
            LogLevel.Warning,
            "Execute upsert [testentity] failed reference rule 'Missing reference'");
    }

    [Fact]
    public async Task Execute_WhenExistingEntityFound_ExecutesUpdateFlow()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        var operatorUser = new Operator("user123", true);

        var nonMatching1 = new TestEntity { Id = "other-111", Name = "Other" };
        var existingEntity = new TestEntity { Id = "test-123", Name = "Original" };
        var nonMatching2 = new TestEntity { Id = "other-222", Name = "Other" };

        var executionOrder = new List<string>();

        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(true);
        operatorContext.Operator.Returns(operatorUser);

        repoContext.WithData([nonMatching1, existingEntity, nonMatching2]);

        var referenceRule = Substitute.For<IReferenceRule>();

        referenceRule.Description.Returns("Valid reference");

        referenceRule.Validator.Returns(_ =>
        {
            executionOrder.Add("ReferenceRule");
            return Task.FromResult(true);
        });

        strategy
            .WithLogger(logger)
            .WithOperatorContext(operatorContext)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithBeforeExecute(() =>
            {
                executionOrder.Add("BeforeExecute");
                return Task.CompletedTask;
            })
            .WithReferenceRules(rules => rules.Add(referenceRule))
            .WithExistenceRules(rules => rules.Add(
                _ =>
                {
                    executionOrder.Add("ExistenceRule");
                    return true;
                },
                "Must exist"))
            .WithConflictRules(rules => rules.Add(
                _ =>
                {
                    executionOrder.Add("ConflictRule");
                    return true;
                },
                "No conflict"))
            .WithBusinessRules(rules => rules.Add(
                _ =>
                {
                    executionOrder.Add("BusinessRule");
                    return true;
                },
                "Valid business"))
            .WithCreate(() =>
            {
                executionOrder.Add("CreateAction");
                return new TestEntity();
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
        result.ShouldNotBeNull();

        result.Name.ShouldBe("Updated");

        executionOrder.ShouldBe([
            "BeforeExecute",
            "ReferenceRule",
            "ExistenceRule",
            "ConflictRule",
            "BusinessRule",
            "UpdateAction",
            "AfterUpdate:Updated",
            "AfterExecute",
        ]);

        repoContext.Calls.GetCallCount.ShouldBe(1);
        repoContext.Calls.UpdateCallCount.ShouldBe(1);
        repoContext.Calls.CreateCallCount.ShouldBe(0);
        repoContext.Calls.LastUpdateResult.ShouldNotBeNull();
        repoContext.Calls.LastUpdateResult.Name.ShouldBe("Updated");

        logger.ShouldHaveReceived(LogLevel.Information, "Executing upsert [testentity] by operator user123");

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Successfully executed upsert [testentity] by operator user123");
    }

    [Fact]
    public async Task Execute_WhenExistingEntityNotFound_ExecutesCreateFlow()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        var operatorUser = new Operator("user123", true);

        var executionOrder = new List<string>();
        var nonMatching1 = new TestEntity { Id = "other-456", Name = "Other1" };
        var nonMatching2 = new TestEntity { Id = "other-789", Name = "Other2" };

        var entityToCreate = new TestEntity { Id = "test-123", Name = "New" };
        var createdEntity = new TestEntity { Id = "test-123", Name = "Created" };

        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(true);
        operatorContext.Operator.Returns(operatorUser);

        repoContext.WithData([nonMatching1, nonMatching2]);
        repoContext.WithCreateResult(_ => createdEntity);

        strategy
            .WithLogger(logger)
            .WithOperatorContext(operatorContext)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequiresAuthenticatedOperator()
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithBeforeExecute(() =>
            {
                executionOrder.Add("BeforeExecute");
                return Task.CompletedTask;
            })
            .WithCreate(() =>
            {
                executionOrder.Add("CreateAction");
                return entityToCreate;
            })
            .WithUpdate(_ => { executionOrder.Add("UpdateAction"); })
            .WithAfterCreate(e =>
            {
                executionOrder.Add($"AfterCreate:{e.Name}");
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
            "CreateAction",
            "AfterCreate:Created",
            "AfterExecute",
        ]);

        repoContext.Calls.GetCallCount.ShouldBe(1);
        repoContext.Calls.CreateCallCount.ShouldBe(1);
        repoContext.Calls.UpdateCallCount.ShouldBe(0);
        repoContext.Calls.LastCreateResult.ShouldBe(createdEntity);

        logger.ShouldHaveReceived(LogLevel.Information, "Executing upsert [testentity] by operator user123");

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Successfully executed upsert [testentity] by operator user123");
    }

    [Fact]
    public async Task ExecuteAndMap_WhenCalled_MapsResultCorrectly()
    {
        // Arrange
        var nonMatching1 = new TestEntity { Id = "other-111", Name = "Other" };
        var existingEntity = new TestEntity { Id = "test-123", Name = "Existing" };
        var nonMatching2 = new TestEntity { Id = "other-222", Name = "Other" };

        repoContext.WithData([nonMatching1, existingEntity, nonMatching2]);

        strategy
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repository)
            .WithEntityDescription("TestEntity")
            .WithActionDescription("Upsert")
            .WithRequest(request)
            .WithEntityFilter(filter)
            .WithCreate(() => new TestEntity())
            .WithUpdate(e => e.Name = "Updated");

        // Act
        var result = await strategy.ExecuteAndMap(e => new TestResult { MappedName = $"Mapped_{e.Name}" });

        // Assert
        result.ShouldNotBeNull();

        result.MappedName.ShouldBe("Mapped_Updated");

        repoContext.Calls.GetCallCount.ShouldBe(1);
        repoContext.Calls.UpdateCallCount.ShouldBe(1);
        repoContext.Calls.CreateCallCount.ShouldBe(0);
        repoContext.Calls.LastUpdateResult.ShouldNotBeNull();
        repoContext.Calls.LastUpdateResult.Name.ShouldBe("Updated");

        logger.ShouldHaveReceived(LogLevel.Information, "Executing upsert [testentity] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed upsert [testentity] by operator ");
    }
}
