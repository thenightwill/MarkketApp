namespace Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message)
        : this(DomainErrorCodes.ValidationError, message)
    {
    }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
