// <copyright file="SoapSchemaBuilderTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Soap.Schemas.Builders;

using System.Reflection;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Schemas.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Schemas.Resolvers;
using Shouldly;
using Xunit;

public class SoapSchemaBuilderTests
{
    private const string EmbeddedSchemaResourceName = "Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.TestData.Soap.Schemas.EmbeddedTestSchema.xsd";
    private static readonly string FileSchemaFilePath = Path.Combine(AppContext.BaseDirectory, "TestFramework", "TestData", "Soap", "Schemas", "FileTestSchema.xsd");

    [Fact]
    public void Build_WithoutSchemas_ShouldThrowSoapSchemaException()
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act & Assert
        var ex = Should.Throw<SoapSchemaException>(() => builder.Build());
        ex.Message.ShouldBe("At least one schema must be added to the schema builder.");
    }

    [Fact]
    public void Add_WithNullSchemaFilesArray_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.Add((string[])null!));
    }

    [Fact]
    public void Add_WithNullSchemaFilesEnumerable_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.Add((IEnumerable<string>)null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Add_WithInvalidSchemaFileString_ShouldThrowArgumentException(string? invalidFile)
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.Add(invalidFile!));
    }

    [Fact]
    public void Add_WithNonExistentSchemaFile_ShouldThrowSoapSchemaException()
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act & Assert
        var ex = Should.Throw<SoapSchemaException>(() => builder.Add("non-existent-schema.xsd"));
        ex.Message.ShouldBe("Schema file 'non-existent-schema.xsd' not found.");
    }

    [Fact]
    public void Add_WithValidSchemaFile_ShouldBuildSchemaSet()
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act
        var returnedBuilder = builder.Add(FileSchemaFilePath);
        var schemaSet = builder.Build();

        // Assert
        returnedBuilder.ShouldBe(builder);
        schemaSet.ShouldNotBeNull();
        schemaSet.IsCompiled.ShouldBeTrue();
        schemaSet.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Add_WithValidEmbeddedResource_ShouldBuildSchemaSet()
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act
        var returnedBuilder = builder.Add(Assembly.GetExecutingAssembly(), EmbeddedSchemaResourceName);
        var schemaSet = builder.Build();

        // Assert
        returnedBuilder.ShouldBe(builder);
        schemaSet.ShouldNotBeNull();
        schemaSet.IsCompiled.ShouldBeTrue();
        schemaSet.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Add_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
        {
            builder.Add((Assembly)null!, "resource");
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Add_WithInvalidResourceNamespace_ShouldThrowArgumentException(string? resourceNamespace)
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.Add(Assembly.GetExecutingAssembly(), resourceNamespace!));
    }

    [Fact]
    public void Add_WithNonExistentResourceNamespace_ShouldThrowSoapSchemaException()
    {
        // Arrange
        var builder = new SoapSchemaBuilder();

        // Act & Assert
        var ex = Should.Throw<SoapSchemaException>(() => builder.Add(Assembly.GetExecutingAssembly(), "NonExistent.Resource.xsd"));
        ex.Message.ShouldContain("No embedded schema resources found in assembly");
    }

    [Fact]
    public void SoapSchemaXmlResolver_ShouldHandleFileRegistrationAndFallback()
    {
        // Arrange
        var resolver = new SoapSchemaXmlResolver();
        resolver.Credentials = System.Net.CredentialCache.DefaultCredentials;
        resolver.RegisterFile(FileSchemaFilePath);

        // Act
        var uri = resolver.ResolveUri(null, FileSchemaFilePath);
        uri.ShouldNotBeNull();
        var entity = resolver.GetEntity(uri, null, typeof(Stream));

        // Assert
        entity.ShouldNotBeNull();
        entity.ShouldBeAssignableTo<Stream>();
        ((Stream)entity).Dispose();
    }

    [Fact]
    public void SoapSchemaXmlResolver_ShouldHandleResourceRegistration()
    {
        // Arrange
        var resolver = new SoapSchemaXmlResolver();
        resolver.RegisterResource(Assembly.GetExecutingAssembly(), EmbeddedSchemaResourceName);

        // Act
        var uri = resolver.ResolveUri(null, $"embedded://Defra.Livestock.Sdk.Api.Strategies.Tests/{EmbeddedSchemaResourceName}");
        uri.ShouldNotBeNull();
        var entity = resolver.GetEntity(uri, null, typeof(Stream));

        // Assert
        entity.ShouldNotBeNull();
        entity.ShouldBeAssignableTo<Stream>();
        ((Stream)entity).Dispose();
    }

    [Fact]
    public void SoapSchemaXmlResolver_ResolveUri_WithRelativeResourceName_MatchesAssembly()
    {
        // Arrange
        var resolver = new SoapSchemaXmlResolver();
        resolver.RegisterResource(Assembly.GetExecutingAssembly(), EmbeddedSchemaResourceName);

        // Act
        var uri = resolver.ResolveUri(null, "EmbeddedTestSchema.xsd");
        uri.ShouldNotBeNull();
        var entity = resolver.GetEntity(uri, null, typeof(Stream));

        // Assert
        uri.Scheme.ShouldBe("embedded");
        entity.ShouldNotBeNull();
        ((Stream)entity).Dispose();
    }

    [Fact]
    public void SoapSchemaXmlResolver_ResolveUri_WithBlankRelativeUri_ShouldReturnBaseOrAboutBlank()
    {
        // Arrange
        var resolver = new SoapSchemaXmlResolver();
        var baseUri = new Uri("http://example.com");

        // Act
        var uriWithoutBase = resolver.ResolveUri(null, string.Empty);
        var uriWithBase = resolver.ResolveUri(baseUri, " ");

        // Assert
        uriWithoutBase.ShouldBe(new Uri("about:blank"));
        uriWithBase.ShouldBe(baseUri);
    }

    [Fact]
    public void SoapSchemaXmlResolver_ResolveUri_WithUnmatchedUri_FallsBackToFallbackResolver()
    {
        // Arrange
        var resolver = new SoapSchemaXmlResolver();
        var baseUri = new Uri("https://example.com/schemas/");

        // Act
        var uri = resolver.ResolveUri(baseUri, "remote.xsd");

        // Assert
        uri.ShouldBe(new Uri("https://example.com/schemas/remote.xsd"));
    }

    [Fact]
    public void SoapSchemaXmlResolver_GetEntity_WithLocalFileUri_ReturnsFileStream()
    {
        // Arrange
        var resolver = new SoapSchemaXmlResolver();
        var fileUri = new Uri(FileSchemaFilePath);

        // Act
        var entity = resolver.GetEntity(fileUri, null, typeof(Stream));

        // Assert
        entity.ShouldNotBeNull();
        entity.ShouldBeAssignableTo<Stream>();
        ((Stream)entity).Dispose();
    }
}
