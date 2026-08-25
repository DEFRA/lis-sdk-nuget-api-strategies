// <copyright file="StrategyBaseTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Base;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Tests;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class StrategyBaseTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;
    private readonly TestStrategy strategy = new();

    public StrategyBaseTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void FluentSetters_SetPropertiesAndReturnParentBuilder()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        Func<Task> beforeAction = () => Task.CompletedTask;
        Func<Task> afterAction = () => Task.CompletedTask;
        Func<Task<RequestValidationResult>> validateAction = () =>
            Task.FromResult(new RequestValidationResult(Array.Empty<RequestValidationFailure>()));

        // Act
        var result = strategy
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator()
            .WithActionDescription("TestAction")
            .WithBeforeExecute(beforeAction)
            .WithAfterExecute(afterAction)
            .WithRequestValidation(validateAction);

        // Assert
        result.ShouldBe(strategy);
        strategy.GetLogger().ShouldBe(logger);
        strategy.GetCancellationToken().ShouldBe(cancellationToken);
        strategy.GetActionDescription().ShouldBe("TestAction");
    }

    [Fact]
    public void GetParentBuilder_WhenParentNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedStrategy = new UninitializedStrategy();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => uninitializedStrategy.CallGetParentBuilder());
        exception.Message.ShouldBe("The parent builder has not been set.");
    }

    [Fact]
    public async Task InvokeBeforeAndAfterExecuteAction_WhenActionsProvided_InvokesThem()
    {
        // Arrange
        var beforeExecuted = false;
        var afterExecuted = false;

        strategy
            .WithBeforeExecute(() =>
            {
                beforeExecuted = true;
                return Task.CompletedTask;
            })
            .WithAfterExecute(() =>
            {
                afterExecuted = true;
                return Task.CompletedTask;
            });

        // Act
        await strategy.CallInvokeBeforeExecuteAction();
        await strategy.CallInvokeAfterExecuteAction();

        // Assert
        beforeExecuted.ShouldBeTrue();
        afterExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeBeforeAndAfterExecuteAction_WhenActionsNull_DoesNotThrow()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () =>
        {
            await strategy.CallInvokeBeforeExecuteAction();
            await strategy.CallInvokeAfterExecuteAction();
        });
    }

    [Fact]
    public void EnsureOperatorHasRequiredPermissions_WhenRequiresAuthenticatedOperatorIsFalse_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => strategy.CallEnsureOperatorHasRequiredPermissions());
    }

    [Fact]
    public void
        EnsureOperatorHasRequiredPermissions_WhenRequiresAuthenticatedOperatorIsTrueAndContextIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy.WithRequiresAuthenticatedOperator();

        // Act & Assert
        var exception =
            Should.Throw<InvalidOperationException>(() => strategy.CallEnsureOperatorHasRequiredPermissions());
        exception.Message.ShouldBe(StrategyConstants.Errors.OperatorContextRequired);
    }

    [Fact]
    public void
        EnsureOperatorHasRequiredPermissions_WhenRequiresAuthenticatedOperatorIsTrueAndNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        operatorContext.HasAuthenticatedOperator.Returns(false);

        strategy
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        var exception =
            Should.Throw<UnauthorizedAccessException>(() => strategy.CallEnsureOperatorHasRequiredPermissions());
        exception.Message.ShouldBe(StrategyConstants.Errors.OperatorContextAuthenticatedOperatorRequired);
    }

    [Fact]
    public void
        EnsureOperatorHasRequiredPermissions_WhenRequiresAuthenticatedOperatorIsTrueAndAuthenticated_DoesNotThrow()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        operatorContext.HasAuthenticatedOperator.Returns(true);

        strategy
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        Should.NotThrow(() => strategy.CallEnsureOperatorHasRequiredPermissions());
    }

    [Fact]
    public async Task ExecuteRequestValidation_WhenLoggerIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithTargetDescription("Target")
            .WithActionDescription("Action");

        // Act & Assert
        var exception =
            await Should.ThrowAsync<InvalidOperationException>(() => strategy.CallExecuteRequestValidation());
        exception.Message.ShouldBe(StrategyConstants.Errors.LoggerRequired);
    }

    [Fact]
    public async Task ExecuteRequestValidation_WhenTargetDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithActionDescription("Action");

        // Act & Assert
        var exception =
            await Should.ThrowAsync<InvalidOperationException>(() => strategy.CallExecuteRequestValidation());
        exception.Message.ShouldBe(StrategyConstants.Errors.TargetDescriptionRequired);
    }

    [Fact]
    public async Task ExecuteRequestValidation_WhenActionDescriptionIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithTargetDescription("Target");

        // Act & Assert
        var exception =
            await Should.ThrowAsync<InvalidOperationException>(() => strategy.CallExecuteRequestValidation());
        exception.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
    }

    [Fact]
    public async Task ExecuteRequestValidation_WhenValidateActionIsNull_DoesNotThrow()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithTargetDescription("Target")
            .WithActionDescription("Action");

        // Act & Assert
        await Should.NotThrowAsync(() => strategy.CallExecuteRequestValidation());
    }

    [Fact]
    public async Task
        ExecuteRequestValidation_WhenValidateActionReturnsInvalid_ThrowsRequestValidationExceptionAndLogsWarning()
    {
        // Arrange
        var failures = new List<RequestValidationFailure> { new("Property", "Error") };
        var validationResult = new RequestValidationResult(failures);

        strategy
            .WithLogger(logger)
            .WithTargetDescription("Target")
            .WithActionDescription("Action")
            .WithRequestValidation(() => Task.FromResult(validationResult));

        // Act & Assert
        var exception =
            await Should.ThrowAsync<RequestValidationException>(() => strategy.CallExecuteRequestValidation());
        exception.Errors.ShouldBe(failures);
        logger.ShouldHaveReceived(LogLevel.Warning, "Execute action [target] failed validation");
    }

    [Fact]
    public void LoggingMethods_WhenPrerequisitesMet_LogsExpectedMessages()
    {
        // Arrange
        var operatorContext = Substitute.For<IOperatorContext>();
        var operatorUser = new Operator("op123", true);
        operatorContext.HasOperator.Returns(true);
        operatorContext.Operator.Returns(operatorUser);

        strategy
            .WithLogger(logger)
            .WithTargetDescription("Target")
            .WithActionDescription("Action")
            .WithOperatorContext(operatorContext);

        // Act
        strategy.CallLogExecutingAction();
        strategy.CallLogSuccessfullyExecutedAction();
        strategy.CallLogExecutingActionWithId("id-1");
        strategy.CallLogSuccessfullyExecutedActionWithId("id-1");

        // Assert
        logger.ShouldHaveReceived(LogLevel.Information, "Executing action [target] by operator op123");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed action [target] by operator op123");
        logger.ShouldHaveReceived(LogLevel.Information, "Executing action [target] with id id-1 by operator op123");
        logger.ShouldHaveReceived(LogLevel.Information,
            "Successfully executed action [target] with id id-1 by operator op123");
    }

    [Fact]
    public void LoggingMethods_WhenTargetDescriptionIsNull_DoesNotLog()
    {
        // Arrange
        strategy
            .WithLogger(logger)
            .WithActionDescription("Action");

        // Act
        strategy.CallLogExecutingAction();
        strategy.CallLogSuccessfullyExecutedAction();
        strategy.CallLogExecutingActionWithId("id-1");
        strategy.CallLogSuccessfullyExecutedActionWithId("id-1");

        // Assert
        logger.DidNotReceiveWithAnyArgs().Log(default, default, default, default, default!);
    }

    [Fact]
    public void GetOperatorLoggableId_WhenOperatorContextNullOrHasNoOperator_ReturnsEmptyString()
    {
        // Act & Assert
        strategy.CallGetOperatorLoggableId().ShouldBe(string.Empty);

        var operatorContext = Substitute.For<IOperatorContext>();
        operatorContext.HasOperator.Returns(false);
        strategy.WithOperatorContext(operatorContext);

        strategy.CallGetOperatorLoggableId().ShouldBe(string.Empty);
    }

    private sealed class UninitializedStrategy : StrategyBase<TestService, UninitializedStrategy>
    {
        public UninitializedStrategy CallGetParentBuilder() => GetParentBuilder();
    }

    private sealed class TestStrategy : StrategyBase<TestService, TestStrategy>
    {
        public TestStrategy()
        {
            SetParentBuilder(this);
        }

        public ILogger<TestService>? GetLogger() => Logger;

        public CancellationToken? GetCancellationToken() => CancellationToken;

        public string? GetActionDescription() => ActionDescription;

        public TestStrategy WithTargetDescription(string target)
        {
            TargetDescription = target;
            return this;
        }

        public Task CallInvokeBeforeExecuteAction() => InvokeBeforeExecuteAction();

        public Task CallInvokeAfterExecuteAction() => InvokeAfterExecuteAction();

        public void CallEnsureOperatorHasRequiredPermissions() => EnsureOperatorHasRequiredPermissions();

        public Task CallExecuteRequestValidation() => ExecuteRequestValidation();

        public void CallLogExecutingAction() => LogExecutingAction();

        public void CallLogSuccessfullyExecutedAction() => LogSuccessfullyExecutedAction();

        public void CallLogExecutingActionWithId(string id) => LogExecutingActionWithId(id);

        public void CallLogSuccessfullyExecutedActionWithId(string id) => LogSuccessfullyExecutedActionWithId(id);

        public string CallGetOperatorLoggableId() => GetOperatorLoggableId();
    }
}
