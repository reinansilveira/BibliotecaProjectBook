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
            var membroGet = await _membroInterface.GetById(id);
            return Ok(membroGet);
        }


        [HttpPost]
        public async Task<ActionResult<Emprestimo>> Post(Membro membro)
        {
            var emprestimoPost = await _membroInterface.Post(membro);
            return Ok(emprestimoPost);
        }


        [HttpPut("id")]
        public async Task<ActionResult> Update(string id, Membro input)
        {
            var emprestimoPut = await _membroInterface.Update(id, input);
            return Ok(emprestimoPut);
        }

        [HttpDelete("id")]
        public async Task<ActionResult> Delete(string id)
        {
            var emprestimoDelete = await _membroInterface.Delete(id);
            return Ok(emprestimoDelete);
        }
    }
}
