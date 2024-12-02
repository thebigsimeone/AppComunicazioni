using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using OfficeOpenXml;

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

                    int rowCount = worksheet.Dimension.Rows;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var dettaglio = new ComunicazioniDettaglio
                        {
                            ComunicazioneId = comunicazioneId,
                            Protocollo = worksheet.Cells[row, 1].Text,
                            CodiceFiscale = worksheet.Cells[row, 2].Text,
                            ColonnaSupplementare = worksheet.Cells[row, 3].Text,
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
