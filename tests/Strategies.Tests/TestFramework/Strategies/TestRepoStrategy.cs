// <copyright file="TestRepoStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;

using Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;

public sealed class TestRepoStrategy : RepoStrategyBase<TestService, TestRepoStrategy>
{
    public TestRepoStrategy()
    {
        SetParentBuilder(this);
    }

    public string? GetTargetDescription() => TargetDescription;

    public void InvokeLogEntityWithIdNotFound(string id) => LogEntityWithIdNotFound(id);
}
