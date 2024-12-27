using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AppComunicazioni.Service;
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
        protected readonly IEncryptionService _encryptionService;
        protected readonly IPaginationService _paginationService;

        public ComunicazioniBaseController(ComDbContext context, IMapper mapper, IEmailService emailService,
                                           IMonitoringService monitoringService, IFiltroComunicazioniService filtroService,
                                           IExcelService excelService, ISendMailService sendMailService,
                                           ILogger<TLogger> logger, IViewBagService viewBagService,
                                           IRetryService retryService, IEncryptionService encryptionService, IPaginationService paginationService)
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
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _paginationService = paginationService;
        }

        protected string EncryptId(int id) => _encryptionService.Encrypt(id.ToString());

        protected int DecryptId(string encryptedId)
        {
            var decrypted = _encryptionService.Decrypt(encryptedId);
            return int.TryParse(decrypted, out var id) ? id : throw new ArgumentException("ID non valido.");
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

                        var query = _context.Comunicazionis.AsQueryable();

                        query = await _filtroService.FiltraComunicazioniAsync(
                            query,
                            tipoAccertamento,
                            filtri.SearchTerm ?? string.Empty,
                            filtri.StartDate,
                            filtri.EndDate,
                            filtri.CodCor ?? string.Empty,
                            filtri.Servizio ?? string.Empty,
                            filtri.MonthYear,
                            filtri.SortField ?? "DateA",
                            filtri.SortOrder ?? "asc",
                            filtri.SoloRigheNonRestituite
                        );

                        // Usare il PaginationService
                        var (paginatedData, totalPages) = await _paginationService.PaginateAsync(
                            query,
                            filtri.PageNumber,
                            filtri.PageSize
                        );

                        var model = new ComunicazioniViewModel
                        {
                            Comunicazioni = await paginatedData.ToListAsync(),
                            CurrentPage = filtri.PageNumber,
                            TotalPages = totalPages,
                            StartDate = filtri.StartDate,
                            EndDate = filtri.EndDate,
                            CodCor = filtri.CodCor ?? string.Empty,
                            Servizio = filtri.Servizio ?? string.Empty,
                            MonthYear = filtri.MonthYear,
                            SortField = filtri.SortField ?? "DateA",
                            SortOrder = filtri.SortOrder ?? "asc",
                            SearchTerm = filtri.SearchTerm ?? string.Empty,
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

        protected async Task<IActionResult> BaseDelete(string encryptedId)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var id = DecryptId(encryptedId);

                        var entity = await _context.Comunicazionis.FindAsync(id);
                        if (entity   == null) return NotFound();

                        await transaction.CommitAsync();
                        return View("Delete", entity);
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

        [NonAction]
        public IActionResult ResetFiltri()
        {
            _filtroService.ResetFiltri();
            return RedirectToAction("Index");
        }
    }
}
