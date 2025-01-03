using AppComunicazioni.Interface;
using AppComunicazioni.Models;

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
                                                                            string? codCor, // Non salviamo questo nella sessione
                                                                            string? servizio,
                                                                            DateTime? monthYear,
                                                                            string sortField,
                                                                            string sortOrder,
                                                                            bool soloRigheNonRestituite)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (string.IsNullOrEmpty(tenant)) throw new ArgumentNullException(nameof(tenant));

        sortField ??= "DateA";  // Default sort field
        sortOrder ??= "asc";    // Default sort order

        // Salva gli altri filtri nella sessione
        _sessionService.Set("SearchTerm", searchTerm ?? string.Empty);
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
            _sessionService.Set("MonthYear", monthYear.Value.ToString("o"));
        else
            _sessionService.Remove("MonthYear");

        // Se i filtri sono stati resettati, ritorna la query primaria
        if (string.IsNullOrEmpty(searchTerm) && !startDate.HasValue && !endDate.HasValue &&
            string.IsNullOrEmpty(servizio) && !monthYear.HasValue && !soloRigheNonRestituite)
        {
            query = FiltraPerTenant(query, tenant);
            await Task.CompletedTask;
            return query;
        }

        // Applica i filtri
        query = FiltraPerTenant(query, tenant);
        query = FiltraPerTermineRicerca(query, searchTerm);
        query = FiltraPerDate(query, startDate, endDate);
        query = FiltraPerServizio(query, servizio);
        query = FiltraPerMeseAnno(query, monthYear);

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
            query = query.Where(x => x.FileName != null && x.FileName.Contains(searchTerm));

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

    private IQueryable<Comunicazioni> FiltraPerCodCor(IQueryable<Comunicazioni> query, string? codCor)
    {
        if (!string.IsNullOrEmpty(codCor))
        {
            query = query.Where(x => x.FileName != null && x.FileName.Contains(codCor));
        }
        _logger.LogInformation($"Applicato filtro per codice correlato: {codCor}");
        return query;
    }

    private IQueryable<Comunicazioni> FiltraPerServizio(IQueryable<Comunicazioni> query, string? servizio)
    {
        if (!string.IsNullOrEmpty(servizio))
        {
            query = query.Where(x => x.Servizio != null && x.Servizio.ToLower() == servizio.ToLower());
        }
        _logger.LogInformation($"Applicato filtro per servizio: {servizio}");
        return query;
    }

    private IQueryable<Comunicazioni> FiltraPerMeseAnno(IQueryable<Comunicazioni> query, DateTime? monthYear)
    {
        if (monthYear.HasValue)
        {
            var start = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            query = query.Where(x => x.DateA >= start && x.DateA <= end);
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
