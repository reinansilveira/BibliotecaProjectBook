using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaProject.Presentation.Controllers
{
    [ApiController]
    [Route("api/black-Events")]
    public class BlackListControllerBase : ControllerBase
    {
       
        private readonly BibliotecaDbContext _context;

        public BlackListControllerBase(BibliotecaDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] BlackList blackList)
        {
            var novaBlackList = new BlackList
            {
                Nome = blackList.Nome,
                IdMembro = blackList.IdMembro
            };
            

            _context.BlackLists.Add(blackList);
            await _context.SaveChangesAsync();

            return Ok("BlackList adicionada com sucesso!");
        }
    }
}