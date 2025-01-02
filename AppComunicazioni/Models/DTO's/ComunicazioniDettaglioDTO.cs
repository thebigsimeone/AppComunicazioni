using System.ComponentModel.DataAnnotations;

namespace AppComunicazioni.Models
{
    public class ComunicazioniDettaglioDTO
    {
        public int Id { get; set; }

        public int ComunicazioneId { get; set; }
        [Required]
        [StringLength(11)]
        public string? Protocollo { get; set; } = null!;
        [Required]
        [StringLength(16)]
        public string? CodiceFiscale { get; set; } = null!;

        [StringLength(15)]
        public string? ColonnaSupplementare { get; set; }
        public DateTime DataInserimento { get; set; }
    }
}
