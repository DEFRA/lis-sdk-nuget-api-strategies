// <copyright file="OperatorContextTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Context;

using System.Security.Claims;
using Defra.Livestock.Sdk.Api.Strategies.Context;
using Shouldly;
using Xunit;

public class OperatorContextTests
{
    [Fact]
    public void Operator_WhenNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new OperatorContext();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _ = context.Operator);
        ex.Message.ShouldBe("Operator has not been set");
    }

    [Fact]
    public void HasOperator_WhenNotSet_ReturnsFalse()
    {
        // Arrange
        var context = new OperatorContext();

        // Act & Assert
        context.HasOperator.ShouldBeFalse();
    }

    [Fact]
    public void HasAuthenticatedOperator_WhenNotSet_ReturnsFalse()
    {
        // Arrange
        var context = new OperatorContext();

        // Act & Assert
        context.HasAuthenticatedOperator.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SetAuthenticatedOperatorById_WhenIdIsNullOrWhiteSpace_ThrowsArgumentException(string? id)
    {
        // Arrange
        var context = new OperatorContext();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.SetAuthenticatedOperatorById(id!));
    }

    [Fact]
    public void SetAuthenticatedOperatorById_WhenValidId_SetsOperatorAndReturnsSelf()
    {
        // Arrange
        var context = new OperatorContext();

        // Act
        var result = context.SetAuthenticatedOperatorById("op-123");

        // Assert
        result.ShouldBeSameAs(context);
        context.HasOperator.ShouldBeTrue();
        context.HasAuthenticatedOperator.ShouldBeTrue();
        context.Operator.Id.ShouldBe("op-123");
        context.Operator.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void SetAuthenticatedOperatorById_WhenAlreadySet_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new OperatorContext();
        context.SetAuthenticatedOperatorById("op-123");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => context.SetAuthenticatedOperatorById("op-456"));
        ex.Message.ShouldBe("Operator is already set");
    }

    [Fact]
    public void SetOperatorByClaimsPrincipal_WhenNullPrincipal_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new OperatorContext();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.SetOperatorByClaimsPrincipal(null!));
    }

    [Fact]
    public void SetOperatorByClaimsPrincipal_WhenNameIdentifierClaimMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new OperatorContext();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => context.SetOperatorByClaimsPrincipal(principal));
        ex.Message.ShouldBe($"Operator id claim '{ClaimTypes.NameIdentifier}' not found");
    }

    [Fact]
    public void SetOperatorByClaimsPrincipal_WhenValidClaims_SetsOperatorAndReturnsSelf()
    {
        // Arrange
        var context = new OperatorContext();
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-456"),
                new Claim(ClaimTypes.Name, "John Doe"),
                new Claim(ClaimTypes.Email, "john.doe@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User"),
            ],
            "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = context.SetOperatorByClaimsPrincipal(principal);

        // Assert
        result.ShouldBeSameAs(context);
        context.HasOperator.ShouldBeTrue();
        context.HasAuthenticatedOperator.ShouldBeTrue();
        context.Operator.Id.ShouldBe("user-456");
        context.Operator.Name.ShouldBe("John Doe");
        context.Operator.Email.ShouldBe("john.doe@example.com");
        context.Operator.Roles.ShouldBe(["Admin", "User"]);
        context.Operator.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void SetOperatorByClaimsPrincipal_WhenUnauthenticatedIdentity_SetsAuthenticatedToFalse()
    {
        // Arrange
        var context = new OperatorContext();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "unauth-user")]);
        var principal = new ClaimsPrincipal(identity);

        // Act
        context.SetOperatorByClaimsPrincipal(principal);

        // Assert
        context.HasOperator.ShouldBeTrue();
        context.HasAuthenticatedOperator.ShouldBeFalse();
        context.Operator.Id.ShouldBe("unauth-user");
        context.Operator.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public void SetOperatorByClaimsPrincipal_WhenAlreadySet_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new OperatorContext();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        context.SetOperatorByClaimsPrincipal(principal);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => context.SetOperatorByClaimsPrincipal(principal));
        ex.Message.ShouldBe("Operator is already set");
    }
}
