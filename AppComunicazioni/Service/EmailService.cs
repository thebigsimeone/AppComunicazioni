using AppComunicazioni.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text;

namespace AppComunicazioni.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["Sender"]));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(emailSettings["MailServer"], emailSettings.GetValue<int>("MailPort"), SecureSocketOptions.SslOnConnect);
            //var Password = Environment.GetEnvironmentVariable("BREVO_PASS");
            await smtp.AuthenticateAsync(emailSettings["Sender"], emailSettings["Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        public string FormatNote(string note)
        {
            if (string.IsNullOrEmpty(note))
                return note;

            var formattedNote = new StringBuilder();
            var lines = note.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Controllo per il formato con pipe (|) o trattino (-)
                if (line.Contains('|'))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        formattedNote.AppendLine($"{parts[0].Trim()} | {parts[1].Trim()}<br>");
                    }
                }
                else if (line.Contains('-'))
                {
                    var parts = line.Split('-');
                    if (parts.Length >= 2)
                    {
                        formattedNote.AppendLine($"{parts[0].Trim()} - {parts[1].Trim()}<br>");
                    }
                }
                else
                {
                    formattedNote.AppendLine($"{line.Trim()}<br>");
                }
            }

            return formattedNote.ToString();
        }
    }
}
