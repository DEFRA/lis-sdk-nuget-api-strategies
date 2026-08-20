// <copyright file="FluentValidationExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

// ReSharper disable CheckNamespace
namespace Defra.Livestock.Sdk.Api.Strategies.Operations;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using FluentValidation;
using FluentValidation.Results;

[ExcludeFromCodeCoverage]
public static class FluentValidationExtensions
{
    public static TParent WithRequestValidation<TService, TParent>(
        this IStrategy<TService, TParent> strategyBuilder,
        Func<Task<ValidationResult>> validateAction)
        where TService : class
        where TParent : class, IStrategy<TService, TParent>
    {
        return strategyBuilder.WithRequestValidation(FluentValidationWrapper);

        async Task<RequestValidationResult> FluentValidationWrapper()
        {
            var validationResult = await validateAction();

            var validationFailures = validationResult.Errors.Select(vf =>
                    new RequestValidationFailure(vf.PropertyName, vf.ErrorMessage)
                    {
                        ErrorCode = vf.ErrorCode,
                        CustomState = vf.CustomState,
                        FormattedMessagePlaceholderValues = vf.FormattedMessagePlaceholderValues,
                        AttemptedValue = vf.AttemptedValue,
                        Severity = vf.Severity switch
                        {
                            Severity.Warning => RequestValidationFailureSeverity.Warning,
                            Severity.Info => RequestValidationFailureSeverity.Info,
                            _ => RequestValidationFailureSeverity.Error,
                        },
                    })
                .ToList();

            return new RequestValidationResult(validationFailures)
            {
                RuleSetsExecuted = validationResult.RuleSetsExecuted,
            };
        }
    }
}
