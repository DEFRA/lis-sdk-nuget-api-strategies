// <copyright file="MockRepoCallInfoTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Utilities.Models;

using Shouldly;

public class MockRepoCallInfoTests
{
    [Fact]
    public void MockRepoCallInfo_Properties_CanBeAssignedAndRetrieved()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var callInfo = new MockRepoCallInfo { CancellationToken = cts.Token };

        // Assert
        callInfo.CancellationToken.ShouldBe(cts.Token);
    }
}
