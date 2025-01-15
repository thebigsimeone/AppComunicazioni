using AppComunicazioni.Models;

namespace AppComunicazioni.Interface
{
    public interface IFornitoreService
    {
        /// <summary>
        /// Restituisce il fornitore associato a un determinato servizio.
        /// </summary>
        /// <param name="servizio">Il tipo di servizio.</param>
        /// <param name="fileName">Nome del file per la logica condizionale.</param>
        /// <returns>Il fornitore associato.</returns>
        CodCorType GetFornitoreByServizio(ServizioType servizio, string fileName);
    }
}
