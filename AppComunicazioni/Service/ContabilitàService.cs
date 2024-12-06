using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Service
{
    public class ContabilitàService : IContabilitàService
    {
        private readonly ComDbContext _context;
        private readonly IFiltroComunicazioniService _filtroService;

        public ContabilitàService(ComDbContext context, IFiltroComunicazioniService filtroService)
        {
            _context = context;
            _filtroService = filtroService;
        }

        public async Task<List<ContabilitaAccertamentiViewModel>> GetTotaleAccertamentiAsync(DateTime? meseAnno = null, string? codCor = null, string? tipoAccertamento = null)
        {
            // Ottieni la query iniziale
            var query = _context.Comunicazionis.AsQueryable();

            // Utilizza il FiltroComunicazioniService per applicare i filtri
            query = await _filtroService.FiltraComunicazioniAsync(query, tipoAccertamento, null, null, null, codCor, null, meseAnno, null, null, false);

            // Raggruppa e calcola i totali richiesti come nella query SQL fornita
            var accertamenti = await query
                                        .GroupBy(x => x.Servizio)
                                        .Select(g => new ContabilitaAccertamentiViewModel
                                        {
                                            Servizio = g.Key != null ? g.Key.ToString() : "N/A",
                                            TotaleAccertamenti = g.Sum(x => x.NProtocol ?? 0),
                                            TotaleAccertamentiRitornati = g.Sum(x => x.DateF.HasValue ? x.NProtocol ?? 0 : 0),
                                            TotaleAccertamentiMancanti = g.Sum(x => !x.DateF.HasValue ? x.NProtocol ?? 0 : 0)
                                        })
                                        .ToListAsync();

            return accertamenti;
        }
    }
}
