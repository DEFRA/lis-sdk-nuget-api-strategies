// <copyright file="PagedQueryValidatorTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Requests.Pagination;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests.Pagination;
using global::Defra.Livestock.Sdk.Api.Strategies.Requests.Pagination;
using Shouldly;
using Xunit;

public class PagedQueryValidatorTests
{
    private readonly PagedQueryValidator validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 50)]
    [InlineData(10, 500)]
    public void Validate_WhenValidPagedQuery_ShouldPass(int pageNumber, int pageSize)
    {
        // Arrange
        var query = new PagedQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WhenPageNumberLessThanOne_ShouldFail(int pageNumber)
    {
        // Arrange
        var query = new PagedQuery
        {
            PageNumber = pageNumber,
            PageSize = 10,
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PagedQuery.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    [InlineData(1000)]
    public void Validate_WhenPageSizeOutOfRange_ShouldFail(int pageSize)
    {
        // Arrange
        var query = new PagedQuery
        {
            PageNumber = 1,
            PageSize = pageSize,
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PagedQuery.PageSize));
    }
}
