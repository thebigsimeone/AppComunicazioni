using AppComunicazioni.Interface;

namespace AppComunicazioni.Service
{
    public class SessionService : ISessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionService> _logger;

        public SessionService(IHttpContextAccessor httpContextAccessor, ILogger<SessionService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("Session is not available");

        public void Set(string key, string value)
        {
            Session.SetString(key, value);
            _logger.LogInformation($"Valore per '{key}' salvato nella sessione.");
        }

        public string? Get(string key)
        {
            return Session.GetString(key);
        }

        public void Remove(string key)
        {
            if (Session.GetString(key) != null)
            {
                Session.Remove(key);
                _logger.LogInformation($"Valore per '{key}' rimosso dalla sessione.");
            }
        }

        public void ResetFilters()
        {
            var keys = new[] { "SearchTerm", "StartDate", "EndDate", "CodCor", "Servizio", "MonthYear", "SoloRigheNonRestituite" };

            foreach (var key in keys)
            {
                Remove(key);
            }

            _logger.LogInformation("Tutti i filtri sono stati resettati. Query tornata allo stato primario.");
        }
    }
}
