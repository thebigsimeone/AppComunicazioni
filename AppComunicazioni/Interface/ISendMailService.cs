using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface ISendMailService
    {
        Task HandlePostEditActionsAsync(Comunicazioni comunicazioniToUpdate);
        Task SendNotificationEmailsAsync(Comunicazioni comunicazioni);
    }
}
