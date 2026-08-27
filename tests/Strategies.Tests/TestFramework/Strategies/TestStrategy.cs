// <copyright file="TestStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;

using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;

public sealed class TestStrategy : StrategyBase<TestService, TestStrategy>
{
    public TestStrategy()
    {
        SetParentBuilder(this);
    }

    public ILogger<TestService>? GetLogger() => Logger;

    public CancellationToken? GetCancellationToken() => CancellationToken;

    public string? GetActionDescription() => ActionDescription;

    public TestStrategy WithTargetDescription(string target)
    {
        TargetDescription = target;
        return this;
    }

    public Task CallInvokeBeforeExecuteAction() => InvokeBeforeExecuteAction();

    public Task CallInvokeAfterExecuteAction() => InvokeAfterExecuteAction();

    public void CallEnsureOperatorHasRequiredPermissions() => EnsureOperatorHasRequiredPermissions();

    public Task CallExecuteRequestValidation() => ExecuteRequestValidation();

    public void CallLogExecutingAction() => LogExecutingAction();

    public void CallLogSuccessfullyExecutedAction() => LogSuccessfullyExecutedAction();

    public void CallLogExecutingActionWithId(string id) => LogExecutingActionWithId(id);

    public void CallLogSuccessfullyExecutedActionWithId(string id) => LogSuccessfullyExecutedActionWithId(id);

    public string CallGetOperatorLoggableId() => GetOperatorLoggableId();
}
