// <copyright file="HttpClientExceptionExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

// ReSharper disable once CheckNamespace
namespace Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;

using System;
using System.Net;
using System.Net.Http;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions.Base;

public static class HttpClientExceptionExtensions
{
    extension(HttpClientException exception)
    {
        public HttpStatusCode? StatusCode()
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.InnerException is HttpRequestException { StatusCode: { } statusCode }
                ? statusCode
                : null;
        }

        public bool IsNetworkError()
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.InnerException is HttpRequestException { StatusCode: null or 0 };
        }

        public bool IsNotFound() => exception.StatusCode() == HttpStatusCode.NotFound;

        public bool IsBadRequest() => exception.StatusCode() == HttpStatusCode.BadRequest;

        public bool IsUnprocessableEntity() => exception.StatusCode() == HttpStatusCode.UnprocessableEntity;

        public bool IsConflict() => exception.StatusCode() == HttpStatusCode.Conflict;

        public bool IsServiceUnavailable() => exception.StatusCode() == HttpStatusCode.ServiceUnavailable;

        public bool IsGatewayTimeout() => exception.StatusCode() == HttpStatusCode.GatewayTimeout;

        public bool IsUnauthorized() => exception.StatusCode() == HttpStatusCode.Unauthorized;

        public bool IsForbidden() => exception.StatusCode() == HttpStatusCode.Forbidden;

        public bool IsTooManyRequests() => exception.StatusCode() == HttpStatusCode.TooManyRequests;

        public bool IsInternalServerError() => exception.StatusCode() == HttpStatusCode.InternalServerError;

        public bool Is400RangeError() => (int?)exception.StatusCode() is >= 400 and <= 499;

        public bool Is500RangeError() => (int?)exception.StatusCode() is >= 500 and <= 599;
    }
}
