// <copyright file="PagedQueryValidator.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Requests.Pagination;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests.Pagination;
using FluentValidation;

public class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    private const int MaxPageSize = 500;

    public PagedQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(MaxPageSize);
    }
}
