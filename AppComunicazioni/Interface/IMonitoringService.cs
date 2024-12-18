using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface IMonitoringService
    {
        Task CheckAndSendNotificationsAsync(List<int>? destinatariIds = null);
    }
}
