// <copyright file="HttpClientExceptionExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Exceptions.Base.Extensions;

using System;
using System.Net;
using System.Net.Http;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions.Base;
using Shouldly;
using Xunit;

public class HttpClientExceptionExtensionsTests
{
    [Fact]
    public void StatusCode_WhenExceptionIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClientException exception = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => exception.StatusCode());
    }

    [Fact]
    public void StatusCode_WhenInnerExceptionIsNull_ReturnsNull()
    {
        // Arrange
        var exception = new TestHttpClientRequestException("Error");

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void StatusCode_WhenInnerExceptionIsNotHttpRequestException_ReturnsNull()
    {
        // Arrange
        var exception = new TestHttpClientRequestException("Error", new InvalidOperationException("Not HTTP"));

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void StatusCode_WhenInnerExceptionIsHttpRequestExceptionWithNullStatusCode_ReturnsNull()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("HTTP error");
        var exception = new TestHttpClientRequestException("Error", httpRequestException);

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void StatusCode_WhenInnerExceptionIsHttpRequestExceptionWithStatusCode_ReturnsStatusCode()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Not Found", null, HttpStatusCode.NotFound);
        var exception = new TestHttpClientRequestException("Error", httpRequestException);

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsNotFound_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsNotFound();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsBadRequest_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsBadRequest();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsUnprocessableEntity_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsUnprocessableEntity();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsConflict_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsConflict();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsServiceUnavailable_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsServiceUnavailable();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsGatewayTimeout_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsGatewayTimeout();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsUnauthorized_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsUnauthorized();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsForbidden_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsForbidden();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsTooManyRequests_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsTooManyRequests();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsInternalServerError_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsInternalServerError();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.UnprocessableEntity, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData((HttpStatusCode)499, true)]
    [InlineData((HttpStatusCode)399, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(null, false)]
    public void Is400RangeError_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.Is400RangeError();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData((HttpStatusCode)599, true)]
    [InlineData((HttpStatusCode)499, false)]
    [InlineData((HttpStatusCode)600, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(null, false)]
    public void Is500RangeError_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.Is500RangeError();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void IsNetworkError_WhenExceptionIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClientException exception = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => exception.IsNetworkError());
    }

    [Fact]
    public void IsNetworkError_WhenInnerExceptionIsNull_ReturnsFalse()
    {
        // Arrange
        var exception = new TestHttpClientRequestException("Error");

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsNetworkError_WhenInnerExceptionIsNotHttpRequestException_ReturnsFalse()
    {
        // Arrange
        var exception = new TestHttpClientRequestException("Error", new InvalidOperationException("Not HTTP"));

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsNetworkError_WhenInnerExceptionIsHttpRequestExceptionWithNullStatusCode_ReturnsTrue()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Network failure");
        var exception = new TestHttpClientRequestException("Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsNetworkError_WhenInnerExceptionIsHttpRequestExceptionWithStatusCodeZero_ReturnsTrue()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Network failure", null, (HttpStatusCode)0);
        var exception = new TestHttpClientRequestException("Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void IsNetworkError_WhenInnerExceptionIsHttpRequestExceptionWithNonZeroStatusCode_ReturnsFalse(
        HttpStatusCode statusCode)
    {
        // Arrange
        var httpRequestException = new HttpRequestException("HTTP error", null, statusCode);
        var exception = new TestHttpClientRequestException("Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void StatusCode_WhenRestRequestExceptionHasNullInnerException_ReturnsNull()
    {
        // Arrange
        var exception = new RestRequestException("Rest Error");

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void StatusCode_WhenRestRequestExceptionInnerExceptionIsNotHttpRequestException_ReturnsNull()
    {
        // Arrange
        var exception = new RestRequestException("Rest Error", new InvalidOperationException("Not HTTP"));

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void StatusCode_WhenRestRequestExceptionInnerExceptionIsHttpRequestExceptionWithNullStatusCode_ReturnsNull()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("HTTP error");
        var exception = new RestRequestException("Rest Error", httpRequestException);

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void
        StatusCode_WhenRestRequestExceptionInnerExceptionIsHttpRequestExceptionWithStatusCode_ReturnsStatusCode()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);
        var exception = new RestRequestException("Rest Error", httpRequestException);

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBe(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsNotFound_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsNotFound();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsBadRequest_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsBadRequest();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsUnprocessableEntity_WhenRestRequestException_ReturnsExpected(
        HttpStatusCode? statusCode,
        bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsUnprocessableEntity();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsConflict_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsConflict();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsServiceUnavailable_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsServiceUnavailable();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsGatewayTimeout_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsGatewayTimeout();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsUnauthorized_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsUnauthorized();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsForbidden_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsForbidden();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsTooManyRequests_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsTooManyRequests();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsInternalServerError_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsInternalServerError();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.UnprocessableEntity, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData((HttpStatusCode)499, true)]
    [InlineData((HttpStatusCode)399, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(null, false)]
    public void Is400RangeError_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.Is400RangeError();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData((HttpStatusCode)599, true)]
    [InlineData((HttpStatusCode)499, false)]
    [InlineData((HttpStatusCode)600, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(null, false)]
    public void Is500RangeError_WhenRestRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateRestRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.Is500RangeError();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void IsNetworkError_WhenRestRequestExceptionHasNullInnerException_ReturnsFalse()
    {
        // Arrange
        var exception = new RestRequestException("Rest Error");

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsNetworkError_WhenRestRequestExceptionInnerExceptionIsNotHttpRequestException_ReturnsFalse()
    {
        // Arrange
        var exception = new RestRequestException("Rest Error", new InvalidOperationException("Not HTTP"));

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsNetworkError_WhenRestRequestExceptionInnerExceptionIsHttpRequestExceptionWithNullStatusCode_ReturnsTrue()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Network failure");
        var exception = new RestRequestException("Rest Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsNetworkError_WhenRestRequestExceptionInnerExceptionIsHttpRequestExceptionWithStatusCodeZero_ReturnsTrue()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Network failure", null, (HttpStatusCode)0);
        var exception = new RestRequestException("Rest Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void IsNetworkError_WhenRestRequestExceptionInnerExceptionIsHttpRequestExceptionWithNonZeroStatusCode_ReturnsFalse(
        HttpStatusCode statusCode)
    {
        // Arrange
        var httpRequestException = new HttpRequestException("HTTP error", null, statusCode);
        var exception = new RestRequestException("Rest Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void StatusCode_WhenSoapRequestExceptionHasNullInnerException_ReturnsNull()
    {
        // Arrange
        var exception = new SoapRequestException("Soap Error");

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void StatusCode_WhenSoapRequestExceptionInnerExceptionIsNotHttpRequestException_ReturnsNull()
    {
        // Arrange
        var exception = new SoapRequestException("Soap Error", new InvalidOperationException("Not HTTP"));

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void StatusCode_WhenSoapRequestExceptionInnerExceptionIsHttpRequestExceptionWithNullStatusCode_ReturnsNull()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("HTTP error");
        var exception = new SoapRequestException("Soap Error", httpRequestException);

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void
        StatusCode_WhenSoapRequestExceptionInnerExceptionIsHttpRequestExceptionWithStatusCode_ReturnsStatusCode()
    {
        // Arrange
        var httpRequestException =
            new HttpRequestException("Service Unavailable", null, HttpStatusCode.ServiceUnavailable);
        var exception = new SoapRequestException("Soap Error", httpRequestException);

        // Act
        var result = exception.StatusCode();

        // Assert
        result.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsNotFound_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsNotFound();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsBadRequest_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsBadRequest();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsUnprocessableEntity_WhenSoapRequestException_ReturnsExpected(
        HttpStatusCode? statusCode,
        bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsUnprocessableEntity();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(null, false)]
    public void IsConflict_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsConflict();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsServiceUnavailable_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsServiceUnavailable();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsGatewayTimeout_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsGatewayTimeout();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsUnauthorized_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsUnauthorized();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsForbidden_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsForbidden();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsTooManyRequests_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsTooManyRequests();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(null, false)]
    public void IsInternalServerError_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.IsInternalServerError();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.UnprocessableEntity, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData((HttpStatusCode)499, true)]
    [InlineData((HttpStatusCode)399, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(null, false)]
    public void Is400RangeError_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.Is400RangeError();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData((HttpStatusCode)599, true)]
    [InlineData((HttpStatusCode)499, false)]
    [InlineData((HttpStatusCode)600, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(null, false)]
    public void Is500RangeError_WhenSoapRequestException_ReturnsExpected(HttpStatusCode? statusCode, bool expected)
    {
        // Arrange
        var exception = CreateSoapRequestExceptionWithStatusCode(statusCode);

        // Act
        var result = exception.Is500RangeError();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void IsNetworkError_WhenSoapRequestExceptionHasNullInnerException_ReturnsFalse()
    {
        // Arrange
        var exception = new SoapRequestException("Soap Error");

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsNetworkError_WhenSoapRequestExceptionInnerExceptionIsNotHttpRequestException_ReturnsFalse()
    {
        // Arrange
        var exception = new SoapRequestException("Soap Error", new InvalidOperationException("Not HTTP"));

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsNetworkError_WhenSoapRequestExceptionInnerExceptionIsHttpRequestExceptionWithNullStatusCode_ReturnsTrue()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Network failure");
        var exception = new SoapRequestException("Soap Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsNetworkError_WhenSoapRequestExceptionInnerExceptionIsHttpRequestExceptionWithStatusCodeZero_ReturnsTrue()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Network failure", null, (HttpStatusCode)0);
        var exception = new SoapRequestException("Soap Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void IsNetworkError_WhenSoapRequestExceptionInnerExceptionIsHttpRequestExceptionWithNonZeroStatusCode_ReturnsFalse(
        HttpStatusCode statusCode)
    {
        // Arrange
        var httpRequestException = new HttpRequestException("HTTP error", null, statusCode);
        var exception = new SoapRequestException("Soap Error", httpRequestException);

        // Act
        var result = exception.IsNetworkError();

        // Assert
        result.ShouldBeFalse();
    }

    private static HttpClientException CreateExceptionWithStatusCode(HttpStatusCode? statusCode)
    {
        if (statusCode.HasValue)
        {
            var httpEx = new HttpRequestException("HTTP Error", null, statusCode.Value);
            return new TestHttpClientRequestException("Error", httpEx);
        }

        return new TestHttpClientRequestException("Error");
    }

    private static RestRequestException CreateRestRequestExceptionWithStatusCode(HttpStatusCode? statusCode)
    {
        if (statusCode.HasValue)
        {
            var httpEx = new HttpRequestException("HTTP Error", null, statusCode.Value);
            return new RestRequestException("Rest Error", httpEx);
        }

        return new RestRequestException("Rest Error");
    }

    private static SoapRequestException CreateSoapRequestExceptionWithStatusCode(HttpStatusCode? statusCode)
    {
        if (statusCode.HasValue)
        {
            var httpEx = new HttpRequestException("HTTP Error", null, statusCode.Value);
            return new SoapRequestException("Soap Error", httpEx);
        }

        return new SoapRequestException("Soap Error");
    }

    private sealed class TestHttpClientRequestException : HttpClientException
    {
        public TestHttpClientRequestException(string message)
            : base(message)
        {
        }

        public TestHttpClientRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
