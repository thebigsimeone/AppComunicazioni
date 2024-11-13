namespace AppComunicazioni.Interface
{
    public interface IMonitoringService
    {
        Task CheckAndSendNotificationsAsync(List<int> destinatariIds = null);
        Task StopMonitoringForComunicazioneAsync(int comunicazioneId);
    }
}
