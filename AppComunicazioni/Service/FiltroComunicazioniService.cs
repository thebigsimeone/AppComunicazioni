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
            RecuperaFiltriDaSessione(ref searchTerm, ref startDate, ref endDate, ref codCor, ref servizio, ref monthYear, ref sortField, ref sortOrder, ref soloRigheNonRestituite);
            SalvaFiltriInSessione(searchTerm, startDate, endDate, codCor, servizio, monthYear, sortField, sortOrder, soloRigheNonRestituite);

            query = query.Include(c => c.Dettagli);

            query = FiltraPerTenant(query, tenant);
            query = FiltraPerTermineRicerca(query, searchTerm);
            query = FiltraPerDate(query, startDate, endDate);
            query = FiltraPerCodCor(query, codCor);
            query = FiltraPerServizio(query, servizio);
            query = FiltraPerMeseAnno(query, monthYear);
            query = FiltraPerRigheNonRestituite(query, soloRigheNonRestituite);
            query = OrdinaQuery(query, sortField, sortOrder);

            return await Task.FromResult(query);
        }

        #region Metodi di Filtraggio
        private IQueryable<Comunicazioni> FiltraPerTenant(IQueryable<Comunicazioni> query, string? tenant)
        {
            if (!string.IsNullOrEmpty(tenant))
            {
                query = query.Where(x => x.FileName != null && x.FileName.Contains(tenant));
            }
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerTermineRicerca(IQueryable<Comunicazioni> query, string? searchTerm)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string trimmedSearchTerm = searchTerm.Trim();

                query = query.Where(x =>
                    (x.FileName != null && x.FileName.Contains(trimmedSearchTerm)) ||
                    (x.Dettagli != null && x.Dettagli.Any(d => d.Protocollo != null && d.Protocollo.Contains(trimmedSearchTerm)))
                );
            }
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
                query = query.Where(x => x.DateF.HasValue && x.DateF.Value <= endDate.Value);
            }
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerCodCor(IQueryable<Comunicazioni> query, string? codCor)
        {
            if (!string.IsNullOrEmpty(codCor))
            {
                switch (codCor)
                {
                    case nameof(CodCorType.CESSIONI):
                        query = query.Where(x => x.FileName != null &&
                            (x.FileName.StartsWith("001-1990") ||
                             x.FileName.StartsWith("001-1989") ||
                             x.FileName.StartsWith("001-8027") ||
                             x.FileName.StartsWith("001-1988")));
                        break;

                    case nameof(CodCorType.FORZA):
                        query = query.Where(x => x.FileName != null && x.FileName.StartsWith("001-8096"));
                        break;

                    case nameof(CodCorType.DATAVIZ):
                        query = query.Where(x => x.FileName != null && x.FileName.StartsWith("001-8033"));
                        break;

                    default:
                        query = query.Where(x => x.FileName != null && x.FileName.Contains(codCor));
                        break;
                }
            }
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerServizio(IQueryable<Comunicazioni> query, string? servizio)
        {
            if (!string.IsNullOrEmpty(servizio))
            {
                query = query.Where(x => x.Servizio != null && x.Servizio == servizio);
            }
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
            return query;
        }

        private IQueryable<Comunicazioni> FiltraPerRigheNonRestituite(IQueryable<Comunicazioni> query, bool soloRigheNonRestituite)
        {
            if (soloRigheNonRestituite)
            {
                query = query.Where(x => !x.DateF.HasValue);
            }
            return query;
        }

        private IQueryable<Comunicazioni> OrdinaQuery(IQueryable<Comunicazioni> query, string? sortField, string? sortOrder)
        {
            return sortOrder switch
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
        }
        #endregion

        #region Gestione Sessione
        private void RecuperaFiltriDaSessione(ref string? searchTerm, ref DateTime? startDate, ref DateTime? endDate, ref string? codCor, ref string? servizio, ref DateTime? monthYear, ref string? sortField, ref string? sortOrder, ref bool soloRigheNonRestituite)
        {
            if (_session != null)
            {
                searchTerm ??= _session.GetString("searchTerm");
                startDate ??= _session.GetString("startDate")?.ParseNullableDate();
                endDate ??= _session.GetString("endDate")?.ParseNullableDate();
                codCor ??= _session.GetString("codCor");
                servizio ??= _session.GetString("servizio");
                monthYear ??= _session.GetString("monthYear")?.ParseNullableDate();
                sortField ??= _session.GetString("sortField") ?? "DateA";
                sortOrder ??= _session.GetString("sortOrder") ?? "default";
                soloRigheNonRestituite = bool.TryParse(_session.GetString("soloRigheNonRestituite"), out bool result) ? result : soloRigheNonRestituite;
            }
        }

        private void SalvaFiltriInSessione(string? searchTerm, DateTime? startDate, DateTime? endDate, string? codCor, string? servizio, DateTime? monthYear, string? sortField, string? sortOrder, bool soloRigheNonRestituite)
        {
            if (_session != null)
            {
                _session.SetString("searchTerm", searchTerm ?? "");
                _session.SetString("startDate", startDate?.ToString("yyyy-MM-dd") ?? "");
                _session.SetString("endDate", endDate?.ToString("yyyy-MM-dd") ?? "");
                _session.SetString("codCor", codCor ?? "");
                _session.SetString("servizio", servizio ?? "");
                _session.SetString("monthYear", monthYear?.ToString("yyyy-MM") ?? "");
                _session.SetString("sortField", sortField ?? "DateA");
                _session.SetString("sortOrder", sortOrder ?? "default");
                _session.SetString("soloRigheNonRestituite", soloRigheNonRestituite.ToString());
            }
        }
        #endregion
    }

    public static class SessionExtensions
    {
        public static DateTime? ParseNullableDate(this string dateString)
        {
            return DateTime.TryParse(dateString, out DateTime date) ? date : (DateTime?)null;
        }
    }
}
