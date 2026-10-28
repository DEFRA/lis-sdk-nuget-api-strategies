// <copyright file="TestHttpMessageHandler.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Handlers;

public sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? handler;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? asyncCancellableHandler;

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        this.handler = handler;
    }

    public TestHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> asyncCancellableHandler)
    {
        this.asyncCancellableHandler = asyncCancellableHandler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return asyncCancellableHandler != null
            ? asyncCancellableHandler(request, cancellationToken)
            : Task.FromResult(handler!(request));
    }
}
