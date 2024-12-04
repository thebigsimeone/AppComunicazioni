using System.ComponentModel.DataAnnotations;

namespace AppComunicazioni.Models
{
    public class ComunicazioniDettaglioDTO
    {
        public int Id { get; set; }

        public int ComunicazioneId { get; set; }
        [Required]
        [StringLength(50)]
        public string Protocollo { get; set; } = null!;
        [Required]
        [StringLength(50)]
        public string CodiceFiscale { get; set; } = null!;

        [StringLength(100)]
        public string? ColonnaSupplementare { get; set; }
        public DateTime DataInserimento { get; set; }
    }
}
