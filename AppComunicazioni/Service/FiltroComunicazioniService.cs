using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Service
{
    public class FiltroComunicazioniService : IFiltroComunicazioniService
    {
        private readonly ISession _session;

        public FiltroComunicazioniService(IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext.Session;
        }

        public void ResetFiltri()
        {
            _session.Remove("searchTerm");
            _session.Remove("startDate");
            _session.Remove("endDate");
            _session.Remove("codCor");
            _session.Remove("monthYear");
            _session.Remove("sortField");
            _session.Remove("sortOrder");
        }

        public async Task<IQueryable<Comunicazioni>> FiltraComunicazioniAsync(IQueryable<Comunicazioni> query, string tenant,
                                                                              string searchTerm, DateTime? startDate,
                                                                              DateTime? endDate, string codCor,
                                                                              DateTime? monthYear, string sortField, string sortOrder)
        {
            // Recupera i valori dei filtri dalla sessione se non forniti dall'utente
            searchTerm ??= _session.GetString("searchTerm");
            if (!_session.TryGetValue("startDate", out _))
                startDate = startDate ?? _session.GetString("startDate")?.ParseNullableDate();
            if (!_session.TryGetValue("endDate", out _))
                endDate = endDate ?? _session.GetString("endDate")?.ParseNullableDate();
            codCor ??= _session.GetString("codCor");
            monthYear ??= _session.GetString("monthYear")?.ParseNullableDate();
            sortField ??= _session.GetString("sortField") ?? "DateA";
            sortOrder ??= _session.GetString("sortOrder") ?? "default";

            // Salva i valori dei filtri nella sessione
            _session.SetString("searchTerm", searchTerm ?? "");
            _session.SetString("startDate", startDate?.ToString("yyyy-MM-dd") ?? "");
            _session.SetString("endDate", endDate?.ToString("yyyy-MM-dd") ?? "");
            _session.SetString("codCor", codCor ?? "");
            _session.SetString("monthYear", monthYear?.ToString("yyyy-MM") ?? "");
            _session.SetString("sortField", sortField);
            _session.SetString("sortOrder", sortOrder);

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
                if (codCor == "CESSIONI")
                {
                    // Filtro per tutti i codici correlati a CESSIONI
                    query = query.Where(x =>
                        x.FileName.StartsWith("001-1990") ||
                        x.FileName.StartsWith("001-1989") ||
                        x.FileName.StartsWith("001-8027") ||
                        x.FileName.StartsWith("001-1988"));
                }
                else if (codCor == "FORZA")
                {
                    // Filtro per tutti i codici correlati a FORZA
                    query = query.Where(x => x.FileName.StartsWith("001-8096"));
                }
                else if (!string.IsNullOrEmpty(codCor))
                {
                    // Filtro per un singolo codice specificato, se è stato inserito dall'utente
                    query = query.Where(x => x.FileName.Contains(codCor));
                }
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

    public static class SessionExtensions
    {
        public static DateTime? ParseNullableDate(this string dateString)
        {
            return DateTime.TryParse(dateString, out DateTime date) ? date : (DateTime?)null;
        }
    }
}
