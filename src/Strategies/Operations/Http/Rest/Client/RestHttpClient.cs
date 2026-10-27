// <copyright file="RestHttpClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Models;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Constants;
using Microsoft.Extensions.Logging;

/// <summary>
/// REST HTTP Client.
/// </summary>
public sealed partial class RestHttpClient : IRestHttpClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<RestHttpClient> logger;

    public RestHttpClient(
        HttpClient httpClient,
        ILogger<RestHttpClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    private Action<string, string?>? VerboseOutputAction { get; set; }

    private string? BaseUrl { get; set; }

    private Dictionary<string, string> Headers { get; } = new();

    private Dictionary<string, string> QueryParameters { get; } = new();

    private string? MediaType { get; set; }

    private int? TimeoutMs { get; set; }

    public IRestHttpClient WithVerboseOutput(Action<string, string?>? verboseOutputAction)
    {
        VerboseOutputAction = verboseOutputAction;
        return this;
    }

    public IRestHttpClient WithBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        BaseUrl = baseUrl;
        return this;
    }

    public IRestHttpClient WithMediaType(string mediaType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

        MediaType = mediaType;
        return this;
    }

    public IRestHttpClient WithHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Headers[name] = value;

        return this;
    }

    public IRestHttpClient WithQueryParameter(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        QueryParameters[name] = value;

        return this;
    }

    public IRestHttpClient WithTimeout(int timeoutMs)
    {
        TimeoutMs = timeoutMs;

        return this;
    }

    public async Task<RestResponse> SendAsync(
        HttpMethod httpMethod,
        string? relativeUrl,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("Base URL must be set before making a rest request.");
        }

        if (string.IsNullOrWhiteSpace(MediaType))
        {
            throw new InvalidOperationException("Media type must be set before making a rest request.");
        }

        using var timeoutCts = TimeoutMs.HasValue
            ? new CancellationTokenSource(TimeSpan.FromMilliseconds(TimeoutMs.Value))
            : null;

        using var linkedCts = timeoutCts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;

        var effectiveCancellationToken = linkedCts?.Token ?? cancellationToken;

        var urlBuilder = new StringBuilder();
        var trimmedBaseUrl = BaseUrl.TrimEnd('/');
        var trimmedRelativeUrl = relativeUrl?.TrimStart('/');

        urlBuilder.Append(trimmedBaseUrl);

        if (!string.IsNullOrEmpty(trimmedRelativeUrl))
        {
            urlBuilder.Append('/').Append(trimmedRelativeUrl);
        }

        if (QueryParameters.Count > 0)
        {
            var queryString = string.Join(
                "&",
                QueryParameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            urlBuilder.Append(urlBuilder.ToString().Contains('?') ? '&' : '?').Append(queryString);
        }

        var absoluteUrl = new Uri(urlBuilder.ToString(), UriKind.Absolute);

        HttpContent httpContent = new StringContent(payload ?? string.Empty, Encoding.UTF8, MediaType);

        if (payload != null)
        {
            EmmitVerboseOutput("Created Request Payload:", payload);
        }

        using var httpRequest = new HttpRequestMessage(httpMethod, absoluteUrl);

        httpRequest.Content = httpContent;
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaType) { CharSet = "utf-8" };

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));

        foreach (var header in Headers)
        {
            httpRequest.Headers.Add(header.Key, header.Value);
        }

        LogCallingRestEndpoint(logger, httpMethod.Method, absoluteUrl.ToString());

        EmmitVerboseOutput($"Sending {httpMethod.Method} Request to:", absoluteUrl.ToString());

        HttpResponseMessage httpResponse;

        try
        {
            httpResponse = await httpClient.SendAsync(httpRequest, effectiveCancellationToken);
        }
        catch (OperationCanceledException ex)
            when (cancellationToken.IsCancellationRequested)
        {
            LogCallerProvidedCancellationTokenCancelled(logger, ex);

            throw new RestRequestException($"{HttpStrategyConstants.Errors.HttpRequestErrorPrefix}: {ex.Message}", ex);
        }
        catch (OperationCanceledException ex)
            when (ex.InnerException is TimeoutException)
        {
            LogHttpRequestTimeout(logger, httpClient.Timeout.Milliseconds, ex);

            throw new RestRequestException($"{HttpStrategyConstants.Errors.HttpRequestErrorPrefix}: {ex.Message}", ex);
        }
        catch (OperationCanceledException ex)
        {
            LogHttpRequestCancelledByAnotherSource(logger, ex);

            throw new RestRequestException($"{HttpStrategyConstants.Errors.HttpRequestErrorPrefix}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new RestRequestException($"{HttpStrategyConstants.Errors.HttpRequestErrorPrefix}: {ex.Message}", ex);
        }

        try
        {
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new RestResponseException($"HTTP Response Error: {ex.Message}", ex);
        }

        var restResponse = await ExtractRestResponse(httpResponse, effectiveCancellationToken);

        LogSuccessfullyCalledRestEndpoint(logger, httpMethod.Method, absoluteUrl.ToString());

        EmmitVerboseOutput($"Successfully sent {httpMethod.Method} Request to:", absoluteUrl.ToString());

        return restResponse;
    }

    private static async Task<RestResponse> ExtractRestResponse(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken = default)
    {
        if (httpResponse.StatusCode == HttpStatusCode.NoContent ||
            httpResponse.Content == null)
        {
            return new RestResponse
            {
                StatusCode = httpResponse.StatusCode,
                HasContent = false,
                Content = null,
                Headers = httpResponse.Headers,
            };
        }

        var content = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        return new RestResponse
        {
            StatusCode = httpResponse.StatusCode,
            HasContent = !string.IsNullOrEmpty(content),
            Content = content,
            Headers = httpResponse.Headers,
        };
    }

    private void EmmitVerboseOutput(string description, string? data)
    {
        VerboseOutputAction?.Invoke(description, data);
    }
}
