// <copyright file="RepoStrategyFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations;

using System.Threading;
using System.Threading.Tasks;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Tests;
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
        var token = new CancellationTokenSource().Token;
        creatableRepo.Create(Arg.Any<TestEntity>(), token).Returns(Task.FromResult(created));

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
            .WithCancellationToken(token)
            .WithRepository(creatableRepo)
            .WithActionDescription("Create")
            .WithCreate(() => new TestEntity { Id = "1" })
            .Execute();

        result.ShouldBe(created);
    }

    [Fact]
    public void BuildUpdateStrategy_ReturnsStrategyInstance()
    {
        // Act
        var strategy = factory.BuildUpdateStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void BuildUpsertStrategy_ReturnsStrategyInstance()
    {
        // Act
        var strategy = factory.BuildUpsertStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void BuildGetStrategy_ReturnsStrategyInstance()
    {
        // Act
        var strategy = factory.BuildGetStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void BuildGetListStrategy_ReturnsStrategyInstance()
    {
        // Act
        var strategy = factory.BuildGetListStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void BuildGetPagedStrategy_ReturnsStrategyInstance()
    {
        // Act
        var strategy = factory.BuildGetPagedStrategy<TestEntity>();

        // Assert
        strategy.ShouldNotBeNull();
    }
}
