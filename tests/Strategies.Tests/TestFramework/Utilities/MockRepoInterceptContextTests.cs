// <copyright file="MockRepoInterceptContextTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Utilities;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Utilities.Models;
using Shouldly;

public class MockRepoInterceptContextTests
{
    [Fact]
    public void InitialState_ShouldHaveZeroCountsAndEmptyLogs()
    {
        // Arrange & Act
        var context = new MockRepoInterceptContext<TestEntity>();

        // Assert
        context.ShouldSatisfyAllConditions(
            x => x.GetCallCount.ShouldBe(0),
            x => x.GetListCallCount.ShouldBe(0),
            x => x.GetPagedCallCount.ShouldBe(0),
            x => x.CreateCallCount.ShouldBe(0),
            x => x.UpdateCallCount.ShouldBe(0),
            x => x.TotalCallCount.ShouldBe(0),
            x => x.LastGetRequest.ShouldBeNull(),
            x => x.LastGetListRequest.ShouldBeNull(),
            x => x.LastGetPagedRequest.ShouldBeNull(),
            x => x.LastCreateRequest.ShouldBeNull(),
            x => x.LastUpdateRequest.ShouldBeNull(),
            x => x.LastGetResult.ShouldBeNull(),
            x => x.LastGetListResult.ShouldBeNull(),
            x => x.LastGetPagedResult.ShouldBeNull(),
            x => x.LastCreateResult.ShouldBeNull(),
            x => x.LastUpdateResult.ShouldBeNull(),
            x => x.GetResultLog.ShouldBeEmpty(),
            x => x.GetListResultLog.ShouldBeEmpty(),
            x => x.GetPagedResultLog.ShouldBeEmpty(),
            x => x.CreateResultLog.ShouldBeEmpty(),
            x => x.UpdateResultLog.ShouldBeEmpty());
    }

    [Fact]
    public void LastGetResult_WhenSet_UpdatesCountAndLog()
    {
        // Arrange
        var context = new MockRepoInterceptContext<TestEntity>();
        var entity1 = new TestEntity { Id = "1", Name = "One" };
        var entity2 = new TestEntity { Id = "2", Name = "Two" };

        // Act
        context.LastGetResult = entity1;
        context.LastGetResult = entity2;

        // Assert
        context.ShouldSatisfyAllConditions(
            x => x.LastGetResult.ShouldBe(entity2),
            x => x.GetCallCount.ShouldBe(2),
            x => x.TotalCallCount.ShouldBe(2),
            x => x.GetResultLog.ShouldBe([entity1, entity2]));
    }

    [Fact]
    public void LastGetListResult_WhenSet_UpdatesCountAndLog()
    {
        // Arrange
        var context = new MockRepoInterceptContext<TestEntity>();
        var list1 = new List<TestEntity> { new() { Id = "1", Name = "One" } };
        var list2 = new List<TestEntity> { new() { Id = "2", Name = "Two" } };

        // Act
        context.LastGetListResult = list1;
        context.LastGetListResult = list2;

        // Assert
        context.ShouldSatisfyAllConditions(
            x => x.LastGetListResult.ShouldBe(list2),
            x => x.GetListCallCount.ShouldBe(2),
            x => x.TotalCallCount.ShouldBe(2),
            x => x.GetListResultLog.ShouldBe([list1, list2]));
    }

    [Fact]
    public void LastGetPagedResult_WhenSet_UpdatesCountAndLog()
    {
        // Arrange
        var context = new MockRepoInterceptContext<TestEntity>();
        var paged1 = new PagedEntities<TestEntity>([new TestEntity { Id = "1" }], 1, 1, 1, 10);
        var paged2 = new PagedEntities<TestEntity>([new TestEntity { Id = "2" }], 2, 1, 1, 10);

        // Act
        context.LastGetPagedResult = paged1;
        context.LastGetPagedResult = paged2;

        // Assert
        context.ShouldSatisfyAllConditions(
            x => x.LastGetPagedResult.ShouldBe(paged2),
            x => x.GetPagedCallCount.ShouldBe(2),
            x => x.TotalCallCount.ShouldBe(2),
            x => x.GetPagedResultLog.ShouldBe([paged1, paged2]));
    }

    [Fact]
    public void LastCreateResult_WhenSet_UpdatesCountAndLog()
    {
        // Arrange
        var context = new MockRepoInterceptContext<TestEntity>();
        var entity1 = new TestEntity { Id = "1", Name = "One" };
        var entity2 = new TestEntity { Id = "2", Name = "Two" };

        // Act
        context.LastCreateResult = entity1;
        context.LastCreateResult = entity2;

        // Assert
        context.ShouldSatisfyAllConditions(
            x => x.LastCreateResult.ShouldBe(entity2),
            x => x.CreateCallCount.ShouldBe(2),
            x => x.TotalCallCount.ShouldBe(2),
            x => x.CreateResultLog.ShouldBe([entity1, entity2]));
    }

    [Fact]
    public void LastUpdateResult_WhenSet_UpdatesCountAndLog()
    {
        // Arrange
        var context = new MockRepoInterceptContext<TestEntity>();
        var entity1 = new TestEntity { Id = "1", Name = "One" };
        var entity2 = new TestEntity { Id = "2", Name = "Two" };

        // Act
        context.LastUpdateResult = entity1;
        context.LastUpdateResult = entity2;

        // Assert
        context.ShouldSatisfyAllConditions(
            x => x.LastUpdateResult.ShouldBe(entity2),
            x => x.UpdateCallCount.ShouldBe(2),
            x => x.TotalCallCount.ShouldBe(2),
            x => x.UpdateResultLog.ShouldBe([entity1, entity2]));
    }

    [Fact]
    public void MultipleOperations_WhenPerformed_TracksTotalCountAndIndividualCountsAccurately()
    {
        // Arrange
        var context = new MockRepoInterceptContext<TestEntity>();
        var entity = new TestEntity { Id = "1", Name = "One" };
        var list = new List<TestEntity> { entity };
        var paged = new PagedEntities<TestEntity>([entity], 1, 1, 1, 10);

        // Act
        context.LastGetResult = entity;
        context.LastGetListResult = list;
        context.LastGetPagedResult = paged;
        context.LastCreateResult = entity;
        context.LastUpdateResult = entity;

        // Assert
        context.ShouldSatisfyAllConditions(
            x => x.GetCallCount.ShouldBe(1),
            x => x.GetListCallCount.ShouldBe(1),
            x => x.GetPagedCallCount.ShouldBe(1),
            x => x.CreateCallCount.ShouldBe(1),
            x => x.UpdateCallCount.ShouldBe(1),
            x => x.TotalCallCount.ShouldBe(5));
    }

    [Fact]
    public void LastRequests_WhenSet_StoresMockRepoCallInfo()
    {
        // Arrange
        var context = new MockRepoInterceptContext<TestEntity>();
        using var cts = new CancellationTokenSource();
        var callInfo = new MockRepoCallInfo { CancellationToken = cts.Token };

        // Act
        context.LastGetRequest = callInfo;
        context.LastGetListRequest = callInfo;
        context.LastGetPagedRequest = callInfo;
        context.LastCreateRequest = callInfo;
        context.LastUpdateRequest = callInfo;

        // Assert
        context.ShouldSatisfyAllConditions(
            x => x.LastGetRequest.ShouldBe(callInfo),
            x => x.LastGetListRequest.ShouldBe(callInfo),
            x => x.LastGetPagedRequest.ShouldBe(callInfo),
            x => x.LastCreateRequest.ShouldBe(callInfo),
            x => x.LastUpdateRequest.ShouldBe(callInfo));
    }
}
