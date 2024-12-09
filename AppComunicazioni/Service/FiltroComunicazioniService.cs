using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Service
{
    public class FiltroComunicazioniService : IFiltroComunicazioniService
    {
        private readonly ISession? _session;

        public FiltroComunicazioniService(IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext?.Session;
        }

        public void ResetFiltri()
        {
            if (_session != null)
            {
                var filtri = new[]
                {
                    "searchTerm", "startDate", "endDate", "codCor", "servizio",
                    "monthYear", "sortField", "sortOrder", "soloRigheNonRestituite"
                };

                foreach (var filtro in filtri)
                {
                    _session.Remove(filtro);
                }
            }
        }

        public async Task<IQueryable<Comunicazioni>> FiltraComunicazioniAsync(
            IQueryable<Comunicazioni> query,
            string tenant,
            string? searchTerm,
            DateTime? startDate,
            DateTime? endDate,
            string? codCor,
            string? servizio,
            DateTime? monthYear,
            string sortField,
            string sortOrder,
            bool soloRigheNonRestituite)
        {
            // Garantisce che i parametri non siano null, usando valori predefiniti se necessario
            tenant = tenant ?? string.Empty;
            searchTerm = searchTerm?.Trim() ?? string.Empty;
            codCor = codCor ?? string.Empty;
            servizio = servizio ?? string.Empty;
            sortField = string.IsNullOrWhiteSpace(sortField) ? "DateA" : sortField;
            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "asc" : sortOrder;

            // Applicazione dei filtri
            query = query.Include(c => c.Dettagli);

            if (!string.IsNullOrEmpty(tenant))
                query = FiltraPerTenant(query, tenant);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = FiltraPerTermineRicerca(query, searchTerm);

            if (startDate.HasValue || endDate.HasValue)
                query = FiltraPerDate(query, startDate, endDate);

            if (!string.IsNullOrEmpty(codCor))
                query = FiltraPerCodCor(query, codCor);

            if (!string.IsNullOrEmpty(servizio))
                query = FiltraPerServizio(query, servizio);

            if (monthYear.HasValue)
                query = FiltraPerMeseAnno(query, monthYear);

            if (soloRigheNonRestituite)
                query = FiltraPerRigheNonRestituite(query, soloRigheNonRestituite);

            query = OrdinaQuery(query, sortField, sortOrder);

            return await Task.FromResult(query);
        }

        #region Metodi di Filtraggio
        private IQueryable<Comunicazioni> FiltraPerTenant(IQueryable<Comunicazioni> query, string tenant)
        {
            return query.Where(x => x.FileName != null && x.FileName.Contains(tenant));
        }

        private IQueryable<Comunicazioni> FiltraPerTermineRicerca(IQueryable<Comunicazioni> query, string searchTerm)
        {
            return query.Where(x =>
                (x.FileName != null && x.FileName.Contains(searchTerm)) ||
                (x.Dettagli != null && x.Dettagli.Any(d => d.Protocollo != null && d.Protocollo.Contains(searchTerm)))
            );
        }

        private IQueryable<Comunicazioni> FiltraPerDate(IQueryable<Comunicazioni> query, DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue)
            {
                query = query.Where(x => x.DateA.HasValue && x.DateA.Value >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(x => x.DateA.HasValue && x.DateA.Value <= endDate.Value);
            }
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerCodCor(IQueryable<Comunicazioni> query, string codCor)
        {
            return codCor switch
            {
                nameof(CodCorType.CESSIONI) => query.Where(x => x.FileName != null &&
                    (x.FileName.StartsWith("001-1990") ||
                     x.FileName.StartsWith("001-1989") ||
                     x.FileName.StartsWith("001-8027") ||
                     x.FileName.StartsWith("001-1988"))),
                nameof(CodCorType.FORZA) => query.Where(x => x.FileName != null && x.FileName.StartsWith("001-8096")),
                nameof(CodCorType.DATAVIZ) => query.Where(x => x.FileName != null && x.FileName.StartsWith("001-8033")),
                _ => query.Where(x => x.FileName != null && x.FileName.Contains(codCor)),
            };
        }

        private IQueryable<Comunicazioni> FiltraPerServizio(IQueryable<Comunicazioni> query, string servizio)
        {
            return query.Where(x => x.Servizio != null && x.Servizio == servizio);
        }

        private IQueryable<Comunicazioni> FiltraPerMeseAnno(IQueryable<Comunicazioni> query, DateTime? monthYear)
        {
            if (monthYear.HasValue)
            {
                var firstDayOfMonth = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                query = query.Where(x => x.DateA.HasValue && x.DateA.Value >= firstDayOfMonth && x.DateA.Value <= lastDayOfMonth);
            }
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerRigheNonRestituite(IQueryable<Comunicazioni> query, bool soloRigheNonRestituite)
        {
            return query.Where(x => x.DateF == null);
        }

        private IQueryable<Comunicazioni> OrdinaQuery(IQueryable<Comunicazioni> query, string sortField, string sortOrder)
        {
            return sortOrder.ToLower() switch
            {
                "asc" => sortField.ToLower() switch
                {
                    "filename" => query.OrderBy(x => x.FileName),
                    "datea" => query.OrderBy(x => x.DateA),
                    "datef" => query.OrderBy(x => x.DateF),
                    "nprotocol" => query.OrderBy(x => x.NProtocol),
                    "nsprotocol" => query.OrderBy(x => x.NsProtocol),
                    _ => query.OrderBy(x => x.DateA),
                },
                "desc" => sortField.ToLower() switch
                {
                    "filename" => query.OrderByDescending(x => x.FileName),
                    "datea" => query.OrderByDescending(x => x.DateA),
                    "datef" => query.OrderByDescending(x => x.DateF),
                    "nprotocol" => query.OrderByDescending(x => x.NProtocol),
                    "nsprotocol" => query.OrderByDescending(x => x.NsProtocol),
                    _ => query.OrderByDescending(x => x.DateA),
                },
                _ => query.OrderBy(x => x.DateA),
            };
        }
        #endregion
    }
}
