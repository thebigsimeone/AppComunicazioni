namespace AppComunicazioni.Interface
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        string FormatNote(string note);

    }
}
