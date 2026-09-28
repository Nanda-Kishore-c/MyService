namespace FlooInsurance.AuthGateway.Application.Common.Exceptions;

public class TokenException : Exception
{
    public TokenException(string message) : base(message)
    {
    }

    public TokenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
