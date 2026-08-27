// <copyright file="TestService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class TestService : ITestService
{
    public string Execute() => "Test";
}
