namespace AppComunicazioni.Models.DTO_s
{
    public class ComunicazioniDTO
    {

        public int Id { get; set; }

        public string? FileName { get; set; }

        public DateTime DateA { get; set; }

        public DateTime DateF { get; set; }

        public int NProtocol { get; set; }

        public int NsProtocol { get; set; }

        public string? Note { get; set; }
    }
}
