// <copyright file="RepoStrategyBase.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;

using Microsoft.Extensions.Logging;

/// <summary>
/// Logging operations for the strategy builder base class.
/// </summary>
/// <typeparam name="TService">The type of the service class that is consuming the derived builder.</typeparam>
/// <typeparam name="TParent">The type of the derived builder.</typeparam>
public abstract partial class RepoStrategyBase<TService, TParent>
{
    [LoggerMessage(LogLevel.Information,
        "Executing {ActionDescription} [{EntityDescription}] by operator {OperatorId}")]
    static partial void LogExecutingAction(ILogger<TService> logger, string actionDescription, string entityDescription,
        string operatorId);

    [LoggerMessage(LogLevel.Information,
        "Successfully executed {ActionDescription} [{EntityDescription}] by operator {OperatorId}")]
    static partial void LogSuccessfullyExecutedAction(ILogger<TService> logger, string actionDescription,
        string entityDescription, string operatorId);

    [LoggerMessage(LogLevel.Information,
        "Executing {ActionDescription} [{EntityDescription}] with id {Id} by operator {OperatorId}")]
    static partial void LogExecutingActionWithId(ILogger<TService> logger, string actionDescription,
        string entityDescription, string id, string operatorId);

    [LoggerMessage(LogLevel.Information,
        "Successfully executed {ActionDescription} [{EntityDescription}] with id {Id} by operator {OperatorId}")]
    static partial void LogSuccessfullyExecutedActionWithId(ILogger<TService> logger, string actionDescription,
        string entityDescription, string id, string operatorId);

    [LoggerMessage(LogLevel.Warning, "{EntityDescription} with id {Id} not found")]
    static partial void LogEntityWithIdNotFound(ILogger<TService> logger, string entityDescription, string id);

    [LoggerMessage(LogLevel.Warning, "Execute {ActionDescription} [{EntityDescription}] failed validation")]
    static partial void LogExecuteActionEntityFailedValidation(ILogger<TService> logger, string actionDescription,
        string entityDescription);
}
