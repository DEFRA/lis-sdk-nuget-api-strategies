// <copyright file="ServiceCollectionExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies;

using System.Reflection;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker.Policies;
using Defra.Livestock.Sdk.Api.Strategies.Context;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Policies;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddStrategyFramework()
        {
            services.AddStrategyOperatorContext();
            services.AddStrategyValidators();
            services.AddCircuitBreakerPolicyFactory();

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

        public IServiceCollection AddCircuitBreakerPolicyFactory()
        {
            services.AddTransient<ICircuitBreakerPolicyFactory, CircuitBreakerPolicyFactory>();

            return services;
        }

        public IServiceCollection AddRepoStrategyFactory<TService>()
            where TService : class
        {
            services.AddTransient<IRepoStrategyFactory<TService>, RepoStrategyFactory<TService>>();

            return services;
        }

        public IServiceCollection AddRepoStrategyFactory<TService>(
            Action<IRepoStrategyFactory<TService>, StrategyFactoryRegistrationContext<TService>> configure)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.AddTransient<IRepoStrategyFactory<TService>>(sp =>
            {
                var factory = new RepoStrategyFactory<TService>();
                var context = CreateRegistrationContext<TService>(sp);

                configure(factory, context);

                return factory;
            });

            return services;
        }

        public IServiceCollection AddRepoStrategyFactory<TService, TOptions>(
            Action<IRepoStrategyFactory<TService>, StrategyFactoryRegistrationContext<TService>, IOptions<TOptions>>
                configure)
            where TService : class
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.AddTransient<IRepoStrategyFactory<TService>>(sp =>
            {
                var factory = new RepoStrategyFactory<TService>();
                var context = CreateRegistrationContext<TService>(sp);
                var options = sp.GetRequiredService<IOptions<TOptions>>();

                if (options.Value is null)
                {
                    throw new InvalidOperationException($"Options value for '{typeof(TOptions).Name}' cannot be null.");
                }

                configure(factory, context, options);

                return factory;
            });

            return services;
        }

        public IServiceCollection AddSoapStrategyFactory<TService>()
            where TService : class
        {
            services.RegisterSoapHttpClientIfNotRegistered();

            services.AddTransient<ISoapStrategyFactory<TService>, SoapStrategyFactory<TService>>();

            return services;
        }

        public IServiceCollection AddSoapStrategyFactory<TService>(
            Action<ISoapStrategyFactory<TService>, StrategyFactoryRegistrationContext<TService>> configure)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.RegisterSoapHttpClientIfNotRegistered();

            services.AddTransient<ISoapStrategyFactory<TService>>(sp =>
            {
                var factory = new SoapStrategyFactory<TService>(sp);
                var context = CreateRegistrationContext<TService>(sp);

                configure(factory, context);

                return factory;
            });

            return services;
        }

        public IServiceCollection AddSoapStrategyFactory<TService, TOptions>(
            Action<ISoapStrategyFactory<TService>, StrategyFactoryRegistrationContext<TService>, IOptions<TOptions>>
                configure)
            where TService : class
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.RegisterSoapHttpClientIfNotRegistered();

            services.AddTransient<ISoapStrategyFactory<TService>>(sp =>
            {
                var factory = new SoapStrategyFactory<TService>(sp);
                var context = CreateRegistrationContext<TService>(sp);
                var options = sp.GetRequiredService<IOptions<TOptions>>();

                if (options.Value is null)
                {
                    throw new InvalidOperationException($"Options value for '{typeof(TOptions).Name}' cannot be null.");
                }

                configure(factory, context, options);

                return factory;
            });

            return services;
        }

        public IServiceCollection AddRestStrategyFactory<TService>()
            where TService : class
        {
            services.RegisterRestHttpClientIfNotRegistered();

            services.AddTransient<IRestStrategyFactory<TService>, RestStrategyFactory<TService>>();

            return services;
        }

        public IServiceCollection AddRestStrategyFactory<TService>(
            Action<IRestStrategyFactory<TService>, StrategyFactoryRegistrationContext<TService>> configure)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.RegisterRestHttpClientIfNotRegistered();

            services.AddTransient<IRestStrategyFactory<TService>>(sp =>
            {
                var factory = new RestStrategyFactory<TService>(sp);
                var context = CreateRegistrationContext<TService>(sp);

                configure(factory, context);

                return factory;
            });

            return services;
        }

        public IServiceCollection AddRestStrategyFactory<TService, TOptions>(
            Action<IRestStrategyFactory<TService>, StrategyFactoryRegistrationContext<TService>, IOptions<TOptions>>
                configure)
            where TService : class
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.RegisterRestHttpClientIfNotRegistered();

            services.AddTransient<IRestStrategyFactory<TService>>(sp =>
            {
                var factory = new RestStrategyFactory<TService>(sp);
                var context = CreateRegistrationContext<TService>(sp);
                var options = sp.GetRequiredService<IOptions<TOptions>>();

                if (options.Value is null)
                {
                    throw new InvalidOperationException($"Options value for '{typeof(TOptions).Name}' cannot be null.");
                }

                configure(factory, context, options);

                return factory;
            });

            return services;
        }

        public IServiceCollection AddCircuitBreakerStrategyFactory<TService>()
            where TService : class
        {
            services.RegisterResiliencePipelineRegistryIfNotRegistered();

            services.AddTransient<ICircuitBreakerStrategyFactory<TService>, CircuitBreakerStrategyFactory<TService>>();

            return services;
        }

        public IServiceCollection AddCircuitBreakerStrategyFactory<TService>(
            Action<ICircuitBreakerStrategyFactory<TService>, StrategyFactoryRegistrationContext<TService>> configure)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.RegisterResiliencePipelineRegistryIfNotRegistered();

            services.AddTransient<ICircuitBreakerStrategyFactory<TService>>(sp =>
            {
                var factory = new CircuitBreakerStrategyFactory<TService>(sp);
                var context = CreateRegistrationContext<TService>(sp);

                configure(factory, context);

                return factory;
            });

            return services;
        }

        public IServiceCollection AddCircuitBreakerStrategyFactory<TService>(
            Action<ICircuitBreakerStrategyFactory<TService>,
                StrategyFactoryRegistrationContext<TService>,
                ICircuitBreakerPolicyFactory> configure)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.RegisterResiliencePipelineRegistryIfNotRegistered();
            services.RegisterCircuitBreakerPolicyFactoryIfNotRegistered();

            services.AddTransient<ICircuitBreakerStrategyFactory<TService>>(sp =>
            {
                var factory = new CircuitBreakerStrategyFactory<TService>(sp);
                var context = CreateRegistrationContext<TService>(sp);
                var policyFactory = sp.GetRequiredService<ICircuitBreakerPolicyFactory>();

                configure(factory, context, policyFactory);

                return factory;
            });

            return services;
        }

        public IServiceCollection AddCircuitBreakerStrategyFactory<TService, TOptions>(
            Action<ICircuitBreakerStrategyFactory<TService>,
                StrategyFactoryRegistrationContext<TService>,
                ICircuitBreakerPolicyFactory,
                IOptions<TOptions>> configure)
            where TService : class
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.RegisterResiliencePipelineRegistryIfNotRegistered();
            services.RegisterCircuitBreakerPolicyFactoryIfNotRegistered();

            services.AddTransient<ICircuitBreakerStrategyFactory<TService>>(sp =>
            {
                var factory = new CircuitBreakerStrategyFactory<TService>(sp);
                var context = CreateRegistrationContext<TService>(sp);
                var options = sp.GetRequiredService<IOptions<TOptions>>();

                if (options.Value is null)
                {
                    throw new InvalidOperationException($"Options value for '{typeof(TOptions).Name}' cannot be null.");
                }

                var policyFactory = sp.GetRequiredService<ICircuitBreakerPolicyFactory>();

                configure(factory, context, policyFactory, options);

                return factory;
            });

            return services;
        }

        private void RegisterCircuitBreakerPolicyFactoryIfNotRegistered()
        {
            if (!services.IsServiceRegistered(typeof(ICircuitBreakerPolicyFactory)))
            {
                services.AddCircuitBreakerPolicyFactory();
            }
        }

        private void RegisterResiliencePipelineRegistryIfNotRegistered()
        {
            if (!services.IsServiceRegistered(typeof(ResiliencePipelineRegistry<string>)))
            {
                services.AddSingleton<ResiliencePipelineRegistry<string>>();
            }
        }

        private void RegisterSoapHttpClientIfNotRegistered()
        {
            if (!services.IsServiceRegistered(typeof(ISoapHttpClient)))
            {
                services.AddHttpClient<ISoapHttpClient, SoapHttpClient>();
            }
        }

        private void RegisterRestHttpClientIfNotRegistered()
        {
            if (!services.IsServiceRegistered(typeof(IRestHttpClient)))
            {
                services.AddHttpClient<IRestHttpClient, RestHttpClient>();
            }
        }

        private bool IsServiceRegistered(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            return services.Any(sd => sd.ServiceType == serviceType);
        }
    }

    private static StrategyFactoryRegistrationContext<TService> CreateRegistrationContext<TService>(
        IServiceProvider serviceProvider)
        where TService : class
    {
        var logger = serviceProvider.GetRequiredService<ILogger<TService>>();
        var operatorContext = serviceProvider.GetRequiredService<IOperatorContext>();

        return new StrategyFactoryRegistrationContext<TService>(
            serviceProvider,
            operatorContext,
            logger);
    }
}
