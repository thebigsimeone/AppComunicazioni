using System.IO;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreateController : ControllerBase
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IExcelService _excelService;
        private readonly IFornitoreService _fornitoreService;
        private readonly ILogger<CreateController> _logger;

        public CreateController(
            ComDbContext context,
            IMapper mapper,
            IExcelService excelService,
            IFornitoreService fornitoreService,
            ILogger<CreateController> logger)
        {
            _context = context;
            _mapper = mapper;
            _excelService = excelService;
            _fornitoreService = fornitoreService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadExcelFile([FromForm] FileUploadDto fileDto)
        {
            if (fileDto.File == null || fileDto.File.Length == 0)
            {
                return BadRequest("File non valido.");
            }

            try
            {
                // Rimuove l'estensione .xlsx dal nome del file
                string fileName = Path.GetFileNameWithoutExtension(fileDto.File.FileName);
                var fileNameParts = fileName.Split('_');

                if (fileNameParts.Length < 3)
                {
                    _logger.LogWarning($"Formato del nome file non valido: {fileName}");
                    return BadRequest("Formato del nome file non valido.");
                }

                // Estrai il codice fornitore, il mandante e il servizio
                string codiceFornitore = fileNameParts[0].Split('-').Last(); // Ottiene 8033
                string mandante = fileNameParts[1]; // SSC o EBI
                string servizioString = fileNameParts[2]; // APT

                // Determina il servizio (converti in enum)
                if (!Enum.TryParse<ServizioType>(servizioString, true, out var servizio))
                {
                    _logger.LogWarning($"Servizio '{servizioString}' non riconosciuto.");
                    return BadRequest($"Servizio '{servizioString}' non valido.");
                }

                // Determina il numero di protocolli (ultimo valore del nome file)
                int nProtocol = 0;
                if (!int.TryParse(fileNameParts.Last(), out nProtocol))
                {
                    _logger.LogWarning($"Numero protocolli '{fileNameParts.Last()}' non valido.");
                }

                // Crea il DTO con i valori estratti
                var comunicazioniDTO = new ComunicazioniDTO
                {
                    FileName = fileName,
                    DateA = DateTime.UtcNow,
                    NProtocol = nProtocol,
                    NsProtocol = 0,
                    Ritornato = false,
                    Email_inviata = false,
                    Report = "N",
                    Mandante = mandante,
                    Servizio = servizio
                };

                // Determinazione fornitore dal servizio
                comunicazioniDTO.Fornitore = _fornitoreService.GetFornitoreByServizio(servizio, fileName).ToString();

                // Mappatura e salvataggio nel DB
                var comunicazione = _mapper.Map<Comunicazioni>(comunicazioniDTO);
                _context.Comunicazionis.Add(comunicazione);
                await _context.SaveChangesAsync();

                // Processa il file Excel e inserisce i dettagli
                var dettagli = await _excelService.ProcessExcelFileAsync(fileDto.File, comunicazione.Id);
                if (dettagli.Any())
                {
                    _context.ComunicazioniDettagli.AddRange(dettagli);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Message = "Comunicazione e dettagli inseriti con successo!", ComunicazioneId = comunicazione.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del file.");
                return StatusCode(500, "Errore interno del server.");
            }
        }
    }

    // DTO per Swagger
    public class FileUploadDto
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
