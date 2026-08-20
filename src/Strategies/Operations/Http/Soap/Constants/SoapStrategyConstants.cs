// <copyright file="SoapStrategyConstants.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Constants;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal static class SoapStrategyConstants
{
    public static class Errors
    {
        public const string ServiceUrlRequired = "Service url must be provided for this operation";
        public const string SoapActionRequired = "Soap action must be provided for this operation";

        public const string PayloadSchemasRequired = "Payload schemas must be provided for this operation";

        public const string SoapResponseBodyRequired = "Expected response body was not found or could not be extracted";
    }
}
