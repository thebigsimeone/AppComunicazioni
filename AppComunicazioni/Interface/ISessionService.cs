namespace AppComunicazioni.Interface
{
    public interface ISessionService
    {
        void Set(string key, string value);
        string? Get(string key);
        void Remove(string key);
        void ResetFilters();
    }
}
