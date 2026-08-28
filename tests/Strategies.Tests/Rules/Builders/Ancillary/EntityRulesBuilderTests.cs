// <copyright file="EntityRulesBuilderTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Rules.Builders.Ancillary;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Rules;
using Defra.Livestock.Sdk.Api.Strategies.Rules.Builders.Ancillary;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using NSubstitute;
using Shouldly;
using Xunit;

public class EntityRulesBuilderTests
{
    [Fact]
    public void Add_WithPredicateFunction_AddsPredicateRuleAndReturnsSelf()
    {
        // Arrange
        var builder = new EntityRulesBuilder<TestEntity>();

        // Act
        var returnedBuilder = builder.Add(e => e.Id == "123", "Must have ID 123", "Invalid ID");

        // Assert
        returnedBuilder.ShouldBeSameAs(builder);

        builder.Predicates.Count.ShouldBe(1);

        builder.Predicates[0].ShouldSatisfyAllConditions(
            x => x.Description.ShouldBe("Must have ID 123"),
            x => x.ErrorMessage.ShouldBe("Invalid ID"),
            x => x.Predicate(new TestEntity { Id = "123" }).ShouldBeTrue(),
            x => x.Predicate(new TestEntity { Id = "456" }).ShouldBeFalse());
    }

    [Fact]
    public void Add_WithRuleInstance_AddsRuleAndReturnsSelf()
    {
        // Arrange
        var builder = new EntityRulesBuilder<TestEntity>();
        var rule = Substitute.For<IPredicateRule<TestEntity>>();

        rule.Description.Returns("Custom Rule");

        // Act
        var returnedBuilder = builder.Add(rule);

        // Assert
        returnedBuilder.ShouldBeSameAs(builder);

        builder.Predicates.Count.ShouldBe(1);

        builder.Predicates[0].ShouldBeSameAs(rule);
    }
}
