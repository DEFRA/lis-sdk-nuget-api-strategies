// <copyright file="TestRequest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests;

/// <summary>
/// Dummy request type implementing <see cref="ILoggableById"/> used for strategy and rule tests.
/// </summary>
public class TestRequest : ILoggableById
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public string Id { get; set; } = "123";

    /// <summary>
    /// Gets the loggable identifier.
    /// </summary>
    /// <returns>The loggable identifier string.</returns>
    public string GetLoggableId() => Id;
}
