// <copyright file="StrategyConstants.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;

internal static class StrategyConstants
{
    public static class Errors
    {
        public const string LoggerRequired = "Logger must be provided for this operation";
        public const string CancellationTokenRequired = "Cancellation token must be provided for this operation";
        public const string OperatorContextRequired = "Operator context must be provided for this operation";

        public const string OperatorContextAuthenticatedOperatorRequired =
            "Authenticated operator must be provided for this operation";

        public const string TargetDescriptionRequired = "Target description must be provided for this operation";
        public const string ActionDescriptionRequired = "Action description must be provided for this operation";
    }
}
