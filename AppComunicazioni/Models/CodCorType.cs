using System.ComponentModel.DataAnnotations;

namespace AppComunicazioni.Models
{
    public enum CodCorType
    {
        [Display(Name = "DATAVIZ")]
        COD_8033,

        [Display(Name = "CESSIONI")]
        COD_1990,
        [Display(Name = "FORZA")]
        COD_8096
    }
}
