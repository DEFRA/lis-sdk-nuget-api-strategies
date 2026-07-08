// <copyright file="ExistenceRulesBuilder.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

using Microsoft.Extensions.Logging;

public partial class ExistenceRulesBuilder<TService, TEntity>
{
    [LoggerMessage(LogLevel.Warning, "{EntityDescription} with id {Id} not found")]
    static partial void LogEntityWithIdNotFound(ILogger<TService> logger, string entityDescription, string id);
}
