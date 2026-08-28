// <copyright file="ExistenceRulesBuilder.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Rules.Builders.Ancillary;
using Microsoft.Extensions.Logging;

public partial class ExistenceRulesBuilder<TService, TEntity> : IExistenceRulesBuilder<TService, TEntity>
    where TService : class
    where TEntity : class
{
    private readonly EntityRulesBuilder<TEntity> entityRulesBuilder = new EntityRulesBuilder<TEntity>();

    public IExistenceRulesBuilder<TService, TEntity> Add(Func<TEntity, bool> expression, string description)
    {
        entityRulesBuilder.Add(expression, description);

        return this;
    }

    public IExistenceRulesBuilder<TService, TEntity> Add(IPredicateRule<TEntity> predicateRule)
    {
        entityRulesBuilder.Add(predicateRule);

        return this;
    }

    public void Validate(
        ILoggableById request,
        TEntity modelToValidate,
        string modelDescription,
        ILogger<TService> logger)
    {
        foreach (var rule in entityRulesBuilder.Predicates)
        {
            var validAgainstExistenceRule = rule.Predicate(modelToValidate);

            if (validAgainstExistenceRule)
            {
                continue;
            }

            LogEntityWithIdNotFound(logger, modelDescription, request.GetLoggableId());

            throw new ExistenceRuleException($"{modelDescription} not found");
        }
    }
}
