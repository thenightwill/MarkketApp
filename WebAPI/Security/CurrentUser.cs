using Application.Common;
using Application.Interfaces.Authentication;
using Domain.Enums;

namespace WebAPI.Security;

public class CurrentUser : ICurrentUser
{
    public const string SubjectClaim = "sub";
    public const string RoleClaim = "role";

    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst(SubjectClaim)?.Value;

            if (!Guid.TryParse(value, out var id))
                throw AppException.Unauthorized(AppErrorCodes.Unauthorized, "Authentication is required.");

            return id;
        }
    }

    public UserRole Role
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst(RoleClaim)?.Value;

            if (!Enum.TryParse<UserRole>(value, out var role))
                throw AppException.Unauthorized(AppErrorCodes.Unauthorized, "Authentication is required.");

            return role;
        }
    }
}
