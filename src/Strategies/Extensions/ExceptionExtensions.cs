// <copyright file="ExceptionExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

// ReSharper disable once CheckNamespace
namespace System;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;

public static class ExceptionExtensions
{
    extension(Exception exception)
    {
        public bool IsCircuitBreakerOpen()
        {
            ArgumentNullException.ThrowIfNull(exception);

            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is CircuitBreakerOpenException)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
