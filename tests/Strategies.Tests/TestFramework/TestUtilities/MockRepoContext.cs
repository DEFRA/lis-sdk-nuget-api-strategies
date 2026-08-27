// <copyright file="MockRepoContext.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestUtilities;

using System.Linq.Expressions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Pagination;
using NSubstitute;
using NSubstitute.Core;

public class MockRepoContext<TEntity>
    where TEntity : class
{
    private readonly IRepoCapability repository;
    private Guid? nextCreateEntityId;
    private Func<TEntity, TEntity>? createResultAction;

    private MockRepoContext(IRepoCapability repository)
    {
        this.repository = repository;

        WithData([]);
    }

    public MockRepoInterceptContext<TEntity> Calls { get; } = new();

    public static MockRepoContext<TEntity> CreateFor(IRepoCapability repository)
    {
        return new MockRepoContext<TEntity>(repository);
    }

    public MockRepoContext<TEntity> WithData(TEntity[] entities)
    {
        if (repository is IRepoGettable<TEntity> gettableRepository)
        {
            gettableRepository.GetSingle(Arg.Any<Expression<Func<TEntity, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Func<CallInfo, TEntity?>)SetFilteredDataForGetSingle);
        }

        if (repository is IRepoListable<TEntity> listableRepository)
        {
            listableRepository.GetList(Arg.Any<Expression<Func<TEntity, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Func<CallInfo, List<TEntity>>)SetFilteredDataForGetList);
        }

        if (repository is IRepoPageable<TEntity> pageableRepository)
        {
            pageableRepository.GetPaged(
                    Arg.Any<Expression<Func<TEntity, bool>>>(),
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<Expression<Func<TEntity, string>>>(),
                    Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns((Func<CallInfo, PagedEntities<TEntity>>)SetFilteredDataForGetPaged);
        }

        if (repository is IRepoCreatable<TEntity> creatableRepository)
        {
            creatableRepository.Create(Arg.Any<TEntity>(), Arg.Any<CancellationToken>())
                .Returns(SetReturnDataForCreate);
        }

        if (repository is IRepoUpdatable<TEntity> updatableRepository)
        {
            updatableRepository.Update(Arg.Any<TEntity>(), Arg.Any<CancellationToken>())
                .Returns(SetReturnDataForUpdate);
        }

        return this;

        TEntity? SetFilteredDataForGetSingle(CallInfo callInfo)
        {
            var result = MockGetSingleEntityResult(callInfo, entities);

            Calls.LastGetResult = result;

            return result;
        }

        List<TEntity> SetFilteredDataForGetList(CallInfo callInfo)
        {
            var result = MockGetListEntityResults(callInfo, entities);

            Calls.LastGetListResult = result;

            return result;
        }

        PagedEntities<TEntity> SetFilteredDataForGetPaged(CallInfo callInfo)
        {
            var result = MockGetPagedResults(callInfo, entities);

            Calls.LastGetPagedResult = result;

            return result;
        }

        TEntity SetReturnDataForCreate(CallInfo callInfo)
        {
            var result = MockCreateEntityResultFromInputEntity(callInfo);

            Calls.LastCreateResult = result;

            return result;
        }

        TEntity SetReturnDataForUpdate(CallInfo callInfo)
        {
            var result = MockUpdateEntityResultFromInputEntity(callInfo);

            Calls.LastUpdateResult = result;

            return result;
        }
    }

    public MockRepoContext<TEntity> WithNextCreateEntityId(Guid id)
    {
        nextCreateEntityId = id;

        return this;
    }

    public MockRepoContext<TEntity> WithCreateResult(Func<TEntity, TEntity> action)
    {
        createResultAction = action;

        return this;
    }

    private static TEntity? MockGetSingleEntityResult(CallInfo callInfo, params TEntity[] filterableEntities)
    {
        var actualFilter = (Expression<Func<TEntity, bool>>)callInfo.Args()[0];
        var compiledFilter = actualFilter.Compile();

        var filteredEntity = filterableEntities.SingleOrDefault(compiledFilter);

        return filteredEntity;
    }

    private static List<TEntity> MockGetListEntityResults(
        CallInfo callInfo,
        params TEntity[] filterableEntities)
    {
        var actualFilter = (Expression<Func<TEntity, bool>>)callInfo.Args()[0];
        var compiledFilter = actualFilter.Compile();

        var filteredEntities = filterableEntities.Where(compiledFilter).ToList();

        return filteredEntities;
    }

    private static PagedEntities<TEntity> MockGetPagedResults(
        CallInfo callInfo,
        params TEntity[] filterableEntities)
    {
        var actualFilter = (Expression<Func<TEntity, bool>>)callInfo.Args()[0];
        var actualOrderBy = (Expression<Func<TEntity, string>>)callInfo.Args()[3];

        var pageNumber = (int)callInfo.Args()[1];
        var pageSize = (int)callInfo.Args()[2];
        var orderByDescending = (bool)callInfo.Args()[4];

        var compiledFilter = actualFilter.Compile();
        var compiledOrderBy = actualOrderBy.Compile();

        var filteredEntities = filterableEntities.Where(compiledFilter);

        var orderedEntities =
            (orderByDescending
                ? filteredEntities.OrderByDescending(compiledOrderBy)
                : filteredEntities.OrderBy(compiledOrderBy)).ToList();

        var totalCount = orderedEntities.Count;
        var totalPages = (totalCount + pageSize - 1) / pageSize;

        var pagedEntities = orderedEntities
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedEntities<TEntity>(pagedEntities, totalCount, totalPages, pageNumber, pageSize);
    }

    private static TEntity MockUpdateEntityResultFromInputEntity(CallInfo callInfo)
    {
        var entityToUpdate = (TEntity)callInfo.Args()[0];

        return entityToUpdate;
    }

    private TEntity MockCreateEntityResultFromInputEntity(CallInfo callInfo)
    {
        var entityToCreate = (TEntity)callInfo.Args()[0];

        var idProperty = typeof(TEntity).GetProperty(
            "Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (idProperty != null && idProperty.PropertyType == typeof(Guid))
        {
            var currentIdValue = (Guid?)idProperty.GetValue(entityToCreate);

            if (currentIdValue == null || currentIdValue == Guid.Empty)
            {
                var newIdValue = GetCreateEntityId();

                if (idProperty.CanWrite)
                {
                    idProperty.SetValue(entityToCreate, newIdValue);
                }
                else
                {
                    throw new InvalidOperationException("The id property could not be set");
                }
            }
        }

        if (createResultAction != null)
        {
            entityToCreate = createResultAction(entityToCreate);
        }

        return entityToCreate;
    }

    private Guid GetCreateEntityId()
    {
        if (!nextCreateEntityId.HasValue)
        {
            return Guid.NewGuid();
        }

        var result = nextCreateEntityId.Value;

        nextCreateEntityId = null;

        return result;
    }
}
