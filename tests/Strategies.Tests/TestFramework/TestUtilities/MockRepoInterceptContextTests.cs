// <copyright file="MockRepoInterceptContextTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestUtilities;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData;
using Shouldly;

public class MockRepoInterceptContextTests
{
    [Fact]
    public void InitialState_ShouldHaveZeroCountsAndEmptyLogs()
    {
        // Arrange & Act
        var context = new MockRepoInterceptContext<TestEntity>();

        // Assert
        context.GetCallCount.ShouldBe(0);
        context.GetListCallCount.ShouldBe(0);
        context.GetPagedCallCount.ShouldBe(0);
        context.CreateCallCount.ShouldBe(0);
        context.UpdateCallCount.ShouldBe(0);
        context.TotalCallCount.ShouldBe(0);
        context.LastGetResult.ShouldBeNull();
        context.LastGetListResult.ShouldBeNull();
        context.LastGetPagedResult.ShouldBeNull();
        context.LastCreateResult.ShouldBeNull();
        context.LastUpdateResult.ShouldBeNull();
        context.GetResultLog.ShouldBeEmpty();
        context.GetListResultLog.ShouldBeEmpty();
        context.GetPagedResultLog.ShouldBeEmpty();
        context.CreateResultLog.ShouldBeEmpty();
        context.UpdateResultLog.ShouldBeEmpty();
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
        context.LastGetResult.ShouldBe(entity2);
        context.GetCallCount.ShouldBe(2);
        context.TotalCallCount.ShouldBe(2);
        context.GetResultLog.ShouldBe([entity1, entity2]);
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
        context.LastGetListResult.ShouldBe(list2);
        context.GetListCallCount.ShouldBe(2);
        context.TotalCallCount.ShouldBe(2);
        context.GetListResultLog.ShouldBe([list1, list2]);
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
        context.LastGetPagedResult.ShouldBe(paged2);
        context.GetPagedCallCount.ShouldBe(2);
        context.TotalCallCount.ShouldBe(2);
        context.GetPagedResultLog.ShouldBe([paged1, paged2]);
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
        context.LastCreateResult.ShouldBe(entity2);
        context.CreateCallCount.ShouldBe(2);
        context.TotalCallCount.ShouldBe(2);
        context.CreateResultLog.ShouldBe([entity1, entity2]);
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
        context.LastUpdateResult.ShouldBe(entity2);
        context.UpdateCallCount.ShouldBe(2);
        context.TotalCallCount.ShouldBe(2);
        context.UpdateResultLog.ShouldBe([entity1, entity2]);
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
        context.GetCallCount.ShouldBe(1);
        context.GetListCallCount.ShouldBe(1);
        context.GetPagedCallCount.ShouldBe(1);
        context.CreateCallCount.ShouldBe(1);
        context.UpdateCallCount.ShouldBe(1);
        context.TotalCallCount.ShouldBe(5);
    }
}
