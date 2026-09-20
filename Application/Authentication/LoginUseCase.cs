using Application.Common;
using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Authentication;

public class LoginUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;

    public LoginUseCase(IUserRepository users, IPasswordHasher hasher, IJwtTokenGenerator jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(request.Email ?? string.Empty, cancellationToken);

        if (user is null || !_hasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
            throw AppException.Unauthorized(AppErrorCodes.InvalidCredentials, "Invalid email or password.");

        var token = _jwt.Generate(user);
        return new AuthResponse(token.Value, token.ExpiresAt, user.ToResponse());
    }
}
