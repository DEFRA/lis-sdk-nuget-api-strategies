// <copyright file="SoapHttpClient.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Client;

using Microsoft.Extensions.Logging;

/// <summary>
/// Logging operations for the soap http client.
/// </summary>
public sealed partial class SoapHttpClient
{
    [LoggerMessage(LogLevel.Information,
        "Calling SOAP endpoint '{absoluteUrl}' ...")]
    static partial void LogCallingSoapEndpoint(ILogger<SoapHttpClient> logger, string absoluteUrl);

    [LoggerMessage(LogLevel.Warning,
        "Caller provided cancellation token was cancelled")]
    static partial void LogCallerProvidedCancellationTokenCancelled(ILogger<SoapHttpClient> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning,
        "Http request timed out after {timeout} milliseconds")]
    static partial void LogHttpRequestTimeout(ILogger<SoapHttpClient> logger, int timeout, Exception ex);

    [LoggerMessage(LogLevel.Warning,
        "Http request was cancelled by another source")]
    static partial void LogHttpRequestCancelledByAnotherSource(ILogger<SoapHttpClient> logger, Exception ex);

    [LoggerMessage(LogLevel.Information,
        "Successfully called SOAP endpoint '{absoluteUrl}'")]
    static partial void LogSuccessfullyCalledSoapEndpoint(ILogger<SoapHttpClient> logger, string absoluteUrl);
}
