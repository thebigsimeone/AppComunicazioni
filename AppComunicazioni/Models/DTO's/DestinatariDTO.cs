using System.ComponentModel.DataAnnotations;

namespace AppComunicazioni.Models.DTO_s
{
    public class DestinatariDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il campo Destinatario è obbligatorio.")]
        [MaxLength(100)]
        public string? Destinatario { get; set; }

        [StringLength(1)]
        public string? Monitor { get; set; }

        [StringLength(1)]
        public string? Attivo { get; set; }
    }
}
