
namespace AppComunicazioni.Models.DTO_s
{
    public class ComunicazioniWithDaysModel
    {
        public Comunicazioni? Comunicazioni { get; set; }
        public string? NomeServizio { get; set; }  // Nome del servizio estratto dal FileName
        public double? TotalDays { get; set; }
        public double? TotalHours { get; set; }
        public double? TotalMinutes { get; set; }
        public DateTime? LocalDateA { get; internal set; }
    }
}
