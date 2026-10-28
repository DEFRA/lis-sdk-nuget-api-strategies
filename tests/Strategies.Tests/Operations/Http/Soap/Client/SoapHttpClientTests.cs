// <copyright file="SoapHttpClientTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Soap.Client;

using System.Net;
using System.Text;
using System.Xml.Linq;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Handlers;
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
        client.WithTimeout(5000).ShouldBe(client);
        client.WithVerboseOutput((_, _) => { }).ShouldBe(client);
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
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.PostAsync("test", new XElement("payload"), TestContext.Current.CancellationToken));

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
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.PostAsync("test", new XElement("payload"), TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("Soap action must be set before making a soap request.");
    }

    [Fact]
    public async Task PostAsync_WithoutMediaType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.PostAsync("test", new XElement("payload"), TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("Media type must be set before making a soap request.");
    }

    [Fact]
    public async Task PostAsync_WhenHttpFails_ShouldThrowSoapResponseException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test")
            .WithMediaType("text/xml");

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapResponseException>(() =>
            client.PostAsync("endpoint", new XElement("Request"), TestContext.Current.CancellationToken));

        ex.Message.ShouldContain("HTTP Response Error:");
    }

    [Fact]
    public async Task PostAsync_WithSuccessfulResponse_ShouldReturnSoapResponseAndLog()
    {
        // Arrange
        var verboseMessages = new List<(string Description, string? Data)>();
        HttpRequestMessage? capturedRequest = null;

        const string soapResponseBody = "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
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

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml")
            .WithHeader("X-Custom-Header", "HeaderValue")
            .WithXmlDeclaration(true)
            .WithVerboseOutput((desc, data) => verboseMessages.Add((desc, data)));

        // Act
        var response = await client.PostAsync(
            "soap/service",
            new XElement("TestRequest", new XElement("Id", 123)),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotBeNull();

        response.ShouldSatisfyAllConditions(
            x => x.HasContent.ShouldBeTrue(),
            x => x.HasBody.ShouldBeTrue(),
            x => x.BodyContent.ShouldNotBeNull(),
            x => x.BodyContent!.Name.LocalName.ShouldBe("TestResponse"),
            x => x.HasSoapFault.ShouldBeFalse(),
            x => x.SoapFaultCode.ShouldBeNull(),
            x => x.SoapFaultDetails.ShouldBeNull());

        capturedRequest.ShouldNotBeNull();

        capturedRequest.ShouldSatisfyAllConditions(
            x => x.RequestUri.ShouldBe(new Uri("https://example.com/soap/service")),
            x => x.Headers.GetValues("SOAPAction").First().ShouldBe("urn:test:action"),
            x => x.Headers.GetValues("X-Custom-Header").First().ShouldBe("HeaderValue"));

        verboseMessages.Count.ShouldBeGreaterThanOrEqualTo(3);

        logger.ShouldHaveReceived(LogLevel.Information, "Calling SOAP endpoint 'https://example.com/soap/service' ...");

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Successfully called SOAP endpoint 'https://example.com/soap/service'");
    }

    [Fact]
    public async Task PostAsync_WithSoapFault_ShouldParseFaultDetails()
    {
        // Arrange
        const string soapResponseBody = "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
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

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync(
            "soap/service",
            new XElement("TestRequest"),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldSatisfyAllConditions(
            x => x.HasContent.ShouldBeTrue(),
            x => x.HasBody.ShouldBeTrue(),
            x => x.HasSoapFault.ShouldBeTrue(),
            x => x.SoapFaultCode.ShouldBe("Client"),
            x => x.SoapFaultDetails.ShouldBe("Invalid input"));
    }

    [Fact]
    public async Task PostAsync_WithNoContentStatusCode_ShouldReturnEmptySoapResponse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync(
            "soap/service",
            new XElement("TestRequest"),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldSatisfyAllConditions(
            x => x.HasContent.ShouldBeFalse(),
            x => x.HasBody.ShouldBeFalse(),
            x => x.BodyContent.ShouldBeNull());
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

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync(
            "soap/service",
            new XElement("TestRequest"),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldSatisfyAllConditions(
            x => x.HasContent.ShouldBeTrue(),
            x => x.HasBody.ShouldBeFalse(),
            x => x.BodyContent.ShouldBeNull());
    }

    [Fact]
    public async Task PostAsync_WhenBodyElementIsEmpty_ShouldReturnHasContentTrueHasBodyTrueAndBodyContentNull()
    {
        // Arrange
        const string soapResponseBody = "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                                        "  <soap:Body/>" +
                                        "</soap:Envelope>";

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(soapResponseBody, Encoding.UTF8, "text/xml"),
        });

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync(
            "soap/service",
            new XElement("TestRequest"),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldSatisfyAllConditions(
            x => x.HasContent.ShouldBeTrue(),
            x => x.HasBody.ShouldBeTrue(),
            x => x.BodyContent.ShouldBeNull());
    }

    [Fact]
    public async Task PostAsync_WhenContentIsNull_ShouldReturnEmptySoapResponse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = null, });
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync(
            "soap/service",
            new XElement("TestRequest"),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldSatisfyAllConditions(
            x => x.HasContent.ShouldBeFalse(),
            x => x.HasBody.ShouldBeFalse(),
            x => x.BodyContent.ShouldBeNull());
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

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync(
            "soap/service",
            new XElement("TestRequest"),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldSatisfyAllConditions(
            x => x.HasContent.ShouldBeFalse(),
            x => x.HasBody.ShouldBeFalse(),
            x => x.BodyContent.ShouldBeNull());
    }

    [Fact]
    public async Task PostAsync_WithSoapFaultWithoutCodeOrDetails_ShouldParseFaultWithNullFields()
    {
        // Arrange
        const string soapResponseBody = "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
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

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync(
            "soap/service",
            new XElement("TestRequest"),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldSatisfyAllConditions(
            x => x.HasContent.ShouldBeTrue(),
            x => x.HasBody.ShouldBeTrue(),
            x => x.HasSoapFault.ShouldBeTrue(),
            x => x.SoapFaultCode.ShouldBeNull(),
            x => x.SoapFaultDetails.ShouldBeNull());
    }

    [Fact]
    public async Task PostAsync_WhenCallerCancellationTokenCancelled_ThrowsSoapRequestExceptionWrappingOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapRequestException>(() =>
            client.PostAsync("soap/service", new XElement("TestRequest"), cts.Token));
        ex.InnerException.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task PostAsync_WhenTimeoutOccurs_ThrowsSoapRequestExceptionWrappingOperationCanceledException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(500, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml")
            .WithTimeout(50);

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapRequestException>(() =>
            client.PostAsync("soap/service", new XElement("TestRequest")));
        ex.InnerException.ShouldBeAssignableTo<OperationCanceledException>();
        logger.ShouldHaveReceived(LogLevel.Warning, "Http request timed out after 50 milliseconds");
    }

    [Fact]
    public async Task PostAsync_WhenOperationCanceledExceptionWithTimeoutInnerOccurs_LogsTimeoutAndThrowsSoapRequestException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) =>
            throw new OperationCanceledException("Timed out", new TimeoutException("Timeout occurred")));

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapRequestException>(() =>
            client.PostAsync("soap/service", new XElement("TestRequest")));

        ex.Message.ShouldContain("HTTP Request Error: Timed out");
        ex.InnerException.ShouldBeOfType<OperationCanceledException>();
        logger.ShouldHaveReceived(LogLevel.Warning, "Http request timed out after 0 milliseconds");
    }

    [Fact]
    public async Task PostAsync_WhenOperationCanceledExceptionByAnotherSourceOccurs_LogsCancellationAndThrowsSoapRequestException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) =>
            throw new OperationCanceledException("Cancelled by external source"));

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapRequestException>(() =>
            client.PostAsync("soap/service", new XElement("TestRequest")));

        ex.Message.ShouldContain("HTTP Request Error: Cancelled by external source");
        ex.InnerException.ShouldBeOfType<OperationCanceledException>();
        logger.ShouldHaveReceived(LogLevel.Warning, "Http request was cancelled by another source");
    }

    [Fact]
    public async Task PostAsync_WhenGenericExceptionOccurs_ThrowsSoapRequestException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("Fatal networking fault"));

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act & Assert
        var ex = await Should.ThrowAsync<SoapRequestException>(() =>
            client.PostAsync("soap/service", new XElement("TestRequest")));

        ex.Message.ShouldContain("HTTP Request Error: Fatal networking fault");
        ex.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task PostAsync_WithoutXmlDeclaration_SendsSoapEnvelopeWithoutXmlDeclaration()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new TestHttpMessageHandler(async (req, ct) =>
        {
            if (req.Content != null)
            {
                capturedBody = await req.Content.ReadAsStringAsync(ct);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><TestResponse/></soap:Body></soap:Envelope>",
                    Encoding.UTF8,
                    "text/xml"),
            };
        });

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml")
            .WithXmlDeclaration(false);

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"));

        // Assert
        response.ShouldNotBeNull();
        capturedBody.ShouldNotBeNull();
        capturedBody.ShouldNotStartWith("<?xml");
    }

    [Fact]
    public async Task PostAsync_WithNonSeekableStream_ExtractsResponseSuccessfully()
    {
        // Arrange
        const string soapResponseBody =
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><CustomResult>123</CustomResult></soap:Body></soap:Envelope>";

        var customContent = new StreamContent(new NonSeekableStream(new MemoryStream(Encoding.UTF8.GetBytes(soapResponseBody))));
        customContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = customContent,
        });

        var httpClient = new HttpClient(handler);
        var client = new SoapHttpClient(httpClient, logger);

        client
            .WithBaseUrl("https://example.com")
            .WithSoapAction("urn:test:action")
            .WithMediaType("text/xml");

        // Act
        var response = await client.PostAsync("soap/service", new XElement("TestRequest"));

        // Assert
        response.ShouldNotBeNull();
        response.HasContent.ShouldBeTrue();
        response.HasBody.ShouldBeTrue();
        response.BodyContent.ShouldNotBeNull();
        response.BodyContent!.Name.LocalName.ShouldBe("CustomResult");
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
    }
}
