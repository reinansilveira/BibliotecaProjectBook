using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaProject.Presentation.Controllers
{
    [ApiController]
    [Route("api/Membro-Events")]
    public class MembroEventsController : ControllerBase
    {
        private IMembro _membroInterface;

        public MembroEventsController(IMembro membroInterface)
        {
            _membroInterface = membroInterface;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(string id)
        {
            return Ok(await _membroInterface.GetById(id));
        }


        [HttpPost]
        public async Task<ActionResult<Emprestimo>> Post(Membro membro)
        {
            return Ok(await _membroInterface.Post(membro));
        }


        [HttpPut("id")]
        public async Task<ActionResult> Update(string id, Membro input)
        {
            return Ok(await _membroInterface.Update(id, input));
        }
    }
}
