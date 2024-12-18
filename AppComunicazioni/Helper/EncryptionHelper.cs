using AppComunicazioni.Interface;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace AppComunicazioni.Helpers
{
    public static class EncryptionHelper
    {
        public static string EncryptId(this IHtmlHelper htmlHelper, string id)
        {
            var serviceProvider = htmlHelper.ViewContext.HttpContext.RequestServices;
            var encryptionService = serviceProvider.GetService<IEncryptionService>();

            if (encryptionService == null) throw new InvalidOperationException("EncryptionService non disponibile.");

            return encryptionService.Encrypt(id);
        }
    }
}
