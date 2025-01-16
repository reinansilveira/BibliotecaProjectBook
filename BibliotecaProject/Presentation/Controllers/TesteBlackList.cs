using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaProject.Presentation.Controllers;

public class TesteBlackList
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlackListController<ApplicationDbContext> : ControllerBase
    {
        private readonly BibliotecaDbContext _context;

        public BlackListController(BibliotecaDbContext context)
        {
            _context = context;
        }

        // Action para testar a inserção de um membro na BlackList
        [HttpPost("add-test")]
        public async Task<IActionResult> AddToBlackListTest()
        {
            // Exemplo de adição manual à BlackList
            var blacklist = new BlackList()
            {
                IdBlackList = Guid.NewGuid(), // Gera um novo GUID para a lista negra
                IdMembro = Guid.NewGuid(), // Simulação de um IdMembro (mude conforme necessário)
                Nome = "Teste Membro" // Nome fictício para teste
            };

            // Adiciona o objeto ao contexto
            _context.BlackLists.Add(blacklist);

            // Salva as mudanças no banco de dados
            await _context.SaveChangesAsync();

            // Retorna uma resposta de sucesso
            return Ok("Membro adicionado à BlackList com sucesso!");
        }
    }


}