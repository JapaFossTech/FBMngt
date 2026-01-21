using NUnit.Framework;

namespace FBMngt.Tests.Services.Players;

[TestFixture]
public class SqlPlayerIdResolverTests
{
    [Test]
    public void
    ResolvePlayerId_WhenExactNameMatch_ReturnsPlayerId()
    {
        // Arrange
        // Create resolver with sample players
        // (e.g. PlayerName = "Aaron Judge", PlayerId = 1)

        // Act
        // var result = resolver.ResolvePlayerId("Aaron Judge");

        // Assert
        // This will fail now because ResolvePlayerId is not implemented
        Assert.That(false, Is.True, "Resolver should return PlayerId for exact match");
    }

    [Test]
    public void
    ResolvePlayerId_WhenNameHasParenthetical_ReturnsPlayerId()
    {
        // Arrange

        // Act

        // Assert
        Assert.That(false, Is.True, "Resolver should strip parenthesis and return PlayerId");
    }
}
