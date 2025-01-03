using System;
using System.Linq;
using System.Threading.Tasks;

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
