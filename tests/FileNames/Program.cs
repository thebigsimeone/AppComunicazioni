using AppComunicazioni.Models;

foreach (var tenant in new[] { "TENANT_A", "TENANT_B", "tenant_a", "tenant_b" })
{
    var name = $"IT-1_{tenant}_VSA_20260929-1200_3.xlsx";
    var parts = CommunicationFileName.Split(Path.GetFileNameWithoutExtension(name));
    if (parts.Length != 5 || parts[0] != "IT-1" || parts[1] != tenant ||
        !Enum.TryParse<ServizioType>(parts[2], true, out var service) ||
        service != ServizioType.VSA || int.Parse(parts[^1]) != 3)
        throw new Exception($"Metadati non riconosciuti per {tenant}.");

    var webTenant = CommunicationFileName.Split(name).FirstOrDefault(part =>
        part.Equals("TENANT_A", StringComparison.OrdinalIgnoreCase) ||
        part.Equals("TENANT_B", StringComparison.OrdinalIgnoreCase));
    if (webTenant != tenant)
        throw new Exception($"Tenant del modulo web non riconosciuto per {tenant}.");
}

if (!CommunicationFileName.Split("IT-1_DEMO_VSA_3").SequenceEqual(new[] { "IT-1", "DEMO", "VSA", "3" }))
    throw new Exception("Formato semplice non preservato.");

Console.WriteLine("5 casi nome file verificati: tenant, servizio e conteggio.");
