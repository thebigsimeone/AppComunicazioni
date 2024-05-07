using apiSanges.Service;
using AppComunicazioni.Data;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComunicazionisController : ControllerBase
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILogger<ComunicazionisController> _logger;

        public ComunicazionisController(ComDbContext context, IMapper mapper, IEmailService emailService, ILogger<ComunicazionisController> logger)
        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: api/Comunicazionis
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComunicazioniDTO>>> GetComunicazionis(int pageNumber = 1, int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            IQueryable<Comunicazioni> query = _context.Comunicazionis;

            if (startDate.HasValue)
            {
                query = query.Where(x => x.DateA >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.DateA <= endDate.Value);
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            var comunicazioni = await query
                                    .Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            return Ok(new
            {
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                Comunicazioni = _mapper.Map<List<ComunicazioniDTO>>(comunicazioni)
            });
        }

        // GET: api/Comunicazionis/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Comunicazioni>> GetComunicazioni(int id)
        {
            var comunicazioni = await _context.Comunicazionis.FindAsync(id);

            if (comunicazioni == null)
            {
                return NotFound();
            }

            return comunicazioni;
        }

        // PUT: api/Comunicazionis/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComunicazioni(int id, Comunicazioni comunicazioni)
        {
            if (id != comunicazioni.Id)
            {
                return BadRequest();
            }

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

        // POST: api/Comunicazionis
        [HttpPost]
        public async Task<ActionResult<ComunicazioniDTO>> PostComunicazioni([FromBody] ComunicazioniDTO comunicazioniDto)
        {
            if (comunicazioniDto == null)
            {
                return BadRequest("Invalid data.");
            }

            // Mappatura del DTO al modello di dominio
            var comunicazioni = _mapper.Map<Comunicazioni>(comunicazioniDto);
            _context.Comunicazionis.Add(comunicazioni);
            await _context.SaveChangesAsync();

            // Recupero di tutti i destinatari dalla tabella Destinatari
            var destinatari = await _context.Destinataris.ToListAsync();
            if (destinatari == null || !destinatari.Any())
            {
                return NotFound("Non ci sono destinatari.");
            }

            // Preparazione dell'email
            string subject = "E' STATA AGGIUNTA UNA NUOVA COMUNICAZIONE NELL'AREA COMUNICAZIONI";
            string message = $"<p>Il seguente file è stato aggiunto nell'area comunicazioni: {comunicazioni.FileName} <br>" +
                        $"con il numero protocolli {comunicazioni.NProtocol} <br>" +
                        $"e questi sono i protocolli da controllare {comunicazioni.NsProtocol}: <br>" +
                        $"{comunicazioni.Note}" +
                        $"<br>" +
                        $"Cordiali saluti,<br>" +
                        $"<br>" +
                        $"Flavio Simeone</p>";

            // Invio dell'email a tutti i destinatari
            foreach (var destinatario in destinatari)
            {
                try
                {
                    await _emailService.SendEmailAsync(destinatario.Destinatario, subject, message);
                }
                catch (Exception ex)
                {
                    // Log the exception (Consider adding more detailed logging or handling here)
                    _logger.LogError($"Failed to send email to {destinatario.Destinatario}: {ex.Message}");
                }
            }

            // Ritorno il risultato
            return CreatedAtAction(nameof(GetComunicazioni), new { id = comunicazioni.Id }, comunicazioniDto);
        }

        // DELETE: api/Comunicazionis/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComunicazioni(int id)
        {
            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
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
