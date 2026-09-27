using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_RoundTripsAndRejectsWrongPassword()
    {
        var hashed = PasswordHasher.Hash("123456");

        Assert.True(PasswordHasher.IsHashed(hashed));
        Assert.True(PasswordHasher.Verify(hashed, "123456", out var needsUpgrade));
        Assert.False(needsUpgrade);
        Assert.False(PasswordHasher.Verify(hashed, "wrong", out _));
    }

    [Fact]
    public void Verify_TreatsLegacyPlaintextAsUpgrade()
    {
        Assert.False(PasswordHasher.IsHashed("123456"));
        Assert.True(PasswordHasher.Verify("123456", "123456", out var needsUpgrade));
        Assert.True(needsUpgrade);
        Assert.False(PasswordHasher.Verify("123456", "654321", out _));
    }

    [Fact]
    public void Verify_DoesNotAcceptHashStringAsPassword()
    {
        var hashed = PasswordHasher.Hash("123456");
        var doubleHashed = PasswordHasher.Hash(hashed);

        Assert.False(PasswordHasher.Verify(doubleHashed, "123456", out _));
        Assert.False(PasswordHasher.Verify(hashed, hashed, out _));
    }
}
