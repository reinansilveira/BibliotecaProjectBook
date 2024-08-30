﻿using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Eventing.Reader;

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
            return Ok(await _livroInterface.GetById(id));
        }
        

        [HttpPost]
        public async Task<ActionResult<Livro>> Post(Livro livro)
        {
            return Ok(await _livroInterface.Post(livro));
        }


         [HttpPut("id")]
         public async Task<ActionResult> Update(string id, Livro input)
         {
            return Ok(await _livroInterface.Update(id, input));
         }

         [HttpDelete("id")]
         public async Task<ActionResult> Delete(string id)
         {
            return await _livroInterface.Delete(id);
         }
     }
    }




