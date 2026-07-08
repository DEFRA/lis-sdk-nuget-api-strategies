// <copyright file="UpdateRepoStrategy.cs" company="Defra">
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
public sealed class UpdateRepoStrategy<TService, TEntity>
    : RepoStrategyBase<TService, IUpdateRepoStrategy<TService, TEntity>>,
        IUpdateRepoStrategy<TService, TEntity>
    where TService : class
    where TEntity : class
{
    public UpdateRepoStrategy()
    {
        SetParentBuilder(this);
    }

    private IRepoGettable<TEntity>? GettableRepository { get; set; }

    private IRepoUpdatable<TEntity>? UpdateableRepository { get; set; }

    private ILoggableById? Request { get; set; }

    private Expression<Func<TEntity, bool>>? EntityFilter { get; set; }

    private Func<TEntity, Task>? BeforeUpdateAction { get; set; }

    private Action<TEntity>? UpdateAction { get; set; }

    private Func<TEntity, Task>? AfterUpdateAction { get; set; }

    private ExistenceRulesBuilder<TService, TEntity>? ExistenceRulesBuilder { get; set; }

    private ReferenceRulesBuilder<TService>? ReferenceRulesBuilder { get; set; }

    private ConflictRulesBuilder<TService, TEntity>? ConflictRulesBuilder { get; set; }

    private BusinessRulesBuilder<TService, TEntity>? BusinessRulesBuilder { get; set; }

    public IUpdateRepoStrategy<TService, TEntity> WithRepository<TRepository>(TRepository repository)
        where TRepository : IRepoGettable<TEntity>, IRepoUpdatable<TEntity>
    {
        GettableRepository = repository;
        UpdateableRepository = repository;
        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithRequest(ILoggableById request)
    {
        Request = request;
        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)
    {
        EntityFilter = entityFilter;
        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithExistenceRules(
        Action<IExistenceRulesBuilder<TService, TEntity>> builder)
    {
        ExistenceRulesBuilder = new ExistenceRulesBuilder<TService, TEntity>();

        builder(ExistenceRulesBuilder);

        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithReferenceRules(
        Action<IReferenceRulesBuilder<TService>> builder)
    {
        ReferenceRulesBuilder = new ReferenceRulesBuilder<TService>();

        builder(ReferenceRulesBuilder);

        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithConflictRules(
        Action<IConflictRulesBuilder<TService, TEntity>> builder)
    {
        ConflictRulesBuilder = new ConflictRulesBuilder<TService, TEntity>();

        builder(ConflictRulesBuilder);

        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithBusinessRules(
        Action<IBusinessRulesBuilder<TService, TEntity>> builder)
    {
        BusinessRulesBuilder = new BusinessRulesBuilder<TService, TEntity>();

        builder(BusinessRulesBuilder);

        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithBeforeUpdate(Func<TEntity, Task> beforeUpdateAction)
    {
        BeforeUpdateAction = beforeUpdateAction;

        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithUpdate(Action<TEntity> updateAction)
    {
        UpdateAction = updateAction;

        return this;
    }

    public IUpdateRepoStrategy<TService, TEntity> WithAfterUpdate(Func<TEntity, Task> afterUpdateAction)
    {
        AfterUpdateAction = afterUpdateAction;

        return this;
    }

    public async Task<TEntity> Execute()
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

        if (UpdateableRepository == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.UpdatableRepositoryRequired);
        }

        if (EntityDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (Request == null || EntityFilter == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
        }

        if (UpdateAction == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.UpdateActionRequired);
        }

        EnsureOperatorHasRequiredPermissions();

        LogExecutingActionWithId(Request.GetLoggableId());

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        if (ReferenceRulesBuilder != null)
        {
            await ReferenceRulesBuilder.Validate(ActionDescription, EntityDescription, Logger, CancellationToken.Value);
        }

        var entityToUpdate = await GettableRepository.GetSingle(EntityFilter, CancellationToken.Value);

        if (entityToUpdate == null)
        {
            LogEntityWithIdNotFound(Request.GetLoggableId());

            throw new EntityNotFoundException($"{EntityDescription} not found");
        }

        ExistenceRulesBuilder?.Validate(Request, entityToUpdate, EntityDescription, Logger);
        ConflictRulesBuilder?.Validate(Request, entityToUpdate, ActionDescription, EntityDescription, Logger);
        BusinessRulesBuilder?.Validate(Request, entityToUpdate, ActionDescription, EntityDescription, Logger);

        if (BeforeUpdateAction != null)
        {
            await BeforeUpdateAction.Invoke(entityToUpdate);
        }

        UpdateAction(entityToUpdate);

        var updatedEntity = await UpdateableRepository.Update(entityToUpdate, CancellationToken.Value);

        if (AfterUpdateAction != null)
        {
            await AfterUpdateAction.Invoke(updatedEntity);
        }

        await InvokeAfterExecuteAction();

        LogSuccessfullyExecutedActionWithId(Request.GetLoggableId());

        return updatedEntity;
    }

    public async Task<TResult> ExecuteAndMap<TResult>(Func<TEntity, TResult> map)
        where TResult : class
    {
        var entity = await Execute();

        return map(entity);
    }
}
