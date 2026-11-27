// <copyright file="StrategyFactoryRegistrationContext.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Microsoft.Extensions.Logging;

public class StrategyFactoryRegistrationContext<TService>
    where TService : class
{
    public StrategyFactoryRegistrationContext(
        IServiceProvider serviceProvider,
        IOperatorContext operatorContext,
#pragma warning disable S6672
        ILogger<TService> logger)
#pragma warning restore S6672
    {
        ServiceProvider = serviceProvider;
        OperatorContext = operatorContext;
        Logger = logger;
    }

    public IServiceProvider ServiceProvider { get; }

    public IOperatorContext OperatorContext { get; }

    public ILogger<TService> Logger { get; }
}
