using AppComunicazioni.Data;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateController : ControllerBase
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;

        public CreateController(ComDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Create
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComunicazioniDTO>>> GetComunicazionis()
        {
            var comunicazioni = await _context.Comunicazionis.Include(c => c.Dettagli).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<ComunicazioniDTO>>(comunicazioni));
        }

        // GET: api/Create/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ComunicazioniDTO>> GetComunicazioni(int id)
        {
            var comunicazioni = await _context.Comunicazionis
                                              .Include(c => c.Dettagli)
                                              .FirstOrDefaultAsync(c => c.Id == id);

            if (comunicazioni == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<ComunicazioniDTO>(comunicazioni));
        }

        // PUT: api/Create/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComunicazioni(int id, ComunicazioniDTO comunicazioniDTO)
        {
            if (id != comunicazioniDTO.Id)
            {
                return BadRequest();
            }

            var comunicazioni = await _context.Comunicazionis.Include(c => c.Dettagli).FirstOrDefaultAsync(c => c.Id == id);
            if (comunicazioni == null)
            {
                return NotFound();
            }

            _mapper.Map(comunicazioniDTO, comunicazioni);
            _context.Entry(comunicazioni).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ComunicazioniExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Create
        [HttpPost]
        public async Task<ActionResult<ComunicazioniDTO>> PostComunicazioni(ComunicazioniDTO comunicazioniDTO)
        {
            var comunicazioni = _mapper.Map<Comunicazioni>(comunicazioniDTO);
            _context.Comunicazionis.Add(comunicazioni);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetComunicazioni), new { id = comunicazioni.Id }, _mapper.Map<ComunicazioniDTO>(comunicazioni));
        }

        // DELETE: api/Create/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComunicazioni(int id)
        {
            var comunicazioni = await _context.Comunicazionis.Include(c => c.Dettagli).FirstOrDefaultAsync(c => c.Id == id);
            if (comunicazioni == null)
            {
                return NotFound();
            }

            _context.Comunicazionis.Remove(comunicazioni);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ComunicazioniExists(int id)
        {
            return _context.Comunicazionis.Any(e => e.Id == id);
        }
    }
}
