using AppComunicazioni.Interface;
using AppComunicazioni.Models;

namespace AppComunicazioni.Service
{
    public class FornitoreService : IFornitoreService
    {
        private readonly ILogger<FornitoreService> _logger;
        private readonly IConfiguration _configuration;

        // Mappa Enum Servizio -> Enum Fornitore
        private static readonly Dictionary<ServizioType, List<CodCorType>> ServizioToFornitoreMap = new()
        {
            // FORNITORE_A
            { ServizioType.APL, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.APP, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.APT, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.ATS, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.BA2, new List<CodCorType>{ CodCorType.FORNITORE_A, CodCorType.FORNITORE_B } },  // Condizione speciale
            { ServizioType.DIM, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.DV,  new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.ERE, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.MA7, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.MIM, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.PDL, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.S035, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.VL1, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.VLA, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.VL3, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.VPP, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.VSA, new List<CodCorType>{ CodCorType.FORNITORE_A } },
            { ServizioType.VSS, new List<CodCorType>{ CodCorType.FORNITORE_A } },

            // FORNITORE_B
            { ServizioType.BAN, new List<CodCorType>{ CodCorType.FORNITORE_B } },
            { ServizioType.CEP, new List<CodCorType>{ CodCorType.FORNITORE_B } },

            // FORNITORE_C
            { ServizioType.DP1, new List<CodCorType>{ CodCorType.FORNITORE_C } },
            { ServizioType.VED, new List<CodCorType>{ CodCorType.FORNITORE_C } }
        };

        public FornitoreService(ILogger<FornitoreService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration;
        }

        /// <summary>
        /// Restituisce il fornitore associato a un determinato servizio.
        /// </summary>
        public CodCorType GetFornitoreByServizio(ServizioType servizio, string fileName)
        {
            if (!ServizioToFornitoreMap.TryGetValue(servizio, out var fornitori))
            {
                _logger.LogWarning($"Servizio '{servizio}' non trovato. Fornitore impostato su 'FORNITORE_A'.");
                return CodCorType.FORNITORE_A;
            }

            // Gestione speciale per BA2
            if (servizio == ServizioType.BA2)
            {
                if ((_configuration.GetSection("SupplierRouting:SupplierBMarkers").Get<string[]>() ?? Array.Empty<string>())
                    .Any(marker => !string.IsNullOrWhiteSpace(marker) && fileName.Contains(marker, StringComparison.Ordinal)))
                {
                    _logger.LogInformation("Servizio 'BA2' associato a 'FORNITORE_B' tramite configurazione privata.");
                    return CodCorType.FORNITORE_B;
                }
                else if ((_configuration.GetSection("SupplierRouting:SupplierAMarkers").Get<string[]>() ?? Array.Empty<string>())
                    .Any(marker => !string.IsNullOrWhiteSpace(marker) && fileName.Contains(marker, StringComparison.Ordinal)))
                {
                    _logger.LogInformation("Servizio 'BA2' associato a 'FORNITORE_A' tramite configurazione privata.");
                    return CodCorType.FORNITORE_A;
                }
            }

            // Se esiste un solo fornitore, lo restituiamo
            if (fornitori.Count == 1)
            {
                return fornitori.First();
            }

            _logger.LogInformation($"Servizio '{servizio}' associato a '{fornitori.First()}'.");
            return fornitori.First();  // Default se ci sono più fornitori
        }
    }
}

