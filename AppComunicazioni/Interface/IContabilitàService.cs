using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface IContabilitàService
    {
        Task<List<ContabilitaAccertamentiViewModel>> GetTotaleAccertamentiAsync(DateTime? meseAnno = null, string? codCor = null, string? tipoAccertamento = null);
    }
}
