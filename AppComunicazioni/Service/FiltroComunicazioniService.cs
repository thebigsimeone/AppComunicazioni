using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using System.Globalization;

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
                        string? tenant,
                        string? searchTerm,
                        DateTime? startDate,
                        DateTime? endDate,
                        string? codCor,
                        string? servizio,
                        DateTime? monthYear,
                        string? sortField,
                        string? sortOrder,
                        bool soloRigheNonRestituite)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        // Validazione del tenant
        if (string.IsNullOrEmpty(tenant))
        {
            _logger.LogWarning("Il valore del tenant è nullo o vuoto. Utilizzo di un valore predefinito.");
            tenant = "SSC"; // Valore predefinito
        }

        sortField ??= "DateA"; // Campo di ordinamento predefinito
        sortOrder ??= "asc";   // Ordine predefinito

        // Se nessun filtro è applicato, ritorna la query primaria
        if (string.IsNullOrEmpty(searchTerm) &&
            !startDate.HasValue &&
            !endDate.HasValue &&
            string.IsNullOrEmpty(codCor) &&
            string.IsNullOrEmpty(servizio) &&
            !monthYear.HasValue &&
            !soloRigheNonRestituite)
        {
            _logger.LogInformation("Nessun filtro applicato. Ritorno alla query primaria.");
            return query.Where(x => x.Mandante == tenant);
        }

        // Salva i filtri nella sessione
        _sessionService.Set("SearchTerm", searchTerm ?? string.Empty);
        _sessionService.Set("CodCor", codCor ?? string.Empty);
        _sessionService.Set("Servizio", servizio ?? string.Empty);
        _sessionService.Set("SortField", sortField);
        _sessionService.Set("SortOrder", sortOrder);
        _sessionService.Set("SoloRigheNonRestituite", soloRigheNonRestituite.ToString());

        if (startDate.HasValue)
            _sessionService.Set("StartDate", startDate.Value.ToString("o"));
        else
            _sessionService.Remove("StartDate");

        if (endDate.HasValue)
            _sessionService.Set("EndDate", endDate.Value.ToString("o"));
        else
            _sessionService.Remove("EndDate");

        if (monthYear.HasValue)
            _sessionService.Set("MonthYear", monthYear.Value.ToString("yyyy-MM"));
        else
            _sessionService.Remove("MonthYear");

        // Applica i filtri
        query = FiltraPerTenant(query, tenant);
        query = FiltraPerTermineRicerca(query, searchTerm);
        query = FiltraPerDate(query, startDate, endDate);
        query = FiltraPerCodCor(query, codCor);
        query = FiltraPerServizio(query, servizio);
        query = FiltraPerAnnoOMese(query, monthYear);

        if (soloRigheNonRestituite)
            query = FiltraPerRigheNonRestituite(query);

        await Task.CompletedTask;

        return OrdinaQuery(query, sortField, sortOrder);
    }

    private IQueryable<Comunicazioni> FiltraPerTenant(IQueryable<Comunicazioni> query, string tenant)
    {
        return query.Where(x => x.Mandante == tenant);
    }

    private IQueryable<Comunicazioni> FiltraPerTermineRicerca(IQueryable<Comunicazioni> query, string? searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // Rimuovi spazi bianchi prima e dopo l'input
            var trimmedSearchTerm = searchTerm.Trim();

            // Unisci con la tabella ComunicazioniDettaglio
            query = query.Where(x =>
                (x.FileName != null && x.FileName.Contains(trimmedSearchTerm)) || // Ricerca per FileName
                x.Dettagli.Any(d =>
                    (d.Protocollo != null && d.Protocollo.Contains(trimmedSearchTerm)) || // Ricerca per Protocollo
                    (d.CodiceFiscale != null && d.CodiceFiscale.Contains(trimmedSearchTerm)) // Ricerca per Codice Fiscale
                )
            );
        }

        return query;
    }

    private IQueryable<Comunicazioni> FiltraPerDate(IQueryable<Comunicazioni> query, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue)
            query = query.Where(x => x.DateA >= startDate);

        if (endDate.HasValue)
            query = query.Where(x => x.DateA <= endDate);

        return query;
    }

    private IQueryable<Comunicazioni> FiltraPerServizio(IQueryable<Comunicazioni> query, string? servizio)
    {
        if (!string.IsNullOrEmpty(servizio))
            query = query.Where(x => x.Servizio != null && x.Servizio.ToLower() == servizio.ToLower());

        return query;
    }
    private IQueryable<Comunicazioni> FiltraPerCodCor(IQueryable<Comunicazioni> query, string? codCor)
    {
        // Il filtro CodCor viene applicato solo quando necessario
        if (!string.IsNullOrEmpty(codCor))
        {
            query = query.Where(x => x.FileName != null && x.FileName.Contains(codCor));
            _logger.LogInformation($"Applicato filtro per codice correlato: {codCor}");
        }
        return query;
    }

    private IQueryable<Comunicazioni> FiltraPerAnnoOMese(IQueryable<Comunicazioni> query, DateTime? monthYear)
    {
        if (monthYear.HasValue)
        {
            if (monthYear.Value.Month == 1 && monthYear.Value.Day == 1) // Solo anno
            {
                query = query.Where(x => x.DateA.HasValue && x.DateA.Value.Year == monthYear.Value.Year);
            }
            else // Mese/anno
            {
                var start = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);
                query = query.Where(x => x.DateA.HasValue && x.DateA.Value >= start && x.DateA.Value <= end);
            }
        }

        return query;
    }

    private IQueryable<Comunicazioni> FiltraPerRigheNonRestituite(IQueryable<Comunicazioni> query)
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
                _ => query.OrderBy(x => x.DateA),
            },
            "desc" => sortField.ToLower() switch
            {
                "filename" => query.OrderByDescending(x => x.FileName),
                "datea" => query.OrderByDescending(x => x.DateA),
                "datef" => query.OrderByDescending(x => x.DateF),
                _ => query.OrderByDescending(x => x.DateA),
            },
            _ => query.OrderBy(x => x.DateA),
        };
    }
}
