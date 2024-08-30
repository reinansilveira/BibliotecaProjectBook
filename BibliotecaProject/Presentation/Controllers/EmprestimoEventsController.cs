using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaProject.Presentation.Controllers
{
    [ApiController]
    [Route("api/Emprestimos-Events")]
    public class EmprestimoEventsController : ControllerBase
    {
        private readonly IEmprestimo _emprestimoInterface;

        public EmprestimoEventsController(IEmprestimo emprestimoInterface)
        {
            _emprestimoInterface = emprestimoInterface;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById( string id)
        {
            return Ok(await _emprestimoInterface.GetById(id));
        }


        [HttpPost]
        public async Task<ActionResult<Emprestimo>> Post( Emprestimo emprestimo)
        {
            return Ok(await _emprestimoInterface.Post(emprestimo));
        }


        [HttpPut("id")]
        public async Task<ActionResult> Update(string id, Emprestimo input)
        {
            return Ok(await _emprestimoInterface.Update(id, input));
        }

        [HttpDelete("id")]
        public async Task<ActionResult> Emprestado(string id)
        {
            return await _emprestimoInterface.Emprestado(id);
        }
    }
}

