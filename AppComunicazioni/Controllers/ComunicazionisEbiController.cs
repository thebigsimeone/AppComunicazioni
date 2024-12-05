using AppComunicazioni.Controllers;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

public class ComunicazionisSscController : ComunicazioniBaseController<ComunicazionisSscController>
{
    public ComunicazionisSscController(ComDbContext context, IMapper mapper, IEmailService emailService,
                                       IMonitoringService monitoringService, IFiltroComunicazioniService filtroComunicazioniService,
                                       IExcelService excelService, ISendMailService sendMailService,
                                       ILogger<ComunicazionisSscController> logger, IViewBagService viewBagService,
                                       IRetryService retryService)
        : base(context, mapper, emailService, monitoringService, filtroComunicazioniService, excelService, sendMailService, logger, viewBagService, retryService)
    {
    }

    // Utilizzare i metodi dal controller base
    public async Task<IActionResult> Index(string searchTerm, DateTime? startDate, DateTime? endDate,
                                           string codCor, string servizio, DateTime? monthYear, bool? soloRigheNonRestituite = null,
                                           int pageNumber = 1, string sortField = "DateA", string sortOrder = "default")
    {
        return await BaseIndex(searchTerm, startDate, endDate, codCor, servizio, monthYear, soloRigheNonRestituite, pageNumber, sortField, sortOrder, "SSC");
    }

    public async Task<IActionResult> Details(int? id)
    {
        return await BaseDetails(id);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        return await BaseEditGet(id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note,Ritornato")] ComunicazioniDTO comunicazioniDTO, IFormFile excelFile)
    {
        return await BaseEditPost(id, comunicazioniDTO, excelFile);
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
