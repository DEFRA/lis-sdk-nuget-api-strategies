// <copyright file="SoapStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap;

using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Schemas.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Schemas.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Serializer;

public sealed class SoapStrategy<TService>
    : HttpStrategyBase<TService, ISoapStrategy<TService>>,
        ISoapStrategy<TService>
    where TService : class
{
    private readonly ISoapHttpClient soapHttpClient;

    public SoapStrategy(ISoapHttpClient soapHttpClient)
    {
        this.soapHttpClient = soapHttpClient;

        SetParentBuilder(this);
    }

    private Action<string, string?>? VerboseOutputAction { get; set; }

    private string? ServiceUrl { get; set; }

    private string? SoapAction { get; set; }

    private Func<XElement>? PayloadAction { get; set; }

    private bool IncludeXmlDeclaration { get; set; }

    private SoapSchemaBuilder? SoapSchemaBuilder { get; set; }

    private bool ValidatePreTransformPayloadSchema { get; set; }

    private string? ValidatePreTransformPayloadSchemaTargetElementName { get; set; }

    private bool ValidatePayloadSchema { get; set; }

    private string? ValidatePayloadSchemaTargetElementName { get; set; }

    private Func<XElement?, XElement?>? PayloadTransformerAction { get; set; }

    private Func<XElement?, XElement?>? ResponseTransformerAction { get; set; }

    public ISoapStrategy<TService> WithServiceUrl(string serviceUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceUrl);

        ServiceUrl = serviceUrl;

        return this;
    }

    public ISoapStrategy<TService> WithSoapAction(string soapAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(soapAction);

        SoapAction = soapAction;

        return this;
    }

    public ISoapStrategy<TService> WithXmlDeclaration(bool includeXmlDeclaration)
    {
        IncludeXmlDeclaration = includeXmlDeclaration;

        return this;
    }

    public ISoapStrategy<TService> WithSchemas(Action<ISoapSchemaBuilder> builder)
    {
        SoapSchemaBuilder = new SoapSchemaBuilder();

        builder(SoapSchemaBuilder);

        return this;
    }

    public ISoapStrategy<TService> WithPayload(Func<XElement> payloadAction)
    {
        PayloadAction = payloadAction;

        return this;
    }

    public ISoapStrategy<TService> WithPayload<TRequest>(Func<TRequest> payloadAction)
    {
        WithPayload(() => SoapSerializer.SerializeToXElement(payloadAction()));

        return this;
    }

    public ISoapStrategy<TService> WithPayload<TRequest>(TRequest payload)
    {
        WithPayload(() => SoapSerializer.SerializeToXElement(payload));

        return this;
    }

    public ISoapStrategy<TService> WithValidatePreTransformPayloadSchema(string? targetElementName = null)
    {
        ValidatePreTransformPayloadSchema = true;

        ValidatePreTransformPayloadSchemaTargetElementName = targetElementName;

        return this;
    }

    public ISoapStrategy<TService> WithValidatePayloadSchema(string? targetElementName = null)
    {
        ValidatePayloadSchema = true;

        ValidatePayloadSchemaTargetElementName = targetElementName;

        return this;
    }

    public ISoapStrategy<TService> WithPayloadTransformer(Func<XElement?, XElement?> payloadTransformerAction)
    {
        PayloadTransformerAction = payloadTransformerAction;

        return this;
    }

    public ISoapStrategy<TService> WithResponseTransformer(Func<XElement?, XElement?> responseTransformerAction)
    {
        ResponseTransformerAction = responseTransformerAction;

        return this;
    }

    public ISoapStrategy<TService> WithVerboseOutput(Action<string, string?> verboseOutputAction)
    {
        VerboseOutputAction = verboseOutputAction;

        return this;
    }

    public async Task<XElement> Execute()
    {
        var response = await ExecuteExtractAndTransformResponse<XElement>(
            transform: bodyContent => bodyContent,
            requiresResponseContent: true,
            requiresResponseBodyContent: true);

        return response!;
    }

    public async Task<TResult> Execute<TResult>()
        where TResult : class
    {
        if (typeof(TResult) == typeof(XElement))
        {
            throw new InvalidOperationException("Cannot deserialize to XElement");
        }

        var response = await ExecuteExtractAndTransformResponse(
            transform: SoapSerializer.DeserializeFromXElement<TResult>,
            requiresResponseContent: true,
            requiresResponseBodyContent: true);

        return response!;
    }

    public async Task<TResult> ExecuteAndTransform<TResult>(Func<XElement, TResult> transform)
        where TResult : class
    {
        var response = await ExecuteExtractAndTransformResponse<TResult>(
            transform: bodyContent => transform(bodyContent!),
            requiresResponseContent: true,
            requiresResponseBodyContent: true);

        return response!;
    }

    public async Task<TResult> ExecuteAndTransform<TResponse, TResult>(Func<TResponse, TResult> transform)
        where TResponse : class
        where TResult : class
    {
        if (typeof(TResponse) == typeof(XElement))
        {
            throw new InvalidOperationException("Cannot deserialize to XElement");
        }

        var response = await ExecuteExtractAndTransformResponse<TResult>(
            transform: bodyContent => transform(SoapSerializer.DeserializeFromXElement<TResponse>(bodyContent!)),
            requiresResponseContent: true,
            requiresResponseBodyContent: true);

        return response!;
    }

    public async Task<TResult> ExecuteAndTransform<TResponse, TResult>(
        Func<TResponse, Func<XElement, TResult>, TResult> transform)
        where TResponse : class
        where TResult : class
    {
        if (typeof(TResponse) == typeof(XElement))
        {
            throw new InvalidOperationException("Cannot deserialize to XElement");
        }

        var response = await ExecuteExtractAndTransformResponse<TResult>(
            transform: bodyContent => transform(
                SoapSerializer.DeserializeFromXElement<TResponse>(bodyContent!),
                SoapSerializer.DeserializeFromXElement<TResult>),
            requiresResponseContent: true,
            requiresResponseBodyContent: true);

        return response!;
    }

    public async Task ExecuteWithoutResponse()
    {
        await ExecuteExtractAndTransformResponse<XElement>(
            transform: _ => null,
            requiresResponseContent: false,
            requiresResponseBodyContent: false);
    }

    private static void ValidateSchema(XmlSchemaSet schemaSet, XElement? element, string? targetElementName)
    {
        if (element == null)
        {
            throw new XmlSchemaValidationException(
                $"Schema validation error: No element was provided for schema validation");
        }

        var targetElement = targetElementName == null
            ? element
            : element.Descendants().FirstOrDefault(descendant => descendant.Name.LocalName == targetElementName);

        if (targetElement == null)
        {
            throw new InvalidOperationException(
                $"Element with local name '{targetElementName}' not found in the provided content");
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
                              XmlSchemaValidationFlags.ProcessIdentityConstraints |
                              XmlSchemaValidationFlags.ProcessInlineSchema |
                              XmlSchemaValidationFlags.AllowXmlAttributes,
        };

        settings.ValidationEventHandler += (_, e) =>
            throw new XmlSchemaValidationException($"Schema validation error: {e.Message}");

        using var reader = XmlReader.Create(targetElement.CreateReader(), settings);

        while (reader.Read())
#pragma warning disable S108
        {
        }
#pragma warning restore S108
    }

    [SuppressMessage(
        "SonarAnalyzer.CSharp",
        "S3776: Cognitive Complexity of methods should not be too high",
        Justification = "Reviewed. Due to necessary checks as part of the fluent builder pattern")
    ]
    private async Task<TResult?> ExecuteExtractAndTransformResponse<TResult>(
        Func<XElement?, TResult?> transform,
        bool requiresResponseContent = true,
        bool requiresResponseBodyContent = true)
        where TResult : class
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.LoggerRequired);
        }

        if (CancellationToken == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.CancellationTokenRequired);
        }

        if (TargetDescription == null)
        {
            throw new InvalidOperationException(HttpStrategyConstants.Errors.ApiDescriptionRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException(HttpStrategyConstants.Errors.BaseUrlRequired);
        }

        if (string.IsNullOrWhiteSpace(ServiceUrl))
        {
            throw new InvalidOperationException(SoapStrategyConstants.Errors.ServiceUrlRequired);
        }

        if (string.IsNullOrWhiteSpace(SoapAction))
        {
            throw new InvalidOperationException(SoapStrategyConstants.Errors.SoapActionRequired);
        }

        if (string.IsNullOrWhiteSpace(MediaType))
        {
            throw new InvalidOperationException(HttpStrategyConstants.Errors.MediaTypeRequired);
        }

        if (PayloadAction == null)
        {
            throw new InvalidOperationException(HttpStrategyConstants.Errors.PayloadActionRequired);
        }

        EnsureOperatorHasRequiredPermissions();

        LogExecutingAction();

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        var schemaSet = SoapSchemaBuilder?.Build();
        var payload = PayloadAction();

        EmmitVerboseOutput("Request Body Untransformed:", payload?.ToString());

        if (ValidatePreTransformPayloadSchema)
        {
            if (schemaSet == null)
            {
                throw new InvalidOperationException(SoapStrategyConstants.Errors.PayloadSchemasRequired);
            }

            ValidateSchema(schemaSet, payload, ValidatePreTransformPayloadSchemaTargetElementName);
        }

        if (PayloadTransformerAction != null)
        {
            payload = PayloadTransformerAction(payload);

            EmmitVerboseOutput("Request Body Transformed:", payload?.ToString());
        }

        if (ValidatePayloadSchema)
        {
            if (schemaSet == null)
            {
                throw new InvalidOperationException(SoapStrategyConstants.Errors.PayloadSchemasRequired);
            }

            ValidateSchema(schemaSet, payload, ValidatePayloadSchemaTargetElementName);
        }

        soapHttpClient
            .WithVerboseOutput(VerboseOutputAction)
            .WithBaseUrl(BaseUrl)
            .WithSoapAction(SoapAction)
            .WithMediaType(MediaType)
            .WithXmlDeclaration(IncludeXmlDeclaration);

        foreach (var header in Headers)
        {
            soapHttpClient.WithHeader(header.Key, header.Value);
        }

        var soapResponse = await soapHttpClient.PostAsync(ServiceUrl, payload!, CancellationToken.Value);

        if (requiresResponseContent && !soapResponse.HasContent)
        {
            throw new SoapResponseException(HttpStrategyConstants.Errors.ResponseContentRequired);
        }

        if (requiresResponseBodyContent && !soapResponse.HasBodyContent)
        {
            throw new SoapResponseException(SoapStrategyConstants.Errors.SoapResponseBodyRequired);
        }

        if (soapResponse.HasSoapFault)
        {
            throw new SoapResponseException(
                $"SOAP Fault occurred with code '{soapResponse.SoapFaultCode}' and details '{soapResponse.SoapFaultDetails}'");
        }

        var bodyContent = soapResponse.BodyContent;

        if (ResponseTransformerAction != null)
        {
            bodyContent = ResponseTransformerAction(bodyContent);

            EmmitVerboseOutput("Response Body Transformed:", bodyContent?.ToString());
        }

        var transformedResponse = transform(bodyContent);

        await InvokeAfterExecuteAction();

        LogSuccessfullyExecutedAction();

        return transformedResponse;
    }

    private void EmmitVerboseOutput(string description, string? data)
    {
        VerboseOutputAction?.Invoke(description, data);
    }
}
