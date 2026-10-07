using System.Text.RegularExpressions;

namespace AppComunicazioni.Models;

public static class CommunicationFileName
{
    // L'underscore interno a TENANT_A/TENANT_B fa parte del tenant.
    public static string[] Split(string fileName) =>
        Regex.Split(fileName, @"(?<!TENANT)_", RegexOptions.IgnoreCase);
}
