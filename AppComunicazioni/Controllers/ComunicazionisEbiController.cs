using AppComunicazioni.Controllers;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

[Route("ComunicazionisEbi")]
public class ComunicazionisEbiController : ComunicazioniBaseController<ComunicazionisEbiController>
{
    private readonly ILogger<EditController> _editLogger;

    public ComunicazionisEbiController(
        ComDbContext context,
        IMapper mapper,
        IEmailService emailService,
        IMonitoringService monitoringService,
        IFiltroComunicazioniService filtroService,
        IExcelService excelService,
        ISendMailService sendMailService,
        IViewBagService viewBagService,
        IRetryService retryService,
        IEncryptionService encryptionService,
        ILogger<ComunicazionisEbiController> logger,
        ILogger<EditController> editLogger,
        IPaginationService paginationService,
        IHttpContextAccessor httpContextAccessor)
        : base(context, mapper, emailService, monitoringService, filtroService, excelService,
               sendMailService, logger, viewBagService, retryService, encryptionService, paginationService, httpContextAccessor)
    {
        _editLogger = editLogger ?? throw new ArgumentNullException(nameof(editLogger));
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ComunicazioniViewModel filtri)
    {
        return await BaseIndex(filtri, "EBI");
    }

    [HttpGet("Details/{id}")]
    public IActionResult Details(string id)
    {
        return RedirectToAction("Index", "Details", new { id });
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
    {
        var editController = new EditController(
            _context,
            _mapper,
            _retryService,
            _viewBagService,
            _sendMailService,
            _encryptionService,
            _editLogger,
            _excelService);

        return await editController.Index(id, comunicazioniDTO, excelFile);
    }

    [HttpPost("ResetFiltri")]
    public IActionResult ResetFiltri()
    {
        return base.ResetFiltri("EBI");
    }
}
