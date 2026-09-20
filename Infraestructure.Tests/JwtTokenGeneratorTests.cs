using System.Text;
using Domain.Entities;
using Domain.Enums;
using Infraestructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Market.Tests.Infraestructure;

[TestClass]
public class JwtTokenGeneratorTests
{
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
    }

    private static JwtTokenGenerator CreateGenerator() =>
        new(
            Options.Create(new JwtOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                Key = TestHost.JwtKey,
                ExpirationMinutes = 30
            }),
            new FixedTime());

    [TestMethod]
    public async Task Generate_ShouldEmitASignedTokenWithUserIdAndRole()
    {
        var user = User.Create("Ana", "ana@supermarket.local", "hash", UserRole.Administrator);

        var token = CreateGenerator().Generate(user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "issuer",
            ValidAudience = "audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestHost.JwtKey)),
            ValidateLifetime = false
        });

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(user.Id.ToString(), result.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.AreEqual("Administrator", result.Claims[JwtTokenGenerator.RoleClaim]);
        Assert.AreEqual("ana@supermarket.local", result.Claims[JwtRegisteredClaimNames.Email]);
    }

    [TestMethod]
    public void Generate_ShouldExpireAfterTheConfiguredMinutes()
    {
        var user = User.Create("Ana", "ana@supermarket.local", "hash", UserRole.Employee);

        var token = CreateGenerator().Generate(user);

        Assert.AreEqual(new DateTime(2026, 9, 19, 12, 30, 0, DateTimeKind.Utc), token.ExpiresAt);
    }

    [TestMethod]
    public async Task Generate_ShouldNotValidateWithAnotherKey()
    {
        var user = User.Create("Ana", "ana@supermarket.local", "hash", UserRole.Employee);
        var token = CreateGenerator().Generate(user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "issuer",
            ValidAudience = "audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("another-signing-key-with-at-least-32-bytes!!")),
            ValidateLifetime = false
        });

        Assert.IsFalse(result.IsValid);
    }
}
