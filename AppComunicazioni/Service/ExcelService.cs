using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AppComunicazioni.Service
{
    public class ExcelService : IExcelService
    {
        public async Task<List<ComunicazioniDettaglio>> ProcessExcelFileAsync(IFormFile excelFile, int comunicazioneId)
        {
            var dettagliList = new List<ComunicazioniDettaglio>();

            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        return null;
                    }

                    // Trova gli indici di colonna per le intestazioni "Protocollo" e "Codice Fiscale"
                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    int colProtocollo = -1;
                    int colCodiceFiscale = -1;

                    for (int col = 1; col <= colCount; col++)
                    {
                        var headerText = worksheet.Cells[1, col].Text.Trim();
                        if (headerText.Equals("Protocollo", StringComparison.OrdinalIgnoreCase))
                        {
                            colProtocollo = col;
                        }
                        else if (headerText.Equals("Codice Fiscale", StringComparison.OrdinalIgnoreCase))
                        {
                            colCodiceFiscale = col;
                        }
                    }

                    // Se non vengono trovate entrambe le colonne necessarie, restituisce null
                    if (colProtocollo == -1 || colCodiceFiscale == -1)
                    {
                        return null;
                    }

                    // Legge i dati dalla seconda riga in poi
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var protocolloValue = worksheet.Cells[row, colProtocollo].Text;
                        var codiceFiscaleValue = worksheet.Cells[row, colCodiceFiscale].Text;

                        // Salta le righe vuote
                        if (string.IsNullOrEmpty(protocolloValue) && string.IsNullOrEmpty(codiceFiscaleValue))
                        {
                            continue;
                        }

                        var dettaglio = new ComunicazioniDettaglio
                        {
                            ComunicazioneId = comunicazioneId,
                            Protocollo = protocolloValue,
                            CodiceFiscale = codiceFiscaleValue,
                            DataInserimento = DateTime.Now
                        };

                        dettagliList.Add(dettaglio);
                    }
                }
            }

            return dettagliList;
        }
    }
}
