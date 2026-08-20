// <copyright file="SoapSchemaXmlResolver.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Schemas.Resolvers;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Xml;

[ExcludeFromCodeCoverage]
internal sealed class SoapSchemaXmlResolver : XmlResolver
{
    private readonly XmlUrlResolver fallbackResolver = new();
    private readonly Dictionary<string, string> fileMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Assembly> assemblies = [];

    private readonly Dictionary<string, (Assembly Assembly, string ResourceName)> resourceMap =
        new(StringComparer.OrdinalIgnoreCase);

    public override ICredentials Credentials
    {
        set => fallbackResolver.Credentials = value;
    }

    public void RegisterFile(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var fileName = Path.GetFileName(fullPath);

        fileMap[filePath] = fullPath;
        fileMap[fullPath] = fullPath;
        fileMap[fileName] = fullPath;
    }

    public void RegisterResource(Assembly assembly, string resourceName)
    {
        if (!assemblies.Contains(assembly))
        {
            assemblies.Add(assembly);
        }

        resourceMap[resourceName] = (assembly, resourceName);
    }

    public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
    {
        if (string.IsNullOrWhiteSpace(relativeUri))
        {
            return baseUri ?? new Uri("about:blank");
        }

        var normalizedName = relativeUri.Replace('\\', '/');
        var fileName = Path.GetFileName(normalizedName);

        if (fileMap.TryGetValue(relativeUri, out var fullFilePath) ||
            fileMap.TryGetValue(normalizedName, out fullFilePath) ||
            fileMap.TryGetValue(fileName, out fullFilePath))
        {
            return new Uri(fullFilePath);
        }

        if (resourceMap.TryGetValue(relativeUri, out var resourceEntry) ||
            resourceMap.TryGetValue(normalizedName, out resourceEntry) ||
            resourceMap.TryGetValue(fileName, out resourceEntry))
        {
            return new Uri($"embedded://{resourceEntry.Assembly.GetName().Name}/{resourceEntry.ResourceName}");
        }

        foreach (var assembly in assemblies)
        {
            var manifestNames = assembly.GetManifestResourceNames();
            var matched = manifestNames.FirstOrDefault(r =>
                r.Equals(relativeUri, StringComparison.OrdinalIgnoreCase) ||
                r.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                r.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase) ||
                r.EndsWith("." + relativeUri, StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                return new Uri($"embedded://{assembly.GetName().Name}/{matched}");
            }
        }

        return fallbackResolver.ResolveUri(baseUri, relativeUri);
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        if (absoluteUri.Scheme.Equals("embedded", StringComparison.OrdinalIgnoreCase))
        {
            var assemblyName = absoluteUri.Host;
            var resourceName = absoluteUri.AbsolutePath.TrimStart('/');

            var assembly = assemblies.FirstOrDefault(a =>
                               string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                           ?? resourceMap.Values.FirstOrDefault(v =>
                               v.ResourceName.Equals(resourceName, StringComparison.OrdinalIgnoreCase)).Assembly
                           ?? AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                               string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

            if (assembly != null)
            {
                var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream != null)
                {
                    return stream;
                }
            }
        }

        if (absoluteUri.IsFile && File.Exists(absoluteUri.LocalPath))
        {
            return File.OpenRead(absoluteUri.LocalPath);
        }

        return fallbackResolver.GetEntity(absoluteUri, role, ofObjectToReturn);
    }
}
