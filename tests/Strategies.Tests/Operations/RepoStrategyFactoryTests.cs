// <copyright file="RepoStrategyFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations;

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class RepoStrategyFactoryTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();
    private readonly IRepoCreatable<TestEntity> creatableRepo = Substitute.For<IRepoCreatable<TestEntity>>();
    private readonly RepoStrategyFactory<TestService> factory = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public interface ITestUpdateRepository : IRepoGettable<TestEntity>, IRepoUpdatable<TestEntity>;

    // ReSharper disable once MemberCanBePrivate.Global
    public interface ITestUpsertRepository : IRepoGettable<TestEntity>, IRepoCreatable<TestEntity>,
        IRepoUpdatable<TestEntity>;

    [Fact]
    public void WithDefaultEntityDescription_ReturnsFactory()
    {
        // Act
        var result = factory.WithDefaultEntityDescription("DefaultEntity");

        // Assert
        result.ShouldBe(factory);
    }

    [Fact]
    public async Task BuildCreateStrategy_AttachesConfiguredDefaults()
    {
        // Arrange
        var created = new TestEntity { Id = "1" };

        creatableRepo.Create(Arg.Any<TestEntity>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(created));

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultEntityDescription("DefaultEntity");

        // Act
        var strategy = factory.BuildCreateStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();

        // Verify defaults were attached by executing the strategy without re-specifying logger or entity description
        var result = await strategy
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(creatableRepo)
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity { Id = "1" })
            .Execute();

        result.ShouldBe(created);
    }

    [Fact]
    public async Task BuildUpdateStrategy_AttachesConfiguredDefaults()
    {
        // Arrange
        var repo = Substitute.For<ITestUpdateRepository>();

        var entity = new TestEntity { Id = "1", Name = "Initial" };
        var updated = new TestEntity { Id = "1", Name = "Updated" };

        repo.GetSingle(Arg.Any<Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestEntity?>(entity));

        repo.Update(Arg.Any<TestEntity>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updated));

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultEntityDescription("DefaultEntity");

        // Act
        var strategy = factory.BuildUpdateStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();

        // Verify defaults were attached by executing the strategy without re-specifying logger or entity description
        var result = await strategy
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repo)
            .WithActionDescription("Update")
            .WithRequest(new TestRequest { Id = "1" })
            .WithEntityFilter(e => e.Id == "1")
            .WithUpdate(e => e.Name = "Updated")
            .Execute();

        result.ShouldBe(updated);
    }

    [Fact]
    public async Task BuildUpsertStrategy_AttachesConfiguredDefaults()
    {
        // Arrange
        var repo = Substitute.For<ITestUpsertRepository>();
        var created = new TestEntity { Id = "1", Name = "Created" };

        repo.GetSingle(Arg.Any<Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestEntity?>(null));

        repo.Create(Arg.Any<TestEntity>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(created));

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultEntityDescription("DefaultEntity");

        // Act
        var strategy = factory.BuildUpsertStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();

        // Verify defaults were attached by executing the strategy without re-specifying logger or entity description
        var result = await strategy
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(repo)
            .WithActionDescription("Upsert")
            .WithRequest(new TestRequest { Id = "1" })
            .WithEntityFilter(e => e.Id == "1")
            .WithCreate(() => new TestEntity { Id = "1", Name = "Created" })
            .WithUpdate(e => e.Name = "Updated")
            .Execute();

        result.ShouldBe(created);
    }

    [Fact]
    public async Task BuildGetStrategy_AttachesConfiguredDefaults()
    {
        // Arrange
        var gettableRepo = Substitute.For<IRepoGettable<TestEntity>>();
        var entity = new TestEntity { Id = "1", Name = "Existing" };

        gettableRepo.GetSingle(Arg.Any<Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestEntity?>(entity));

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultEntityDescription("DefaultEntity");

        // Act
        var strategy = factory.BuildGetStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();

        // Verify defaults were attached by executing the strategy without re-specifying logger or entity description
        var result = await strategy
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(gettableRepo)
            .WithActionDescription("Get")
            .WithRequest(new TestRequest { Id = "1" })
            .WithEntityFilter(e => e.Id == "1")
            .Execute();

        result.ShouldBe(entity);
    }

    [Fact]
    public async Task BuildGetListStrategy_AttachesConfiguredDefaults()
    {
        // Arrange
        var listableRepo = Substitute.For<IRepoListable<TestEntity>>();
        var list = new List<TestEntity> { new() { Id = "1", Name = "Item1" } };

        listableRepo.GetList(Arg.Any<Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(list));

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultEntityDescription("DefaultEntity");

        // Act
        var strategy = factory.BuildGetListStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();

        // Verify defaults were attached by executing the strategy without re-specifying logger or entity description
        var result = await strategy
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(listableRepo)
            .WithActionDescription("GetList")
            .WithEntityFilter(e => e.Id == "1")
            .Execute();

        result.ShouldBe(list);
    }

    [Fact]
    public async Task BuildGetPagedStrategy_AttachesConfiguredDefaults()
    {
        // Arrange
        var pageableRepo = Substitute.For<IRepoPageable<TestEntity>>();
        var paged = new PagedEntities<TestEntity>([new TestEntity { Id = "1", Name = "Item1" }], 1, 1, 1, 10);

        pageableRepo.GetPaged(
                Arg.Any<Expression<Func<TestEntity, bool>>>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Expression<Func<TestEntity, string>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(paged));

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultEntityDescription("DefaultEntity");

        // Act
        var strategy = factory.BuildGetPagedStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();

        // Verify defaults were attached by executing the strategy without re-specifying logger or entity description
        var result = await strategy
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithRepository(pageableRepo)
            .WithActionDescription("GetPaged")
            .WithRequest(new PagedQuery { PageNumber = 1, PageSize = 10 })
            .WithEntityFilter(e => e.Id == "1")
            .Execute(e => e.Id);

        result.ShouldBe(paged);
    }
}
