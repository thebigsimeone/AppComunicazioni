using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppComunicazioni.Interface
{
    public interface IViewBagService
    {
        List<SelectListItem> GetCodCorOptions(string? codCor = null);
        List<SelectListItem> GetServizioOptions(string? codCor = null, string? servizio = null);
    }
}
