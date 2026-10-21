using AgroTrack.Security;
using Xunit;

namespace AgroTrack.Tests;

public class SecurityTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_GeneratesNonEmptyHashedStringWithSalt()
    {
        // Act
        string hash = _hasher.HashPassword("AdminPassword@123");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Contains(":", hash);
        string[] parts = hash.Split(':');
        Assert.Equal(2, parts.Length);
        Assert.True(Convert.FromBase64String(parts[0]).Length >= 16);
        Assert.True(Convert.FromBase64String(parts[1]).Length >= 32);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        const string rawPassword = "ManagerPassword@123";
        string hash = _hasher.HashPassword(rawPassword);

        // Act
        bool result = _hasher.VerifyPassword(rawPassword, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        string hash = _hasher.HashPassword("PetugasPassword@123");

        // Act
        bool result = _hasher.VerifyPassword("WrongPassword!999", hash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_EmptyOrWhitespace_ThrowsArgumentException(string invalidPassword)
    {
        Assert.Throws<ArgumentException>(() => _hasher.HashPassword(invalidPassword));
    }
}
