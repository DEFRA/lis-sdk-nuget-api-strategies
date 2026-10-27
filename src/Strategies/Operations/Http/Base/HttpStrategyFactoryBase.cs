// <copyright file="HttpStrategyFactoryBase.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Base;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;

public abstract class HttpStrategyFactoryBase<TService, TParent> : StrategyFactoryBase<TService, TParent>,
    IHttpStrategyFactory<TService, TParent>
    where TService : class
    where TParent : class
{
    protected Action<string, string?>? DefaultVerboseOutputAction { get; private set; }

    private string? DefaultApiDescription { get; set; }

    private string? DefaultBaseUrl { get; set; }

    private string? DefaultApiKey { get; set; }

    private Func<bool>? DefaultApiKeyExpression { get; set; }

    private (string Username, string Password)? DefaultBasicAuthCredentials { get; set; }

    private string? DefaultMediaType { get; set; }

    private int? DefaultTimeoutMs { get; set; }

    public TParent WithDefaultApiDescription(string apiDescription)
    {
        DefaultApiDescription = apiDescription;
        return GetParentFactory();
    }

    public TParent WithDefaultBaseUrl(string baseUrl)
    {
        DefaultBaseUrl = baseUrl;
        return GetParentFactory();
    }

    public TParent WithDefaultApiKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        DefaultApiKey = apiKey;
        DefaultApiKeyExpression = null;
        return GetParentFactory();
    }

    public TParent WithDefaultApiKey(Func<bool> expression, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        DefaultApiKey = apiKey;
        DefaultApiKeyExpression = expression;
        return GetParentFactory();
    }

    public TParent WithDefaultBasicAuth(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        DefaultBasicAuthCredentials = (username, password);
        return GetParentFactory();
    }

    public TParent WithDefaultMediaType(string mediaType)
    {
        DefaultMediaType = mediaType;
        return GetParentFactory();
    }

    public TParent WithDefaultTimeout(int timeoutMs)
    {
        DefaultTimeoutMs = timeoutMs;
        return GetParentFactory();
    }

    public TParent WithDefaultVerboseOutput(Action<string, string?> verboseOutputAction)
    {
        DefaultVerboseOutputAction = verboseOutputAction;
        return GetParentFactory();
    }

    protected void AttachHttpDefaultsToBuilder<TParentBuilder>(IHttpStrategy<TService, TParentBuilder> strategyBuilder)
        where TParentBuilder : class
    {
        AttachDefaultsToBuilder(strategyBuilder);

        if (DefaultApiDescription != null)
        {
            strategyBuilder.WithApiDescription(DefaultApiDescription);
        }

        if (DefaultBaseUrl != null)
        {
            strategyBuilder.WithBaseUrl(DefaultBaseUrl);
        }

        if (DefaultMediaType != null)
        {
            strategyBuilder.WithMediaType(DefaultMediaType);
        }

        if (DefaultApiKey != null)
        {
            if (DefaultApiKeyExpression != null)
            {
                strategyBuilder.WithApiKey(DefaultApiKeyExpression, DefaultApiKey);
            }
            else
            {
                strategyBuilder.WithApiKey(DefaultApiKey);
            }
        }

        if (DefaultBasicAuthCredentials != null)
        {
            strategyBuilder.WithBasicAuth(
                DefaultBasicAuthCredentials.Value.Username,
                DefaultBasicAuthCredentials.Value.Password);
        }

        if (DefaultTimeoutMs != null)
        {
            strategyBuilder.WithTimeout(DefaultTimeoutMs.Value);
        }
    }
}
