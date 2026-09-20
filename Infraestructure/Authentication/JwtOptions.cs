namespace Infraestructure.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Supermarket.Api";

    public string Audience { get; set; } = "Supermarket.Web";

    public string Key { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}
