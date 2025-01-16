namespace BibliotecaProject.Exceptions;

public class ResponseErrors
{
    public IList<string> Errors { get; set; }

    public ResponseErrors(IList<string> errors)
    {
        Errors = errors;
    }

    public ResponseErrors(string error)
    {
        Errors = new List<string> { error };
    }
    
}