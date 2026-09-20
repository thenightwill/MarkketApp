using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class User
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string name, string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCodes.InvalidUser, "Name is required.");

        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email.Trim()))
            throw new DomainException(DomainErrorCodes.InvalidUser, "A valid email is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException(DomainErrorCodes.InvalidUser, "Password hash is required.");

        if (!Enum.IsDefined(role))
            throw new DomainException(DomainErrorCodes.InvalidUser, "Role is not valid.");

        return new User
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static bool IsValidEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0
            && at == email.LastIndexOf('@')
            && at < email.Length - 1
            && !email.Contains(' ')
            && email.IndexOf('.', at) > at + 1
            && !email.EndsWith('.');
    }
}
