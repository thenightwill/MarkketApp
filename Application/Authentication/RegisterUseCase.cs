using Application.Common;
using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;
using Domain.Entities;
using Domain.Enums;

namespace Application.Authentication;

public class RegisterUseCase
{
    private const int MinimumPasswordLength = 8;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUseCase(
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenGenerator jwt,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> ExecuteAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.Password);

        if (await _users.EmailExistsAsync(request.Email ?? string.Empty, cancellationToken))
            throw AppException.Conflict(AppErrorCodes.EmailAlreadyExists, "The email is already registered.");

        var user = User.Create(request.Name, request.Email!, _hasher.Hash(request.Password), UserRole.Employee);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwt.Generate(user);
        return new AuthResponse(token.Value, token.ExpiresAt, user.ToResponse());
    }

    private static void ValidatePassword(string? password)
    {
        var valid = !string.IsNullOrEmpty(password)
            && password.Length >= MinimumPasswordLength
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit);

        if (!valid)
            throw AppException.Validation(
                AppErrorCodes.ValidationError,
                "The password must have at least 8 characters, an uppercase letter, a lowercase letter and a digit.");
    }
}
