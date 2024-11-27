using AppComunicazioni.ViewModels;

namespace AppComunicazioni.Interface
{
    public interface IContabilitaService
    {
        Task<List<ContabilitaAccertamentiViewModel>> GetTotaleAccertamentiAsync(DateTime? meseAnno = null, string codCor = null, string tipoAccertamento = null);
    }
}
