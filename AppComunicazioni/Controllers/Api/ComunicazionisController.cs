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

    }
}
