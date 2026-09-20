using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace Application.Authentication;

public sealed record RegisterRequest(
    [StringLength(200)] string Name,
    [StringLength(256)] string Email,
    [StringLength(128)] string Password);

public sealed record LoginRequest(
    [StringLength(256)] string Email,
    [StringLength(128)] string Password);

public sealed record UserResponse(Guid Id, string Name, string Email, string Role);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);

internal static class AuthMapping
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString());
}
