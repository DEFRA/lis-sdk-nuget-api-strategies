// <copyright file="SoapHttpClientTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Soap.Client;

using System.Net;
using System.Text;
using System.Xml.Linq;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class SoapHttpClientTests
{
    private readonly ILogger<SoapHttpClient> logger = Substitute.For<ILogger<SoapHttpClient>>();

    public SoapHttpClientTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void FluentSetters_WithValidInputs_ShouldReturnInstance()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);

        // Act & Assert
        client.WithBaseUrl("https://example.com/api").ShouldBe(client);
        client.WithSoapAction("urn:test:action").ShouldBe(client);
        client.WithMediaType("text/xml").ShouldBe(client);
        client.WithHeader("X-Custom", "Value").ShouldBe(client);
        client.WithHeader("X-Custom", "OverwrittenValue").ShouldBe(client);
        client.WithXmlDeclaration(true).ShouldBe(client);
        client.WithVerboseOutput((desc, data) => { }).ShouldBe(client);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithBaseUrl_WithInvalidValue_ShouldThrowArgumentException(string? baseUrl)
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);

        // Act & Assert
        Should.Throw<ArgumentException>(() => client.WithBaseUrl(baseUrl!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithSoapAction_WithInvalidValue_ShouldThrowArgumentException(string? soapAction)
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);

        // Act & Assert
        Should.Throw<ArgumentException>(() => client.WithSoapAction(soapAction!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithMediaType_WithInvalidValue_ShouldThrowArgumentException(string? mediaType)
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);

        // Act & Assert
        Should.Throw<ArgumentException>(() => client.WithMediaType(mediaType!));
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData(" ", "value")]
    [InlineData("name", null)]
    [InlineData("name", "")]
    [InlineData("name", " ")]
    public void WithHeader_WithInvalidValue_ShouldThrowArgumentException(string? name, string? value)
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);

        // Act & Assert
        Should.Throw<ArgumentException>(() => client.WithHeader(name!, value!));
    }

    [Fact]
    public async Task PostAsync_WithoutBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => client.PostAsync("test", new XElement("payload"), TestContext.Current.CancellationToken));
        ex.Message.ShouldBe("Base URL must be set before making a soap request.");
    }

    [Fact]
    public async Task PostAsync_WithoutSoapAction_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => client.PostAsync("test", new XElement("payload"), TestContext.Current.CancellationToken));
        ex.Message.ShouldBe("Soap action must be set before making a soap request.");
    }

    [Fact]
    public async Task PostAsync_WithoutMediaType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com");
        client.WithSoapAction("urn:test");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => client.PostAsync("test", new XElement("payload"), TestContext.Current.CancellationToken));
        ex.Message.ShouldBe("Media type must be set before making a soap request.");
    }

    [Fact]
    public async Task PostAsync_WhenHttpFails_ShouldThrowSoapResponseException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test")
            .WithMediaType("text/xml");

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapResponseException>(() => client.PostAsync("endpoint", new XElement("Request"), TestContext.Current.CancellationToken));
        ex.Message.ShouldContain("HTTP Response Error:");
    }

    [Fact]
    public async Task PostAsync_WithSuccessfulResponse_ShouldReturnSoapResponseAndLog()
    {
        // Arrange
        var verboseMessages = new List<(string Description, string? Data)>();
        HttpRequestMessage? capturedRequest = null;

        var soapResponseBody =
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "  <soap:Body>" +
            "    <TestResponse xmlns=\"urn:test\"><Result>Success</Result></TestResponse>" +
            "  </soap:Body>" +
            "</soap:Envelope>";

        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(soapResponseBody, Encoding.UTF8, "text/xml"),
            };
        });

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml")
            .WithHeader("X-Custom-Header", "HeaderValue")
            .WithXmlDeclaration(true)
            .WithVerboseOutput((desc, data) => verboseMessages.Add((desc, data)));

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest", new XElement("Id", 123)), TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.HasContent.ShouldBeTrue();
        response.HasBody.ShouldBeTrue();
        response.BodyContent.ShouldNotBeNull();
        response.BodyContent.Name.LocalName.ShouldBe("TestResponse");
        response.HasSoapFault.ShouldBeFalse();
        response.SoapFaultCode.ShouldBeNull();
        response.SoapFaultDetails.ShouldBeNull();

        capturedRequest.ShouldNotBeNull();
        capturedRequest.RequestUri.ShouldBe(new Uri("https://example.com/soap/service"));
        capturedRequest.Headers.GetValues("SOAPAction").First().ShouldBe("urn:test:action");
        capturedRequest.Headers.GetValues("X-Custom-Header").First().ShouldBe("HeaderValue");

        verboseMessages.Count.ShouldBeGreaterThanOrEqualTo(3);

        logger.ShouldHaveReceived(LogLevel.Information, "Calling SOAP endpoint 'https://example.com/soap/service' ...");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully called SOAP endpoint 'https://example.com/soap/service'");
    }

    [Fact]
    public async Task PostAsync_WithSoapFault_ShouldParseFaultDetails()
    {
        // Arrange
        var soapResponseBody =
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "  <soap:Body>" +
            "    <FaultWrapper>" +
            "      <soap:Fault>" +
            "        <faultcode>Client</faultcode>" +
            "        <faultstring>Invalid input</faultstring>" +
            "      </soap:Fault>" +
            "    </FaultWrapper>" +
            "  </soap:Body>" +
            "</soap:Envelope>";

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(soapResponseBody, Encoding.UTF8, "text/xml"),
        });

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"), TestContext.Current.CancellationToken);

        // Assert
        response.HasContent.ShouldBeTrue();
        response.HasBody.ShouldBeTrue();
        response.HasSoapFault.ShouldBeTrue();
        response.SoapFaultCode.ShouldBe("Client");
        response.SoapFaultDetails.ShouldBe("Invalid input");
    }

    [Fact]
    public async Task PostAsync_WithNoContentStatusCode_ShouldReturnEmptySoapResponse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"), TestContext.Current.CancellationToken);

        // Assert
        response.HasContent.ShouldBeFalse();
        response.HasBody.ShouldBeFalse();
        response.BodyContent.ShouldBeNull();
    }

    [Fact]
    public async Task PostAsync_WhenXmlHasNoBodyElement_ShouldReturnHasContentTrueAndHasBodyFalse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<root><other/></root>", Encoding.UTF8, "text/xml"),
        });
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"), TestContext.Current.CancellationToken);

        // Assert
        response.HasContent.ShouldBeTrue();
        response.HasBody.ShouldBeFalse();
        response.BodyContent.ShouldBeNull();
    }

    [Fact]
    public async Task PostAsync_WhenBodyElementIsEmpty_ShouldReturnHasContentTrueHasBodyTrueAndBodyContentNull()
    {
        // Arrange
        var soapResponseBody =
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "  <soap:Body/>" +
            "</soap:Envelope>";

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(soapResponseBody, Encoding.UTF8, "text/xml"),
        });
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"), TestContext.Current.CancellationToken);

        // Assert
        response.HasContent.ShouldBeTrue();
        response.HasBody.ShouldBeTrue();
        response.BodyContent.ShouldBeNull();
    }

    [Fact]
    public async Task PostAsync_WhenContentIsNull_ShouldReturnEmptySoapResponse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = null,
        });
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"), TestContext.Current.CancellationToken);

        // Assert
        response.HasContent.ShouldBeFalse();
        response.HasBody.ShouldBeFalse();
        response.BodyContent.ShouldBeNull();
    }

    [Fact]
    public async Task PostAsync_WhenContentLengthIsZero_ShouldReturnEmptySoapResponse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "text/xml"),
        });
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"), TestContext.Current.CancellationToken);

        // Assert
        response.HasContent.ShouldBeFalse();
        response.HasBody.ShouldBeFalse();
        response.BodyContent.ShouldBeNull();
    }

    [Fact]
    public async Task PostAsync_WithSoapFaultWithoutCodeOrDetails_ShouldParseFaultWithNullFields()
    {
        // Arrange
        var soapResponseBody =
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "  <soap:Body>" +
            "    <FaultWrapper>" +
            "      <soap:Fault>" +
            "      </soap:Fault>" +
            "    </FaultWrapper>" +
            "  </soap:Body>" +
            "</soap:Envelope>";

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(soapResponseBody, Encoding.UTF8, "text/xml"),
        });

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"), TestContext.Current.CancellationToken);

        // Assert
        response.HasContent.ShouldBeTrue();
        response.HasBody.ShouldBeTrue();
        response.HasSoapFault.ShouldBeTrue();
        response.SoapFaultCode.ShouldBeNull();
        response.SoapFaultDetails.ShouldBeNull();
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            this.handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
