// <copyright file="GetRepoStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

[ExcludeFromCodeCoverage]
public sealed class GetRepoStrategy<TService, TEntity>
    : RepoStrategyBase<TService, IGetRepoStrategy<TService, TEntity>>, IGetRepoStrategy<TService, TEntity>
    where TService : class
    where TEntity : class
{
    public GetRepoStrategy()
    {
        SetParentBuilder(this);
    }

    private IRepoGettable<TEntity>? GettableRepository { get; set; }

    private ILoggableById? Request { get; set; }

    private Expression<Func<TEntity, bool>>? EntityFilter { get; set; }

    private ExistenceRulesBuilder<TService, TEntity>? ExistenceRulesBuilder { get; set; }

    public IGetRepoStrategy<TService, TEntity> WithRepository<TRepository>(TRepository repository)
        where TRepository : IRepoGettable<TEntity>, IRepoUpdatable<TEntity>
    {
        GettableRepository = repository;
        return this;
    }

    public IGetRepoStrategy<TService, TEntity> WithRequest(ILoggableById request)
    {
        Request = request;
        return this;
    }

    public IGetRepoStrategy<TService, TEntity> WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)
    {
        EntityFilter = entityFilter;
        return this;
    }

    public IGetRepoStrategy<TService, TEntity> WithExistenceRules(
        Action<IExistenceRulesBuilder<TService, TEntity>> builder)
    {
        ExistenceRulesBuilder = new ExistenceRulesBuilder<TService, TEntity>();

        builder(ExistenceRulesBuilder);

        return this;
    }

    public async Task<TEntity> Execute()
    {
        return await ExecuteAndMap(entity => entity);
    }

    public async Task<TResult> ExecuteAndMap<TResult>(Func<TEntity, TResult> map)
        where TResult : class
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.LoggerRequired);
        }

        if (CancellationToken == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.CancellationTokenRequired);
        }

        if (GettableRepository == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.GettableRepositoryRequired);
        }

        if (EntityDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
        }

        if (Request == null || EntityFilter == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
        }

        EnsureOperatorHasRequiredPermissions();

        LogExecutingActionWithId(Request.GetLoggableId());

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        var entity = await GettableRepository.GetSingle(EntityFilter, CancellationToken.Value);

        if (entity == null)
        {
            LogEntityWithIdNotFound(Request.GetLoggableId());

            throw new EntityNotFoundException($"{EntityDescription} not found");
        }

        ExistenceRulesBuilder?.Validate(Request, entity, EntityDescription, Logger);

        var mappedEntity = map(entity);

        await InvokeAfterExecuteAction();

        LogSuccessfullyExecutedActionWithId(Request.GetLoggableId());

        return mappedEntity;
    }
}
