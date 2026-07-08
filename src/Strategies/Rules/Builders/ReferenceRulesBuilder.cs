// <copyright file="ReferenceRulesBuilder.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules.Builders;
using Microsoft.Extensions.Logging;

[ExcludeFromCodeCoverage]
public partial class ReferenceRulesBuilder<TService> : IReferenceRulesBuilder<TService>
    where TService : class
{
    private List<IReferenceRule> ReferenceRules { get; } = [];

    public IReferenceRulesBuilder<TService> Add<TEntity>(
        IRepoGettable<TEntity> repository,
        Expression<Func<TEntity, bool>> predicate,
        string description)
        where TEntity : class
    {
        ReferenceRules.Add(new EntityReferenceRule<TEntity>(repository, predicate, description));

        return this;
    }

    public IReferenceRulesBuilder<TService> Add(IReferenceRule rule)
    {
        ReferenceRules.Add(rule);

        return this;
    }

    public async Task Validate(
        string actionDescription,
        string modelDescription,
        ILogger<TService> logger,
        CancellationToken cancellationToken)
    {
        foreach (var rule in ReferenceRules)
        {
            await ValidateReferenceRule(rule, actionDescription, modelDescription, logger, cancellationToken);
        }
    }

    private static async Task ValidateReferenceRule(
        IReferenceRule rule,
        string actionDescription,
        string primaryEntityDescription,
        ILogger<TService> logger,
        CancellationToken cancellationToken)
    {
        var entityExists = await rule.Validator(cancellationToken);

        if (!entityExists)
        {
            LogReferenceRuleFailure(
                logger,
                actionDescription.ToLowerInvariant(),
                primaryEntityDescription.ToLowerInvariant(),
                rule.Description);

            throw new ReferenceRuleException(rule.Description);
        }
    }
}
