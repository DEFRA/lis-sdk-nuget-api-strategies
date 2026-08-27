// <copyright file="FluentValidationExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Extensions;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Shouldly;
using Xunit;

public class FluentValidationExtensionsTests
{
    private readonly IStrategy<TestService, ITestStrategy> strategy =
        Substitute.For<IStrategy<TestService, ITestStrategy>, ITestStrategy>();

    public interface ITestStrategy : IStrategy<TestService, ITestStrategy>
    {
    }

    [Fact]
    public async Task WithRequestValidation_MapsFluentValidationResultToRequestValidationResult()
    {
        // Arrange
        Func<Task<RequestValidationResult>>? capturedWrapper = null;

        strategy.WithRequestValidation(Arg.Do<Func<Task<RequestValidationResult>>>(func => capturedWrapper = func))
            .Returns((ITestStrategy)strategy);

        var placeholderValues = new Dictionary<string, object> { { "key", "val" } };

        var validationFailures = new List<ValidationFailure>
        {
            new("PropWarning", "Warning msg")
            {
                ErrorCode = "ERR_WARN",
                CustomState = "state1",
                FormattedMessagePlaceholderValues = placeholderValues,
                AttemptedValue = 123,
                Severity = Severity.Warning,
            },
            new("PropInfo", "Info msg")
            {
                ErrorCode = "ERR_INFO",
                CustomState = "state2",
                FormattedMessagePlaceholderValues = null,
                AttemptedValue = "abc",
                Severity = Severity.Info,
            },
            new("PropError", "Error msg")
            {
                ErrorCode = "ERR_ERR",
                CustomState = "state3",
                FormattedMessagePlaceholderValues = null,
                AttemptedValue = true,
                Severity = Severity.Error,
            },
        };

        var validationResult = new ValidationResult(validationFailures)
        {
            RuleSetsExecuted = ["RuleSet1", "RuleSet2"],
        };

        // Act
        var returnedStrategy = strategy.WithRequestValidation(() => Task.FromResult(validationResult));

        capturedWrapper.ShouldNotBeNull();

        var result = await capturedWrapper();

        // Assert
        returnedStrategy.ShouldBeSameAs(strategy);

        result.IsValid.ShouldBeFalse();
        result.RuleSetsExecuted.ShouldBe(["RuleSet1", "RuleSet2"]);
        result.Errors.Count.ShouldBe(3);

        var errorList = result.Errors.ToList();

        errorList[0].ShouldSatisfyAllConditions(
            x => x.PropertyName.ShouldBe("PropWarning"),
            x => x.ErrorMessage.ShouldBe("Warning msg"),
            x => x.ErrorCode.ShouldBe("ERR_WARN"),
            x => x.CustomState.ShouldBe("state1"),
            x => x.FormattedMessagePlaceholderValues.ShouldBe(placeholderValues),
            x => x.AttemptedValue.ShouldBe(123),
            x => x.Severity.ShouldBe(RequestValidationFailureSeverity.Warning));

        errorList[1].ShouldSatisfyAllConditions(
            x => x.PropertyName.ShouldBe("PropInfo"),
            x => x.ErrorMessage.ShouldBe("Info msg"),
            x => x.ErrorCode.ShouldBe("ERR_INFO"),
            x => x.CustomState.ShouldBe("state2"),
            x => x.AttemptedValue.ShouldBe("abc"),
            x => x.Severity.ShouldBe(RequestValidationFailureSeverity.Info));

        errorList[2].ShouldSatisfyAllConditions(
            x => x.PropertyName.ShouldBe("PropError"),
            x => x.ErrorMessage.ShouldBe("Error msg"),
            x => x.ErrorCode.ShouldBe("ERR_ERR"),
            x => x.CustomState.ShouldBe("state3"),
            x => x.AttemptedValue.ShouldBe(true),
            x => x.Severity.ShouldBe(RequestValidationFailureSeverity.Error));
    }

    [Fact]
    public async Task WithRequestValidation_WhenNoErrors_ReturnsValidRequestValidationResult()
    {
        // Arrange
        Func<Task<RequestValidationResult>>? capturedWrapper = null;

        strategy.WithRequestValidation(Arg.Do<Func<Task<RequestValidationResult>>>(func => capturedWrapper = func))
            .Returns((ITestStrategy)strategy);

        var validationResult = new ValidationResult();

        // Act
        strategy.WithRequestValidation(() => Task.FromResult(validationResult));

        capturedWrapper.ShouldNotBeNull();

        var result = await capturedWrapper();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
