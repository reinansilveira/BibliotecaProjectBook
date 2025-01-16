using BibliotecaProject.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaProject.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly TesteEmail _testeEmail;

    public EmailController(TesteEmail testeEmail)
    {
        _testeEmail = testeEmail;
    }

    [HttpGet]
    [Route("enviar-teste")]
    public async Task<IActionResult> EnviarEmailTeste()
    {
        await _testeEmail.TestarEnvioEmail();
        return Ok("E-mail de teste enviado com sucesso!");
    }
}