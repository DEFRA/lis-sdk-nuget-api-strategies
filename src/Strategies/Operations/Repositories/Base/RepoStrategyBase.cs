// <copyright file="RepoStrategyBase.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Base;

using System.Diagnostics.CodeAnalysis;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Repositories.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;

/// <summary>
/// Base class for repository strategy builders.
/// </summary>
/// <typeparam name="TService">The type of the service class that is consuming the derived builder.</typeparam>
/// <typeparam name="TParent">The type of the derived builder.</typeparam>
[ExcludeFromCodeCoverage]
public abstract partial class RepoStrategyBase<TService, TParent> : StrategyBase<TService, TParent>,
    IRepoStrategy<TService, TParent>
    where TService : class
    where TParent : class
{
    public TParent WithEntityDescription(string entityDescription)
    {
        TargetDescription = entityDescription;
        return GetParentBuilder();
    }

    protected void LogEntityWithIdNotFound(string id)
    {
        EnsureLoggingPreRequisitesProvided();

        if (Logger != null && ActionDescription != null && TargetDescription != null)
        {
            LogEntityWithIdNotFound(
                Logger,
                TargetDescription,
                id);
        }
    }
}
