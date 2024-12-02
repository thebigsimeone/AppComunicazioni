using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppComunicazioni.Models;

[Table("comunicazioni")]
public partial class Comunicazioni
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("file_name")]
    [StringLength(50)]
    [Display(Name = "Nome file")]
    public string? FileName { get; set; }

    [Column("date_a")]
    [Display(Name = "Data di invio")]
    public DateTimeOffset? DateA { get; set; }

    [Column("date_f")]
    [Display(Name = "Data di fine smarco")]
    public DateTimeOffset? DateF { get; set; }

    [Column("n_protocol")]
    [Display(Name = "Numero protocolli")]
    [Range(0, int.MaxValue, ErrorMessage = "Il numero di protocolli non può essere negativo.")]
    public int? NProtocol { get; set; }

    [Column("ns_protocol")]
    [Display(Name = "Protocolli da rivedere")]
    [Range(0, int.MaxValue, ErrorMessage = "Il numero di protocolli non può essere negativo.")]
    public int? NsProtocol { get; set; }

    [Column("note")]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    [Column("servizio")]
    [Display(Name = "Servizio")]
    public string? Servizio { get; set; }

    [Column("notificato")]
    [Display(Name = "Notificato")]
    public bool? Notificato { get; set; }

    [Column("ritornato")]
    [Display(Name = "Ritornato")]
    public bool? Ritornato { get; set; }

    public ICollection<ComunicazioniDettaglio>? Dettagli { get; set; }

}
