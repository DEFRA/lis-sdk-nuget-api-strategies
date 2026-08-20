// <copyright file="StrategyBase.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Base;

using Microsoft.Extensions.Logging;

/// <summary>
/// Logging operations for the strategy builder base class.
/// </summary>
/// <typeparam name="TService">The type of the service class that is consuming the derived builder.</typeparam>
/// <typeparam name="TParent">The type of the derived builder.</typeparam>
public abstract partial class StrategyBase<TService, TParent>
{
    [LoggerMessage(LogLevel.Information,
        "Executing {ActionDescription} [{targetDescription}] by operator {OperatorId}")]
    static partial void LogExecutingAction(ILogger<TService> logger, string actionDescription, string targetDescription,
        string operatorId);

    [LoggerMessage(LogLevel.Information,
        "Successfully executed {targetDescription} [{EntityDescription}] by operator {OperatorId}")]
    static partial void LogSuccessfullyExecutedAction(ILogger<TService> logger, string targetDescription,
        string entityDescription, string operatorId);

    [LoggerMessage(LogLevel.Information,
        "Executing {ActionDescription} [{targetDescription}] with id {Id} by operator {OperatorId}")]
    static partial void LogExecutingActionWithId(ILogger<TService> logger, string actionDescription,
        string targetDescription, string id, string operatorId);

    [LoggerMessage(LogLevel.Information,
        "Successfully executed {ActionDescription} [{targetDescription}] with id {Id} by operator {OperatorId}")]
    static partial void LogSuccessfullyExecutedActionWithId(ILogger<TService> logger, string actionDescription,
        string targetDescription, string id, string operatorId);

    [LoggerMessage(LogLevel.Warning, "Execute {ActionDescription} [{targetDescription}] failed validation")]
    static partial void LogExecuteActionFailedValidation(ILogger<TService> logger, string actionDescription,
        string targetDescription);
}
