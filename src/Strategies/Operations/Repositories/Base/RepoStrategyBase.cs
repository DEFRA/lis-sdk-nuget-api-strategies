// <copyright file="RepoStrategyBase.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Base class for all strategy builders.
/// </summary>
/// <typeparam name="TService">The type of the service class that is consuming the derived builder.</typeparam>
/// <typeparam name="TParent">The type of the derived builder.</typeparam>
[ExcludeFromCodeCoverage]
public abstract partial class RepoStrategyBase<TService, TParent> : IRepoStrategy<TService, TParent>
    where TService : class
    where TParent : class
{
    protected ILogger<TService>? Logger { get; private set; }

    protected CancellationToken? CancellationToken { get; private set; }

    protected string? EntityDescription { get; private set; }

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

    public TParent WithEntityDescription(string entityDescription)
    {
        EntityDescription = entityDescription;
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
            throw new InvalidOperationException(RepoStrategyConstants.Errors.OperatorContextRequired);
        }

        if (!OperatorContext.HasAuthenticatedOperator)
        {
            throw new UnauthorizedAccessException(RepoStrategyConstants.Errors
                .OperatorContextAuthenticatedOperatorRequired);
        }
    }

    protected async Task ExecuteRequestValidation()
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.LoggerRequired);
        }

        if (EntityDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (ValidateAction != null)
        {
            var validationResult = await ValidateAction();

            if (!validationResult.IsValid)
            {
                LogExecuteActionEntityFailedValidation(
                    Logger,
                    ActionDescription.ToLowerInvariant(),
                    EntityDescription.ToLowerInvariant());

                throw new RequestValidationException(validationResult.Errors);
            }
        }
    }

    protected void SetParentBuilder(TParent parent)
    {
        this.ParentBuilder = parent;
    }

    protected void LogExecutingAction()
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && EntityDescription != null)
        {
            LogExecutingAction(
                Logger,
                ActionDescription.ToLowerInvariant(),
                EntityDescription.ToLowerInvariant(),
                GetOperatorLoggableId());
        }
    }

    protected void LogSuccessfullyExecutedAction()
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && EntityDescription != null)
        {
            LogSuccessfullyExecutedAction(
                Logger,
                ActionDescription.ToLowerInvariant(),
                EntityDescription.ToLowerInvariant(),
                GetOperatorLoggableId());
        }
    }

    protected void LogExecutingActionWithId(string id)
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && EntityDescription != null)
        {
            LogExecutingActionWithId(
                Logger,
                ActionDescription.ToLowerInvariant(),
                EntityDescription.ToLowerInvariant(),
                id,
                GetOperatorLoggableId());
        }
    }

    protected void LogSuccessfullyExecutedActionWithId(string id)
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && EntityDescription != null)
        {
            LogSuccessfullyExecutedActionWithId(
                Logger,
                ActionDescription.ToLowerInvariant(),
                EntityDescription.ToLowerInvariant(),
                id,
                GetOperatorLoggableId());
        }
    }

    protected void LogEntityWithIdNotFound(string id)
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && EntityDescription != null)
        {
            LogEntityWithIdNotFound(
                Logger,
                EntityDescription,
                id);
        }
    }

    private void EnsureLoggingPreRequisitesProvided()
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.LoggerRequired);
        }

        if (EntityDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.ActionDescriptionRequired);
        }
    }

    private string GetOperatorLoggableId() =>
        OperatorContext is { HasOperator: true } ? OperatorContext.Operator.LoggableId : string.Empty;

    private TParent GetParentBuilder()
    {
        return this.ParentBuilder ?? throw new InvalidOperationException("The parent builder has not been set.");
    }
}
