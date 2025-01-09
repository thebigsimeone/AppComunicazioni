using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppComunicazioni.Models;

[Table("destinatari")]
public partial class Destinatari
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("destinatario")]
    [StringLength(100)]
    [Required(ErrorMessage = "Il campo Destinatario è obbligatorio.")]
    public string? Destinatario { get; set; }

    [Column("monitor")]
    [StringLength(1)]
    [Display(Name = "Monitor")]
    public string? Monitor { get; set; }

    [Column("attivo")]
    [StringLength(1)]
    [Display(Name = "Attivo")]
    public string? Attivo { get; set; }
    [Column("report")]
    [StringLength(1)]
    [Display(Name = "Report")]
    public string? Report { get; set; }
}
