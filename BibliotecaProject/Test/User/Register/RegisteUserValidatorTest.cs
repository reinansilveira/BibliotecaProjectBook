using BibliotecaProject.Domain.RegistroUsuarioValidator;
using Test.Requests;
using Xunit;

namespace Test.User.Register;

public class RegisteUserValidatorTest
{
    [Fact] //Necessário para realizar o teste
    public void Sucess()
    {
        var validator = new RegisteUserValidator();

        var request = RequestRegisterMembro.Build();

        var result = validator.Validate(request);

       Assert.True(result.IsValid);
}
    }