// <copyright file="MockRepoContextTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Utilities;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using NSubstitute;
using Shouldly;

public class MockRepoContextTests
{
    private readonly ITestRepository repository = Substitute.For<ITestRepository>();

    [Fact]
    public void CreateFor_WhenBareCapability_InitializesWithoutErrors()
    {
        // Arrange
        var bareRepo = Substitute.For<IRepoCapability>();

        // Act
        var context = MockRepoContext<TestEntity>.CreateFor(bareRepo);

        // Assert
        context.ShouldNotBeNull();
        context.Calls.ShouldNotBeNull();
        context.Calls.TotalCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetSingle_WhenMatchingEntityExists_ReturnsEntityAndUpdatesCalls()
    {
        // Arrange
        var entity1 = new TestEntity { Id = "1", Name = "One" };
        var entity2 = new TestEntity { Id = "2", Name = "Two" };
        var context = MockRepoContext<TestEntity>.CreateFor(repository).WithData([entity1, entity2]);

        // Act
        var result = await repository.GetSingle(x => x.Id == "2", CancellationToken.None);

        // Assert
        result.ShouldBe(entity2);
        context.Calls.GetCallCount.ShouldBe(1);
        context.Calls.LastGetResult.ShouldBe(entity2);
    }

    [Fact]
    public async Task GetSingle_WhenNoMatchingEntity_ReturnsNullAndUpdatesCalls()
    {
        // Arrange
        var entity1 = new TestEntity { Id = "1", Name = "One" };
        var context = MockRepoContext<TestEntity>.CreateFor(repository).WithData([entity1]);

        // Act
        var result = await repository.GetSingle(x => x.Id == "999", CancellationToken.None);

        // Assert
        result.ShouldBeNull();
        context.Calls.GetCallCount.ShouldBe(1);
        context.Calls.LastGetResult.ShouldBeNull();
    }

    [Fact]
    public async Task GetList_WhenCalled_ReturnsMatchingEntitiesAndUpdatesCalls()
    {
        // Arrange
        var entity1 = new TestEntity { Id = "1", Name = "Alpha" };
        var entity2 = new TestEntity { Id = "2", Name = "Beta" };
        var entity3 = new TestEntity { Id = "3", Name = "Alpha" };
        var context = MockRepoContext<TestEntity>.CreateFor(repository).WithData([entity1, entity2, entity3]);

        // Act
        var result = await repository.GetList(x => x.Name == "Alpha", CancellationToken.None);

        // Assert
        result.ShouldBe([entity1, entity3]);
        context.Calls.GetListCallCount.ShouldBe(1);
        context.Calls.LastGetListResult.ShouldBe([entity1, entity3]);
    }

    [Fact]
    public async Task GetPaged_WhenAscending_OrdersAndPaginatesCorrectly()
    {
        // Arrange
        var entity1 = new TestEntity { Id = "1", Name = "C" };
        var entity2 = new TestEntity { Id = "2", Name = "A" };
        var entity3 = new TestEntity { Id = "3", Name = "B" };
        var entity4 = new TestEntity { Id = "4", Name = "D" };
        var entity5 = new TestEntity { Id = "5", Name = "E" };
        var context = MockRepoContext<TestEntity>.CreateFor(repository)
            .WithData([entity1, entity2, entity3, entity4, entity5]);

        // Act
        var page1 = await repository.GetPaged(
            x => true,
            1,
            2,
            x => x.Name,
            false,
            CancellationToken.None);

        var page2 = await repository.GetPaged(
            x => true,
            2,
            2,
            x => x.Name,
            false,
            CancellationToken.None);

        // Assert
        page1.ShouldSatisfyAllConditions(
            x => x.TotalCount.ShouldBe(5),
            x => x.TotalPages.ShouldBe(3),
            x => x.PageNumber.ShouldBe(1),
            x => x.PageSize.ShouldBe(2),
            x => x.Items.Select(item => item.Name).ShouldBe(["A", "B"]));

        page2.Items.Select(x => x.Name).ShouldBe(["C", "D"]);
        context.Calls.GetPagedCallCount.ShouldBe(2);
        context.Calls.LastGetPagedResult.ShouldBe(page2);
    }

    [Fact]
    public async Task GetPaged_WhenDescending_OrdersDescendingCorrectly()
    {
        // Arrange
        var entity1 = new TestEntity { Id = "1", Name = "A" };
        var entity2 = new TestEntity { Id = "2", Name = "B" };
        var entity3 = new TestEntity { Id = "3", Name = "C" };
        var context = MockRepoContext<TestEntity>.CreateFor(repository).WithData([entity1, entity2, entity3]);

        // Act
        var result = await repository.GetPaged(
            x => true,
            1,
            2,
            x => x.Name,
            true,
            CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            x => x.TotalCount.ShouldBe(3),
            x => x.TotalPages.ShouldBe(2),
            x => x.Items.Select(item => item.Name).ShouldBe(["C", "B"]));
        context.Calls.GetPagedCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Update_WhenCalled_ReturnsInputEntityAndUpdatesCalls()
    {
        // Arrange
        var entity = new TestEntity { Id = "1", Name = "Updated" };
        var context = MockRepoContext<TestEntity>.CreateFor(repository);

        // Act
        var result = await repository.Update(entity, CancellationToken.None);

        // Assert
        result.ShouldBe(entity);
        context.Calls.UpdateCallCount.ShouldBe(1);
        context.Calls.LastUpdateResult.ShouldBe(entity);
    }

    [Fact]
    public async Task Create_WithStringId_ReturnsEntityAndUpdatesCalls()
    {
        // Arrange
        var entity = new TestEntity { Id = "custom-id", Name = "Created" };
        var context = MockRepoContext<TestEntity>.CreateFor(repository);

        // Act
        var result = await repository.Create(entity, CancellationToken.None);

        // Assert
        result.ShouldBe(entity);
        result.Id.ShouldBe("custom-id");
        context.Calls.CreateCallCount.ShouldBe(1);
        context.Calls.LastCreateResult.ShouldBe(entity);
    }

    [Fact]
    public async Task Create_WithGuidEntityEmptyId_AutoGeneratesGuidId()
    {
        // Arrange
        var guidRepo = Substitute.For<IRepoCreatable<GuidEntity>, IRepoCapability>();
        var entity = new GuidEntity { Id = Guid.Empty, Name = "AutoId" };
        var context = MockRepoContext<GuidEntity>.CreateFor(guidRepo);

        // Act
        var result = await guidRepo.Create(entity, CancellationToken.None);

        // Assert
        result.Id.ShouldNotBe(Guid.Empty);
        context.Calls.CreateCallCount.ShouldBe(1);
        context.Calls.LastCreateResult.ShouldBe(result);
    }

    [Fact]
    public async Task Create_WithGuidEntityExistingId_DoesNotOverwriteId()
    {
        // Arrange
        var guidRepo = Substitute.For<IRepoCreatable<GuidEntity>, IRepoCapability>();
        var existingGuid = Guid.NewGuid();
        var entity = new GuidEntity { Id = existingGuid, Name = "ExistingId" };
        var context = MockRepoContext<GuidEntity>.CreateFor(guidRepo);

        // Act
        var result = await guidRepo.Create(entity, CancellationToken.None);

        // Assert
        result.Id.ShouldBe(existingGuid);
        context.Calls.CreateCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Create_WithNextCreateEntityId_UsesSpecifiedGuidThenFallsBackToGeneratedGuid()
    {
        // Arrange
        var guidRepo = Substitute.For<IRepoCreatable<GuidEntity>, IRepoCapability>();
        var specifiedGuid = Guid.NewGuid();
        var entity1 = new GuidEntity { Id = Guid.Empty, Name = "First" };
        var entity2 = new GuidEntity { Id = Guid.Empty, Name = "Second" };
        var context = MockRepoContext<GuidEntity>.CreateFor(guidRepo).WithNextCreateEntityId(specifiedGuid);

        // Act
        var result1 = await guidRepo.Create(entity1, CancellationToken.None);
        var result2 = await guidRepo.Create(entity2, CancellationToken.None);

        // Assert
        result1.Id.ShouldBe(specifiedGuid);
        result2.Id.ShouldNotBe(Guid.Empty);
        result2.Id.ShouldNotBe(specifiedGuid);
        context.Calls.CreateCallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Create_WithReadOnlyGuidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var readOnlyRepo = Substitute.For<IRepoCreatable<ReadOnlyGuidEntity>, IRepoCapability>();
        var entity = new ReadOnlyGuidEntity();
        MockRepoContext<ReadOnlyGuidEntity>.CreateFor(readOnlyRepo);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => readOnlyRepo.Create(entity, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithCreateResultAction_AppliesActionAndUpdatesCalls()
    {
        // Arrange
        var entity = new TestEntity { Id = "1", Name = "Input" };
        var modifiedEntity = new TestEntity { Id = "1", Name = "Transformed" };
        var context = MockRepoContext<TestEntity>.CreateFor(repository)
            .WithCreateResult(_ => modifiedEntity);

        // Act
        var result = await repository.Create(entity, CancellationToken.None);

        // Assert
        result.ShouldBe(modifiedEntity);
        context.Calls.CreateCallCount.ShouldBe(1);
        context.Calls.LastCreateResult.ShouldBe(modifiedEntity);
    }

    public interface ITestRepository :
        IRepoGettable<TestEntity>,
        IRepoListable<TestEntity>,
        IRepoPageable<TestEntity>,
        IRepoCreatable<TestEntity>,
        IRepoUpdatable<TestEntity>
    {
    }

    public class GuidEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class ReadOnlyGuidEntity
    {
        public Guid Id { get; } = Guid.Empty;
    }
}
