// <copyright file="RestHttpClient.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;

using Microsoft.Extensions.Logging;

/// <summary>
/// Logging operations for the rest http client.
/// </summary>
public sealed partial class RestHttpClient
{
    [LoggerMessage(LogLevel.Information,
        "Calling REST endpoint '{method}' '{absoluteUrl}' ...")]
    static partial void LogCallingRestEndpoint(ILogger<RestHttpClient> logger, string method, string absoluteUrl);

    [LoggerMessage(LogLevel.Warning,
        "Caller provided cancellation token was cancelled")]
    static partial void LogCallerProvidedCancellationTokenCancelled(ILogger<RestHttpClient> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning,
        "Http request timed out after {timeout} milliseconds")]
    static partial void LogHttpRequestTimeout(ILogger<RestHttpClient> logger, int timeout, Exception ex);

    [LoggerMessage(LogLevel.Warning,
        "Http request was cancelled by another source")]
    static partial void LogHttpRequestCancelledByAnotherSource(ILogger<RestHttpClient> logger, Exception ex);

    [LoggerMessage(LogLevel.Information,
        "Successfully called REST endpoint '{method}' '{absoluteUrl}'")]
    static partial void LogSuccessfullyCalledRestEndpoint(ILogger<RestHttpClient> logger, string method,
        string absoluteUrl);
}
