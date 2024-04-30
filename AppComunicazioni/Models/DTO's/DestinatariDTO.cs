using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AppComunicazioni.Models.DTO_s
{
    public class DestinatariDTO
    {

        public int Id { get; set; }

        public string? Destinatario { get; set; }
    }
}
