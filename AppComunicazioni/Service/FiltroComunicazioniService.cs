using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Utility;

namespace AppComunicazioni.Service
{
    public class FiltroComunicazioniService : IFiltroComunicazioniService
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<FiltroComunicazioniService> _logger;

        public FiltroComunicazioniService(ISessionService sessionService, ILogger<FiltroComunicazioniService> logger)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ResetFiltri()
        {
            _sessionService.ResetFilters();
            _logger.LogInformation("I filtri sono stati resettati tramite il servizio di sessione.");
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
            // Salva i filtri nella sessione
            _sessionService.Set("SearchTerm", searchTerm ?? _sessionService.Get("SearchTerm") ?? string.Empty);
            _sessionService.Set("CodCor", codCor ?? _sessionService.Get("CodCor") ?? string.Empty);
            _sessionService.Set("Servizio", servizio ?? _sessionService.Get("Servizio") ?? string.Empty);
            _sessionService.Set("SortField", sortField ?? _sessionService.Get("SortField") ?? "DateA");
            _sessionService.Set("SortOrder", sortOrder ?? _sessionService.Get("SortOrder") ?? "asc");

            if (startDate.HasValue)
                _sessionService.Set("StartDate", startDate.Value.ToString("o"));
            else if (_sessionService.Get("StartDate") != null)
                startDate = DateTime.Parse(_sessionService.Get("StartDate")!);

            if (endDate.HasValue)
                _sessionService.Set("EndDate", endDate.Value.ToString("o"));
            else if (_sessionService.Get("EndDate") != null)
                endDate = DateTime.Parse(_sessionService.Get("EndDate")!);

            _sessionService.Set("SoloRigheNonRestituite", soloRigheNonRestituite.ToString());

            // Applica i filtri
            query = FiltraPerTenant(query, tenant);
            query = FiltraPerTermineRicerca(query, searchTerm);
            query = FiltraPerDate(query, startDate, endDate);
            query = FiltraPerCodCor(query, codCor);
            query = FiltraPerServizio(query, servizio);
            query = FiltraPerMeseAnno(query, monthYear);

            if (soloRigheNonRestituite)
                query = FiltraPerRigheNonRestituite(query);

            await Task.CompletedTask; // Metodo asincrono

            return OrdinaQuery(query, sortField, sortOrder);
        }

        private IQueryable<Comunicazioni> FiltraPerTenant(IQueryable<Comunicazioni> query, string tenant)
        {
            if (!string.IsNullOrEmpty(tenant))
            {
                query = query.Where(x => x.Mandante != null && x.Mandante == tenant);
                _logger.LogInformation($"Applicato filtro per tenant: {query}");
            }
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerTermineRicerca(IQueryable<Comunicazioni> query, string? searchTerm)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                bool isProtocollo = searchTerm.Length == 11 && searchTerm.All(char.IsDigit);
                bool isCodiceFiscale = searchTerm.Length >= 11 && searchTerm.All(char.IsLetterOrDigit);

                query = query.Where(x =>
                    (x.FileName != null && x.FileName.Contains(searchTerm)) ||
                    (isProtocollo && x.Dettagli != null && x.Dettagli.Any(d => d.Protocollo != null && d.Protocollo.Contains(searchTerm))) ||
                    (isCodiceFiscale && x.Dettagli != null && x.Dettagli.Any(d => d.CodiceFiscale != null && d.CodiceFiscale.Contains(searchTerm)))
                );
            }
            _logger.LogInformation($"Applicato filtro per termine di ricerca: {query}");
            return query;
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

        private IQueryable<Comunicazioni> FiltraPerCodCor(IQueryable<Comunicazioni> query, string? codCor)
        {
            if (!string.IsNullOrEmpty(codCor))
            {
                query = codCor switch
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
            _logger.LogInformation($"Applicato filtro per termine di ricerca: {query}");
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerServizio(IQueryable<Comunicazioni> query, string? servizio)
        {
            if (!string.IsNullOrEmpty(servizio))
            {
                query = query.Where(x => x.Servizio != null && x.Servizio == servizio);
            }
            _logger.LogInformation($"Applicato filtro per termine di ricerca: {query}");
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerMeseAnno(IQueryable<Comunicazioni> query, DateTime? monthYear)
        {
            if (monthYear.HasValue)
            {
                var firstDayOfMonth = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                query = query.Where(x => x.DateA.HasValue && x.DateA.Value >= firstDayOfMonth && x.DateA.Value <= lastDayOfMonth);
            }
            _logger.LogInformation($"Applicato filtro per termine di ricerca: {query}");
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerRigheNonRestituite(IQueryable<Comunicazioni> query)
        {
            _logger.LogInformation($"Applicato filtro per termine di ricerca: {query}");
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

    }
}
