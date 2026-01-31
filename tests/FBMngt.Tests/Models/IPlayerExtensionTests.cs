using FBMngt.Models;
using NUnit.Framework;

namespace FBMngt.Tests.Models;

[TestFixture]
public sealed class IPlayerExtensionTests
{
    private static Player CreatePlayer(string? position)
    {
        return new Player
        {
            PlayerID = 1,
            PlayerName = "Test Player",
            Position = position
        };
    }

    #region IsPitcher

    [TestCase("SP")]
    [TestCase("SP1")]
    [TestCase("SP2")]
    [TestCase("RP")]
    [TestCase("RP1")]
    [TestCase("RP9")]
    [TestCase("P")]
    public void GivenPitcherPosition_WhenCallingIsPitcher_ThenReturnsTrue(
        string position)
    {
        // Arrange
        IPlayer player = CreatePlayer(position);

        // Act
        bool result = player.IsPitcher();

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("Util")]
    [TestCase("OF")]
    [TestCase("1B")]
    [TestCase("2B")]
    [TestCase("SS")]
    [TestCase("C")]
    [TestCase("Pitcher")]
    public void GivenBatterPosition_WhenCallingIsPitcher_ThenReturnsFalse(
        string position)
    {
        // Arrange
        IPlayer player = CreatePlayer(position);

        // Act
        bool result = player.IsPitcher();

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void 
    GivenMissingPosition_WhenCallingIsPitcher_ThenReturnsFalse(
        string? position)
    {
        // Arrange
        IPlayer player = CreatePlayer(position);

        // Act
        bool result = player.IsPitcher();

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region IsBatter

    [TestCase("Util")]
    [TestCase("OF")]
    [TestCase("1B")]
    public void 
    GivenBatterPosition_WhenCallingIsBatter_ThenReturnsTrue(string position)
    {
        // Arrange
        IPlayer player = CreatePlayer(position);

        // Act
        bool result = player.IsBatter();

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("SP")]
    [TestCase("SP1")]
    [TestCase("RP")]
    [TestCase("P")]
    public void 
    GivenPitcherPosition_WhenCallingIsBatter_ThenReturnsFalse(string position)
    {
        // Arrange
        IPlayer player = CreatePlayer(position);

        // Act
        bool result = player.IsBatter();

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion
}
