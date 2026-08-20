// <copyright file="SoapStrategyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap;
using Microsoft.Extensions.DependencyInjection;

[ExcludeFromCodeCoverage]
public sealed class SoapStrategyFactory<TService> : StrategyFactoryBase<TService, ISoapStrategyFactory<TService>>,
    ISoapStrategyFactory<TService>
    where TService : class
{
    private readonly IServiceProvider serviceProvider;

    public SoapStrategyFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        SetParentFactory(this);
    }

    private string? DefaultApiDescription { get; set; }

    private string? DefaultBaseUrl { get; set; }

    private string? DefaultServiceUrl { get; set; }

    private string? DefaultSoapAction { get; set; }

    private string? DefaultMediaType { get; set; }

    private bool? DefaultXmlDeclaration { get; set; }

    private Action<string, string?>? DefaultVerboseOutputAction { get; set; }

    public ISoapStrategyFactory<TService> WithDefaultApiDescription(string entityDescription)
    {
        DefaultApiDescription = entityDescription;
        return this;
    }

    public ISoapStrategyFactory<TService> WithDefaultBaseUrl(string baseUrl)
    {
        DefaultBaseUrl = baseUrl;
        return this;
    }

    public ISoapStrategyFactory<TService> WithDefaultServiceUrl(string serviceUrl)
    {
        DefaultServiceUrl = serviceUrl;
        return this;
    }

    public ISoapStrategyFactory<TService> WithDefaultSoapAction(string soapAction)
    {
        DefaultSoapAction = soapAction;
        return this;
    }

    public ISoapStrategyFactory<TService> WithDefaultMediaType(string mediaType)
    {
        DefaultMediaType = mediaType;
        return this;
    }

    public ISoapStrategyFactory<TService> WithDefaultXmlDeclaration(bool withDefaultXmlDeclaration)
    {
        DefaultXmlDeclaration = withDefaultXmlDeclaration;
        return this;
    }

    public ISoapStrategyFactory<TService> WithDefaultVerboseOutput(Action<string, string?> verboseOutputAction)
    {
        DefaultVerboseOutputAction = verboseOutputAction;
        return this;
    }

    public ISoapStrategy<TService> BuildSoapStrategy()
    {
        var soapHttpClient = serviceProvider.GetRequiredService<ISoapHttpClient>();

        var soapStrategy = new SoapStrategy<TService>(soapHttpClient);

        AttachDefaults(soapStrategy);

        return soapStrategy;
    }

    private void AttachDefaults(SoapStrategy<TService> strategyBuilder)
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

        if (DefaultServiceUrl != null)
        {
            strategyBuilder.WithServiceUrl(DefaultServiceUrl);
        }

        if (DefaultSoapAction != null)
        {
            strategyBuilder.WithSoapAction(DefaultSoapAction);
        }

        if (DefaultMediaType != null)
        {
            strategyBuilder.WithMediaType(DefaultMediaType);
        }

        if (DefaultXmlDeclaration != null)
        {
            strategyBuilder.WithXmlDeclaration(DefaultXmlDeclaration.Value);
        }

        if (DefaultVerboseOutputAction != null)
        {
            strategyBuilder.WithVerboseOutput(DefaultVerboseOutputAction);
        }
    }
}
