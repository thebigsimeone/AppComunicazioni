using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Service
{
    public class ContabilitaService : IContabilitaService
    {
        private readonly ComDbContext _context;

        public ContabilitaService(ComDbContext context)
        {
            _context = context;
        }

        public async Task<List<ContabilitaAccertamentiViewModel>> GetTotaleAccertamentiAsync(DateTime? meseAnno = null, string? codCor = null, string? tipoAccertamento = null)
        {
            var query = _context.Comunicazionis.AsQueryable();

            // Applica il filtro per mese e anno se fornito
            if (meseAnno.HasValue)
            {
                var firstDayOfMonth = new DateTime(meseAnno.Value.Year, meseAnno.Value.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                query = query.Where(x => x.DateF >= firstDayOfMonth && x.DateF <= lastDayOfMonth);
            }

            // Applica il filtro per CodCor se fornito
            if (!string.IsNullOrEmpty(codCor))
            {
                switch (codCor)
                {
                    case nameof(CodCorType.CESSIONI):
                        query = query.Where(x =>
                            x.FileName.StartsWith("001-1990") ||
                            x.FileName.StartsWith("001-1989") ||
                            x.FileName.StartsWith("001-8027") ||
                            x.FileName.StartsWith("001-1988"));
                        break;

                    case nameof(CodCorType.FORZA):
                        query = query.Where(x => x.FileName.StartsWith("001-8096"));
                        break;

                    case nameof(CodCorType.DATAVIZ):
                        query = query.Where(x => x.FileName.StartsWith("001-8033"));
                        break;

                    default:
                        // Filtro per un singolo codice specificato
                        query = query.Where(x => x.FileName.Contains(codCor));
                        break;
                }
            }

            // Applica il filtro per tipoAccertamento (EBI o SSC) se fornito
            if (!string.IsNullOrEmpty(tipoAccertamento))
            {
                query = query.Where(x => x.FileName.Contains(tipoAccertamento));
            }

            var accertamenti = await query
                .GroupBy(x => x.Servizio)
                .Select(g => new ContabilitaAccertamentiViewModel
                {
                    Servizio = g.Key.ToString(),
                    TotaleAccertamenti = (int)g.Sum(x => x.DateA != null ? x.NProtocol : 0),
                    TotaleAccertamentiRitornati = (int)g.Sum(x => x.DateF != null ? x.NProtocol : 0),
                    TotaleAccertamentiMancanti = (int)g.Sum(x => x.DateF == null ? x.NProtocol : 0)
                })
                .ToListAsync();

            return accertamenti;
        }
    }
}
