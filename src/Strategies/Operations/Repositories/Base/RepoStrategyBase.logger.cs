// <copyright file="RepoStrategyBase.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;

using Microsoft.Extensions.Logging;

/// <content>
/// Logging operations for the repository strategy builder base class.
/// </content>
public abstract partial class RepoStrategyBase<TService, TParent>
{
    [LoggerMessage(LogLevel.Warning, "{targetDescription} with id {Id} not found")]
    static partial void LogEntityWithIdNotFound(ILogger<TService> logger, string targetDescription, string id);
}
