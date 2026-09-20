using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Market.Tests.Domain;

[TestClass]
public class UserTests
{
    [TestMethod]
    public void Create_ShouldCreateUserWithNormalizedEmail()
    {
        var user = User.Create("Ana Perez", "  Ana.Perez@Supermarket.LOCAL ", "hash", UserRole.Employee);

        Assert.AreNotEqual(Guid.Empty, user.Id);
        Assert.AreEqual("Ana Perez", user.Name);
        Assert.AreEqual("ana.perez@supermarket.local", user.Email);
        Assert.AreEqual("hash", user.PasswordHash);
        Assert.AreEqual(UserRole.Employee, user.Role);
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyName()
    {
        Assert.Throws<DomainException>(() => User.Create(" ", "ana@supermarket.local", "hash", UserRole.Employee));
    }

    [TestMethod]
    public void Create_ShouldRejectInvalidEmail()
    {
        Assert.Throws<DomainException>(() => User.Create("Ana", "not-an-email", "hash", UserRole.Employee));
        Assert.Throws<DomainException>(() => User.Create("Ana", string.Empty, "hash", UserRole.Employee));
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyPasswordHash()
    {
        Assert.Throws<DomainException>(() => User.Create("Ana", "ana@supermarket.local", " ", UserRole.Employee));
    }

    [TestMethod]
    public void Create_ShouldRejectUndefinedRole()
    {
        Assert.Throws<DomainException>(() => User.Create("Ana", "ana@supermarket.local", "hash", (UserRole)99));
    }
}
