// <copyright file="SoapSerializerTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Soap.Serializer;

using System.Xml.Linq;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Serializer;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using Shouldly;
using Xunit;

public class SoapSerializerTests
{
    [Fact]
    public void SerializeToXElement_WithValidObject_ShouldReturnExpectedXElement()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "TestName" };

        // Act
        var element = SoapSerializer.SerializeToXElement(entity);

        // Assert
        element.ShouldNotBeNull();

        element.ShouldSatisfyAllConditions(
            () => element.Name.LocalName.ShouldBe(nameof(TestEntity)),
            () => element.Element("Id")?.Value.ShouldBe("123"),
            () => element.Element("Name")?.Value.ShouldBe("TestName"));
    }

    [Fact]
    public void DeserializeFromXElement_WithValidElement_ShouldReturnObject()
    {
        // Arrange
        var element = new XElement(
            nameof(TestEntity),
            new XElement("Id", "456"),
            new XElement("Name", "DeserializedName"));

        // Act
        var result = SoapSerializer.DeserializeFromXElement<TestEntity>(element);

        // Assert
        result.ShouldNotBeNull();

        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBe("456"),
            () => result.Name.ShouldBe("DeserializedName"));
    }

    [Fact]
    public void DeserializeFromXElement_WithNullElement_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            SoapSerializer.DeserializeFromXElement<TestEntity>(null));

        ex.Message.ShouldBe("Failed to deserialize XElement to object");
    }

    [Fact]
    public void DeserializeFromXElement_WhenNilElement_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        var nilElement = new XElement(
            nameof(TestEntity),
            new XAttribute(xsi + "nil", "true"));

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            SoapSerializer.DeserializeFromXElement<TestEntity>(nilElement));

        ex.Message.ShouldBe("Failed to deserialize XElement to object");
    }
}
