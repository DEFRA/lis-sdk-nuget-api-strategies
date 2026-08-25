// <copyright file="FluentValidationExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Extensions;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using FluentValidation;
using FluentValidation.Results;
using global::Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Base;
using global::Defra.Livestock.Sdk.Api.Strategies.Operations;
using NSubstitute;
using Shouldly;
using Xunit;

public class FluentValidationExtensionsTests
{
    private readonly IStrategy<TestService, ITestStrategy> strategy = Substitute.For<IStrategy<TestService, ITestStrategy>, ITestStrategy>();

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

        errorList[0].PropertyName.ShouldBe("PropWarning");
        errorList[0].ErrorMessage.ShouldBe("Warning msg");
        errorList[0].ErrorCode.ShouldBe("ERR_WARN");
        errorList[0].CustomState.ShouldBe("state1");
        errorList[0].FormattedMessagePlaceholderValues.ShouldBe(placeholderValues);
        errorList[0].AttemptedValue.ShouldBe(123);
        errorList[0].Severity.ShouldBe(RequestValidationFailureSeverity.Warning);

        errorList[1].PropertyName.ShouldBe("PropInfo");
        errorList[1].ErrorMessage.ShouldBe("Info msg");
        errorList[1].ErrorCode.ShouldBe("ERR_INFO");
        errorList[1].CustomState.ShouldBe("state2");
        errorList[1].AttemptedValue.ShouldBe("abc");
        errorList[1].Severity.ShouldBe(RequestValidationFailureSeverity.Info);

        errorList[2].PropertyName.ShouldBe("PropError");
        errorList[2].ErrorMessage.ShouldBe("Error msg");
        errorList[2].ErrorCode.ShouldBe("ERR_ERR");
        errorList[2].CustomState.ShouldBe("state3");
        errorList[2].AttemptedValue.ShouldBe(true);
        errorList[2].Severity.ShouldBe(RequestValidationFailureSeverity.Error);
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
