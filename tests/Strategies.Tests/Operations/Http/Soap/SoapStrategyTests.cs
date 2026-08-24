// <copyright file="SoapStrategyTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Soap;

using System.Reflection;
using System.Security.Authentication;
using System.Xml.Linq;
using System.Xml.Schema;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Models;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Serializer;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestServices;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using TestResult = Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData.TestResult;

public class SoapStrategyTests
{
    private const string TestSchemaEmbeddedResource = "Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData.Soap.Schemas.EmbeddedTestSchema.xsd";
    private static readonly string TestSchemaFilePath = Path.Combine(AppContext.BaseDirectory, "TestFramework", "TestData", "Soap", "Schemas", "FileTestSchema.xsd");
    private readonly ISoapHttpClient soapHttpClient = Substitute.For<ISoapHttpClient>();
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();

    public SoapStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        soapHttpClient.WithVerboseOutput(Arg.Any<Action<string, string?>>()).Returns(soapHttpClient);
        soapHttpClient.WithBaseUrl(Arg.Any<string>()).Returns(soapHttpClient);
        soapHttpClient.WithSoapAction(Arg.Any<string>()).Returns(soapHttpClient);
        soapHttpClient.WithMediaType(Arg.Any<string>()).Returns(soapHttpClient);
        soapHttpClient.WithXmlDeclaration(Arg.Any<bool>()).Returns(soapHttpClient);
        soapHttpClient.WithHeader(Arg.Any<string>(), Arg.Any<string>()).Returns(soapHttpClient);
    }

    [Fact]
    public void FluentSetters_WithValidInputs_ShouldReturnStrategy()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);

        // Act & Assert
        strategy.WithServiceUrl("service").ShouldBe(strategy);
        strategy.WithSoapAction("urn:action").ShouldBe(strategy);
        strategy.WithXmlDeclaration(true).ShouldBe(strategy);
        strategy.WithVerboseOutput((desc, data) => { }).ShouldBe(strategy);
        strategy.WithPayload(() => new XElement("payload")).ShouldBe(strategy);
        strategy.WithPayload(new TestEntity { Id = "1", Name = "A" }).ShouldBe(strategy);
        strategy.WithPayload(() => new TestEntity { Id = "1", Name = "A" }).ShouldBe(strategy);
        strategy.WithValidatePreTransformPayloadSchema("payload").ShouldBe(strategy);
        strategy.WithValidatePayloadSchema("payload").ShouldBe(strategy);
        strategy.WithPayloadTransformer(e => e).ShouldBe(strategy);
        strategy.WithResponseTransformer(e => e).ShouldBe(strategy);
        strategy.WithSchemas(b => { }).ShouldBe(strategy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithServiceUrl_WithInvalidUrl_ShouldThrowArgumentException(string? url)
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithServiceUrl(url!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithSoapAction_WithInvalidAction_ShouldThrowArgumentException(string? action)
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithSoapAction(action!));
    }

    [Fact]
    public async Task Execute_WhenLoggerMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(StrategyConstants.Errors.LoggerRequired);
    }

    [Fact]
    public async Task Execute_WhenCancellationTokenMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        strategy.WithLogger(logger);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task Execute_WhenApiDescriptionMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        strategy.WithLogger(logger).WithCancellationToken(CancellationToken.None);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(HttpStrategyConstants.Errors.ApiDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WhenActionDescriptionMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        strategy.WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("API");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WhenBaseUrlMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        strategy.WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("API")
            .WithActionDescription("Action");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(HttpStrategyConstants.Errors.BaseUrlRequired);
    }

    [Fact]
    public async Task Execute_WhenServiceUrlMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        strategy.WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("API")
            .WithActionDescription("Action")
            .WithBaseUrl("https://example.com");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(SoapStrategyConstants.Errors.ServiceUrlRequired);
    }

    [Fact]
    public async Task Execute_WhenSoapActionMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        strategy.WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("API")
            .WithActionDescription("Action")
            .WithBaseUrl("https://example.com")
            .WithServiceUrl("service");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(SoapStrategyConstants.Errors.SoapActionRequired);
    }

    [Fact]
    public async Task Execute_WhenMediaTypeMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        strategy.WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("API")
            .WithActionDescription("Action")
            .WithBaseUrl("https://example.com")
            .WithServiceUrl("service")
            .WithSoapAction("urn:action");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(HttpStrategyConstants.Errors.MediaTypeRequired);
    }

    [Fact]
    public async Task Execute_WhenPayloadActionMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        strategy.WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("API")
            .WithActionDescription("Action")
            .WithBaseUrl("https://example.com")
            .WithServiceUrl("service")
            .WithSoapAction("urn:action")
            .WithMediaType("text/xml");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(HttpStrategyConstants.Errors.PayloadActionRequired);
    }

    [Fact]
    public async Task Execute_WhenOperatorUnauthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(false);
        operatorContext.Operator.Returns(new Operator("op-1", false));

        strategy
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() => strategy.Execute());
    }

    [Fact]
    public async Task Execute_WhenOperatorContextMissingButRequired_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        strategy.WithRequiresAuthenticatedOperator();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(StrategyConstants.Errors.OperatorContextRequired);
    }

    [Fact]
    public async Task Execute_WhenRequestValidationFails_ShouldThrowRequestValidationExceptionAndLogWarning()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        strategy.WithRequestValidation(() => Task.FromResult(new RequestValidationResult(new[] { new RequestValidationFailure("Name", "Name is required") })));

        // Act & Assert
        var ex = await Should.ThrowAsync<RequestValidationException>(() => strategy.Execute());
        ex.Errors.Count().ShouldBe(1);

        logger.ShouldHaveReceived(LogLevel.Warning, "Execute do action [sample api] failed validation");
    }

    [Fact]
    public async Task Execute_WhenPreTransformSchemaValidationEnabledWithoutSchemas_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        strategy.WithValidatePreTransformPayloadSchema();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(SoapStrategyConstants.Errors.PayloadSchemasRequired);
    }

    [Fact]
    public async Task Execute_WhenPayloadSchemaValidationEnabledWithoutSchemas_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        strategy.WithValidatePayloadSchema();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe(SoapStrategyConstants.Errors.PayloadSchemasRequired);
    }

    [Fact]
    public async Task Execute_WithValidExecutionFlow_ShouldCallAllCallbacksInOrderAndReturnResult()
    {
        // Arrange
        var executionOrder = new List<string>();
        var verboseMessages = new List<(string Description, string? Data)>();

        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var responseXml = new XElement("Response", new XElement("Result", "OK"));
        var soapResponse = new SoapResponse
        {
            HasContent = true,
            HasBody = true,
            BodyContent = responseXml,
        };

        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        strategy
            .WithBeforeExecute(() =>
            {
                executionOrder.Add("BeforeExecute");
                return Task.CompletedTask;
            })
            .WithPayloadTransformer(xml =>
            {
                executionOrder.Add("PayloadTransformer");
                return xml;
            })
            .WithResponseTransformer(xml =>
            {
                executionOrder.Add("ResponseTransformer");
                return xml;
            })
            .WithAfterExecute(() =>
            {
                executionOrder.Add("AfterExecute");
                return Task.CompletedTask;
            })
            .WithVerboseOutput((desc, data) => verboseMessages.Add((desc, data)))
            .WithHeader("X-Test", "123");

        // Act
        var result = await strategy.Execute();

        // Assert
        result.ShouldBe(responseXml);
        executionOrder.ShouldBe(["BeforeExecute", "PayloadTransformer", "ResponseTransformer", "AfterExecute"]);
        verboseMessages.Count.ShouldBeGreaterThanOrEqualTo(2);

        logger.ShouldHaveReceived(LogLevel.Information, "Executing do action [sample api] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed do action [sample api] by operator ");
    }

    [Fact]
    public async Task Execute_WithPayloadObject_ShouldSerializeAndExecute()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        var requestPayload = new TestEntity { Id = "123", Name = "DirectPayload" };
        ConfigureValidStrategyWithObjectPayload(strategy, requestPayload);

        var responseXml = new XElement("Response", new XElement("Result", "OK"));
        var soapResponse = new SoapResponse
        {
            HasContent = true,
            HasBody = true,
            BodyContent = responseXml,
        };

        XElement? capturedPayload = null;
        soapHttpClient.PostAsync("service", Arg.Do<XElement>(xml => capturedPayload = xml), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act
        var result = await strategy.Execute();

        // Assert
        result.ShouldBe(responseXml);
        capturedPayload.ShouldNotBeNull();
        capturedPayload.Element("Id")?.Value.ShouldBe("123");
        capturedPayload.Element("Name")?.Value.ShouldBe("DirectPayload");
        logger.ShouldHaveReceived(LogLevel.Information, "Executing do action [sample api] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed do action [sample api] by operator ");
    }

    [Fact]
    public async Task Execute_WithPayloadFactory_ShouldSerializeAndExecute()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        var requestPayload = new TestEntity { Id = "456", Name = "FactoryPayload" };
        ConfigureValidStrategyWithPayloadFactory(strategy, () => requestPayload);

        var responseXml = new XElement("Response", new XElement("Result", "OK"));
        var soapResponse = new SoapResponse
        {
            HasContent = true,
            HasBody = true,
            BodyContent = responseXml,
        };

        XElement? capturedPayload = null;
        soapHttpClient.PostAsync("service", Arg.Do<XElement>(xml => capturedPayload = xml), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act
        var result = await strategy.Execute();

        // Assert
        result.ShouldBe(responseXml);
        capturedPayload.ShouldNotBeNull();
        capturedPayload.Element("Id")?.Value.ShouldBe("456");
        capturedPayload.Element("Name")?.Value.ShouldBe("FactoryPayload");
        logger.ShouldHaveReceived(LogLevel.Information, "Executing do action [sample api] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed do action [sample api] by operator ");
    }

    [Fact]
    public async Task ExecuteGeneric_WithValidExecutionFlow_ShouldDeserializeResult()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var entity = new TestEntity { Id = "999", Name = "Deserialized" };
        var responseXml = SoapSerializer.SerializeToXElement(entity);
        var soapResponse = new SoapResponse
        {
            HasContent = true,
            HasBody = true,
            BodyContent = responseXml,
        };

        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act
        var result = await strategy.Execute<TestEntity>();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe("999");
        result.Name.ShouldBe("Deserialized");
    }

    [Fact]
    public async Task ExecuteGeneric_WithXElementGeneric_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<XElement>());
        ex.Message.ShouldBe("Cannot deserialize to XElement");
    }

    [Fact]
    public async Task ExecuteAndTransform_WithXElementTransform_ShouldReturnTransformed()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var responseXml = new XElement("Result", new XElement("Value", "123"));
        var soapResponse = new SoapResponse
        {
            HasContent = true,
            HasBody = true,
            BodyContent = responseXml,
        };

        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act
        var result = await strategy.ExecuteAndTransform(xml => new TestResult { MappedName = xml.Element("Value")?.Value ?? string.Empty });

        // Assert
        result.ShouldNotBeNull();
        result.MappedName.ShouldBe("123");
    }

    [Fact]
    public async Task ExecuteAndTransform_WithTypedTransform_ShouldReturnTransformed()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var entity = new TestEntity { Id = "789", Name = "TransformedName" };
        var responseXml = SoapSerializer.SerializeToXElement(entity);
        var soapResponse = new SoapResponse
        {
            HasContent = true,
            HasBody = true,
            BodyContent = responseXml,
        };

        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act
        var result = await strategy.ExecuteAndTransform<TestEntity, TestResult>(e => new TestResult { MappedName = e.Name });

        // Assert
        result.ShouldNotBeNull();
        result.MappedName.ShouldBe("TransformedName");
    }

    [Fact]
    public async Task ExecuteWithoutResponse_WithSuccessfulCall_ShouldCompleteAndLog()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var soapResponse = new SoapResponse
        {
            HasContent = false,
            HasBody = false,
            BodyContent = null,
        };

        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act
        await strategy.ExecuteWithoutResponse();

        // Assert
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed do action [sample api] by operator ");
    }

    [Fact]
    public async Task Execute_WhenResponseContentRequiredButMissing_ShouldThrowSoapResponseException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var soapResponse = new SoapResponse
        {
            HasContent = false,
            HasBody = false,
            BodyContent = null,
        };

        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapResponseException>(() => strategy.Execute());
        ex.Message.ShouldBe(HttpStrategyConstants.Errors.ResponseContentRequired);
    }

    [Fact]
    public async Task Execute_WhenResponseBodyContentRequiredButMissing_ShouldThrowSoapResponseException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var soapResponse = new SoapResponse
        {
            HasContent = true,
            HasBody = true,
            BodyContent = null,
        };

        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapResponseException>(() => strategy.Execute());
        ex.Message.ShouldBe(SoapStrategyConstants.Errors.SoapResponseBodyRequired);
    }

    [Fact]
    public async Task Execute_WhenSoapFaultOccurred_ShouldThrowSoapResponseException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var soapResponse = new SoapResponse
        {
            HasContent = true,
            HasBody = true,
            BodyContent = new XElement("Fault"),
            HasSoapFault = true,
            SoapFaultCode = "Client",
            SoapFaultDetails = "Invalid payload format",
        };

        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(soapResponse);

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapResponseException>(() => strategy.Execute());
        ex.Message.ShouldBe("SOAP Fault occurred with code 'Client' and details 'Invalid payload format'");
    }

    [Fact]
    public async Task Execute_WithSchemaValidation_WhenPayloadMatchesSchema_ShouldPass()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        var ns = XNamespace.Get("http://example.com/test");
        strategy
            .WithSchemas(b => b.Add(TestSchemaFilePath))
            .WithPayload(() => new XElement(ns + "Payload", new XElement(ns + "Id", "123")))
            .WithValidatePreTransformPayloadSchema()
            .WithValidatePayloadSchema();

        var responseXml = new XElement("Response");
        soapHttpClient.PostAsync("service", Arg.Any<XElement>(), Arg.Any<CancellationToken>())
            .Returns(new SoapResponse { HasContent = true, HasBody = true, BodyContent = responseXml });

        // Act
        var result = await strategy.Execute();

        // Assert
        result.ShouldBe(responseXml);
    }

    [Fact]
    public async Task Execute_WithSchemaValidation_WhenTargetElementNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        strategy
            .WithSchemas(b => b.Add(Assembly.GetExecutingAssembly(), TestSchemaEmbeddedResource))
            .WithPayload(() => new XElement("DifferentElement"))
            .WithValidatePayloadSchema("NonExistentElement");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute());
        ex.Message.ShouldBe("Element with local name 'NonExistentElement' not found in the provided content");
    }

    [Fact]
    public async Task Execute_WhenPayloadIsNullAndSchemaValidationEnabled_ThrowsXmlSchemaValidationException()
    {
        // Arrange
        var strategy = new SoapStrategy<TestService>(soapHttpClient);
        ConfigureValidStrategyWithXElementPayload(strategy);

        strategy
            .WithSchemas(b => b.Add(TestSchemaFilePath))
            .WithPayload(() => (XElement)null!)
            .WithValidatePreTransformPayloadSchema();

        // Act & Assert
        var ex = await Should.ThrowAsync<System.Xml.Schema.XmlSchemaValidationException>(() => strategy.Execute());
        ex.Message.ShouldBe("Schema validation error: No element was provided for schema validation");
    }

    private void ConfigureValidStrategyWithXElementPayload(SoapStrategy<TestService> strategy)
    {
        strategy
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("Sample API")
            .WithActionDescription("Do Action")
            .WithBaseUrl("https://example.com")
            .WithServiceUrl("service")
            .WithSoapAction("urn:sample:action")
            .WithMediaType("text/xml")
            .WithPayload(() => new XElement("Request"));
    }

    private void ConfigureValidStrategyWithObjectPayload<TRequest>(SoapStrategy<TestService> strategy, TRequest payload)
    {
        strategy
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("Sample API")
            .WithActionDescription("Do Action")
            .WithBaseUrl("https://example.com")
            .WithServiceUrl("service")
            .WithSoapAction("urn:sample:action")
            .WithMediaType("text/xml")
            .WithPayload(payload);
    }

    private void ConfigureValidStrategyWithPayloadFactory<TRequest>(SoapStrategy<TestService> strategy, Func<TRequest> payloadFactory)
    {
        strategy
            .WithLogger(logger)
            .WithCancellationToken(CancellationToken.None)
            .WithApiDescription("Sample API")
            .WithActionDescription("Do Action")
            .WithBaseUrl("https://example.com")
            .WithServiceUrl("service")
            .WithSoapAction("urn:sample:action")
            .WithMediaType("text/xml")
            .WithPayload(payloadFactory);
    }
}
