using AppComunicazioni.Models;

namespace Api.ModelsDTO
{
    public class FileParsedDTO
    {
        public string? FileName { get; set; }
        public string? CodiceFornitore { get; set; }
        public string? Mandante { get; set; }
        public ServizioType Servizio { get; set; }
        public int NProtocol { get; set; }
    }
}
