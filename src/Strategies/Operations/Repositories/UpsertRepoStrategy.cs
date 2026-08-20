// <copyright file="UpsertRepoStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

[ExcludeFromCodeCoverage]
public sealed class UpsertRepoStrategy<TService, TEntity>
    : RepoStrategyBase<TService, IUpsertRepoStrategy<TService, TEntity>>,
        IUpsertRepoStrategy<TService, TEntity>
    where TService : class
    where TEntity : class
{
    public UpsertRepoStrategy()
    {
        SetParentBuilder(this);
    }

    private IRepoGettable<TEntity>? GettableRepository { get; set; }

    private IRepoCreatable<TEntity>? CreatableRepository { get; set; }

    private IRepoUpdatable<TEntity>? UpdateableRepository { get; set; }

    private ILoggableById? Request { get; set; }

    private Expression<Func<TEntity, bool>>? EntityFilter { get; set; }

    private Func<TEntity>? CreateAction { get; set; }

    private Action<TEntity>? UpdateAction { get; set; }

    private Func<TEntity, Task>? AfterCreateAction { get; set; }

    private Func<TEntity, Task>? AfterUpdateAction { get; set; }

    private ExistenceRulesBuilder<TService, TEntity>? ExistenceRulesBuilder { get; set; }

    private ReferenceRulesBuilder<TService>? ReferenceRulesBuilder { get; set; }

    private ConflictRulesBuilder<TService, TEntity>? ConflictRulesBuilder { get; set; }

    private BusinessRulesBuilder<TService, TEntity>? BusinessRulesBuilder { get; set; }

    public IUpsertRepoStrategy<TService, TEntity> WithRepository<TRepository>(TRepository repository)
        where TRepository : IRepoGettable<TEntity>, IRepoCreatable<TEntity>, IRepoUpdatable<TEntity>
    {
        GettableRepository = repository;
        UpdateableRepository = repository;
        CreatableRepository = repository;
        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithRequest(ILoggableById request)
    {
        Request = request;
        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithEntityFilter(Expression<Func<TEntity, bool>> entityFilter)
    {
        EntityFilter = entityFilter;
        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithExistenceRules(
        Action<IExistenceRulesBuilder<TService, TEntity>> builder)
    {
        ExistenceRulesBuilder = new ExistenceRulesBuilder<TService, TEntity>();

        builder(ExistenceRulesBuilder);

        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithReferenceRules(
        Action<IReferenceRulesBuilder<TService>> builder)
    {
        ReferenceRulesBuilder = new ReferenceRulesBuilder<TService>();

        builder(ReferenceRulesBuilder);

        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithConflictRules(
        Action<IConflictRulesBuilder<TService, TEntity>> builder)
    {
        ConflictRulesBuilder = new ConflictRulesBuilder<TService, TEntity>();

        builder(ConflictRulesBuilder);

        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithBusinessRules(
        Action<IBusinessRulesBuilder<TService, TEntity>> builder)
    {
        BusinessRulesBuilder = new BusinessRulesBuilder<TService, TEntity>();

        builder(BusinessRulesBuilder);

        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithCreate(Func<TEntity> createAction)
    {
        CreateAction = createAction;

        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithUpdate(Action<TEntity> updateAction)
    {
        UpdateAction = updateAction;

        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithAfterCreate(Func<TEntity, Task> afterCreateAction)
    {
        AfterCreateAction = afterCreateAction;

        return this;
    }

    public IUpsertRepoStrategy<TService, TEntity> WithAfterUpdate(Func<TEntity, Task> afterUpdateAction)
    {
        AfterUpdateAction = afterUpdateAction;

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
            throw new InvalidOperationException(StrategyConstants.Errors.LoggerRequired);
        }

        if (CancellationToken == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.CancellationTokenRequired);
        }

        if (GettableRepository == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.GettableRepositoryRequired);
        }

        if (CreatableRepository == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.CreatableRepositoryRequired);
        }

        if (UpdateableRepository == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.UpdatableRepositoryRequired);
        }

        if (TargetDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (Request == null || EntityFilter == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.RequestAndEntityFilterRequired);
        }

        if (CreateAction == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.CreateActionRequired);
        }

        if (UpdateAction == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.UpdateActionRequired);
        }

        EnsureOperatorHasRequiredPermissions();

        LogExecutingAction();

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        if (ReferenceRulesBuilder != null)
        {
            await ReferenceRulesBuilder.Validate(ActionDescription, TargetDescription, Logger, CancellationToken.Value);
        }

        var existingEntity = await GettableRepository.GetSingle(EntityFilter, CancellationToken.Value);

        if (existingEntity != null)
        {
            ExistenceRulesBuilder?.Validate(Request, existingEntity, TargetDescription, Logger);
            ConflictRulesBuilder?.Validate(Request, existingEntity, ActionDescription, TargetDescription, Logger);
            BusinessRulesBuilder?.Validate(Request, existingEntity, ActionDescription, TargetDescription, Logger);

            UpdateAction(existingEntity);

            var updatedEntity = await UpdateableRepository.Update(existingEntity, CancellationToken.Value);

            if (AfterUpdateAction != null)
            {
                await AfterUpdateAction.Invoke(updatedEntity);
            }

            var mappedUpdatedEntity = map(updatedEntity);

            await InvokeAfterExecuteAction();

            LogSuccessfullyExecutedAction();

            return mappedUpdatedEntity;
        }

        var entityToCreate = CreateAction();

        var createdEntity = await CreatableRepository.Create(entityToCreate, CancellationToken.Value);

        if (AfterCreateAction != null)
        {
            await AfterCreateAction.Invoke(createdEntity);
        }

        var mappedCreatedEntity = map(createdEntity);

        await InvokeAfterExecuteAction();

        LogSuccessfullyExecutedAction();

        return mappedCreatedEntity;
    }
}
