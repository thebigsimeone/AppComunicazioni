using AppComunicazioni.Controllers;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

[Route("ComunicazionisSsc")]
public class ComunicazionisSscController : ComunicazioniBaseController<ComunicazionisSscController>
{
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
        ILogger<ComunicazionisSscController> logger)
        : base(context, mapper, emailService, monitoringService, filtroService, excelService, sendMailService, logger, viewBagService, retryService, encryptionService)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ComunicazioniViewModel filtri)
    {
        if (filtri.PageNumber < 1)
        {
            filtri.PageNumber = 1;
        }
        return await BaseIndex(filtri, "SSC");
    }

    [HttpGet("Details/{id}")]
    public IActionResult Details(string id)
    {
        return RedirectToAction("Index", "Details", new { id });
    }

    /*    [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            return await BaseDetails(id);
        }*/

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
    {
        var editController = new EditController(_context, _mapper, _retryService, _viewBagService, _sendMailService, _encryptionService);
        return await editController.Index(id, comunicazioniDTO, excelFile);
    }

    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        return await BaseDelete(id);
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        return await BaseDeleteConfirmed(id);
    }

    [HttpPost("ResetFiltri")]
    public new IActionResult ResetFiltri()
    {
        base.ResetFiltri(); // Richiama il metodo della classe base, se necessario.
        return RedirectToAction("Index");
    }

}
