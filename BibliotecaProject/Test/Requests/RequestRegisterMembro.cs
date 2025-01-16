using BibliotecaProject.Domain.Entities;
using Bogus;

namespace Test.Requests;

public class RequestRegisterMembro
{
    public static Membro Build()
    {
        return new Faker<Membro>()
            .RuleFor(user => user.Nome, (f) => f.Person.FirstName)
            .RuleFor(user => user.Email, (f, user) => f.Internet.Email(user.Nome))
            .RuleFor(user => user.Senha, (f) => f.Internet.Password())
            .RuleFor(user => user.Telefone, (f) => f.Phone.PhoneNumber());
    }
}