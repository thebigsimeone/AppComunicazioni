using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface ISendMailService
    {
        Task HandlePostEditActionsAsync(Comunicazioni comunicazioniToUpdate);
        Task SendMarkingEmailsAsync(Comunicazioni comunicazioni);
        Task SendNotificationEmailAsync(List<ComunicazioniWithDaysModel> notifications);
        Task SendEmailReportAsync(List<Comunicazioni> ritardi, List<Comunicazioni> ritorni);
    }
}
