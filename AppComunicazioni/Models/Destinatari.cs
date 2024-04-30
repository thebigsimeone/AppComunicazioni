using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Models;

[Table("destinatari")]
public partial class Destinatari
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("destinatario")]
    [StringLength(100)]
    public string? Destinatario { get; set; }
}
