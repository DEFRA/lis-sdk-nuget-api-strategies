// <copyright file="ConflictRulesBuilder.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

using Microsoft.Extensions.Logging;

/// <content>
/// Logging operations for the conflict rules builder.
/// </content>
public partial class ConflictRulesBuilder<TService, TEntity>
{
    [LoggerMessage(LogLevel.Warning,
        "Execute {ActionDescription} [{EntityDescription}] with id {Id} failed conflict rule '{Description}'")]
    static partial void LogFailedRule(ILogger<TService> logger, string actionDescription, string entityDescription,
        string id, string description);
}
