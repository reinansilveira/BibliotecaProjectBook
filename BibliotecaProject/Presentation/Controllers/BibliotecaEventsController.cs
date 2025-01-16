using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Eventing.Reader;
using BibliotecaProject.Domain.EntitiesDTO;

namespace BibliotecaProject.Presentation.Controllers
{
    [ApiController]
    [Route("api/Livro-Events")]
    public class BibliotecaEventsController : ControllerBase
    {
        private Ilivro _livroInterface;

        public BibliotecaEventsController(Ilivro livroInterface)
        {
            _livroInterface = livroInterface;
        }

         [HttpGet("{id}")]
         public async Task<ActionResult> GetById(string id)
         {
             var getLivro = await _livroInterface.GetById(id);
            return Ok (getLivro);
         }
        

        [HttpPost]
        public async Task<ActionResult<LivroDTO>> Post(LivroDTO livroDto)
        {
              var postLivro = await _livroInterface.Post(livroDto);
              return Ok(postLivro);
        }


         [HttpPut("id")]
         public async Task<ActionResult> Update(string id, Livro input)
         {
            var updatedLivro = await _livroInterface.Update(id, input);
            return Ok(updatedLivro);
         }

         [HttpDelete("id")]
         public async Task<ActionResult> Delete(string id)
         {
           var deleteLivro = await _livroInterface.Delete(id);
            return deleteLivro;
         }
     }
    }




