namespace AppComunicazioni.Interface
{
    public interface IStopMonitoringService
    {
        Task StopMonitoringForComunicazioneAsync(int comunicazioneId);
    }
}
