using System.IO;
using Api.ModelsDTO;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation(Summary = "Carica un file Excel e registra i dati nel database", Description = "Analizza il nome del file e inserisce i dettagli nel DB.")]
        [SwaggerResponse(200, "File processato con successo", typeof(ResponseDTO))]
        [SwaggerResponse(400, "Errore nel formato del file o nei dati")]
        [SwaggerResponse(500, "Errore interno del server")]
        public async Task<IActionResult> UploadExcelFile([FromForm] FileUploadDTO fileDto)
        {
            if (fileDto.File == null || fileDto.File.Length == 0)
            {
                return BadRequest("File non valido.");
            }

            try
            {
                // Estrai i dati dal nome del file
                var parsedData = ParseFileName(fileDto.File.FileName);
                if (parsedData == null)
                {
                    return BadRequest("Formato del nome file non valido.");
                }

                // Creazione del DTO con i valori estratti
                var comunicazioniDTO = new ComunicazioniDTO
                {
                    FileName = parsedData.FileName,
                    DateA = DateTimeOffset.Now,
                    NProtocol = parsedData.NProtocol,
                    NsProtocol = 0,
                    Ritornato = false,
                    Email_inviata = false,
                    Report = "N",
                    Mandante = parsedData.Mandante,
                    Servizio = parsedData.Servizio
                };

                // Determinazione fornitore dal servizio
                comunicazioniDTO.Fornitore = _fornitoreService.GetFornitoreByServizio(parsedData.Servizio, parsedData.FileName).ToString();

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

                return Ok(new ResponseDTO { Message = "Comunicazione e dettagli inseriti con successo!", ComunicazioneId = comunicazione.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del file.");
                return StatusCode(500, "Errore interno del server.");
            }
        }

        private FileParsedDTO? ParseFileName(string fullFileName)
        {
            try
            {
                // Rimuove l'estensione .xlsx
                string fileName = Path.GetFileNameWithoutExtension(fullFileName);
                var fileNameParts = fileName.Split('_');

                if (fileNameParts.Length < 3)
                {
                    _logger.LogWarning($"Formato del nome file non valido: {fileName}");
                    return null;
                }

                // Estrai il codice fornitore, il mandante e il servizio
                string codiceFornitore = fileNameParts[0].Split('-').Last(); // Ottiene il codice fornitore
                string mandante = fileNameParts[1]; // TENANT_B o TENANT_A
                string servizioString = fileNameParts[2]; // APT

                // Determina il servizio
                if (!Enum.TryParse<ServizioType>(servizioString, true, out var servizio))
                {
                    _logger.LogWarning($"Servizio '{servizioString}' non riconosciuto.");
                    return null;
                }

                // Determina il numero di protocolli
                int nProtocol = 0;
                if (!int.TryParse(fileNameParts.Last(), out nProtocol))
                {
                    _logger.LogWarning($"Numero protocolli '{fileNameParts.Last()}' non valido.");
                }

                return new FileParsedDTO
                {
                    FileName = fileName,
                    CodiceFornitore = codiceFornitore,
                    Mandante = mandante,
                    Servizio = servizio,
                    NProtocol = nProtocol
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'analisi del nome file.");
                return null;
            }
        }
    }
}
