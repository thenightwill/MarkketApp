using Domain.Entities;

namespace Application.Interfaces.Authentication;

public sealed record AccessToken(string Value, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}
