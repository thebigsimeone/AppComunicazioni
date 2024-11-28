using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface IExcelService
    {
        Task<List<ComunicazioniDettaglio>> ProcessExcelFileAsync(IFormFile excelFile, int comunicazioneId);
    }
}
