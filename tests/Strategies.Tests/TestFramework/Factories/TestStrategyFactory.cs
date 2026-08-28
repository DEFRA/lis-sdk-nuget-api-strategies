// <copyright file="TestStrategyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Factories;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;

public sealed class TestStrategyFactory : StrategyFactoryBase<TestService, TestStrategyFactory>
{
    public TestStrategyFactory()
    {
        SetParentFactory(this);
    }

    public void CallAttachDefaultsToBuilder<TParentBuilder>(IStrategy<TestService, TParentBuilder> strategyBuilder)
        where TParentBuilder : class
    {
        AttachDefaultsToBuilder(strategyBuilder);
    }
}
