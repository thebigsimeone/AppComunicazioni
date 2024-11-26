using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Service
{
    public class FiltroComunicazioniService : IFiltroComunicazioniService
    {
        public async Task<IQueryable<Comunicazioni>> FiltraComunicazioniAsync(
            IQueryable<Comunicazioni> query,
            string tenant,
            string searchTerm,
            DateTime? startDate,
            DateTime? endDate,
            string codCor,
            DateTime? monthYear,
            string sortField,
            string sortOrder)
        {
            // Aggiunge un filtro per selezionare solo le comunicazioni in base al tenant
            query = query.Where(x => x.FileName.Contains(tenant));

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(x => x.FileName.Contains(searchTerm));
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.DateA >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.DateF <= endDate.Value);
            }

            // Filtro basato sul CodCor (può trovarsi ovunque nella stringa del nome file)
            if (!string.IsNullOrEmpty(codCor))
            {
                query = query.Where(x => x.FileName.Contains(codCor));
            }

            // Filtro basato sul mese e anno
            if (monthYear.HasValue)
            {
                var firstDayOfMonth = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                query = query.Where(x => x.DateA >= firstDayOfMonth && x.DateA <= lastDayOfMonth);
            }

            // Ordina i risultati
            query = sortOrder switch
            {
                "asc" => sortField switch
                {
                    "FileName" => query.OrderBy(x => x.FileName),
                    "DateA" => query.OrderBy(x => x.DateA),
                    "DateF" => query.OrderBy(x => x.DateF),
                    "NProtocol" => query.OrderBy(x => x.NProtocol),
                    "NsProtocol" => query.OrderBy(x => x.NsProtocol),
                    _ => query.OrderBy(x => x.DateA),
                },
                "desc" => sortField switch
                {
                    "FileName" => query.OrderByDescending(x => x.FileName),
                    "DateA" => query.OrderByDescending(x => x.DateA),
                    "DateF" => query.OrderByDescending(x => x.DateF),
                    "NProtocol" => query.OrderByDescending(x => x.NProtocol),
                    "NsProtocol" => query.OrderByDescending(x => x.NsProtocol),
                    _ => query.OrderByDescending(x => x.DateA),
                },
                _ => query.OrderBy(x => x.DateA),
            };

            return query;
        }
    }
}
