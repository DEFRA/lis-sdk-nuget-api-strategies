// <copyright file="TestStrategyUninitialized.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;

using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;

public sealed class TestStrategyUninitialized : StrategyBase<TestService, TestStrategyUninitialized>
{
    public TestStrategyUninitialized CallGetParentBuilder() => GetParentBuilder();
}
