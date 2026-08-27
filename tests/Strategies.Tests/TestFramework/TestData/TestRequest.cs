// <copyright file="TestRequest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests;

public class TestRequest : ILoggableById
{
    public string Id { get; init; } = "123";

    public string GetLoggableId() => Id;
}
