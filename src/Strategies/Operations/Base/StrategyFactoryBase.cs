// <copyright file="StrategyFactoryBase.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Base;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Base;
using Microsoft.Extensions.Logging;

public abstract class StrategyFactoryBase<TService, TParent> : IStrategyFactory<TService, TParent>
    where TService : class
    where TParent : class
{
    private ILogger<TService>? DefaultLogger { get; set; }

    private IOperatorContext? DefaultOperatorContext { get; set; }

    private TParent? ParentFactory { get; set; }

    public TParent WithDefaultLogger(ILogger<TService> logger)
    {
        DefaultLogger = logger;
        return GetParentFactory();
    }

    public TParent WithDefaultOperatorContext(IOperatorContext operatorContext)
    {
        DefaultOperatorContext = operatorContext;
        return GetParentFactory();
    }

    protected void AttachDefaultsToBuilder<TParentBuilder>(IStrategy<TService, TParentBuilder> strategyBuilder)
        where TParentBuilder : class
    {
        if (DefaultLogger != null)
        {
            strategyBuilder.WithLogger(DefaultLogger);
        }

        if (DefaultOperatorContext != null)
        {
            strategyBuilder.WithOperatorContext(DefaultOperatorContext);
        }
    }

    protected TParent GetParentFactory()
    {
        return ParentFactory ?? throw new InvalidOperationException("The parent factory has not been set.");
    }

    protected void SetParentFactory(TParent parent)
    {
        ParentFactory = parent;
    }
}
