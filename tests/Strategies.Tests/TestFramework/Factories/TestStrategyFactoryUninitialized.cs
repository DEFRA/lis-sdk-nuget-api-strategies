// <copyright file="TestStrategyFactoryUninitialized.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Factories;

using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;

public sealed class TestStrategyFactoryUninitialized : StrategyFactoryBase<TestService, TestStrategyFactoryUninitialized>
{
    public TestStrategyFactoryUninitialized CallGetParentFactory() => GetParentFactory();
}
