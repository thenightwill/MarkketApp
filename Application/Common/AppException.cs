namespace Application.Common;

public enum AppErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

public class AppException : Exception
{
    public string Code { get; }

    public AppErrorKind Kind { get; }

    public AppException(AppErrorKind kind, string code, string message)
        : base(message)
    {
        Kind = kind;
        Code = code;
    }

    public static AppException Validation(string code, string message) => new(AppErrorKind.Validation, code, message);

    public static AppException Unauthorized(string code, string message) => new(AppErrorKind.Unauthorized, code, message);

    public static AppException Forbidden(string code, string message) => new(AppErrorKind.Forbidden, code, message);

    public static AppException NotFound(string code, string message) => new(AppErrorKind.NotFound, code, message);

    public static AppException Conflict(string code, string message) => new(AppErrorKind.Conflict, code, message);
}
