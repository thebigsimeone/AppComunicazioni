using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppComunicazioni.Data;
using AppComunicazioni.Models;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateController : ControllerBase
    {
        private readonly ComDbContext _context;

        public CreateController(ComDbContext context)
        {
            _context = context;
        }

        // POST: api/CreateApi
        [HttpPost]
        public async Task<ActionResult<Comunicazioni>> PostComunicazioni(Comunicazioni comunicazioni)
        {
            _context.Comunicazionis.Add(comunicazioni);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetComunicazioni", new { id = comunicazioni.Id }, comunicazioni);
        }

    }
}
