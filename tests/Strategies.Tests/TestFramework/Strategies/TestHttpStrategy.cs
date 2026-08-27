// <copyright file="TestHttpStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;

using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;

public sealed class TestHttpStrategy : HttpStrategyBase<TestService, TestHttpStrategy>
{
    public TestHttpStrategy()
    {
        SetParentBuilder(this);
    }

    public string? GetTargetDescription() => TargetDescription;

    public string? GetBaseUrl() => BaseUrl;

    public string? GetMediaType() => MediaType;

    public Dictionary<string, string> GetHeaders() => Headers;
}
