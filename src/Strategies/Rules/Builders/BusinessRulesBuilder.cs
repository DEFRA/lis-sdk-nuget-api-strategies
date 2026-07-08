// <copyright file="BusinessRulesBuilder.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Rules.Builders.Ancillary;
using Microsoft.Extensions.Logging;

[ExcludeFromCodeCoverage]
public class BusinessRulesBuilder<TService, TEntity> : IBusinessRulesBuilder<TService, TEntity>
    where TService : class
    where TEntity : class
{
    private readonly EntityRulesBuilder<TEntity> entityRulesBuilder = new EntityRulesBuilder<TEntity>();

    public IBusinessRulesBuilder<TService, TEntity> Add(
        Func<TEntity, bool> expression,
        string description,
        string? errorMessage = null)
    {
        entityRulesBuilder.Add(expression, description, errorMessage);

        return this;
    }

    public IBusinessRulesBuilder<TService, TEntity> Add(IPredicateRule<TEntity> predicateRule)
    {
        entityRulesBuilder.Add(predicateRule);

        return this;
    }

    public void Validate(
        ILoggableById request,
        TEntity modelToValidate,
        string actionDescription,
        string modelDescription,
        ILogger<TService> logger)
    {
        foreach (var rule in entityRulesBuilder.Predicates)
        {
            var validAgainstBusinessRule = rule.Predicate(modelToValidate);

            if (validAgainstBusinessRule)
            {
                continue;
            }

            logger.LogWarning(
                "Execute {ActionDescription} [{EntityDescription}] with id {Id} failed business rule '{Description}'",
                actionDescription.ToLowerInvariant(),
                modelDescription.ToLowerInvariant(),
                request.GetLoggableId(),
                rule.Description);

            throw new BusinessRuleException(rule.Description);
        }
    }
}
