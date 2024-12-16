using AppComunicazioni.Controllers;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

public class ComunicazionisEbiController : ComunicazioniBaseController<ComunicazionisEbiController>
{
    private readonly EditController _editController;

    public ComunicazionisEbiController(ComDbContext context, IMapper mapper, IEmailService emailService,
                                       IMonitoringService monitoringService, IFiltroComunicazioniService filtroComunicazioniService,
                                       IExcelService excelService, ISendMailService sendMailService,
                                       ILogger<ComunicazionisEbiController> logger, IViewBagService viewBagService,
                                       IRetryService retryService)
        : base(context, mapper, emailService, monitoringService, filtroComunicazioniService, excelService, sendMailService, logger, viewBagService, retryService)
    {
        _editController = new EditController(context, mapper, retryService, viewBagService, monitoringService, sendMailService, excelService);
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

    public async Task<IActionResult> Details(int? id)
    {
        return await BaseDetails(id);
    }

    // Delego al controller EditController
    public async Task<IActionResult> Edit(int? id)
    {
        return await _editController.Index(id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ComunicazioniDTO comunicazioniDTO, IFormFile excelFile)
    {
        return await _editController.Index(id, comunicazioniDTO, excelFile);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        return await BaseDelete(id);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        return await BaseDeleteConfirmed(id);
    }
}
