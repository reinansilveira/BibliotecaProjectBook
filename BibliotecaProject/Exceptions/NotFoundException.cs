namespace BibliotecaProject.Exceptions;

public class NotFoundException : SystemException
{
    public int StatusCode;
    
    public NotFoundException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    protected NotFoundException() : base("Exception not implemented.")
    {
    }
}