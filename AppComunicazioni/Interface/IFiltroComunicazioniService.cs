using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface IFiltroComunicazioniService
    {
        void ResetFiltri();
        Task<IQueryable<Comunicazioni>> FiltraComunicazioniAsync(
            IQueryable<Comunicazioni> query,
            string tenant,
            string searchTerm,
            DateTime? startDate,
            DateTime? endDate,
            string codCor,
            string servizio,
            DateTime? monthYear,
            string sortField,
            string sortOrder,
            bool soloRigheNonRestituite);
    }
}
