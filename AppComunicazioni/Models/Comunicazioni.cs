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
    public string? FileName { get; set; }

    [Column("date_a", TypeName = "datetime")]
    public DateTime? DateA { get; set; }

    [Column("date_f", TypeName = "datetime")]
    public DateTime? DateF { get; set; }

    [Column("n_protocol")]
    public int? NProtocol { get; set; }

    [Column("ns_protocol")]
    public int? NsProtocol { get; set; }

    [Column("note")]
    public string? Note { get; set; }
}
