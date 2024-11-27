using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface IContabilitaService
    {
        Task<List<ContabilitaAccertamentiViewModel>> GetTotaleAccertamentiAsync(DateTime? meseAnno = null, string codCor = null);
    }
}
