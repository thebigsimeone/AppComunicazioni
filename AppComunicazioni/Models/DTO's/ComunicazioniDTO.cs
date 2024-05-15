using System.ComponentModel.DataAnnotations;

namespace AppComunicazioni.Models.DTO_s
{
    public class ComunicazioniDTO
    {

        public int Id { get; set; }

        [Required(ErrorMessage = "Il campo Nome file è obbligatorio.")]
        [MaxLength(50)]
        public string? FileName { get; set; }

        [Required(ErrorMessage = "Il campo Data di arrivo è obbligatorio.")]
        public DateTime DateA { get; set; }

        [Required(ErrorMessage = "Il campo Data di fine smarco è obbligatorio.")]
        public DateTime DateF { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Il numero di protocolli non può essere negativo.")]
        [Required(ErrorMessage = "Il campo Numero di protocolli è obbligatorio.")]
        public int NProtocol { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Il numero di protocolli non può essere negativo.")]
        [Required(ErrorMessage = "Il campo Protocolli da rivedere è obbligatorio.")]
        public int NsProtocol { get; set; }

        public string? Note { get; set; }
    }
}
