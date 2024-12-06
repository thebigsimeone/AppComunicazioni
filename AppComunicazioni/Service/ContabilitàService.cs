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
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _filtroService = filtroService ?? throw new ArgumentNullException(nameof(filtroService));
        }

        public async Task<List<ContabilitaAccertamentiViewModel>> GetTotaleAccertamentiAsync(DateTime? meseAnno = null, string? codCor = null, string? tipoAccertamento = null)
        {
            // Ottieni la query iniziale
            var query = _context.Comunicazionis.AsQueryable();

            // Utilizza il FiltroComunicazioniService per applicare i filtri, garantendo che i valori null siano sostituiti da valori di default
            query = await _filtroService.FiltraComunicazioniAsync(
                query,
                tipoAccertamento ?? string.Empty, // Se 'tipoAccertamento' è null, passiamo una stringa vuota
                string.Empty,                     // 'searchTerm' è impostato come stringa vuota per evitare il valore null
                null,                             // 'startDate' è nullable, quindi può rimanere null
                null,                             // 'endDate' è nullable, quindi può rimanere null
                codCor ?? string.Empty,           // Se 'codCor' è null, passiamo una stringa vuota
                string.Empty,                     // 'servizio' è impostato come stringa vuota per evitare il valore null
                meseAnno,                         // 'monthYear' è nullable, quindi può rimanere null
                string.Empty,                     // 'sortField' è impostato come stringa vuota per evitare il valore null
                string.Empty,                     // 'sortOrder' è impostato come stringa vuota per evitare il valore null
                false                             // 'soloRigheNonRestituite' è impostato a false di default
            );

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
