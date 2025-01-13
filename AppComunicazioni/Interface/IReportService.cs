using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface IReportService
    {
        Task GenerateAndSendDailyReportAsync();
    }
}
