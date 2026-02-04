using System;
using FBMngt;
using NUnit.Framework;

namespace FBMngt.Tests.Framework;

[TestFixture]
public class DbValueExtensionsTests
{
    [Test]
    public void ToDbValue_StringNull_ReturnsDBNull()
    {
        string? value = null;

        object result = value.ToDbValue();

        Assert.That(result, Is.EqualTo(DBNull.Value));
    }

    [Test]
    public void ToDbValue_StringValue_ReturnsString()
    {
        string? value = "Juan Soto";

        object result = value.ToDbValue();

        Assert.That(result, Is.EqualTo("Juan Soto"));
    }

    [Test]
    public void ToDbValue_NullableIntNull_ReturnsDBNull()
    {
        int? value = null;

        object result = value.ToDbValue();

        Assert.That(result, Is.EqualTo(DBNull.Value));
    }

    [Test]
    public void ToDbValue_NullableIntValue_ReturnsInt()
    {
        int? value = 42;

        object result = value.ToDbValue();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ToDbValue_DateTimeValue_ReturnsDateTime()
    {
        DateTime dt = new DateTime(2025, 1, 1);

        object result = dt.ToDbValue();

        Assert.That(result, Is.EqualTo(dt));
    }
}
