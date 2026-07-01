namespace WrenchBox.Application.Common;

public class AppException : Exception
{
    public AppException(string message) : base(message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

public class UnauthorizedApplicationException : AppException
{
    public UnauthorizedApplicationException(string message) : base(message) { }
}
