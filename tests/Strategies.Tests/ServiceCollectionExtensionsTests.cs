// <copyright file="ServiceCollectionExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests;

using Defra.Livestock.Sdk.Api.Strategies;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Resilience.CircuitBreaker.Policies;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Context;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Resilience.CircuitBreaker.Policies;
using Defra.Livestock.Sdk.Api.Strategies.Requests.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;
using Shouldly;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStrategyFramework_ShouldRegisterOperatorContextAndValidatorsAndPolicyFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddStrategyFramework();

        // Assert
        returnedServices.ShouldBe(services);

        var operatorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IOperatorContext));

        operatorDescriptor.ShouldNotBeNull();
        operatorDescriptor.ImplementationType.ShouldBe(typeof(OperatorContext));
        operatorDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);

        var validatorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IValidator<PagedQuery>));

        validatorDescriptor.ShouldNotBeNull();
        validatorDescriptor.ImplementationType.ShouldBe(typeof(PagedQueryValidator));

        var policyFactoryDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(ICircuitBreakerPolicyFactory));

        policyFactoryDescriptor.ShouldNotBeNull();
        policyFactoryDescriptor.ImplementationType.ShouldBe(typeof(CircuitBreakerPolicyFactory));
        policyFactoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddCircuitBreakerPolicyFactory_ShouldRegisterTransientPolicyFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddCircuitBreakerPolicyFactory();

        // Assert
        returnedServices.ShouldBe(services);

        var factoryDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ICircuitBreakerPolicyFactory));

        factoryDescriptor.ShouldNotBeNull();
        factoryDescriptor.ImplementationType.ShouldBe(typeof(CircuitBreakerPolicyFactory));
        factoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddStrategyOperatorContext_ShouldRegisterScopedOperatorContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddStrategyOperatorContext();

        // Assert
        returnedServices.ShouldBe(services);

        var operatorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IOperatorContext));

        operatorDescriptor.ShouldNotBeNull();
        operatorDescriptor.ImplementationType.ShouldBe(typeof(OperatorContext));
        operatorDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddStrategyValidators_ShouldRegisterValidatorsFromExecutingAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddStrategyValidators();

        // Assert
        returnedServices.ShouldBe(services);

        var validatorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IValidator<PagedQuery>));

        validatorDescriptor.ShouldNotBeNull();
        validatorDescriptor.ImplementationType.ShouldBe(typeof(PagedQueryValidator));
    }

    [Fact]
    public void AddRepoStrategyFactory_ShouldRegisterTransientRepoStrategyFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddRepoStrategyFactory<TestService>();

        // Assert
        returnedServices.ShouldBe(services);

        var factoryDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(IRepoStrategyFactory<TestService>));

        factoryDescriptor.ShouldNotBeNull();
        factoryDescriptor.ImplementationType.ShouldBe(typeof(RepoStrategyFactory<TestService>));
        factoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddSoapStrategyFactory_ShouldRegisterHttpClientAndTransientSoapStrategyFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddSoapStrategyFactory<TestService>();

        // Assert
        returnedServices.ShouldBe(services);

        var httpClientDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISoapHttpClient));

        httpClientDescriptor.ShouldNotBeNull();

        var factoryDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(ISoapStrategyFactory<TestService>));

        factoryDescriptor.ShouldNotBeNull();
        factoryDescriptor.ImplementationType.ShouldBe(typeof(SoapStrategyFactory<TestService>));
        factoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddSoapStrategyFactory_WhenSoapHttpClientAlreadyRegistered_ShouldNotReRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<ISoapHttpClient, SoapHttpClient>();

        // Act
        services.AddSoapStrategyFactory<TestService>();

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(ISoapHttpClient));
        count.ShouldBe(1);
    }

    [Fact]
    public void AddRestStrategyFactory_ShouldRegisterHttpClientAndTransientRestStrategyFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddRestStrategyFactory<TestService>();

        // Assert
        returnedServices.ShouldBe(services);

        var httpClientDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IRestHttpClient));

        httpClientDescriptor.ShouldNotBeNull();

        var factoryDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(IRestStrategyFactory<TestService>));

        factoryDescriptor.ShouldNotBeNull();
        factoryDescriptor.ImplementationType.ShouldBe(typeof(RestStrategyFactory<TestService>));
        factoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRestStrategyFactory_WhenRestHttpClientAlreadyRegistered_ShouldNotReRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<IRestHttpClient, RestHttpClient>();

        // Act
        services.AddRestStrategyFactory<TestService>();

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(IRestHttpClient));

        count.ShouldBe(1);
    }

    [Fact]
    public void AddCircuitBreakerStrategyFactory_ShouldRegisterRegistryAndTransientFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddCircuitBreakerStrategyFactory<TestService>();

        // Assert
        returnedServices.ShouldBe(services);

        var registryDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(ResiliencePipelineRegistry<string>));

        registryDescriptor.ShouldNotBeNull();
        registryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        var factoryDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(ICircuitBreakerStrategyFactory<TestService>));

        factoryDescriptor.ShouldNotBeNull();
        factoryDescriptor.ImplementationType.ShouldBe(typeof(CircuitBreakerStrategyFactory<TestService>));
        factoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddCircuitBreakerStrategyFactory_WhenRegistryAlreadyRegistered_ShouldNotReRegisterRegistry()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new ResiliencePipelineRegistry<string>());

        // Act
        services.AddCircuitBreakerStrategyFactory<TestService>();

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(ResiliencePipelineRegistry<string>));

        count.ShouldBe(1);
    }

    [Fact]
    public void AddRepoStrategyFactory_WithContextConfigureAction_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddRepoStrategyFactory<TestService>(null!));
    }

    [Fact]
    public void
        AddRepoStrategyFactory_WithContextConfigureAction_ShouldRegisterFactoryAndInvokeConfigureWithContextOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddStrategyOperatorContext();

        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;

        // Act
        var returnedServices = services.AddRepoStrategyFactory<TestService>((factory, context) =>
        {
            capturedContext = context;
            factory.WithDefaultEntityDescription("TestEntity");
        });

        // Assert
        returnedServices.ShouldBe(services);

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IRepoStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
    }

    [Fact]
    public void
        AddRepoStrategyFactory_WithOptions_WithContextConfigureAction_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddRepoStrategyFactory<TestService, TestOptions>(null!));
    }

    [Fact]
    public void
        AddRepoStrategyFactory_WithOptions_WithContextConfigureAction_ShouldRegisterFactoryAndInvokeConfigureWithContextAndOptionsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;
        string? configuredValue = null;

        services.AddLogging();
        services.AddStrategyOperatorContext();
        services.Configure<TestOptions>(opt => opt.Value = "ConfiguredEntity");

        // Act
        var returnedServices = services.AddRepoStrategyFactory<TestService, TestOptions>((factory, context, options) =>
        {
            capturedContext = context;
            configuredValue = options.Value.Value;
            factory.WithDefaultEntityDescription(options.Value.Value);
        });

        // Assert
        returnedServices.ShouldBe(services);

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IRepoStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
        configuredValue.ShouldBe("ConfiguredEntity");
    }

    [Fact]
    public void AddSoapStrategyFactory_WithContextConfigureAction_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddSoapStrategyFactory<TestService>(null!));
    }

    [Fact]
    public void
        AddSoapStrategyFactory_WithContextConfigureAction_ShouldRegisterHttpClientAndFactoryAndInvokeConfigureWithContextOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;

        services.AddLogging();
        services.AddStrategyOperatorContext();

        // Act
        var returnedServices = services.AddSoapStrategyFactory<TestService>((factory, context) =>
        {
            capturedContext = context;
            factory.WithDefaultSoapAction("action");
        });

        // Assert
        returnedServices.ShouldBe(services);

        var httpClientDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISoapHttpClient));

        httpClientDescriptor.ShouldNotBeNull();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ISoapStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
    }

    [Fact]
    public void
        AddSoapStrategyFactory_WithContextConfigureAction_WhenSoapHttpClientAlreadyRegistered_ShouldNotReRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ISoapHttpClient, SoapHttpClient>();

        // Act
        services.AddSoapStrategyFactory<TestService>((_, _) => { });

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(ISoapHttpClient));

        count.ShouldBe(1);
    }

    [Fact]
    public void
        AddSoapStrategyFactory_WithOptions_WithContextConfigureAction_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddSoapStrategyFactory<TestService, TestOptions>(null!));
    }

    [Fact]
    public void
        AddSoapStrategyFactory_WithOptions_WithContextConfigureAction_ShouldRegisterHttpClientAndFactoryAndInvokeConfigureWithContextAndOptionsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;
        string? configuredValue = null;

        services.AddLogging();
        services.AddStrategyOperatorContext();
        services.Configure<TestOptions>(opt => opt.Value = "action");

        // Act
        var returnedServices = services.AddSoapStrategyFactory<TestService, TestOptions>((factory, context, options) =>
        {
            capturedContext = context;
            configuredValue = options.Value.Value;
            factory.WithDefaultSoapAction(options.Value.Value);
        });

        // Assert
        returnedServices.ShouldBe(services);

        var httpClientDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISoapHttpClient));

        httpClientDescriptor.ShouldNotBeNull();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ISoapStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
        configuredValue.ShouldBe("action");
    }

    [Fact]
    public void
        AddSoapStrategyFactory_WithOptions_WithContextConfigureAction_WhenSoapHttpClientAlreadyRegistered_ShouldNotReRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<ISoapHttpClient, SoapHttpClient>();

        // Act
        services.AddSoapStrategyFactory<TestService, TestOptions>((_, _, _) => { });

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(ISoapHttpClient));

        count.ShouldBe(1);
    }

    [Fact]
    public void AddRestStrategyFactory_WithContextConfigureAction_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddRestStrategyFactory<TestService>(null!));
    }

    [Fact]
    public void
        AddRestStrategyFactory_WithContextConfigureAction_ShouldRegisterHttpClientAndFactoryAndInvokeConfigureWithContextOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;

        services.AddLogging();
        services.AddStrategyOperatorContext();

        // Act
        var returnedServices = services.AddRestStrategyFactory<TestService>((factory, context) =>
        {
            capturedContext = context;
            factory.WithDefaultBaseUrl("https://api.example.com");
        });

        // Assert
        returnedServices.ShouldBe(services);

        var httpClientDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IRestHttpClient));

        httpClientDescriptor.ShouldNotBeNull();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IRestStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
    }

    [Fact]
    public void
        AddRestStrategyFactory_WithContextConfigureAction_WhenRestHttpClientAlreadyRegistered_ShouldNotReRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRestHttpClient, RestHttpClient>();

        // Act
        services.AddRestStrategyFactory<TestService>((_, _) => { });

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(IRestHttpClient));

        count.ShouldBe(1);
    }

    [Fact]
    public void
        AddRestStrategyFactory_WithOptions_WithContextConfigureAction_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddRestStrategyFactory<TestService, TestOptions>(null!));
    }

    [Fact]
    public void
        AddRestStrategyFactory_WithOptions_WithContextConfigureAction_ShouldRegisterHttpClientAndFactoryAndInvokeConfigureWithContextAndOptionsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;
        string? configuredValue = null;

        services.AddLogging();
        services.AddStrategyOperatorContext();
        services.Configure<TestOptions>(opt => opt.Value = "https://api.example.com");

        // Act
        var returnedServices = services.AddRestStrategyFactory<TestService, TestOptions>((factory, context, options) =>
        {
            capturedContext = context;
            configuredValue = options.Value.Value;
            factory.WithDefaultBaseUrl(options.Value.Value);
        });

        // Assert
        returnedServices.ShouldBe(services);

        var httpClientDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IRestHttpClient));

        httpClientDescriptor.ShouldNotBeNull();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IRestStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
        configuredValue.ShouldBe("https://api.example.com");
    }

    [Fact]
    public void
        AddRestStrategyFactory_WithOptions_WithContextConfigureAction_WhenRestHttpClientAlreadyRegistered_ShouldNotReRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRestHttpClient, RestHttpClient>();

        // Act
        services.AddRestStrategyFactory<TestService, TestOptions>((_, _, _) => { });

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(IRestHttpClient));

        count.ShouldBe(1);
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithContextConfigureAction_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddCircuitBreakerStrategyFactory(
#pragma warning disable SA1003
                (Action<ICircuitBreakerStrategyFactory<TestService>, StrategyFactoryRegistrationContext<TestService>>)
#pragma warning restore SA1003
                null!));
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithContextConfigureAction_ShouldRegisterRegistryAndFactoryAndInvokeConfigureWithContextOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;

        services.AddLogging();
        services.AddStrategyOperatorContext();

        // Act
        var returnedServices = services.AddCircuitBreakerStrategyFactory<TestService>((factory, context) =>
        {
            capturedContext = context;
            factory.WithDefaultCircuitKey("test-circuit");
        });

        // Assert
        returnedServices.ShouldBe(services);

        var registryDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(ResiliencePipelineRegistry<string>));

        registryDescriptor.ShouldNotBeNull();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ICircuitBreakerStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithContextConfigureAction_WhenRegistryAlreadyRegistered_ShouldNotReRegisterRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new ResiliencePipelineRegistry<string>());

        // Act
        services.AddCircuitBreakerStrategyFactory<TestService>((_, _) => { });

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(ResiliencePipelineRegistry<string>));

        count.ShouldBe(1);
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithContextConfigureActionWithPolicyFactory_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddCircuitBreakerStrategyFactory(
                (Action<ICircuitBreakerStrategyFactory<TestService>, StrategyFactoryRegistrationContext<TestService>,
                    ICircuitBreakerPolicyFactory>)null!));
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithContextConfigureActionWithPolicyFactory_ShouldRegisterDependenciesAndInvokeConfigureOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;
        ICircuitBreakerPolicyFactory? capturedPolicyFactory = null;

        services.AddLogging();
        services.AddStrategyOperatorContext();

        // Act
        var returnedServices =
            services.AddCircuitBreakerStrategyFactory<TestService>((factory, context, policyFactory) =>
            {
                capturedContext = context;
                capturedPolicyFactory = policyFactory;
                var policy = policyFactory.CreateHttpPolicy();
                factory.WithDefaultPolicy(policy);
            });

        // Assert
        returnedServices.ShouldBe(services);

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<ICircuitBreakerStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
        capturedPolicyFactory.ShouldNotBeNull();
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithContextConfigureActionWithPolicyFactory_WhenAlreadyRegistered_ShouldNotReRegisterDependencies()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new ResiliencePipelineRegistry<string>());
        services.AddCircuitBreakerPolicyFactory();

        // Act
        services.AddCircuitBreakerStrategyFactory<TestService>((_, _, _) => { });

        // Assert
        services.Count(sd => sd.ServiceType == typeof(ResiliencePipelineRegistry<string>)).ShouldBe(1);
        services.Count(sd => sd.ServiceType == typeof(ICircuitBreakerPolicyFactory)).ShouldBe(1);
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithOptions_WithContextConfigureActionWithPolicyFactoryAndOptions_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddCircuitBreakerStrategyFactory<TestService, TestOptions>(null!));
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithOptions_WithContextConfigureActionWithPolicyFactoryAndOptions_ShouldRegisterDependenciesAndInvokeConfigureOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        StrategyFactoryRegistrationContext<TestService>? capturedContext = null;
        ICircuitBreakerPolicyFactory? capturedPolicyFactory = null;
        string? configuredValue = null;

        services.AddLogging();
        services.AddStrategyOperatorContext();
        services.Configure<TestOptions>(opt => opt.Value = "custom-circuit-3");

        // Act
        var returnedServices =
            services.AddCircuitBreakerStrategyFactory<TestService, TestOptions>((
                factory,
                context,
                policyFactory,
                options) =>
            {
                capturedContext = context;
                capturedPolicyFactory = policyFactory;
                configuredValue = options.Value.Value;
                var policy = policyFactory.CreateHttpPolicy();
                factory.WithDefaultPolicy(policy).WithDefaultCircuitKey(options.Value.Value);
            });

        // Assert
        returnedServices.ShouldBe(services);

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<ICircuitBreakerStrategyFactory<TestService>>();

        factory.ShouldNotBeNull();
        capturedContext.ShouldNotBeNull();
        capturedContext.ServiceProvider.ShouldNotBeNull();
        capturedContext.OperatorContext.ShouldNotBeNull();
        capturedContext.Logger.ShouldNotBeNull();
        capturedPolicyFactory.ShouldNotBeNull();
        configuredValue.ShouldBe("custom-circuit-3");
    }

    [Fact]
    public void
        AddCircuitBreakerStrategyFactory_WithOptions_WithContextConfigureActionWithPolicyFactoryAndOptions_WhenAlreadyRegistered_ShouldNotReRegisterDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new ResiliencePipelineRegistry<string>());
        services.AddCircuitBreakerPolicyFactory();

        // Act
        services.AddCircuitBreakerStrategyFactory<TestService, TestOptions>((_, _, _, _) => { });

        // Assert
        services.Count(sd => sd.ServiceType == typeof(ResiliencePipelineRegistry<string>)).ShouldBe(1);
        services.Count(sd => sd.ServiceType == typeof(ICircuitBreakerPolicyFactory)).ShouldBe(1);
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class TestOptions
    {
        public string Value { get; set; } = "default-value";
    }
}
