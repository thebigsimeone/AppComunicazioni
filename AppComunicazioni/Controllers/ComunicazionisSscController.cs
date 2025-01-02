using AppComunicazioni.Controllers;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

[Route("ComunicazionisSsc")]
public class ComunicazionisSscController : ComunicazioniBaseController<ComunicazionisSscController>
{
    private readonly ILogger<EditController> _editLogger;

    public ComunicazionisSscController(
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
                                        ILogger<ComunicazionisSscController> logger,
                                        ILogger<EditController> editLogger,
                                        IPaginationService paginationService,
                                        IHttpContextAccessor httpContextAccessor // Aggiunto IHttpContextAccessor
                                        )
                                        : base(context, mapper, emailService, monitoringService, filtroService, excelService,
                                               sendMailService, logger, viewBagService, retryService, encryptionService, paginationService, httpContextAccessor) // Passato alla classe base
    {
        _editLogger = editLogger ?? throw new ArgumentNullException(nameof(editLogger));
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ComunicazioniViewModel filtri)
    {
        return await BaseIndex(filtri, "SSC");
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
            _editLogger);

        return await editController.Index(id, comunicazioniDTO, excelFile);
    }

    [HttpGet("Delete/{id}")]
    public IActionResult Delete(string id)
    {
        return RedirectToAction("Index", "Details", new { id });
    }

    [HttpPost("ResetFiltri")]
    public new IActionResult ResetFiltri()
    {
        base.ResetFiltri(); // Richiama il metodo della classe base, se necessario.
        return RedirectToAction("Index");
    }
}
