namespace Mesty.Core;

public class MestyException : Exception
{
    public MestyException()
    {
    }

    public MestyException(string message) : base(message) { }

    public MestyException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}