// <copyright file="EntityRulesBuilder.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Rules.Builders.Ancillary;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;

[ExcludeFromCodeCoverage]
internal class EntityRulesBuilder<TEntity>
    where TEntity : class
{
    public List<IPredicateRule<TEntity>> Predicates { get; } = [];

    public EntityRulesBuilder<TEntity> Add(
        Func<TEntity, bool> predicate,
        string description,
        string? errorMessage = null)
    {
        Predicates.Add(new EntityPredicateRule<TEntity>(predicate, description, errorMessage));

        return this;
    }

    public EntityRulesBuilder<TEntity> Add(IPredicateRule<TEntity> rule)
    {
        Predicates.Add(rule);

        return this;
    }
}
