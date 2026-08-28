// <copyright file="RepoStrategyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;

public sealed class RepoStrategyFactory<TService> : StrategyFactoryBase<TService, IRepoStrategyFactory<TService>>,
    IRepoStrategyFactory<TService>
    where TService : class
{
    public RepoStrategyFactory()
    {
        SetParentFactory(this);
    }

    private string? DefaultEntityDescription { get; set; }

    public IRepoStrategyFactory<TService> WithDefaultEntityDescription(string entityDescription)
    {
        DefaultEntityDescription = entityDescription;
        return this;
    }

    public ICreateRepoStrategy<TService, TEntity> BuildCreateStrategy<TEntity>()
        where TEntity : class
    {
        var createStrategyBuilder = new CreateRepoStrategy<TService, TEntity>();

        AttachDefaults(createStrategyBuilder);

        return createStrategyBuilder;
    }

    public IUpdateRepoStrategy<TService, TEntity> BuildUpdateStrategy<TEntity>()
        where TEntity : class
    {
        var updateStrategyBuilder = new UpdateRepoStrategy<TService, TEntity>();

        AttachDefaults(updateStrategyBuilder);

        return updateStrategyBuilder;
    }

    public IUpsertRepoStrategy<TService, TEntity> BuildUpsertStrategy<TEntity>()
        where TEntity : class
    {
        var upsertStrategyBuilder = new UpsertRepoStrategy<TService, TEntity>();

        AttachDefaults(upsertStrategyBuilder);

        return upsertStrategyBuilder;
    }

    public IGetRepoStrategy<TService, TEntity> BuildGetStrategy<TEntity>()
        where TEntity : class
    {
        var getStrategyBuilder = new GetRepoStrategy<TService, TEntity>();

        AttachDefaults(getStrategyBuilder);

        return getStrategyBuilder;
    }

    public IGetListRepoStrategy<TService, TEntity> BuildGetListStrategy<TEntity>()
        where TEntity : class
    {
        var getListStrategyBuilder = new GetListRepoStrategy<TService, TEntity>();

        AttachDefaults(getListStrategyBuilder);

        return getListStrategyBuilder;
    }

    public IGetPagedRepoStrategy<TService, TEntity> BuildGetPagedStrategy<TEntity>()
        where TEntity : class
    {
        var getPagedStrategyBuilder = new GetPagedRepoStrategy<TService, TEntity>();

        AttachDefaults(getPagedStrategyBuilder);

        return getPagedStrategyBuilder;
    }

    private void AttachDefaults<TBuilder>(IRepoStrategy<TService, TBuilder> strategyBuilder)
        where TBuilder : class, IRepoStrategy<TService, TBuilder>
    {
        AttachDefaultsToBuilder(strategyBuilder);

        if (DefaultEntityDescription != null)
        {
            strategyBuilder.WithEntityDescription(DefaultEntityDescription);
        }
    }
}
