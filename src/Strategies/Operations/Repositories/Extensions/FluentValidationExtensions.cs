// <copyright file="FluentValidationExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

// ReSharper disable CheckNamespace
namespace Defra.Livestock.Sdk.Api.Strategies.Operations;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using FluentValidation;
using FluentValidation.Results;

public static class FluentValidationExtensions
{
    public static TParent WithRequestValidation<TService, TParent>(
        this IRepoStrategy<TService, TParent> strategyBuilder,
        Func<Task<ValidationResult>> validateAction)
        where TService : class
        where TParent : class, IRepoStrategy<TService, TParent>
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
