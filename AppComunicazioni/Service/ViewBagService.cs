using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppComunicazioni.Service
{
    public class ViewBagService : IViewBagService

    {
        public List<SelectListItem> GetCodCorOptions(string? codCor = null)
        {
            return Enum.GetValues(typeof(CodCorType))
                       .Cast<CodCorType>()
                       .Select(c => new SelectListItem
                       {
                           Value = c.ToString(),
                           Text = c.GetDisplayName(),
                           Selected = codCor != null && codCor.Equals(c.ToString(), StringComparison.OrdinalIgnoreCase)
                       }).ToList();
        }

        public List<SelectListItem> GetServizioOptions(string? codCor = null, string? servizio = null)
        {
            var servizioOptions = codCor switch
            {
                nameof(CodCorType.CESSIONI) => new List<ServizioType> { ServizioType.BA2, ServizioType.BAN, ServizioType.CEP },
                nameof(CodCorType.DATAVIZ) => new List<ServizioType> { ServizioType.APP, ServizioType.ATP, ServizioType.DIM, ServizioType.DV, ServizioType.ERE, ServizioType.MA7, ServizioType.MIM, ServizioType.PDL, ServizioType.S035, ServizioType.VL1, ServizioType.VLA, ServizioType.VL3, ServizioType.VPP, ServizioType.VSA, ServizioType.VSS },
                nameof(CodCorType.FORZA) => new List<ServizioType> { ServizioType.DP1, ServizioType.VED },
                _ => Enum.GetValues(typeof(ServizioType)).Cast<ServizioType>().ToList() // Aggiungi questo per restituire tutti i servizi come fallback
            };

            return servizioOptions.Select(s => new SelectListItem
            {
                Value = s.ToString(),
                Text = s.GetDisplayName(),
                Selected = servizio != null && servizio.Equals(s.ToString(), StringComparison.OrdinalIgnoreCase)
            }).ToList();
        }


    }
}