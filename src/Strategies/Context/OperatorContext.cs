// <copyright file="OperatorContext.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Context;

using System.Security.Claims;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;

public class OperatorContext : IOperatorContext
{
    private const string OperatorHasNotBeenSet = "Operator has not been set";
    private const string OperatorIsAlreadySet = "Operator is already set";
    private const string OperatorIdClaimType = ClaimTypes.NameIdentifier;
    private const string OperatorNameClaimType = ClaimTypes.Name;
    private const string OperatorEmailClaimType = ClaimTypes.Email;
    private const string OperatorRoleClaimType = ClaimTypes.Role;

    private Operator? currentOperator;

    public Operator Operator
    {
        get => currentOperator ?? throw new InvalidOperationException(OperatorHasNotBeenSet);

        private set
        {
            if (currentOperator != null)
            {
                throw new InvalidOperationException(OperatorIsAlreadySet);
            }

            currentOperator = value;
        }
    }

    public bool HasOperator => currentOperator != null;

    public bool HasAuthenticatedOperator => currentOperator?.IsAuthenticated ?? false;

    public IOperatorContext SetAuthenticatedOperatorById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (currentOperator != null)
        {
            throw new InvalidOperationException(OperatorIsAlreadySet);
        }

        Operator = new Operator(id, true);

        return this;
    }

    public IOperatorContext SetOperatorByClaimsPrincipal(ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var operatorId = claimsPrincipal.FindFirst(OperatorIdClaimType)?.Value;
        var operatorRoles = claimsPrincipal.FindAll(OperatorRoleClaimType).Select(c => c.Value).ToArray();
        var isAuthenticated = claimsPrincipal.Identity?.IsAuthenticated ?? false;

        var name = claimsPrincipal.FindFirst(OperatorNameClaimType)?.Value;
        var email = claimsPrincipal.FindFirst(OperatorEmailClaimType)?.Value;

        if (operatorId == null)
        {
            throw new InvalidOperationException($"Operator id claim '{OperatorIdClaimType}' not found");
        }

        Operator = new Operator(operatorId, name, email, operatorRoles, isAuthenticated);

        return this;
    }
}
