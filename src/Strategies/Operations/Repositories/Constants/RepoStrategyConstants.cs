// <copyright file="RepoStrategyConstants.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Repositories.Constants;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal static class RepoStrategyConstants
{
    public static class Errors
    {
        public const string LoggerRequired = "Logger must be provided for this operation";
        public const string CancellationTokenRequired = "Cancellation token must be provided for this operation";
        public const string OperatorContextRequired = "Operator context must be provided for this operation";

        public const string OperatorContextAuthenticatedOperatorRequired =
            "Authenticated operator must be provided for this operation";

        public const string GettableRepositoryRequired = "Gettable repository must be provided for this operation";
        public const string PageableRepositoryRequired = "Pageable repository must be provided for this operation";
        public const string ListableRepositoryRequired = "Listable repository must be provided for this operation";
        public const string CreatableRepositoryRequired = "Creatable repository must be provided for this operation";
        public const string UpdatableRepositoryRequired = "Updateable repository must be provided for this operation";

        public const string PrimaryEntityDescriptionRequired =
            "Primary entity description must be provided for this operation";

        public const string ActionDescriptionRequired = "Action description must be provided for this operation";

        public const string RequestAndEntityFilterRequired =
            "Request and entity filter must be provided for this operation";

        public const string EntityFilterRequired = "Entity filter must be provided for this operation";
        public const string CreateActionRequired = "Create action must be provided for this operation";
        public const string UpdateActionRequired = "Update action must be provided for this operation";
    }
}
