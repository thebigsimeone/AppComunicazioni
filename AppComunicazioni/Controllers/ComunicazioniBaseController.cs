using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    public abstract class ComunicazioniBaseController<TLogger> : Controller
    {
        protected readonly ComDbContext _context;
        protected readonly IMapper _mapper;
        protected readonly IEmailService _emailService;
        protected readonly IMonitoringService _monitoringService;
        protected readonly IFiltroComunicazioniService _filtroService;
        protected readonly IExcelService _excelService;
        protected readonly ISendMailService _sendMailService;
        protected readonly IViewBagService _viewBagService;
        protected readonly ILogger<TLogger> _logger;
        protected readonly IRetryService _retryService;

        public ComunicazioniBaseController(ComDbContext context, IMapper mapper, IEmailService emailService,
                                           IMonitoringService monitoringService, IFiltroComunicazioniService filtroService,
                                           IExcelService excelService, ISendMailService sendMailService,
                                           ILogger<TLogger> logger, IViewBagService viewBagService,
                                           IRetryService retryService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
            _filtroService = filtroService ?? throw new ArgumentNullException(nameof(filtroService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _sendMailService = sendMailService ?? throw new ArgumentNullException(nameof(sendMailService));
            _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
        }

        protected void SetViewBagOptions(string? codCor = null)
        {
            ViewBag.CodCorOptions = _viewBagService.GetCodCorOptions(codCor) ?? new List<SelectListItem>();
            ViewBag.ServizioOptions = _viewBagService.GetServizioOptions(codCor) ?? new List<SelectListItem>();
        }

        protected async Task<IActionResult> BaseIndex(ComunicazioniViewModel filtri, string tipoAccertamento)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        SetViewBagOptions(filtri.CodCor);

                        // Gestione dei possibili valori null
                        var searchTerm = filtri.SearchTerm ?? string.Empty;
                        var codCor = filtri.CodCor ?? string.Empty;
                        var servizio = filtri.Servizio ?? string.Empty;
                        var sortField = filtri.SortField ?? "DateA"; // Campo di ordinamento predefinito
                        var sortOrder = filtri.SortOrder ?? "asc";   // Ordinamento predefinito

                        // Applicazione dei filtri tramite il servizio
                        var query = _context.Comunicazionis.AsQueryable();
                        query = await _filtroService.FiltraComunicazioniAsync(
                            query,
                            tipoAccertamento,
                            searchTerm,
                            filtri.StartDate,
                            filtri.EndDate,
                            codCor,
                            servizio,
                            filtri.MonthYear,
                            sortField,
                            sortOrder,
                            filtri.SoloRigheNonRestituite
                        );

                        // Paginazione
                        var totalItems = await query.CountAsync();
                        var items = await query
                            .Skip((filtri.PageNumber - 1) * filtri.PageSize)
                            .Take(filtri.PageSize)
                            .ToListAsync();

                        // Creazione del ViewModel
                        var model = new ComunicazioniViewModel
                        {
                            Comunicazioni = items,
                            CurrentPage = filtri.PageNumber,
                            TotalPages = (int)Math.Ceiling(totalItems / (double)filtri.PageSize),
                            StartDate = filtri.StartDate,
                            EndDate = filtri.EndDate,
                            CodCor = filtri.CodCor,
                            Servizio = filtri.Servizio,
                            MonthYear = filtri.MonthYear,
                            SortField = filtri.SortField,
                            SortOrder = filtri.SortOrder,
                            SearchTerm = filtri.SearchTerm,
                            SoloRigheNonRestituite = filtri.SoloRigheNonRestituite
                        };

                        await transaction.CommitAsync();
                        return View("Index", model);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }, _logger, this);
        }

        protected async Task<IActionResult> BaseDetails(int? id)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (!id.HasValue) return NotFound();

                        var comunicazioni = await _context.Comunicazionis.FirstOrDefaultAsync(c => c.Id == id.Value);
                        if (comunicazioni == null) return NotFound();

                        await _context.Entry(comunicazioni).Collection(c => c.Dettagli!).LoadAsync();
                        var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);

                        await transaction.CommitAsync();
                        return View("Details", comunicazioniDTO);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }, _logger, this);
        }

        /*protected async Task<IActionResult> BaseEditGet(int? id)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (!id.HasValue) return NotFound();

                        var comunicazioni = await _context.Comunicazionis.FindAsync(id);
                        if (comunicazioni == null) return NotFound();

                        SetViewBagOptions();
                        var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);

                        await transaction.CommitAsync();
                        return View("Edit", comunicazioniDTO);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }, _logger, this);
        }

        protected async Task<IActionResult> BaseEditPost(int id, ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (id != comunicazioniDTO.Id) return NotFound();

                        ModelState.Remove("excelFile");

                        if (!ModelState.IsValid)
                        {
                            SetViewBagOptions();
                            return View("Edit", comunicazioniDTO);
                        }

                        var comunicazioniToUpdate = await _context.Comunicazionis.FindAsync(id);
                        if (comunicazioniToUpdate == null) return NotFound();

                        _mapper.Map(comunicazioniDTO, comunicazioniToUpdate);

                        // Logica per DateF e Ritornato
                        if (comunicazioniDTO.DateF.HasValue)
                        {
                            // Se l'utente ha valorizzato DateF, imposta Ritornato a true, invia email e stoppa il monitoraggio
                            comunicazioniToUpdate.Ritornato = true;
                            await _sendMailService.HandlePostEditActionsAsync(comunicazioniToUpdate);
                            await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
                        }
                        else if (comunicazioniDTO.Ritornato)
                        {
                            // Se l'utente ha impostato solo Ritornato = true, non modifica DateF e stoppa il monitoraggio
                            comunicazioniToUpdate.Ritornato = true;
                            comunicazioniToUpdate.DateF = null; // Garantiamo che non venga valorizzato
                            await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
                        }

                        await _context.SaveChangesAsync();

                        // Gestione del file Excel
                        if (excelFile != null && excelFile.Length > 0)
                        {
                            var existingDetails = _context.ComunicazioniDettagli.Where(d => d.ComunicazioneId == comunicazioniToUpdate.Id);
                            _context.ComunicazioniDettagli.RemoveRange(existingDetails);
                            await _context.SaveChangesAsync();

                            var dettagli = await _excelService.ProcessExcelFileAsync(excelFile, comunicazioniToUpdate.Id);
                            if (dettagli != null)
                            {
                                _context.ComunicazioniDettagli.AddRange(dettagli);
                                await _context.SaveChangesAsync();
                            }
                        }

                        await transaction.CommitAsync();
                        return RedirectToAction("Index");
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }, _logger, this);
        }
*/
        protected async Task<IActionResult> BaseDelete(int? id)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (!id.HasValue) return NotFound();

                        var comunicazioni = await _context.Comunicazionis.FirstOrDefaultAsync(m => m.Id == id.Value);
                        if (comunicazioni == null) return NotFound();

                        await transaction.CommitAsync();
                        return View("Delete", comunicazioni);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }, _logger, this);
        }

        protected async Task<IActionResult> BaseDeleteConfirmed(int id)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var comunicazioni = await _context.Comunicazionis.FirstOrDefaultAsync(c => c.Id == id);

                        if (comunicazioni != null)
                        {
                            await _context.Entry(comunicazioni).Collection(c => c.Dettagli!).LoadAsync();

                            if (comunicazioni.Dettagli != null && comunicazioni.Dettagli.Any())
                            {
                                _context.ComunicazioniDettagli.RemoveRange(comunicazioni.Dettagli);
                            }

                            _context.Comunicazionis.Remove(comunicazioni);
                            await _context.SaveChangesAsync();
                        }

                        await transaction.CommitAsync();
                        return RedirectToAction("Index");
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }, _logger, this);
        }

        [HttpPost]
        public IActionResult ResetFiltri()
        {
            _filtroService.ResetFiltri();
            return RedirectToAction("Index");
        }
    }
}
