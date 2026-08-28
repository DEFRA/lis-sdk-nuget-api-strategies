// <copyright file="SoapSerializer.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Serializer;

using System.Xml.Linq;
using System.Xml.Serialization;

public static class SoapSerializer
{
    private const string FailedToSerializeToXElement = "Failed to serialize object to XElement";
    private const string FailedToDeserializeXElementToObject = "Failed to deserialize XElement to object";

    public static XElement SerializeToXElement<T>(T @object)
    {
        var serializer = new XmlSerializer(typeof(T));

        var doc = new XDocument();

        using (var writer = doc.CreateWriter())
        {
            serializer.Serialize(writer, @object);
        }

        return doc.Root ?? throw new InvalidOperationException(FailedToSerializeToXElement);
    }

    public static T DeserializeFromXElement<T>(XElement? element)
    {
        if (element == null)
        {
            throw new InvalidOperationException(FailedToDeserializeXElementToObject);
        }

        var serializer = new XmlSerializer(typeof(T));

        using var reader = element.CreateReader();

        object? @object = (T?)serializer.Deserialize(reader);

        if (@object != null)
        {
            return (T)@object;
        }

        throw new InvalidOperationException(FailedToDeserializeXElementToObject);
    }
}
