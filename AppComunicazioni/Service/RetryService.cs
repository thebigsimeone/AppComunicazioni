using AppComunicazioni.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace AppComunicazioni.Services
{
    public class RetryService : IRetryService
    {
        private readonly List<int> _sqlTransientErrorNumbers = new List<int>
        {
            4060,   // Cannot open database requested by the login.
            10928,  // Resource limit reached.
            10929,  // Resource limit reached.
            40197,  // The service has encountered an error.
            40501,  // The service is currently busy.
            40613   // Database unavailable.
        };

        public async Task<IActionResult> ExecuteWithRetry(Func<Task<IActionResult>> action, ILogger logger, Controller controller, int maxRetryCount = 3, int delaySeconds = 2)
        {
            int retryCount = 0;
            while (retryCount < maxRetryCount)
            {
                try
                {
                    return await action();
                }
                catch (SqlException sqlEx) when (_sqlTransientErrorNumbers.Contains(sqlEx.Number))
                {
                    logger.LogWarning($"Errore SQL transiente (Codice: {sqlEx.Number}): {sqlEx.Message}. Tentativo {retryCount + 1} di {maxRetryCount}.");
                }
                catch (DbUpdateException dbEx)
                {
                    logger.LogError($"Errore durante l'aggiornamento del database: {dbEx.Message}");
                    controller.ModelState.AddModelError("", "Errore di aggiornamento dei dati. Riprova più tardi.");
                    break;
                }
                catch (TaskCanceledException ex)
                {
                    logger.LogError($"Errore di timeout: {ex.Message}. Tentativo {retryCount + 1} di {maxRetryCount}.");
                    controller.ModelState.AddModelError("", "La richiesta ha superato il tempo massimo di attesa. Riprova più tardi.");
                }
                catch (SocketException ex)
                {
                    logger.LogError($"Errore di rete: {ex.Message}. Tentativo {retryCount + 1} di {maxRetryCount}.");
                    controller.ModelState.AddModelError("", "Errore di rete. Verifica la connessione e riprova.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Errore generico: {ex.Message}. Tentativo {retryCount + 1} di {maxRetryCount}.");
                    controller.ModelState.AddModelError("", "Si è verificato un errore. Riprova più tardi.");
                }

                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds * retryCount)); // Backoff esponenziale
            }

            logger.LogError("Raggiunto il numero massimo di tentativi. Operazione fallita.");
            controller.ModelState.AddModelError("", "Operazione fallita dopo ripetuti tentativi. Riprova più tardi.");

            return new ViewResult
            {
                ViewName = "Error",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), controller.ModelState)
                {
                    Model = null
                }
            };
        }
    }
}
