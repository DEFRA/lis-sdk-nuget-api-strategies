// <copyright file="GetListRepoStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;

[ExcludeFromCodeCoverage]
public sealed class GetListRepoStrategy<TService, TEntity>
    : RepoStrategyBase<TService, IGetListRepoStrategy<TService, TEntity>>,
        IGetListRepoStrategy<TService, TEntity>
    where TService : class
    where TEntity : class
{
    public GetListRepoStrategy()
    {
        SetParentBuilder(this);
    }

    private IRepoListable<TEntity>? ListableRepository { get; set; }

    private Expression<Func<TEntity, bool>>? EntityFilter { get; set; }

    public IGetListRepoStrategy<TService, TEntity> WithRepository<TRepository>(TRepository repository)
        where TRepository : IRepoListable<TEntity>
    {
        ListableRepository = repository;
        return this;
    }

    public IGetListRepoStrategy<TService, TEntity> WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)
    {
        EntityFilter = entityFilter;
        return this;
    }

    public async Task<List<TEntity>> Execute()
    {
        return await ExecuteAndTransform(entities => entities.ToList());
    }

    public async Task<List<TResult>> ExecuteAndMap<TResult>(Func<TEntity, TResult> map)
        where TResult : class
    {
        return await ExecuteAndTransform(entities => entities.Select(map).ToList());
    }

    public async Task<TResult> ExecuteAndTransform<TResult>(Func<List<TEntity>, TResult> transform)
        where TResult : class
    {
        if (CancellationToken == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.CancellationTokenRequired);
        }

        if (ListableRepository == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.ListableRepositoryRequired);
        }

        if (EntityFilter == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.EntityFilterRequired);
        }

        EnsureOperatorHasRequiredPermissions();

        LogExecutingAction();

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        var entities = await ListableRepository.GetList(EntityFilter, CancellationToken.Value);

        var transformedResults = transform(entities);

        await InvokeAfterExecuteAction();

        LogSuccessfullyExecutedAction();

        return transformedResults;
    }
}
