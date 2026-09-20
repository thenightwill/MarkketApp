using Domain.Enums;

namespace Application.Interfaces.Authentication;

public interface ICurrentUser
{
    Guid UserId { get; }

    UserRole Role { get; }

    bool IsAdministrator => Role == UserRole.Administrator;
}
