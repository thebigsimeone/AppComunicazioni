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
    [Display(Name = "Data di ritorno")]
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

    [Column("email_inviata")]
    [Display(Name = "Email inviata")]
    public bool? Email_inviata { get; set; }

    [Column("mandante")]
    [Display(Name = "Mandante")]
    public string? Mandante { get; set; }

    [Column("data_notifica")]
    [Display(Name = "Data di notifica")]

    public DateTimeOffset? Data_Notifica { get; set; }
    [Column("report")]
    [StringLength(1)]
    [Display(Name = "Report")]

    public string? Report { get; set; }

    [Column("fornitore")]
    [Display(Name = "Fornitore")]

    public string? Fornitore { get; set; }

    public ICollection<ComunicazioniDettaglio>? Dettagli { get; set; }
}
