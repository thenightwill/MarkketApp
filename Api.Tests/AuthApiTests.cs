using System.Net;
using Application.Authentication;

namespace Market.Tests.Api;

[TestClass]
public class AuthApiTests
{
    private static string NewEmail() => $"user-{Guid.NewGuid():N}@supermarket.local";

    [TestMethod]
    public async Task Register_ShouldCreateAnEmployeeAndReturnAToken()
    {
        var client = ApiHelpers.CreateAnonymousClient();

        var response = await client.PostJsonAsync("/api/auth/register", new RegisterRequest("Ana Perez", NewEmail(), "Secret123"));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.ReadAsync<AuthResponse>();
        Assert.AreEqual("Employee", auth.User.Role);
        Assert.IsFalse(string.IsNullOrWhiteSpace(auth.AccessToken));
    }

    [TestMethod]
    public async Task Register_ShouldIgnoreAnAttemptToChooseTheAdministratorRole()
    {
        var client = ApiHelpers.CreateAnonymousClient();

        var response = await client.PostJsonAsync(
            "/api/auth/register",
            new { name = "Sneaky", email = NewEmail(), password = "Secret123", role = "Administrator" });

        var auth = await response.ReadAsync<AuthResponse>();
        Assert.AreEqual("Employee", auth.User.Role);
    }

    [TestMethod]
    public async Task Register_ShouldRejectDuplicatedEmail()
    {
        var client = ApiHelpers.CreateAnonymousClient();
        var email = NewEmail();
        await client.PostJsonAsync("/api/auth/register", new RegisterRequest("Ana", email, "Secret123"));

        var response = await client.PostJsonAsync("/api/auth/register", new RegisterRequest("Ana", email.ToUpperInvariant(), "Secret123"));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("EMAIL_ALREADY_EXISTS", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Register_ShouldRejectWeakPasswords()
    {
        var client = ApiHelpers.CreateAnonymousClient();

        var response = await client.PostJsonAsync("/api/auth/register", new RegisterRequest("Ana", NewEmail(), "weak"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual("VALIDATION_ERROR", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Register_ShouldRejectInvalidEmail()
    {
        var client = ApiHelpers.CreateAnonymousClient();

        var response = await client.PostJsonAsync("/api/auth/register", new RegisterRequest("Ana", "not-an-email", "Secret123"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_ShouldReturnATokenForSeededUsers()
    {
        var client = ApiHelpers.CreateAnonymousClient();

        var response = await client.PostJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@supermarket.local", ApiFixture.AdminPassword));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.ReadAsync<AuthResponse>();
        Assert.AreEqual("Administrator", auth.User.Role);
    }

    [TestMethod]
    public async Task Login_ShouldRejectWrongPasswordAndUnknownUserWithTheSameCode()
    {
        var client = ApiHelpers.CreateAnonymousClient();

        var wrongPassword = await client.PostJsonAsync("/api/auth/login", new LoginRequest("admin@supermarket.local", "WrongPass1"));
        var unknownUser = await client.PostJsonAsync("/api/auth/login", new LoginRequest("nobody@supermarket.local", "WrongPass1"));

        Assert.AreEqual(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.AreEqual("INVALID_CREDENTIALS", await wrongPassword.CodeAsync());
        Assert.AreEqual("INVALID_CREDENTIALS", await unknownUser.CodeAsync());
    }

    [TestMethod]
    public async Task ProtectedEndpoints_ShouldRequireAToken()
    {
        var client = ApiHelpers.CreateAnonymousClient();

        foreach (var url in new[] { "/api/products", "/api/inventory", "/api/sales", "/api/tasks" })
        {
            var response = await client.GetAsync(url);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, url);
            Assert.AreEqual("UNAUTHORIZED", await response.CodeAsync());
        }
    }

    [TestMethod]
    public async Task ProtectedEndpoints_ShouldRejectAnInvalidToken()
    {
        var client = ApiHelpers.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not.a.token");

        var response = await client.GetAsync("/api/products");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_ShouldRejectMalformedBodies()
    {
        var client = ApiHelpers.CreateAnonymousClient();

        var response = await client.PostAsync("/api/auth/login", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
