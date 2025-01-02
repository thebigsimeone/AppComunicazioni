namespace AppComunicazioni.Utility
{
    using Microsoft.AspNetCore.Http;

    public static class SessionExtensions
    {
        public static void SetBoolean(this ISession session, string key, bool value)
        {
            session.SetString(key, value.ToString());
        }

        public static bool GetBoolean(this ISession session, string key)
        {
            var value = session.GetString(key);
            return bool.TryParse(value, out var result) && result;
        }
    }

}
