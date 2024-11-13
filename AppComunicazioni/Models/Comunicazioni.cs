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

    [Column("date_a", TypeName = "datetime")]
    [Display(Name = "Data di invio")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy | HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DateA { get; set; }

    [Column("date_f", TypeName = "datetime")]
    [Display(Name = "Data di fine smarco")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy | HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DateF { get; set; }

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
}
