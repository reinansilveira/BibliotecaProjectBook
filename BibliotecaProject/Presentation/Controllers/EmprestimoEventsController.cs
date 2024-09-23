using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Exceptions;
using FluentValidation;
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
            try
            {
                var emprestimoGet = await _emprestimoInterface.GetById(id);
                return Ok(emprestimoGet);
            }
            catch (NotFoundException exception)
            {
                return StatusCode(exception.StatusCode,exception.Message);
            }
            
        }


        [HttpPost]
        public async Task<ActionResult<Emprestimo>> DateEmprestimo( Emprestimo emprestimo, IValidator<Emprestimo> validator)
        {
            try
            {
                var emprestimoPost = await _emprestimoInterface.Post(emprestimo, validator);
                return Ok(emprestimoPost);
            }
            catch (NotFoundException exception)
            {
                return StatusCode(exception.StatusCode, exception.Message);
            }
            
        }


        [HttpPut("id")]
        public async Task<ActionResult> Update(string id, Emprestimo input)
        {
            try
            {
                var emprestimoUpdate = await _emprestimoInterface.Update(id, input);
                return Ok(emprestimoUpdate);
            }
            catch (NotFoundException exception)
            {
              return StatusCode(exception.StatusCode, exception.Message);  
            }
            
        }

        [HttpDelete("id")]
        public async Task<ActionResult> Emprestado(string id)
        {
            try
            {
                var emprestimoDelete = await _emprestimoInterface.Emprestado(id);
                return emprestimoDelete;
            }
            catch (NotFoundException exception)
            {
                return StatusCode(exception.StatusCode, exception.Message);
            }
        }
    }
}

