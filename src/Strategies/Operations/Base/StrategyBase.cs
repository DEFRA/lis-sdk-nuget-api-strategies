// <copyright file="StrategyBase.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Base;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Base class for strategy builders.
/// </summary>
/// <typeparam name="TService">The type of the service class that is consuming the derived builder.</typeparam>
/// <typeparam name="TParent">The type of the derived builder.</typeparam>
[ExcludeFromCodeCoverage]
public abstract partial class StrategyBase<TService, TParent> : IStrategy<TService, TParent>
    where TService : class
    where TParent : class
{
    protected ILogger<TService>? Logger { get; private set; }

    protected CancellationToken? CancellationToken { get; private set; }

    protected string? TargetDescription { get; set; }

    protected string? ActionDescription { get; private set; }

    private IOperatorContext? OperatorContext { get; set; }

    private bool RequiresAuthenticatedOperator { get; set; }

    private Func<Task>? BeforeExecuteAction { get; set; }

    private Func<Task>? AfterExecuteAction { get; set; }

    private Func<Task<RequestValidationResult>>? ValidateAction { get; set; }

    private TParent? ParentBuilder { get; set; }

    public TParent WithLogger(ILogger<TService> logger)
    {
        Logger = logger;
        return GetParentBuilder();
    }

    public TParent WithCancellationToken(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        return GetParentBuilder();
    }

    public TParent WithOperatorContext(IOperatorContext operatorContext)
    {
        OperatorContext = operatorContext;
        return GetParentBuilder();
    }

    public TParent WithRequiresAuthenticatedOperator()
    {
        RequiresAuthenticatedOperator = true;
        return GetParentBuilder();
    }

    public TParent WithActionDescription(string actionDescription)
    {
        ActionDescription = actionDescription;
        return GetParentBuilder();
    }

    public TParent WithBeforeExecute(Func<Task> beforeExecuteAction)
    {
        BeforeExecuteAction = beforeExecuteAction;
        return GetParentBuilder();
    }

    public TParent WithAfterExecute(Func<Task> afterExecuteAction)
    {
        AfterExecuteAction = afterExecuteAction;
        return GetParentBuilder();
    }

    public TParent WithRequestValidation(Func<Task<RequestValidationResult>> validateAction)
    {
        ValidateAction = validateAction;
        return GetParentBuilder();
    }

    protected async Task InvokeBeforeExecuteAction()
    {
        if (BeforeExecuteAction != null)
        {
            await BeforeExecuteAction();
        }
    }

    protected async Task InvokeAfterExecuteAction()
    {
        if (AfterExecuteAction != null)
        {
            await AfterExecuteAction();
        }
    }

    protected void EnsureOperatorHasRequiredPermissions()
    {
        if (!RequiresAuthenticatedOperator)
        {
            return;
        }

        if (OperatorContext == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.OperatorContextRequired);
        }

        if (!OperatorContext.HasAuthenticatedOperator)
        {
            throw new UnauthorizedAccessException(StrategyConstants.Errors
                .OperatorContextAuthenticatedOperatorRequired);
        }
    }

    protected async Task ExecuteRequestValidation()
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.LoggerRequired);
        }

        if (TargetDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.TargetDescriptionRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (ValidateAction != null)
        {
            var validationResult = await ValidateAction();

            if (!validationResult.IsValid)
            {
                LogExecuteActionFailedValidation(
                    Logger,
                    ActionDescription.ToLowerInvariant(),
                    TargetDescription.ToLowerInvariant());

                throw new RequestValidationException(validationResult.Errors);
            }
        }
    }

    protected TParent GetParentBuilder()
    {
        return ParentBuilder ?? throw new InvalidOperationException("The parent builder has not been set.");
    }

    protected void SetParentBuilder(TParent parent)
    {
        ParentBuilder = parent;
    }

    protected void LogExecutingAction()
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && TargetDescription != null)
        {
            LogExecutingAction(
                Logger,
                ActionDescription.ToLowerInvariant(),
                TargetDescription.ToLowerInvariant(),
                GetOperatorLoggableId());
        }
    }

    protected void LogSuccessfullyExecutedAction()
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && TargetDescription != null)
        {
            LogSuccessfullyExecutedAction(
                Logger,
                ActionDescription.ToLowerInvariant(),
                TargetDescription.ToLowerInvariant(),
                GetOperatorLoggableId());
        }
    }

    protected void LogExecutingActionWithId(string id)
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && TargetDescription != null)
        {
            LogExecutingActionWithId(
                Logger,
                ActionDescription.ToLowerInvariant(),
                TargetDescription.ToLowerInvariant(),
                id,
                GetOperatorLoggableId());
        }
    }

    protected void LogSuccessfullyExecutedActionWithId(string id)
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && TargetDescription != null)
        {
            LogSuccessfullyExecutedActionWithId(
                Logger,
                ActionDescription.ToLowerInvariant(),
                TargetDescription.ToLowerInvariant(),
                id,
                GetOperatorLoggableId());
        }
    }

    protected void EnsureLoggingPreRequisitesProvided()
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.LoggerRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.ActionDescriptionRequired);
        }
    }

    protected string GetOperatorLoggableId() =>
        OperatorContext is { HasOperator: true } ? OperatorContext.Operator.LoggableId : string.Empty;
}
