// <copyright file="HttpStrategyConstants.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Constants;

internal static class HttpStrategyConstants
{
    public static class Errors
    {
        public const string ApiDescriptionRequired = "Api description must be provided for this operation";

        public const string BaseUrlRequired = "Base url must be provided for this operation";
        public const string MediaTypeRequired = "Media type must be provided for this operation";
        public const string PayloadActionRequired = "Payload action must be provided for this operation";

        public const string ResponseContentRequired = "Expected response content was not found";
    }
}
