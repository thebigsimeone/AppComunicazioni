using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AppComunicazioni.Utility;
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
        protected readonly IStopMonitoringService _stopMonitoringService;
        protected readonly IFiltroComunicazioniService _filtroService;
        protected readonly IExcelService _excelService;
        protected readonly ISendMailService _sendMailService;
        protected readonly IViewBagService _viewBagService;
        protected readonly ILogger<TLogger> _logger;
        protected readonly IRetryService _retryService;
        protected readonly IEncryptionService _encryptionService;
        protected readonly IPaginationService _paginationService;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public ComunicazioniBaseController(
                                            ComDbContext context,
                                            IMapper mapper,
                                            IEmailService emailService,
                                            IMonitoringService monitoringService,
                                            IStopMonitoringService stopMonitoringService,
                                            IFiltroComunicazioniService filtroService,
                                            IExcelService excelService,
                                            ISendMailService sendMailService,
                                            ILogger<TLogger> logger,
                                            IViewBagService viewBagService,
                                            IRetryService retryService,
                                            IEncryptionService encryptionService,
                                            IPaginationService paginationService,
                                            IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
            _stopMonitoringService = stopMonitoringService ?? throw new ArgumentNullException(nameof(stopMonitoringService));
            _filtroService = filtroService ?? throw new ArgumentNullException(nameof(filtroService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _sendMailService = sendMailService ?? throw new ArgumentNullException(nameof(sendMailService));
            _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _paginationService = paginationService ?? throw new ArgumentNullException(nameof(paginationService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        protected ISession Session => _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("Session is not available");

        protected string EncryptId(int id) => _encryptionService.Encrypt(id.ToString());

        protected int DecryptId(string encryptedId)
        {
            var decrypted = _encryptionService.Decrypt(encryptedId);
            return int.TryParse(decrypted, out var id) ? id : throw new ArgumentException("ID non valido.");
        }

        protected void SetViewBagOptions(string? codCor = null)
        {
            ViewBag.CodCorOptions = _viewBagService.GetCodCorOptions() ?? new List<SelectListItem>();
            ViewBag.ServizioOptions = _viewBagService.GetServizioOptions(codCor) ?? new List<SelectListItem>();
        }

        protected async Task<IActionResult> BaseIndex(ComunicazioniViewModel filtri, string tipoAccertamento)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                SetViewBagOptions(filtri.CodCor);

                var query = _context.Comunicazionis.AsQueryable();

                query = await _filtroService.FiltraComunicazioniAsync(
                    query,
                    tipoAccertamento,
                    filtri.SearchTerm ?? Session?.GetString("SearchTerm") ?? string.Empty,
                    filtri.StartDate ?? (Session?.GetString("StartDate") != null ? DateTime.Parse(Session.GetString("StartDate")!) : null),
                    filtri.EndDate ?? (Session?.GetString("EndDate") != null ? DateTime.Parse(Session.GetString("EndDate")!) : null),
                    filtri.CodCor ?? Session?.GetString("CodCor") ?? string.Empty,
                    filtri.Servizio ?? Session?.GetString("Servizio") ?? string.Empty,
                    filtri.MonthYear ?? (Session?.GetString("MonthYear") != null ? DateTime.Parse(Session.GetString("MonthYear")!) : DateTime.UtcNow),
                    filtri.SortField ?? "DateA",
                    filtri.SortOrder ?? "asc",
                    filtri.SoloRigheNonRestituite || (Session?.GetBoolean("SoloRigheNonRestituite") ?? false)
                );

                var (paginatedData, totalPages) = await _paginationService.PaginateAsync(query, filtri.CurrentPage, filtri.PageSize > 0 ? filtri.PageSize : 10);

                var model = new ComunicazioniViewModel
                {
                    Comunicazioni = await paginatedData.ToListAsync(),
                    CurrentPage = filtri.CurrentPage,
                    TotalPages = totalPages,
                    PageSize = filtri.PageSize,
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

                return View("Index", model);
            });
        }

        [HttpPost]
        public IActionResult ResetFiltri(string tenant)
        {
            _filtroService.ResetFiltri();
            return RedirectToAction("Index");
        }
    }
}
