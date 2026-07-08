// <copyright file="ServiceCollectionExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies;

using System.Reflection;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Context;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddStrategyFramework()
        {
            services.AddStrategyOperatorContext();
            services.AddStrategyValidators();

            return services;
        }

        public IServiceCollection AddStrategyOperatorContext()
        {
            services.AddScoped<IOperatorContext, OperatorContext>();

            return services;
        }

        public IServiceCollection AddStrategyValidators()
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        public IServiceCollection AddRepoStrategyFactory<TService>()
            where TService : class
        {
            services.AddTransient<IRepoStrategyFactory<TService>, RepoStrategyFactory<TService>>();

            return services;
        }
    }
}
