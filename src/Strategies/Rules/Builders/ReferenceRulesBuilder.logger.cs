// <copyright file="ReferenceRulesBuilder.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

using Microsoft.Extensions.Logging;

public partial class ReferenceRulesBuilder<TService>
{
    [LoggerMessage(LogLevel.Warning,
        "Execute {ActionDescription} [{EntityDescription}] failed reference rule '{Description}'")]
    static partial void LogReferenceRuleFailure(ILogger<TService> logger, string actionDescription,
        string entityDescription, string description);
}
