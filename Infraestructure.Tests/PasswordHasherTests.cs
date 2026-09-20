using Infraestructure.Authentication;

namespace Market.Tests.Infraestructure;

[TestClass]
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [TestMethod]
    public void Hash_ShouldNotContainThePlainPassword()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.DoesNotContain("Secret123", hash);
    }

    [TestMethod]
    public void Hash_ShouldUseADifferentSaltEachTime()
    {
        Assert.AreNotEqual(_hasher.Hash("Secret123"), _hasher.Hash("Secret123"));
    }

    [TestMethod]
    public void Verify_ShouldAcceptTheCorrectPassword()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.IsTrue(_hasher.Verify("Secret123", hash));
    }

    [TestMethod]
    public void Verify_ShouldRejectAWrongPassword()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.IsFalse(_hasher.Verify("secret123", hash));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("garbage")]
    [DataRow("v1.abc.def.ghi")]
    [DataRow("v2.1.AAAA.AAAA")]
    public void Verify_ShouldRejectMalformedHashesWithoutThrowing(string hash)
    {
        Assert.IsFalse(_hasher.Verify("Secret123", hash));
    }
}
