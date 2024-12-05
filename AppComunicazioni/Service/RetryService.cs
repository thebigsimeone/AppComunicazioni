using AppComunicazioni.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AppComunicazioni.Services
{
    public class RetryService : IRetryService
    {
        public async Task<IActionResult> ExecuteWithRetry(Func<Task<IActionResult>> action, ILogger logger, Controller controller, int maxRetryCount = 3)
        {
            int retryCount = 0;
            while (retryCount < maxRetryCount)
            {
                try
                {
                    return await action();
                }
                catch (TaskCanceledException ex)
                {
                    logger.LogError($"Errore di timeout durante l'elaborazione della richiesta: {ex.Message}");
                    controller.ModelState.AddModelError("", "La richiesta ha superato il tempo massimo di attesa. Riprova più tardi.");

                    return new ViewResult
                    {
                        ViewName = "Error",
                        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), controller.ModelState)
                        {
                            Model = null
                        }
                    };
                }
                catch (Exception ex)
                {
                    logger.LogError($"Errore durante l'elaborazione della richiesta: {ex.Message}");
                    controller.ModelState.AddModelError("", "Si è verificato un errore durante l'elaborazione della richiesta.");

                    return new ViewResult
                    {
                        ViewName = "Error",
                        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), controller.ModelState)
                        {
                            Model = null
                        }
                    };
                }

                retryCount++;
            }

            logger.LogError("Si sono verificati troppi errori. Riprova più tardi.");
            controller.ModelState.AddModelError("", "Si sono verificati troppi errori. Riprova più tardi.");

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
