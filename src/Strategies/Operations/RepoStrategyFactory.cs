// <copyright file="RepoStrategyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;
using Microsoft.Extensions.Logging;

public sealed class RepoStrategyFactory<TService> : IRepoStrategyFactory<TService>
    where TService : class
{
    private ILogger<TService>? DefaultLogger { get; set; }

    private IOperatorContext? DefaultOperatorContext { get; set; }

    private string? DefaultEntityDescription { get; set; }

    public IRepoStrategyFactory<TService> WithDefaultLogger(ILogger<TService> logger)
    {
        this.DefaultLogger = logger;
        return this;
    }

    public IRepoStrategyFactory<TService> WithDefaultOperatorContext(IOperatorContext operatorContext)
    {
        this.DefaultOperatorContext = operatorContext;
        return this;
    }

    public IRepoStrategyFactory<TService> WithDefaultEntityDescription(string entityDescription)
    {
        this.DefaultEntityDescription = entityDescription;
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
        if (DefaultLogger != null)
        {
            strategyBuilder.WithLogger(DefaultLogger);
        }

        if (DefaultOperatorContext != null)
        {
            strategyBuilder.WithOperatorContext(DefaultOperatorContext);
        }

        if (DefaultEntityDescription != null)
        {
            strategyBuilder.WithEntityDescription(DefaultEntityDescription);
        }
    }
}
