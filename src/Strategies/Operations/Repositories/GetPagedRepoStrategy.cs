// <copyright file="GetPagedRepoStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;

using System.Linq.Expressions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Responses.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;

public sealed class GetPagedRepoStrategy<TService, TEntity> : RepoStrategyBase<TService,
    IGetPagedRepoStrategy<TService, TEntity>>, IGetPagedRepoStrategy<TService, TEntity>
    where TService : class
    where TEntity : class
{
    public GetPagedRepoStrategy()
    {
        SetParentBuilder(this);
    }

    private IRepoPageable<TEntity>? PageableRepository { get; set; }

    private PagedQuery? Request { get; set; }

    private Expression<Func<TEntity, bool>>? EntityFilter { get; set; }

    public IGetPagedRepoStrategy<TService, TEntity> WithRepository<TRepository>(TRepository repository)
        where TRepository : IRepoPageable<TEntity>
    {
        PageableRepository = repository;
        return this;
    }

    public IGetPagedRepoStrategy<TService, TEntity> WithRequest<TRequest>(TRequest request)
        where TRequest : PagedQuery
    {
        Request = request;
        return this;
    }

    public IGetPagedRepoStrategy<TService, TEntity> WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)
    {
        EntityFilter = entityFilter;
        return this;
    }

    public async Task<PagedEntities<TEntity>> Execute<TOrderBy>(Expression<Func<TEntity, TOrderBy>> orderBy)
    {
        return await ExecuteAndTransform(pagedEntities => pagedEntities, orderBy);
    }

    public async Task<PagedResults<TResult>> ExecuteAndMap<TResult, TOrderBy>(
        Func<TEntity, TResult> map,
        Expression<Func<TEntity, TOrderBy>> orderBy)
        where TResult : class
    {
        return await ExecuteAndTransform(pagedEntities => pagedEntities.ToPagedResults(map), orderBy);
    }

    public async Task<TResult> ExecuteAndTransform<TResult, TOrderBy>(
        Func<PagedEntities<TEntity>, TResult> transform,
        Expression<Func<TEntity, TOrderBy>> orderBy)
        where TResult : class
    {
        if (CancellationToken == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.CancellationTokenRequired);
        }

        if (TargetDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
        }

        if (PageableRepository == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.PageableRepositoryRequired);
        }

        if (Request == null || EntityFilter == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
        }

        EnsureOperatorHasRequiredPermissions();

        LogExecutingAction();

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        var pagedEntities = await PageableRepository.GetPaged(
            EntityFilter,
            Request.PageNumber,
            Request.PageSize,
            orderBy,
            Request.OrderByDescending ?? false,
            CancellationToken.Value);

        var transformedResults = transform(pagedEntities);

        await InvokeAfterExecuteAction();

        LogSuccessfullyExecutedAction();

        return transformedResults;
    }
}
