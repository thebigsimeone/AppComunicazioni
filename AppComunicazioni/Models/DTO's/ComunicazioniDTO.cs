using System.ComponentModel.DataAnnotations;

namespace AppComunicazioni.Models.DTO_s
{
    public class ComunicazioniDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il campo Nome file è obbligatorio.")]
        [MaxLength(50)]
        public string? FileName { get; set; }

        [Required(ErrorMessage = "Il campo Data di invio è obbligatorio.")]
        public DateTimeOffset? DateA { get; set; }

        public DateTimeOffset? DateF { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Il numero di protocolli non può essere negativo.")]
        [Required(ErrorMessage = "Il campo Numero di protocolli è obbligatorio.")]
        public int NProtocol { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Il numero di protocolli non può essere negativo.")]
        public int? NsProtocol { get; set; }

        public string? Note { get; set; }

        [Required(ErrorMessage = "Il campo Servizio è obbligatorio.")]
        public ServizioType? Servizio { get; set; }
        public bool Ritornato { get; set; }
        public bool Email_inviata { get; set; }

        // Nuova proprietà per i dettagli
        public List<ComunicazioniDettaglioDTO>? Dettagli { get; set; }
    }

}
