using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppComunicazioni.Models
{
    [Table("comunicazioni_dettagli")]
    public class ComunicazioniDettaglio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("comunicazione_id")]
        public int ComunicazioneId { get; set; }

        [ForeignKey("ComunicazioneId")]
        public Comunicazioni? Comunicazione { get; set; }

        [Column("protocollo")]
        [StringLength(50)]
        public string? Protocollo { get; set; }

        [Column("codice_fiscale")]
        [StringLength(50)]
        public string? CodiceFiscale { get; set; }

        [Column("colonna_supplementare")]
        [StringLength(100)]
        public string? ColonnaSupplementare { get; set; }

        [Column("data_inserimento")]
        public DateTime DataInserimento { get; set; }
    }
}
