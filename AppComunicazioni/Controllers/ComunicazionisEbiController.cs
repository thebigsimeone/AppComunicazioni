using AppComunicazioni.Controllers;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

[Route("ComunicazionisEbi")]
public class ComunicazionisEbiController : ComunicazioniBaseController<ComunicazionisEbiController>
{
    public ComunicazionisEbiController(
        ComDbContext context,
        IMapper mapper,
        IEmailService emailService,
        IMonitoringService monitoringService,
        IFiltroComunicazioniService filtroService, // Aggiunto
        IExcelService excelService,                // Aggiunto
        ISendMailService sendMailService,
        IViewBagService viewBagService,
        IRetryService retryService,
        IEncryptionService encryptionService,
        ILogger<ComunicazionisEbiController> logger) // Logger
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
        return await BaseIndex(filtri, "EBI");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        return await BaseDetails(id);
    }

    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
    {
        var editController = new EditController(_context, _mapper, _retryService, _viewBagService, _sendMailService, _encryptionService);
        return await editController.Index(id, comunicazioniDTO, excelFile);
    }

    public async Task<IActionResult> Details(string id)
    {
        return await BaseDetails(id);
    }

    public async Task<IActionResult> Delete(string id)
    {
        return await BaseDelete(id);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        return await BaseDeleteConfirmed(id);
    }

    [HttpPost("ResetFiltri")]
    public IActionResult ResetFiltri()
    {
        return base.ResetFiltri();
    }

}
