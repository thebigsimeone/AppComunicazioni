using AppComunicazioni.Interface;
using AppComunicazioni.Models;

namespace AppComunicazioni.Service
{
    public class FornitoreService : IFornitoreService
    {
        private readonly ILogger<FornitoreService> _logger;

        // Mappa Enum Servizio -> Enum Fornitore
        private static readonly Dictionary<ServizioType, List<CodCorType>> ServizioToFornitoreMap = new()
        {
            // DATAVIZ
            { ServizioType.APL, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.APP, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.APT, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.ATS, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.BA2, new List<CodCorType>{ CodCorType.DATAVIZ, CodCorType.CESSIONI } },  // Condizione speciale
            { ServizioType.DIM, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.DV,  new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.ERE, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.MA7, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.MIM, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.PDL, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.S035, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.VL1, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.VLA, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.VL3, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.VPP, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.VSA, new List<CodCorType>{ CodCorType.DATAVIZ } },
            { ServizioType.VSS, new List<CodCorType>{ CodCorType.DATAVIZ } },

            // CESSIONI
            { ServizioType.BAN, new List<CodCorType>{ CodCorType.CESSIONI } },
            { ServizioType.CEP, new List<CodCorType>{ CodCorType.CESSIONI } },

            // FORZA
            { ServizioType.DP1, new List<CodCorType>{ CodCorType.FORZA } },
            { ServizioType.VED, new List<CodCorType>{ CodCorType.FORZA } }
        };

        public FornitoreService(ILogger<FornitoreService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Restituisce il fornitore associato a un determinato servizio.
        /// </summary>
        public CodCorType GetFornitoreByServizio(ServizioType servizio, string fileName)
        {
            if (!ServizioToFornitoreMap.TryGetValue(servizio, out var fornitori))
            {
                _logger.LogWarning($"Servizio '{servizio}' non trovato. Fornitore impostato su 'DATAVIZ'.");
                return CodCorType.DATAVIZ;
            }

            // Gestione speciale per BA2
            if (servizio == ServizioType.BA2)
            {
                if (fileName.Contains("1988") || fileName.Contains("1990"))
                {
                    _logger.LogInformation("Servizio 'BA2' associato a 'CESSIONI' per presenza di 1988 o 1990.");
                    return CodCorType.CESSIONI;
                }
                else if (fileName.Contains("8033"))
                {
                    _logger.LogInformation("Servizio 'BA2' associato a 'DATAVIZ' per presenza di 8033.");
                    return CodCorType.DATAVIZ;
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
