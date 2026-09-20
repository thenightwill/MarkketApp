using Application.Authentication;
using Application.Common;
using Domain.Enums;
using Domain.Exceptions;
using Market.Tests.Application.Fakes;

namespace Market.Tests.Application;

[TestClass]
public class AuthenticationTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeJwtTokenGenerator _jwt = new();
    private readonly FakeUnitOfWork _uow = new();

    private RegisterUseCase Register() => new(_users, _hasher, _jwt, _uow);

    private LoginUseCase Login() => new(_users, _hasher, _jwt);

    [TestMethod]
    public async Task Register_ShouldCreateEmployeeWithHashedPassword()
    {
        var response = await Register().ExecuteAsync(new RegisterRequest("Ana Perez", "Ana@Supermarket.local", "Secret123"));

        var user = _users.Items.Single();
        Assert.AreEqual(UserRole.Employee, user.Role);
        Assert.AreEqual("ana@supermarket.local", user.Email);
        Assert.AreEqual("hashed::Secret123", user.PasswordHash);
        Assert.AreEqual(user.Id, response.User.Id);
        Assert.AreEqual("Employee", response.User.Role);
        Assert.IsFalse(string.IsNullOrEmpty(response.AccessToken));
        Assert.AreEqual(1, _uow.SaveCount);
    }

    [TestMethod]
    public async Task Register_ShouldRejectDuplicatedEmailIgnoringCase()
    {
        await Register().ExecuteAsync(new RegisterRequest("Ana", "ana@supermarket.local", "Secret123"));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Register().ExecuteAsync(new RegisterRequest("Otra Ana", "ANA@supermarket.local", "Secret123")));

        Assert.AreEqual(AppErrorCodes.EmailAlreadyExists, ex.Code);
        Assert.AreEqual(AppErrorKind.Conflict, ex.Kind);
    }

    [TestMethod]
    [DataRow("short1")]
    [DataRow("alllowercase")]
    [DataRow("12345678")]
    [DataRow("")]
    public async Task Register_ShouldRejectWeakPasswords(string password)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Register().ExecuteAsync(new RegisterRequest("Ana", "ana@supermarket.local", password)));

        Assert.AreEqual(AppErrorKind.Validation, ex.Kind);
        Assert.IsEmpty(_users.Items);
    }

    [TestMethod]
    public async Task Register_ShouldRejectInvalidEmail()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            Register().ExecuteAsync(new RegisterRequest("Ana", "not-an-email", "Secret123")));
    }

    [TestMethod]
    public async Task Login_ShouldReturnTokenForValidCredentials()
    {
        await Register().ExecuteAsync(new RegisterRequest("Ana", "ana@supermarket.local", "Secret123"));

        var response = await Login().ExecuteAsync(new LoginRequest("ANA@supermarket.local", "Secret123"));

        Assert.AreEqual(_users.Items.Single().Id, response.User.Id);
        Assert.IsFalse(string.IsNullOrEmpty(response.AccessToken));
    }

    [TestMethod]
    public async Task Login_ShouldRejectWrongPassword()
    {
        await Register().ExecuteAsync(new RegisterRequest("Ana", "ana@supermarket.local", "Secret123"));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Login().ExecuteAsync(new LoginRequest("ana@supermarket.local", "WrongPass1")));

        Assert.AreEqual(AppErrorCodes.InvalidCredentials, ex.Code);
        Assert.AreEqual(AppErrorKind.Unauthorized, ex.Kind);
    }

    [TestMethod]
    public async Task Login_ShouldRejectUnknownEmailWithTheSameError()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Login().ExecuteAsync(new LoginRequest("nobody@supermarket.local", "Secret123")));

        Assert.AreEqual(AppErrorCodes.InvalidCredentials, ex.Code);
    }
}
