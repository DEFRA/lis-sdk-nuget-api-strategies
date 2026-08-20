// <copyright file="CreateRepoStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Rules.Builders;

[ExcludeFromCodeCoverage]
public sealed class CreateRepoStrategy<TService, TEntity>
    : RepoStrategyBase<TService, ICreateRepoStrategy<TService, TEntity>>,
        ICreateRepoStrategy<TService, TEntity>
    where TService : class
    where TEntity : class
{
    public CreateRepoStrategy()
    {
        SetParentBuilder(this);
    }

    private IRepoCreatable<TEntity>? CreatableRepository { get; set; }

    private Func<TEntity>? CreateAction { get; set; }

    private Func<TEntity, Task>? AfterCreateAction { get; set; }

    private ReferenceRulesBuilder<TService>? ReferenceRulesBuilder { get; set; }

    public ICreateRepoStrategy<TService, TEntity> WithRepository(
        IRepoCreatable<TEntity> repository)
    {
        CreatableRepository = repository;
        return this;
    }

    public ICreateRepoStrategy<TService, TEntity> WithReferenceRules(
        Action<IReferenceRulesBuilder<TService>> builder)
    {
        ReferenceRulesBuilder = new ReferenceRulesBuilder<TService>();

        builder(ReferenceRulesBuilder);

        return this;
    }

    public ICreateRepoStrategy<TService, TEntity> WithCreate(Func<TEntity> createAction)
    {
        CreateAction = createAction;

        return this;
    }

    public ICreateRepoStrategy<TService, TEntity> WithAfterCreate(Func<TEntity, Task> afterCreateAction)
    {
        AfterCreateAction = afterCreateAction;

        return this;
    }

    public async Task<TEntity> Execute()
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.LoggerRequired);
        }

        if (CancellationToken == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.CancellationTokenRequired);
        }

        if (CreatableRepository == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.CreatableRepositoryRequired);
        }

        if (TargetDescription == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.PrimaryEntityDescriptionRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (CreateAction == null)
        {
            throw new InvalidOperationException(RepoStrategyConstants.Errors.CreateActionRequired);
        }

        EnsureOperatorHasRequiredPermissions();

        LogExecutingAction();

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        if (ReferenceRulesBuilder != null)
        {
            await ReferenceRulesBuilder.Validate(ActionDescription, TargetDescription, Logger, CancellationToken.Value);
        }

        var entityToCreate = CreateAction();

        var createdEntity = await CreatableRepository.Create(entityToCreate, CancellationToken.Value);

        if (AfterCreateAction != null)
        {
            await AfterCreateAction.Invoke(createdEntity);
        }

        await InvokeAfterExecuteAction();

        LogSuccessfullyExecutedAction();

        return createdEntity;
    }

    public async Task<TResult> ExecuteAndMap<TResult>(Func<TEntity, TResult> map)
        where TResult : class
    {
        var entity = await Execute();

        return map(entity);
    }
}
