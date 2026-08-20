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

    [LoggerMessage(LogLevel.Information,
        "Successfully called SOAP endpoint '{absoluteUrl}'")]
    static partial void LogSuccessfullyCalledSoapEndpoint(ILogger<SoapHttpClient> logger, string absoluteUrl);
}
