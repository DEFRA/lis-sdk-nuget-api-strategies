// <copyright file="SoapSchemaBuilder.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Schemas.Builders;

using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Schemas.Builders;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Schemas.Resolvers;

public class SoapSchemaBuilder : ISoapSchemaBuilder
{
    private readonly SoapSchemaXmlResolver soapSchemaXmlResolver = new();
    private readonly XmlSchemaSet schemaSet;

    public SoapSchemaBuilder()
    {
        schemaSet = new XmlSchemaSet { XmlResolver = soapSchemaXmlResolver };
    }

    public ISoapSchemaBuilder Add(params string[] schemaFiles)
    {
        ArgumentNullException.ThrowIfNull(schemaFiles);

        return Add((IEnumerable<string>)schemaFiles);
    }

    public ISoapSchemaBuilder Add(IEnumerable<string> schemaFiles)
    {
        ArgumentNullException.ThrowIfNull(schemaFiles);

        foreach (var schemaFile in schemaFiles)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schemaFile);

            var resolvedPath = File.Exists(schemaFile)
                ? schemaFile
                : Path.Combine(AppContext.BaseDirectory, schemaFile);

            if (!File.Exists(resolvedPath))
            {
                throw new SoapSchemaException($"Schema file '{schemaFile}' not found.");
            }

            var fullPath = Path.GetFullPath(resolvedPath);

            soapSchemaXmlResolver.RegisterFile(fullPath);

            var settings = new XmlReaderSettings { XmlResolver = soapSchemaXmlResolver };

            using var stream = File.OpenRead(fullPath);
            using var reader = XmlReader.Create(stream, settings, new Uri(fullPath).AbsoluteUri);

            schemaSet.Add(null, reader);
        }

        return this;
    }

    public ISoapSchemaBuilder Add(Assembly assembly, string resourceNamespace)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceNamespace);

        var matchedResourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(r => r.Equals(resourceNamespace, StringComparison.OrdinalIgnoreCase));

        if (matchedResourceName == null)
        {
            throw new SoapSchemaException(
                $"No embedded schema resources found in assembly '{assembly.FullName}' matching resource namespace '{resourceNamespace}'");
        }

        soapSchemaXmlResolver.RegisterResource(assembly, matchedResourceName);

        using var stream = assembly.GetManifestResourceStream(matchedResourceName)
                           ?? throw new SoapSchemaException(
                               $"Unable to open embedded resource stream for '{matchedResourceName}'");

        var settings = new XmlReaderSettings { XmlResolver = soapSchemaXmlResolver };

        var baseUri = $"embedded://{assembly.GetName().Name}/{matchedResourceName}";

        using var reader = XmlReader.Create(stream, settings, baseUri);

        schemaSet.Add(null, reader);

        return this;
    }

    public XmlSchemaSet Build()
    {
        if (schemaSet.Count == 0)
        {
            throw new SoapSchemaException("At least one schema must be added to the schema builder.");
        }

        schemaSet.Compile();

        return schemaSet;
    }
}
