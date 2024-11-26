using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Interface
{
    public interface IFiltroComunicazioniService
    {
        Task<IQueryable<Comunicazioni>> FiltraComunicazioniAsync(
            IQueryable<Comunicazioni> query,
            string tenant,
            string searchTerm,
            DateTime? startDate,
            DateTime? endDate,
            string codCor,
            DateTime? monthYear,
            string sortField,
            string sortOrder);
    }
}
