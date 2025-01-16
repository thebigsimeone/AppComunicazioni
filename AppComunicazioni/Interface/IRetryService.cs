using Microsoft.AspNetCore.Mvc;

namespace AppComunicazioni.Interface
{
    public interface IRetryService
    {
        Task<IActionResult> ExecuteWithRetry(Func<Task<IActionResult>> action, ILogger logger, Controller controller, int maxRetryCount = 3, int delaySeconds = 2);
    }
}
