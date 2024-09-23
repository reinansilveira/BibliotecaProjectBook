namespace BibliotecaProject.Exceptions;

public class NotFoundException : Exception
{
    public int StatusCode;
    
    public NotFoundException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}