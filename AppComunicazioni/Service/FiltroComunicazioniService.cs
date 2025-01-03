using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Utility;

namespace AppComunicazioni.Service
{
    public class FiltroComunicazioniService : IFiltroComunicazioniService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<FiltroComunicazioniService> _logger;

        public FiltroComunicazioniService(IHttpContextAccessor httpContextAccessor, ILogger<FiltroComunicazioniService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("Session is not available");

        public void ResetFiltri()
        {
            var filtri = new[] { "searchTerm", "startDate", "endDate", "codCor", "servizio", "monthYear", "soloRigheNonRestituite" };

            foreach (var filtro in filtri)
            {
                if (Session.GetString(filtro) != null)
                {
                    Session.Remove(filtro);
                    _logger.LogInformation($"Filtro '{filtro}' rimosso dalla sessione.");
                }
            }
            _logger.LogInformation("Tutti i filtri sono stati resettati.");
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
            // Salva i parametri di filtro nella sessione
            Session.SetString("SearchTerm", searchTerm ?? Session.GetString("SearchTerm") ?? string.Empty);
            Session.SetString("CodCor", codCor ?? Session.GetString("CodCor") ?? string.Empty);
            Session.SetString("Servizio", servizio ?? Session.GetString("Servizio") ?? string.Empty);
            Session.SetString("SortField", sortField ?? Session.GetString("SortField") ?? "DateA");
            Session.SetString("SortOrder", sortOrder ?? Session.GetString("SortOrder") ?? "asc");
            Session?.SetBoolean("SoloRigheNonRestituite", soloRigheNonRestituite);

            if (startDate.HasValue)
                Session?.SetString("StartDate", startDate.Value.ToString("o"));
            else if (Session?.GetString("StartDate") != null)
                startDate = DateTime.Parse(Session.GetString("StartDate")!);

            if (endDate.HasValue)
                Session?.SetString("EndDate", endDate.Value.ToString("o"));
            else if (Session?.GetString("EndDate") != null)
                endDate = DateTime.Parse(Session.GetString("EndDate")!);

            // Applica i filtri
            query = FiltraPerTenant(query, tenant);
            query = FiltraPerTermineRicerca(query, searchTerm);
            query = FiltraPerDate(query, startDate, endDate);
            query = FiltraPerCodCor(query, codCor);
            query = FiltraPerServizio(query, servizio);
            query = FiltraPerMeseAnno(query, monthYear);

            if (soloRigheNonRestituite)
                query = FiltraPerRigheNonRestituite(query);

            await Task.CompletedTask; // Per mantenere il metodo asincrono

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
